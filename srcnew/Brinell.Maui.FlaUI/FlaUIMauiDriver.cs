using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Brinell.Core;
using Brinell.Core.Diagnostics;
using Brinell.Core.Exceptions;
using Brinell.Core.Utilities;
using Brinell.Maui.Configuration;
using Brinell.Maui.FlaUI.Bridge;
using Brinell.Uia;
using Brinell.Maui.Enums;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// FlaUI-based implementation of <see cref="IMauiDriver"/> for Windows platform.
/// Provides native Windows UI Automation support for MAUI desktop apps.
/// </summary>
public sealed class FlaUIMauiDriver : IMauiDriver, IDisposable
{
    private readonly UIA3Automation _automation;
    private readonly Application? _application;
    private AutomationElement _rootElement;
    private readonly Lock _rootGate = new();
    private readonly ConditionFactory _conditionFactory;
    private readonly nint _rootWindowHandle;
    private bool _disposed;
    private CancellationTokenSource? _watchdog;
    private int _foregroundGrabs;

    
    /// <summary>
    /// Creates a new FlaUIMauiDriver for an existing window.
    /// </summary>
    /// <param name="windowHandle">The window handle to attach to.</param>
    public FlaUIMauiDriver(IntPtr windowHandle)
    {
        _automation = new UIA3Automation();
        _rootElement = AttachToWindow(_automation, windowHandle);
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);
        _rootWindowHandle = windowHandle;
    }
    
    /// <summary>
    /// Creates a new FlaUIMauiDriver by launching an application.
    /// </summary>
    /// <param name="executablePath">Path to the application executable.</param>
    /// <param name="arguments">Optional command line arguments.</param>
    public FlaUIMauiDriver(string executablePath, string? arguments = null)
    {
        _automation = new UIA3Automation();
        
        var processStartInfo = new ProcessStartInfo(executablePath)
        {
            Arguments = arguments ?? string.Empty,

            // FALSE, AND THIS MATTERS MORE THAN IT LOOKS.
            //
            // With UseShellExecute the launch goes through the shell, and what the child
            // inherits is not reliably this process's environment. Measured: roughly one run in
            // six came up with no bridge at all - every test failing with "no element published
            // on the app's bridge", for seventeen seconds at a time, against an app that was
            // running perfectly well and had simply never been told to instrument itself. It
            // looked like flakiness in the bridge and was a launch that lost a variable.
            //
            // False also lets the environment be stated per launch instead of being smuggled
            // through this process's own, which was always a wart: the driver used to set
            // BRINELL_UIA_BRIDGE on itself and hope.
            UseShellExecute = false,
        };

        // Ask the app to turn its gesture bridge on. Asking is all this is: an app that was not
        // built with a bridge has nothing to turn on, which is the gate that actually controls
        // anything - see BrinellBridgeGate. An uninstrumented app is unaffected.
        processStartInfo.Environment[BrinellBridgeGate.EnableVariable] = "1";

        // Whoever the user was working in before the run. Windows hands a freshly launched
        // process the foreground, so without this the app steals focus and keeps it for the
        // whole run - see RestoreForegroundWindow.
        var previousForeground = GetForegroundWindow();

        var process = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {executablePath}");

        // Before anything waits on the app: launch is when it is certain to grab the foreground,
        // and the wait for its main window below takes seconds. Started any later, the watchdog
        // only cleans up after the flash the person at the machine has already seen.
        StartForegroundWatchdog(process.Id, previousForeground);
        
        // Give the process time to initialize before attaching
        process.WaitForInputIdle();
        
        _application = Application.Attach(process);
        
        // Wait for main window
        var window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        _rootElement = window ?? throw new InvalidOperationException("Failed to get main window");
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);
        _rootWindowHandle = _rootElement.Properties.NativeWindowHandle.ValueOrDefault;
        TryApplyRequestedWindowPlacement();
        RefuseActivation();
        RestoreForegroundWindow(previousForeground);
    }
    
    /// <summary>
    /// Creates a new FlaUIMauiDriver by attaching to a running process.
    /// </summary>
    /// <param name="process">The process to attach to.</param>
    public FlaUIMauiDriver(Process process)
    {
        _automation = new UIA3Automation();
        _application = Application.Attach(process);
        
        var window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        _rootElement = window ?? throw new InvalidOperationException("Failed to get main window");
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);
        _rootWindowHandle = _rootElement.Properties.NativeWindowHandle.ValueOrDefault;
    }
    
    /// <summary>
    /// Attaches to a window, waiting for UI Automation to be willing to resolve it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A window that exists is not always a window UI Automation will hand you.</b>
    /// <c>FromHandle</c> throws <c>Win32Exception: "Unexpected HRESULT has been returned from a
    /// call to a COM component"</c> for a handle it cannot resolve yet - not a null, so there is
    /// nothing to test for and nothing that reads as "not ready".
    /// </para>
    /// <para>
    /// <b>It fails in a constructor, which is what makes it worth handling here.</b> A fixture
    /// that cannot build takes its whole collection down at once, and the report is a COM error
    /// with no mention of a window - it reads as the automation stack being broken rather than a
    /// window being a few milliseconds young.
    /// </para>
    /// </remarks>
    /// <param name="automation">The client session.</param>
    /// <param name="windowHandle">The window to attach to.</param>
    /// <returns>The window as an automation element.</returns>
    private static AutomationElement AttachToWindow(UIA3Automation automation, IntPtr windowHandle)
    {
        const int timeoutMs = 10_000;
        const int pollMs = 20;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Exception? last = null;

        while (clock.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                return automation.FromHandle(windowHandle);
            }
            catch (Exception attaching)
            {
                last = attaching;
                Thread.Sleep(pollMs);
            }
        }

        throw new InvalidOperationException(
            $"UI Automation would not resolve window 0x{windowHandle:X} within {timeoutMs} ms. "
            + "The window exists but the automation stack will not hand it over, which is not "
            + "the same as the app being absent.",
            last);
    }

    #region Platform
    
    /// <inheritdoc />
    public MauiPlatform Platform => MauiPlatform.Windows;
    
    #endregion
    
    #region Internal
    
    /// <summary>
    /// Gets the condition factory for building search conditions.
    /// </summary>
    internal ConditionFactory ConditionFactory => _conditionFactory;
    
    /// <summary>
    /// Gets the underlying automation instance.
    /// </summary>
    internal UIA3Automation Automation => _automation;

    /// <summary>The app's top-level window. Where a bridge lookup starts.</summary>
    /// <remarks>
    /// <para>
    /// <b>Re-resolved when UI Automation has invalidated it, and that is the fix for what looked
    /// like an app freeze.</b> Two of eight full runs, and a stress probe within forty page
    /// changes, ended with every later test failing in milliseconds: bridge silent, window
    /// "unreadable". Captured in the act, the app was idle and responding, its window handle
    /// unchanged - and the element this driver had held since launch answered
    /// <c>UIA_E_ELEMENTNOTAVAILABLE</c>, while a fresh attach to the same handle worked at once,
    /// bridge and all. UI Automation had retired the element; the driver never asked again, so one
    /// invalidation blinded it for the rest of the run.
    /// </para>
    /// <para>
    /// <b>The window handle is the identity, not the element.</b> The check is one property read,
    /// cheap next to the tree searches every caller of this goes on to do, and the re-attach is
    /// taken under a lock because the verb runners and the test thread can reach it together.
    /// See <c>.my/fix/rca-app-freeze-was-a-stale-root.md</c>.
    /// </para>
    /// </remarks>
    internal AutomationElement RootElement
    {
        get
        {
            var root = _rootElement;

            if (IsStillAvailable(root) || _rootWindowHandle == IntPtr.Zero)
            {
                return root;
            }

            lock (_rootGate)
            {
                if (!ReferenceEquals(root, _rootElement) && IsStillAvailable(_rootElement))
                {
                    return _rootElement;
                }

                _rootElement = AttachToWindow(_automation, _rootWindowHandle);
                Interlocked.Increment(ref _rootReattachments);
                return _rootElement;
            }
        }
    }

    /// <summary>How many times the root element had gone stale and was attached again.</summary>
    /// <remarks>Diagnostics. A number that grows during a run is the invalidation happening.</remarks>
    public int RootReattachments => Volatile.Read(ref _rootReattachments);

    private int _rootReattachments;

    private static bool IsStillAvailable(AutomationElement element)
    {
        try
        {
            _ = element.Properties.ProcessId.Value;
            return true;
        }
        catch (System.Runtime.InteropServices.COMException stale)
            when (stale.HResult == unchecked((int)0x80040201))
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures the root window is focused and activated before physical input.
    /// </summary>
    /// <remarks>
    /// Real input is positional and focus-relative, and FlaUI does not do this for you:
    /// <c>AutomationElement.Click()</c> is clickable-point plus mouse, with no activation, and
    /// <c>Keyboard.Type</c> reaches whichever window holds focus. Without this, keystrokes meant
    /// for the app land in whatever the user is doing — which the launch-time
    /// <see cref="RestoreForegroundWindow"/> makes a live possibility rather than a theoretical
    /// one.
    /// </remarks>
    internal void EnsureRootWindowFocused()
    {
        try
        {
            if (RootElement.Patterns.Window.IsSupported)
            {
                var windowPattern = RootElement.Patterns.Window.Pattern;
                if (windowPattern.WindowVisualState.Value == WindowVisualState.Minimized)
                {
                    windowPattern.SetWindowVisualState(WindowVisualState.Normal);
                }
            }
        }
        catch
        {
            // Ignore window visual state failures and continue with focus fallback.
        }

        // Outside the try: the catch below swallows everything, and a refusal that gets
        // swallowed is not a refusal.
        PhysicalInput.Used("FlaUIMauiDriver.EnsureRootWindowFocused", "the Focus verb");

        try
        {
            RootElement.SetForeground();
        }
        catch
        {
            // SetForeground can fail if the window is not top-level; fall back to Focus.
            try
            {
                RootElement.Focus();
            }
            catch
            {
                // Ignore focus failures; interaction will proceed regardless.
            }
        }
    }

    internal void PointerLongPress(Point point, int durationMs)
    {
        PhysicalInput.Used("FlaUIMauiDriver.PointerLongPress", "the LongPress gesture verb (step 18)");
        EnsureRootWindowFocused();
        Mouse.Position = point;
        Mouse.Down(MouseButton.Left);
        try
        {
            WaitHelper.Pause(durationMs);
        }
        finally
        {
            Mouse.Up(MouseButton.Left);
        }
    }

    /// <remarks>
    /// Steps the pointer by hand rather than calling <c>Mouse.Drag</c>, which assigns
    /// <c>Mouse.Position</c> twice and so teleports. WinUI only recognises a drag when it receives
    /// the moves in between, and <c>Mouse.Drag</c> has nowhere to put a duration either.
    /// </remarks>
    internal void PointerDrag(
        Point start,
        Point end,
        int durationMs)
    {
        PhysicalInput.Used("FlaUIMauiDriver.PointerDrag", "a swipe gesture verb (step 18)");
        EnsureRootWindowFocused();
        Mouse.MoveTo(start);
        Mouse.Down(MouseButton.Left);
        try
        {
            var steps = Math.Max(10, durationMs / 50);
            var dx = (end.X - start.X) / (double)steps;
            var dy = (end.Y - start.Y) / (double)steps;
            var stepDelay = durationMs / steps;

            for (var i = 1; i <= steps; i++)
            {
                var x = (int)(start.X + dx * i);
                var y = (int)(start.Y + dy * i);
                Mouse.MoveTo(new Point(x, y));
                WaitHelper.Pause(stepDelay);
            }
        }
        finally
        {
            Mouse.Up(MouseButton.Left);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Hands the foreground back to the window that had it before the app was launched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Launching a process is the one moment automation cannot avoid taking the foreground:
    /// Windows grants it to a new process, and nothing in the automation path takes it back,
    /// because UI Automation patterns drive the app without focus. The window stays shown, only
    /// unfocused, so its layout and bounding rectangles remain valid.
    /// </para>
    /// <para>
    /// <b>Asking for the foreground back usually does not work, and that is the point of the
    /// first half below.</b> Windows only permits a foreground change from the process that
    /// currently holds it, so a test process calling <c>SetForegroundWindow</c> on someone
    /// else's window is normally refused outright - silently. Relying on it alone is why a run
    /// still appeared over whatever the person at the machine was using.
    /// </para>
    /// <para>
    /// Pushing the app <i>down</i> the z-order needs no such permission: it is this process's own
    /// window handle, and <c>SWP_NOACTIVATE</c> moves it without focusing anything. So the app is
    /// sent behind, and the foreground is then asked for politely - if that request is refused,
    /// the app is still out of the way.
    /// </para>
    /// <para>
    /// <b>Only when nothing is going to click.</b> A pointer click is positional: with the app
    /// behind another window, a click at the right coordinates lands on the wrong window
    /// entirely. So the demotion happens only where physical input is refused or audited, which
    /// is exactly where it is safe and exactly where somebody is trying to work at the same
    /// machine.
    /// </para>
    /// </remarks>
    private void RestoreForegroundWindow(IntPtr previousForeground)
    {
        if (PhysicalInput.Policy != PhysicalInputPolicy.Allowed)
        {
            SendAppBehind();
        }

        if (previousForeground == IntPtr.Zero)
        {
            return;
        }

        try
        {
            User32.SetForegroundWindow(previousForeground);
        }
        catch
        {
            // Losing this race only means the app keeps focus; it changes no test outcome.
        }
    }

    /// <summary>
    /// Puts the app under test at the bottom of the z-order without activating it.
    /// </summary>
    /// <remarks>
    /// Behind, not minimized and not hidden. A minimized WinUI window can stop laying out and may
    /// never realize virtualized content, so the suite would start failing on elements that
    /// genuinely are not there - the same reason <c>AutPlacement</c> refuses to minimize. Behind
    /// is invisible to the person using the machine and fully composed to UI Automation.
    /// </remarks>
    private void SendAppBehind()
    {
        if (_rootWindowHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            SetWindowPos(
                _rootWindowHandle,
                HwndBottom,
                0, 0, 0, 0,
                SwpNoMove | SwpNoSize | SwpNoActivate);
        }
        catch
        {
            // Out of the way is a courtesy, not a requirement. It changes no test outcome.
        }
    }

    /// <summary>
    /// Where the harness wants the app under test put once it has launched.
    /// </summary>
    /// <remarks>
    /// All of these keep the window <b>composed</b>. Minimizing is not among them and must not
    /// be: a minimized WinUI window can stop laying out, and virtualized content may never
    /// realize, so a suite driving a minimized app fails on elements that genuinely are not
    /// there. Out of the way is fine; not rendered is not.
    /// </remarks>
    private enum AutPlacement
    {
        /// <summary>Leave the window wherever Windows put it.</summary>
        Default,

        /// <summary>The right of the primary work area, leaving a column for a presenter.</summary>
        Right,

        /// <summary>Beyond every monitor: driveable, screenshotable, and not in anyone's way.</summary>
        OffScreen,

        /// <summary>A non-primary monitor, falling back to <see cref="Right"/> when there is none.</summary>
        Secondary,
    }

    /// <summary>
    /// Reads the requested placement, honouring the original switch as well as the current one.
    /// </summary>
    /// <remarks>
    /// <c>BRINELL_AUT_PLACE_RIGHT=1</c> predates <c>BRINELL_AUT_PLACE</c> and is still set by
    /// existing scripts, so it keeps working and means <see cref="AutPlacement.Right"/>. The
    /// newer variable wins when both are set.
    /// </remarks>
    private static AutPlacement ReadRequestedPlacement(out string? unknownValue)
    {
        unknownValue = null;

        var requested = Environment.GetEnvironmentVariable("BRINELL_AUT_PLACE");
        if (!string.IsNullOrWhiteSpace(requested))
        {
            switch (requested.Trim().ToLowerInvariant())
            {
                case "right": return AutPlacement.Right;
                case "offscreen": return AutPlacement.OffScreen;
                case "secondary": return AutPlacement.Secondary;
                default:
                    // A typo should not silently leave the window where it was, and should not
                    // end the run either. Record it and place nothing.
                    unknownValue = requested;
                    return AutPlacement.Default;
            }
        }

        // NOT off-screen by default, though it was tried for exactly the reason it is tempting: a
        // window on no monitor cannot appear over anyone's editor. Measured, it breaks visibility.
        // UI Automation's IsOffscreen counts the desktop, not just the scroll viewport, so with the
        // window at x=-1144 a button plainly inside its page reported visible=False, and every
        // "scroll into view, then wait for visible" in the suite could time out on it. Keeping the
        // app out of the way is the foreground watchdog's job, by z-order, which leaves geometry
        // alone. BRINELL_AUT_PLACE=offscreen still exists for a run that asks for it knowingly.
        return string.Equals(
                Environment.GetEnvironmentVariable("BRINELL_AUT_PLACE_RIGHT"),
                "1",
                StringComparison.Ordinal)
            ? AutPlacement.Right
            : AutPlacement.Default;
    }

    private void TryApplyRequestedWindowPlacement()
    {
        var placement = ReadRequestedPlacement(out var unknownValue);

        if (unknownValue != null)
        {
            WriteAutPlacementReport(
                Rectangle.Empty, Rectangle.Empty, AutPlacement.Default, "not supported",
                $"BRINELL_AUT_PLACE='{unknownValue}' is not one of right, offscreen, secondary.");
            return;
        }

        if (placement == AutPlacement.Default)
        {
            return;
        }

        var workArea = GetPrimaryWorkArea();
        var requested = ComputeRequestedBounds(placement, workArea, out var presenter, out var effective);

        try
        {
            if (!RootElement.Patterns.Transform.IsSupported)
            {
                WriteAutPlacementReport(presenter, requested, effective, "not supported", "Transform pattern is not supported.");
                return;
            }

            var transform = RootElement.Patterns.Transform.Pattern;
            if (!transform.CanMove.Value)
            {
                WriteAutPlacementReport(presenter, requested, effective, "not supported", "Window cannot be moved.");
                return;
            }

            if (transform.CanResize.Value)
            {
                transform.Resize(requested.Width, requested.Height);
            }

            transform.Move(requested.Left, requested.Top);

            var actual = RootElement.BoundingRectangle;
            if (!LandedWhereAsked(actual, requested))
            {
                // UIA's Transform pattern keeps an element reachable on screen, so WinUI clamps a
                // move that would put the window past the desktop edge: an off-screen request
                // lands back at the origin, and the window is still in the user's way. Measured,
                // not assumed — the placement report is what caught it.
                //
                // Positioning the app under test is harness business rather than app automation,
                // so drop to the Win32 call, which holds no such opinion.
                if (TryMoveWindowDirectly(requested))
                {
                    actual = RootElement.BoundingRectangle;
                }
            }

            WriteAutPlacementReport(
                presenter, requested, effective,
                LandedWhereAsked(actual, requested) ? "moved" : "clamped",
                LandedWhereAsked(actual, requested) ? null : "The window was not allowed to move where asked.",
                actual);
        }
        catch (Exception ex)
        {
            WriteAutPlacementReport(presenter, requested, effective, $"failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Works out where the window should go, and where a presenter could sit beside it.
    /// </summary>
    /// <param name="effective">
    /// What was actually chosen. <see cref="AutPlacement.Secondary"/> degrades to
    /// <see cref="AutPlacement.Right"/> on a single-monitor desktop, and the report should say so
    /// rather than claim a placement that did not happen.
    /// </param>
    private Rectangle ComputeRequestedBounds(
        AutPlacement placement,
        Rectangle workArea,
        out Rectangle presenter,
        out AutPlacement effective)
    {
        if (placement == AutPlacement.OffScreen)
        {
            effective = AutPlacement.OffScreen;
            presenter = workArea;   // the whole primary screen is left to the user
            return ComputeOffScreenBounds();
        }

        if (placement == AutPlacement.Secondary && TryGetSecondaryWorkArea(out var secondary))
        {
            effective = AutPlacement.Secondary;
            presenter = workArea;
            return secondary;
        }

        effective = AutPlacement.Right;

        const int Gap = 20;
        const int MinimumWidth = 320;

        var presenterWidth = Math.Max(MinimumWidth, workArea.Width / 4);
        presenter = new Rectangle(workArea.Left, workArea.Top, presenterWidth, workArea.Height);

        var left = Math.Min(workArea.Right - MinimumWidth, workArea.Left + presenterWidth + Gap);
        var width = Math.Max(MinimumWidth, workArea.Right - left);

        return new Rectangle(left, workArea.Top, width, workArea.Height);
    }

    /// <summary>
    /// Puts the window off the left of every monitor bar a sliver, at the size it already has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A sliver, not the whole window.</b> Moved entirely outside the desktop, a WinUI window
    /// stops publishing its UI Automation tree — the page root is simply not there, and every
    /// test fails with <c>MissingRoot</c> exactly as if the app were minimized. Measured, not
    /// assumed. Leaving a few pixels intersecting the desktop keeps the window composed and the
    /// tree alive, which is the whole point of preferring this over minimizing.
    /// </para>
    /// <para>
    /// Off the <i>virtual</i> screen rather than the primary one: on a multi-monitor desktop the
    /// space beside the primary is usually another screen, and "off the primary" would park the
    /// app in the middle of it.
    /// </para>
    /// <para>
    /// Keeps the current size. Resizing to fill a work area would change how the app lays out,
    /// and a suite that only passes at one window size is not a suite anyone can trust.
    /// </para>
    /// </remarks>
    private Rectangle ComputeOffScreenBounds()
    {
        // How much of the window stays on the desktop.
        const int Sliver = 8;

        var virtualScreen = GetVirtualScreen();
        var current = RootElement.BoundingRectangle;
        var size = current is { Width: > 0, Height: > 0 }
            ? current.Size
            : new Size(1024, 768);

        return new Rectangle(
            virtualScreen.Left - size.Width + Sliver,
            virtualScreen.Top,
            size.Width,
            size.Height);
    }

    private static Rectangle GetPrimaryWorkArea()
    {
        if (TryGetPrimaryWorkArea(out var workArea))
        {
            return workArea;
        }

        return new Rectangle(0, 0, GetSystemMetrics(SmCxScreen), GetSystemMetrics(SmCyScreen));
    }

    /// <summary>
    /// Whether the window ended up at the requested origin, allowing for frame differences.
    /// </summary>
    /// <remarks>
    /// Origin only. A window that declines to resize is still correctly placed, and reporting
    /// that as a failure would hide the one case that matters — a move that did not happen.
    /// </remarks>
    private static bool LandedWhereAsked(Rectangle actual, Rectangle requested)
    {
        const int Tolerance = 16;

        return Math.Abs(actual.Left - requested.Left) <= Tolerance
               && Math.Abs(actual.Top - requested.Top) <= Tolerance;
    }

    /// <summary>
    /// Moves and sizes the window through Win32, bypassing the Transform pattern's clamping.
    /// </summary>
    private bool TryMoveWindowDirectly(Rectangle bounds)
    {
        if (_rootWindowHandle == 0)
        {
            return false;
        }

        return SetWindowPos(
            _rootWindowHandle, IntPtr.Zero,
            bounds.Left, bounds.Top, bounds.Width, bounds.Height,
            SwpNoZOrder | SwpNoActivate);
    }

    /// <summary>The bounding box of every monitor together.</summary>
    private static Rectangle GetVirtualScreen()
        => new(
            GetSystemMetrics(SmXVirtualScreen),
            GetSystemMetrics(SmYVirtualScreen),
            GetSystemMetrics(SmCxVirtualScreen),
            GetSystemMetrics(SmCyVirtualScreen));

    /// <summary>
    /// Finds the work area of the first non-primary monitor, if the desktop has one.
    /// </summary>
    /// <remarks>
    /// Work area rather than full bounds, so the window does not sit under the taskbar when that
    /// monitor has one.
    /// </remarks>
    private static bool TryGetSecondaryWorkArea(out Rectangle workArea)
    {
        Rectangle? secondary = null;

        MonitorEnumProc callback = (nint monitor, nint _, ref NativeRect _, nint _) =>
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };

            if (!GetMonitorInfo(monitor, ref info) || (info.Flags & MonitorInfoPrimary) != 0)
            {
                return true;    // keep looking
            }

            secondary = Rectangle.FromLTRB(
                info.WorkArea.Left, info.WorkArea.Top, info.WorkArea.Right, info.WorkArea.Bottom);

            return false;       // first one will do
        };

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

        workArea = secondary ?? Rectangle.Empty;
        return secondary is not null;
    }

    private static bool TryGetPrimaryWorkArea(out Rectangle workArea)
    {
        if (SystemParametersInfo(0x0030, 0, out var nativeRect, 0))
        {
            workArea = Rectangle.FromLTRB(
                nativeRect.Left,
                nativeRect.Top,
                nativeRect.Right,
                nativeRect.Bottom);
            return true;
        }

        workArea = Rectangle.Empty;
        return false;
    }

    private static void WriteAutPlacementReport(
        Rectangle presenter,
        Rectangle requested,
        AutPlacement placement,
        string result,
        string? reason = null,
        Rectangle? actual = null)
    {
        var path = Environment.GetEnvironmentVariable("BRINELL_AUT_PLACEMENT_RESULT_FILE");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            List<string> lines =
            [
                "AUT placement:",
                $"Placement: {placement}",
                $"Presenter bounds: {FormatRectangle(presenter)}",
                $"Requested AUT bounds: {FormatRectangle(requested)}",
                $"Result: {result}"
            ];

            if (actual is not null)
            {
                lines.Add($"Actual AUT bounds: {FormatRectangle(actual.Value)}");
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                lines.Add($"Reason: {reason}");
            }

            File.WriteAllText(path, string.Join(Environment.NewLine, lines));
        }
        catch
        {
            // Placement diagnostics should never make a test session fail.
        }
    }

    private static string FormatRectangle(Rectangle rectangle)
    {
        return $"x={rectangle.X} y={rectangle.Y} w={rectangle.Width} h={rectangle.Height}";
    }

    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;

    /// <summary>MONITORINFOF_PRIMARY.</summary>
    private const uint MonitorInfoPrimary = 0x1;

    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;

    /// <summary>HWND_BOTTOM: behind every other window, without being minimized.</summary>
    private static readonly IntPtr HwndBottom = new(1);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private delegate bool MonitorEnumProc(nint monitor, nint deviceContext, ref NativeRect clip, nint data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(nint deviceContext, nint clip, MonitorEnumProc callback, nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(
        uint uiAction,
        uint uiParam,
        out NativeRect pvParam,
        uint fWinIni);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    
    #endregion
    
    #region Element Finding (IDriver<IMauiElement>)
    
    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator, int timeoutMs = 5000)
    {
        var condition = locator.ToCondition(_conditionFactory);
        
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var found = RootElement.FindFirstDescendant(condition);
            if (found != null)
            {
                return new FlaUIMauiElement(found, this);
            }
            
            if (timeoutMs <= 0) break;
            WaitHelper.Pause(100);
        }
        
        throw new ElementNotFoundException(locator);
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator, int timeoutMs = 0)
    {
        var condition = locator.ToCondition(_conditionFactory);
        
        if (timeoutMs > 0)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                var found = RootElement.FindAllDescendants(condition);
                if (found.Length > 0)
                {
                    return found.Select(e => new FlaUIMauiElement(e, this)).ToList();
                }
                WaitHelper.Pause(100);
            }
        }
        
        var elements = RootElement.FindAllDescendants(condition);
        return elements.Select(e => new FlaUIMauiElement(e, this)).ToList();
    }
    
    /// <inheritdoc />
    public bool TryFindElement(Locator locator, out IMauiElement? element, int timeoutMs = 0)
    {
        try
        {
            element = FindElement(locator, timeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            element = null;
            return false;
        }
    }
    
    #endregion
    
    #region Window Management
    
    /// <inheritdoc />
    public string CurrentWindowHandle => RootElement.Properties.NativeWindowHandle.Value.ToString();
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> WindowHandles
    {
        get
        {
            if (_application != null)
            {
                return _application.GetAllTopLevelWindows(_automation)
                    .Select(w => w.Properties.NativeWindowHandle.Value.ToString())
                    .ToList();
            }
            return new[] { CurrentWindowHandle };
        }
    }
    
    #endregion
    
    #region Session Management
    
    /// <inheritdoc />
    public void Quit()
    {
        _application?.Close();
    }
    
    /// <inheritdoc />
    public void Close()
    {
        if (RootElement.Patterns.Window.IsSupported)
        {
            RootElement.Patterns.Window.Pattern.Close();
        }
    }
    
    #endregion
    
    /// <summary>
    /// How many times the app under test took the foreground and had to be put back.
    /// </summary>
    /// <remarks>
    /// <b>Zero is the claim this framework makes.</b> A run that never takes the keyboard or the
    /// pointer but puts its window over what somebody is reading has still taken the machine.
    /// This is the number that says whether that happened, rather than a person watching for it -
    /// which was the previous state of the art and cannot be done in CI.
    /// </remarks>
    public int ForegroundGrabs => Volatile.Read(ref _foregroundGrabs);

    /// <summary>
    /// Watches for the app raising itself, and puts it back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Placement is not enough on its own, and demoting once is not either.</b> The window is
    /// put out of the way after it exists, and a window can raise itself afterwards - at launch,
    /// on a dialog, on a page transition. Reported from a real desktop as "the screen still pops
    /// over the editor", which is the whole thing this framework is trying not to do.
    /// </para>
    /// <para>
    /// <b>A polling thread rather than <c>SetWinEventHook</c>.</b> The hook is the precise
    /// instrument and it delivers through a message queue; a test runner's threads do not pump
    /// one, so the events would arrive when nobody is listening. Polling needs nothing from the
    /// host and misses nothing that lasts longer than its interval.
    /// </para>
    /// <para>
    /// <b>Fast at first, then slow.</b> Launch is the one moment the app is certain to grab the
    /// foreground, so the first second is checked every few milliseconds to make that flash as
    /// short as it can be; after that a raise is unexpected and a slower cadence is enough to
    /// catch it and to count it.
    /// </para>
    /// <para>
    /// <b>Only when physical input is refused.</b> With input allowed the run is clicking at
    /// coordinates, and a window pushed behind another would take those clicks somewhere else.
    /// </para>
    /// </remarks>
    /// <param name="processId">The app under test, whose windows are watched.</param>
    /// <param name="previousForeground">The window the person was using, to hand back.</param>
    private void StartForegroundWatchdog(int processId, IntPtr previousForeground)
    {
        if (PhysicalInput.Policy == PhysicalInputPolicy.Allowed)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _watchdog = cancellation;

        var thread = new Thread(() => WatchTheForeground(processId, previousForeground, cancellation.Token))
        {
            IsBackground = true,
            Name = "Brinell foreground watchdog",
        };

        thread.Start();
    }

    /// <remarks>
    /// <b>By process, not by window handle.</b> The handle is not known until the main window has
    /// been found, seconds after launch - which is precisely the stretch in which the app takes
    /// the foreground. Any window of the app's process that becomes foreground is the app raising
    /// itself, whichever window it is: the main one, a splash, a dialog.
    /// </remarks>
    private void WatchTheForeground(int processId, IntPtr previousForeground, CancellationToken cancellation)
    {
        const int eagerMs = 5;
        const int settledMs = 100;
        const int eagerForMs = 15_000;

        var clock = System.Diagnostics.Stopwatch.StartNew();

        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                var foreground = GetForegroundWindow();

                if (foreground != IntPtr.Zero
                    && GetWindowThreadProcessId(foreground, out var owner) != 0
                    && owner == processId)
                {
                    Interlocked.Increment(ref _foregroundGrabs);

                    SetWindowPos(
                        foreground, HwndBottom, 0, 0, 0, 0,
                        SwpNoMove | SwpNoSize | SwpNoActivate);

                    if (previousForeground != IntPtr.Zero)
                    {
                        User32.SetForegroundWindow(previousForeground);
                    }
                }
            }
            catch
            {
                // The app has gone, or the handle is stale. Neither is worth taking a test run
                // down over: this is a courtesy thread.
            }

            // Eager for as long as launch can take, then settled.
            Thread.Sleep(clock.ElapsedMilliseconds < eagerForMs ? eagerMs : settledMs);
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out int processId);

    /// <summary>
    /// Marks the app's window as one that does not become the foreground window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Measured, not supposed: every button click raised the app over the person's work.</b>
    /// The watchdog counted two foreground grabs per <c>Click</c>, navigating or not, and none for
    /// a read. <c>Click</c> is <c>InvokePattern.Invoke</c> - not physical input, and never refused
    /// as such - and WinUI answers it by activating the window, presumably through the focus the
    /// button takes when it is clicked. Refusing input could never catch it, because no input was
    /// ever sent.
    /// </para>
    /// <para>
    /// <b><c>WS_EX_NOACTIVATE</c> refuses it at the source.</b> The window keeps working and keeps
    /// answering UI Automation; it simply is not made the foreground window when something inside
    /// it asks. Set from here, on the app's own handle, so it needs nothing from the app under test.
    /// </para>
    /// <para>
    /// <b>Only with input refused.</b> A run that clicks at coordinates needs the window to take
    /// the foreground, or its keystrokes land in whatever else is in front.
    /// </para>
    /// </remarks>
    private void RefuseActivation()
    {
        if (PhysicalInput.Policy == PhysicalInputPolicy.Allowed || _rootWindowHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var style = GetWindowLongPtr(_rootWindowHandle, GwlExStyle);
            SetWindowLongPtr(_rootWindowHandle, GwlExStyle, style | WsExNoActivate);
        }
        catch
        {
            // Out of the way is a courtesy; the watchdog still pushes the window back if this
            // could not be applied.
        }
    }

    private const int GwlExStyle = -20;
    private const nint WsExNoActivate = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(IntPtr hwnd, int index, nint value);

    #region Screenshots
    
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Asks the window to render itself first, so a screenshot is of the app even when the app
    /// is behind something else. The fallback — FlaUI's <c>Capture.Element</c> — reads the
    /// screen at the element's bounding rectangle, and with the app occluded that is a picture
    /// of whatever is on top. A failure diagnostic showing the wrong application is worse than
    /// none, because nothing about it looks wrong.
    /// </para>
    /// <para>
    /// The fallback is kept rather than replaced: <c>PW_RENDERFULLCONTENT</c> returns black for
    /// some GPU-composed content, and a minimized window has nothing to render. Reading the
    /// screen is wrong only when the window is covered, and right the rest of the time.
    /// </para>
    /// </remarks>
    public byte[] GetScreenshot()
    {
        var windowContent = TryCaptureWindowContent();
        if (windowContent != null)
        {
            using (windowContent)
                return ToPng(windowContent);
        }

        using var capture = Capture.Element(RootElement);
        return ToPng(capture.Bitmap);
    }

    /// <summary>
    /// Captures the app window's own content, or null when it declined to render.
    /// </summary>
    private System.Drawing.Bitmap? TryCaptureWindowContent()
    {
        var bitmap = WindowCapture.TryCapture(_rootWindowHandle);
        if (bitmap == null)
            return null;

        if (!WindowCapture.LooksBlank(bitmap))
            return bitmap;

        // Rendered, but empty: the GPU-composition failure mode. Reading the screen is the
        // better answer even though it may catch an overlapping window.
        bitmap.Dispose();
        return null;
    }

    private static byte[] ToPng(System.Drawing.Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }
    
    #endregion
    
    #region Context Switching (Not applicable for FlaUI)
    
    /// <inheritdoc />
    public string Context
    {
        get => "NATIVE_APP";
        set { } // No-op for FlaUI
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> Contexts => new[] { "NATIVE_APP" };
    
    #endregion
    
    #region Script Execution (Not applicable for FlaUI)
    
    /// <inheritdoc />
    public object? ExecuteScript(string script, params object[] args)
    {
        // FlaUI doesn't support script execution
        throw new NotSupportedException("Script execution is not supported by FlaUI driver");
    }
    
    #endregion
    
    #region IDiagnosticDriver
    
    /// <inheritdoc />
    public string GetPageSource()
    {
        // Build an XML representation of the automation tree
        return BuildAutomationTree(RootElement);
    }
    
    /// <inheritdoc />
    public string GetAutomationTree()
    {
        return BuildAutomationTree(RootElement);
    }
    
    private static string BuildAutomationTree(AutomationElement element, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        var sb = new System.Text.StringBuilder();
        
        // Use safe property access - some elements don't support all properties
        string automationId = "";
        string name = "";
        string className = "";
        string controlType = "Unknown";
        
        try { automationId = element.Properties.AutomationId.ValueOrDefault ?? ""; } catch { }
        try { name = element.Properties.Name.ValueOrDefault ?? ""; } catch { }
        try { className = element.Properties.ClassName.ValueOrDefault ?? ""; } catch { }
        try { controlType = element.ControlType.ToString(); } catch { }
        
        sb.AppendLine($"{indent}<{controlType} AutomationId=\"{automationId}\" Name=\"{name}\" ClassName=\"{className}\">");
        
        try
        {
            foreach (var child in element.FindAllChildren())
            {
                sb.Append(BuildAutomationTree(child, depth + 1));
            }
        }
        catch
        {
            // Ignore errors traversing children
        }
        
        sb.AppendLine($"{indent}</{controlType}>");
        return sb.ToString();
    }

    #endregion

    #region Gestures (Brinell UI Automation bridge)

    /// <inheritdoc />
    /// <remarks>
    /// Answered by asking the app under test what it declared, not by inspecting the control.
    /// False for a control that could obviously be swiped means the app has not opted that
    /// element in, which is a change to the app's markup rather than to the test.
    /// </remarks>
    public bool SupportsGesture(string automationId, MauiGesture gesture)
        => GestureRunner.Supports(RootElement, Automation, automationId, gesture);

    /// <inheritdoc />
    /// <remarks>
    /// Works on elements this driver cannot find at all. A MAUI <c>SwipeView</c> publishes no
    /// <c>AutomationId</c> on Windows, so <c>FindElement</c> will never return it - but its
    /// bridge element is addressable, and that is what carries the verb.
    /// </remarks>
    /// <exception cref="Bridge.GestureUnavailableException">
    /// The app publishes no bridge, the element was not declared, or the verb was refused.
    /// </exception>
    public void PerformGesture(string automationId, MauiGesture gesture)
        => GestureRunner.Perform(RootElement, Automation, automationId, gesture);

    /// <inheritdoc />
    /// <exception cref="Bridge.GestureUnavailableException">
    /// The app publishes no bridge, the element was not declared, or the verb was refused.
    /// </exception>
    public void PerformGesture(string automationId, MauiGesture gesture, int arg1, int arg2)
        => GestureRunner.Perform(RootElement, Automation, automationId, gesture, arg1, arg2);

    /// <summary>
    /// Whether the app under test publishes a Brinell bridge at all.
    /// </summary>
    /// <remarks>
    /// The one call that distinguishes "this app has no instrumentation" from "this element was
    /// not declared". Worth checking once in a fixture rather than inferring it from a failure.
    /// </remarks>
    /// <returns>Whether a bridge window is present.</returns>
    public bool HasGestureBridge()
        => Bridge.BrinellBridgeLookup.HasBridge(RootElement, Automation);

    /// <summary>
    /// Describes the raw automation tree just below the app window.
    /// </summary>
    /// <remarks>
    /// A diagnostic, for when a gesture is not found. The three causes present identically -
    /// no bridge window, a bridge window whose provider never answered, or a fragment root with
    /// nothing registered on it - and this is what tells them apart.
    /// </remarks>
    /// <param name="maxDepth">How far below the window to walk.</param>
    /// <returns>One line per element, indented by depth.</returns>
    public string DescribeGestureBridge(int maxDepth = 3)
        => Bridge.BrinellBridgeLookup.Describe(RootElement, Automation, maxDepth);

    /// <summary>
    /// Reports what each instrumented element offers somebody who is not using a pointer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An accessibility backlog, not a diagnostic.</b> Instrumenting an element for gestures
    /// is the moment to ask whether it is gesture-<i>only</i>, and this is what asks: for every
    /// element the app published, it finds the real control in the accessibility tree and reports
    /// whether a keyboard reaches it and whether it carries <c>InvokePattern</c>. The bridge does
    /// not fix a gesture-only control; it only lets Brinell drive one. This list is what keeps
    /// that honest.
    /// </para>
    /// <para>
    /// <b>Covers what is published now</b>, which is the pages that are open - elements publish
    /// on load and withdraw on unload. Call it as the app is walked and merge the passes.
    /// </para>
    /// </remarks>
    /// <param name="page">Where the app is, so a merged report says where each element was seen.</param>
    /// <returns>One finding per published element.</returns>
    public Bridge.AccessibilityAuditReport AuditGestureAccessibility(string page = "")
        => Bridge.AccessibilityAudit.Run(RootElement, Automation, page);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Asks whichever element the app published for the job, without naming one, because there
    /// is nothing to name: going back is about the app rather than about a control, and on
    /// Windows the affordance that would carry an <c>AutomationId</c> for it is a
    /// <c>ToolbarItem</c> - drawn into native chrome, and measured four separate ways not to be
    /// activatable through any automation pattern at all. That measurement is what makes this
    /// method necessary rather than convenient: without it, returning to a previous page is the
    /// one thing in the suite that has to be a real mouse click.
    /// </para>
    /// <para>
    /// Never falls back to real input; see the interface.
    /// </para>
    /// </remarks>
    public bool IsAtNavigationRoot() => NavigationDepth() <= 1;

    /// <inheritdoc />
    public bool IsIdle(int timeoutMs = 2000)
    {
        var answer = Bridge.BridgeVerbRunner.ExchangeAnywhere(
            RootElement,
            Automation,
            BrinellVerb.IsIdle,
            timeoutMs.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (!answer.Delivered)
        {
            throw new NotSupportedException(
                "The app under test cannot say whether it is idle, so there is nothing to wait on "
                + "and every such wait has to go back to being a sleep. Declare IsIdle on its "
                + $"pages - see GestureAutomation.Verbs. The bridge said: {answer.Reason}");
        }

        return bool.TryParse(answer.Value, out var idle) && idle;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Retries briefly, because every way of getting no answer here is also a moment in a page
    /// transition.</b> The obvious reading - nobody declares <c>GetState</c>, so waiting cannot
    /// help - was tried and is wrong: between one page withdrawing and the next publishing, the
    /// bridge is briefly empty and there is nobody to ask at all. An app that genuinely declares
    /// none pays the budget once and then gets a message naming the fix.
    /// <para>
    /// Neither case existed until step 43 stopped a page answering for a stack it is no longer
    /// part of. Before that a page mid-teardown answered with a depth of its own, which is how a
    /// suite came to be told it was at the hub while looking at another page.
    /// </para>
    /// </remarks>
    public int NavigationDepth()
    {
        const int transitionBudgetMs = 2_000;
        const int pollMs = 25;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Bridge.BridgeVerbResult answer;
        int depth;

        while (true)
        {
            answer = Bridge.BridgeVerbRunner.ExchangeAnywhere(
                RootElement, Automation, BrinellVerb.GetState, "NavigationDepth");

            if (answer.Delivered
                && int.TryParse(
                    answer.Value, System.Globalization.CultureInfo.InvariantCulture, out depth))
            {
                break;
            }

            // Any non-delivery is retried, including "nothing published answers GetState at all".
            //
            // That case looks like a configuration error and usually is one - but measured, it is
            // also what a page transition produces: the outgoing page has withdrawn and the
            // incoming one has not published yet, so for a few milliseconds the bridge is empty
            // and there is nobody to ask. Distinguishing them by the count was tried and is
            // wrong. An app that really declares no GetState pays the budget once and then gets
            // the message below, which names the fix.
            if (clock.ElapsedMilliseconds >= transitionBudgetMs)
            {
                break;
            }

            Thread.Sleep(pollMs);
        }

        if (!answer.Delivered
            || !int.TryParse(answer.Value, System.Globalization.CultureInfo.InvariantCulture, out depth))
        {
            throw new NotSupportedException(
                "The app under test cannot say how deep its navigation stack is, so whether it is "
                + "at the root is not knowable. Declare GetState on the app's pages - see "
                + "GestureAutomation.Verbs - or drive the back affordance as a control instead. "
                + $"The bridge said: {answer.Reason}" + Environment.NewLine
                + "What is below the app window, so an empty bridge can be told from no bridge:"
                + Environment.NewLine
                + DescribeBridgeSafely());
        }

        return depth;
    }

    /// <summary>The raw tree under the window, or why it could not be read.</summary>
    /// <remarks>
    /// For failure messages only. "Nothing answered" has three causes that read identically -
    /// no bridge window, a bridge window whose provider is gone, a bridge with nobody published -
    /// and the tree is what separates them. It must never throw on the way to describing a failure.
    /// </remarks>
    private string DescribeBridgeSafely()
    {
        try
        {
            return DescribeGestureBridge(maxDepth: 2);
        }
        catch (Exception describing)
        {
            return $"(the tree could not be read: {describing.Message})";
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>No grace period, and that is the fix.</b> This used to poll for two seconds whenever
    /// the verb did not answer <c>S_OK</c>, guarded on "does this app have a bridge" - which is
    /// always true for the app under test. So it fired on the commonest answer of all, *we are
    /// already at the root*, and every fixture reset that started at the hub paid two seconds to
    /// be told something it had been told immediately. See
    /// <c>.my/fix/rca-navigation-tests-stall.md</c>.
    /// </para>
    /// <para>
    /// <b>The race the grace period was added for is real, and the answers now say which is
    /// which.</b> A page publishes its bridge target on <c>Loaded</c>, later than its root
    /// appearing in the automation tree, so there is a window in which no live page answers -
    /// milliseconds after any navigation. Since step 43 the app distinguishes the three:
    /// <c>UIA_E_ELEMENTNOTAVAILABLE</c> for a stale target, <c>BRINELL_E_DECLINED</c> for the
    /// root with nothing to pop, and <c>S_OK</c> for a pop. All three cross the wire, which the
    /// old <c>S_FALSE</c> did not.
    /// </para>
    /// <para>
    /// <b>So the wait is back, bounded and only for the window it was meant for.</b> Not a grace
    /// period on every call - the two answers that mean something is genuinely wrong return at
    /// once, and only "nobody is published yet" is retried. A caller that commands without
    /// asking pays it once at the root, which is what <see cref="IsAtNavigationRoot"/> is for.
    /// </para>
    /// </remarks>
    public void NavigateBack()
    {
        const int publishWindowMs = 1_000;
        const int pollMs = 25;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Bridge.BridgeVerbResult result;

        while (true)
        {
            result = Bridge.BridgeVerbRunner.InvokeAnywhere(
                RootElement, Automation, BrinellVerb.NavigateBack);

            if (result.Delivered)
            {
                return;
            }

            // Something was asked and refused: the app has answered, and the answer is no.
            // Nothing was asked at all means no page has published yet, which a page transition
            // produces for a few milliseconds and a misconfigured app produces forever - the
            // budget tells them apart without needing to know which.
            if (result.Declined > 0 || clock.ElapsedMilliseconds >= publishWindowMs)
            {
                break;
            }

            Thread.Sleep(pollMs);
        }

        throw new BrinellException(
            "The app under test did not go back. This is a navigation failure with a specific "
            + $"cause, not something to retry: {result.Reason}");
    }


    #endregion

    #region Navigation
    
    /// <inheritdoc />
    /// <remarks>
    /// A Shell route, handed to the app's own <c>GoToAsync</c>. It used to say desktop apps have
    /// no URLs, which is true of desktop apps in general and not of this one: a MAUI Shell app
    /// navigates by route on every platform it runs on, and the bridge is how that route reaches
    /// it without a pointer.
    /// </remarks>
    public void NavigateTo(string destination)
    {
        var result = Bridge.BridgeVerbRunner.ExchangeAnywhere(
            RootElement, Automation, BrinellVerb.NavigateTo, destination);

        if (!result.Delivered)
        {
            throw new BrinellException(
                $"The app under test did not navigate to '{destination}'. Routes are a Shell "
                + "concept, so an app using a NavigationPage has none to go to. "
                + $"The bridge said: {result.Reason}");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Shell answers with its route. An app built on <c>NavigationPage</c> has no such thing, so
    /// it answers with the identity of the page on top - which is what a test asserting "we are
    /// on the hub" means anyway. Inventing a route for the second case would make two different
    /// navigation models look alike.
    /// </remarks>
    public string CurrentRoute()
    {
        var result = Bridge.BridgeVerbRunner.ExchangeAnywhere(
            RootElement, Automation, BrinellVerb.CurrentRoute);

        if (!result.Delivered)
        {
            throw new NotSupportedException(
                "The app under test cannot say where it is. Declare CurrentRoute on its pages - "
                + $"see GestureAutomation.Verbs. The bridge said: {result.Reason}");
        }

        return result.Value;
    }
    
    /// <inheritdoc />
    /// <remarks>
    /// <b>Re-navigates to where the app already is</b>, rather than sending F5. A MAUI app has no
    /// refresh key: F5 was desktop-wide keyboard input landing wherever the foreground happened
    /// to be, swallowing its own failure, and doing nothing at all in the common case. Asking the
    /// app to go to its current route is the nearest thing that is actually defined - and it
    /// fails loudly on an app that has no routes, rather than silently on every app.
    /// </remarks>
    public void Refresh() => NavigateTo(CurrentRoute());
    
    /// <inheritdoc />
    public byte[] TakeScreenshot() => GetScreenshot();
    
    /// <inheritdoc />
    public void ResetAppState()
    {
        // For desktop apps, close and relaunch
        if (_application != null)
        {
            // Note: This doesn't fully reset - caller may need to recreate the driver
            _application.Close();
        }
    }
    
    #endregion

    private bool TryInvokeBackButton()
    {
        try
        {
            var rootBounds = RootElement.BoundingRectangle;
            var buttons = RootElement.FindAllDescendants(_conditionFactory.ByControlType(ControlType.Button));

            var candidate = buttons
                .Where(IsBackButtonCandidate)
                .OrderBy(button => button.BoundingRectangle.Top)
                .ThenBy(button => button.BoundingRectangle.Left)
                .FirstOrDefault();

            if (candidate == null)
            {
                candidate = buttons
                    .Where(button => IsTopLeftButton(button, rootBounds))
                    .OrderBy(button => button.BoundingRectangle.Top)
                    .ThenBy(button => button.BoundingRectangle.Left)
                    .FirstOrDefault();
            }

            if (candidate == null)
                return false;

            if (candidate.Patterns.Invoke.IsSupported)
            {
                candidate.Patterns.Invoke.Pattern.Invoke();
                return true;
            }

            if (candidate.Patterns.LegacyIAccessible.IsSupported)
            {
                candidate.Patterns.LegacyIAccessible.Pattern.DoDefaultAction();
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsBackButtonCandidate(AutomationElement button)
    {
        var name = button.Properties.Name.ValueOrDefault ?? string.Empty;
        var automationId = button.Properties.AutomationId.ValueOrDefault ?? string.Empty;
        var className = button.Properties.ClassName.ValueOrDefault ?? string.Empty;
        var haystack = $"{name} {automationId} {className}";

        return haystack.Contains("back", StringComparison.OrdinalIgnoreCase)
               || haystack.Contains("terug", StringComparison.OrdinalIgnoreCase)
               || haystack.Contains("navigate", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTopLeftButton(AutomationElement button, Rectangle rootBounds)
    {
        if (!button.IsEnabled || button.IsOffscreen)
            return false;

        var bounds = button.BoundingRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return false;

        var maxX = rootBounds.Left + 96;
        var maxY = rootBounds.Top + 140;
        return bounds.Left >= rootBounds.Left
               && bounds.Left <= maxX
               && bounds.Top >= rootBounds.Top
               && bounds.Top <= maxY;
    }
    
    #region Dialogs

    /// <inheritdoc />
    public void InvokeMenuItem(string automationId)
    {
        var answer = Bridge.BridgeVerbRunner.ExchangeAnywhere(
            RootElement, Automation, BrinellVerb.InvokeMenuItem, automationId);

        // The outcome is in the payload, not in the HRESULT, and that is not a style choice.
        // A success HRESULT does not survive UI Automation's custom-pattern marshalling: the app
        // returning S_FALSE - "I found it and did nothing" - reaches this side as S_OK, measured
        // both ways round. Anything a caller must be able to tell apart from success therefore
        // has to travel as a value or as a failure HRESULT.
        if (answer.Delivered)
        {
            if (answer.Value != MenuItemDisabled)
            {
                return;
            }

            throw new BrinellException(
                $"Could not invoke menu item '{automationId}': it is disabled, so a user could "
                + "not have picked it either.");
        }

        var reason = answer.HResult switch
        {
            HResults.E_INVALIDARG => "no AutomationId was given",

            HResults.UIA_E_ELEMENTNOTAVAILABLE =>
                "no menu item on any open page carries that AutomationId. Menu items are matched "
                + "by the id in the app's markup, not by their text",

            _ => answer.Reason,
        };

        throw new BrinellException($"Could not invoke menu item '{automationId}': {reason}.");
    }

    /// <summary>
    /// What <c>InvokeMenuItem</c> answers with when it declined to raise the item.
    /// </summary>
    /// <remarks>
    /// Duplicated from the provider rather than shared, like the date formats above and for the
    /// same reason: the app under test is not always one Brinell can add a reference to. A
    /// mismatch is caught by <c>MenuVerbTests</c>, which drives a real disabled entry.
    /// </remarks>
    private const string MenuItemDisabled = "disabled";

    /// <inheritdoc />
    public void OpenFlyout() => PresentFlyout(BrinellVerb.OpenFlyout, "open");

    /// <inheritdoc />
    /// <remarks>
    /// All three - open, close, and the read that says which - because a caller that can open
    /// through the app but has to check through the chrome has not escaped the chrome.
    /// </remarks>
    public bool SupportsFlyoutVerbs
        => Bridge.BrinellBridgeLookup.Targets(RootElement, Automation).Any(target =>
        {
            var verbs = target.SupportedVerbs();
            return verbs.Contains(BrinellVerb.OpenFlyout)
                   && verbs.Contains(BrinellVerb.CloseFlyout)
                   && verbs.Contains(BrinellVerb.GetState);
        });

    /// <inheritdoc />
    public void CloseFlyout() => PresentFlyout(BrinellVerb.CloseFlyout, "close");

    /// <summary>
    /// Sends one of the two flyout verbs, and reports what came back.
    /// </summary>
    /// <remarks>
    /// <c>S_FALSE</c> - the flyout was already in the state asked for - is success here rather
    /// than a failure. The caller asked for a state, not for a transition, and a test that had
    /// to know which of the two happened would be asserting the order of the tests before it.
    /// </remarks>
    private void PresentFlyout(BrinellVerb verb, string what)
    {
        var answer = Bridge.BridgeVerbRunner.InvokeAnywhere(RootElement, Automation, verb);

        // A flyout already in the state asked for is success: the caller asked for a state, not
        // for a transition. The app does answer S_FALSE for it, which shows in the bridge log,
        // but that never reaches here - UI Automation reports every success HRESULT as S_OK - so
        // this deliberately does not pretend to tell the two apart.
        if (answer.Delivered)
        {
            return;
        }

        throw new BrinellException(
            $"Could not {what} the app's flyout. Either the app has no Shell - only a Shell has a "
            + "flyout - or no element declares the verb. The bridge said: " + answer.Reason);
    }

    /// <inheritdoc />
    public bool IsFlyoutOpen()
    {
        var answer = Bridge.BridgeVerbRunner.ExchangeAnywhere(
            RootElement, Automation, BrinellVerb.GetState, "FlyoutIsPresented");

        if (!answer.Delivered)
        {
            throw new NotSupportedException(
                "The app under test cannot say whether its flyout is open. Declare GetState on "
                + "its Shell - see GestureAutomation.Verbs - or ask the flyout's own elements, "
                + "remembering that Windows leaves them in the tree once it has been opened. "
                + $"The bridge said: {answer.Reason}");
        }

        return bool.TryParse(answer.Value, out var presented) && presented;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Null covers two different things on purpose here</b>, and that is unusual enough to
    /// say: no dialog is on screen, and no app-side declaration. Both mean "there is no question
    /// to read", both are ordinary rather than exceptional, and a test that wants to distinguish
    /// them asks <see cref="TryFindActiveDialogRoot"/> - a dialog on screen with no readable
    /// question is exactly the app that has not declared <c>CurrentAlert</c>.
    /// </remarks>
    public AlertContents? CurrentAlert()
    {
        // The screen decides whether there is an alert; the app only says what it asks.
        //
        // Without this the two could disagree, and did: the app clears its record when
        // DisplayAlert's await resumes, which is a continuation queued on the UI thread and
        // therefore some moments after the dialog has already left the tree. A test that
        // dismissed a prompt and immediately asked was told about the prompt it had just closed.
        // Each end answering only what it can see removes the window rather than narrowing it.
        if (TryFindActiveDialogRoot() is null)
        {
            return null;
        }

        var answer = Bridge.BridgeVerbRunner.ExchangeAnywhere(
            RootElement, Automation, BrinellVerb.CurrentAlert);

        if (!answer.Delivered
            || !AlertPayload.TryParse(
                answer.Value, out var title, out var message, out var accept, out var cancel))
        {
            return null;
        }

        return new AlertContents(title, message, accept, cancel);
    }

    /// <inheritdoc />
    public IMauiElement? TryFindActiveDialogRoot()
    {
        var popupCondition = _conditionFactory.ByControlType(ControlType.Window)
            .And(_conditionFactory.ByClassName("Popup"));
        var contentDialogCondition = _conditionFactory.ByClassName("ContentDialog");
        var buttonCondition = _conditionFactory.ByControlType(ControlType.Button);

        // Searched inside the app's own window: a WinUI ContentDialog renders as a Popup
        // descendant of it, not as the sibling top-level window one might expect. Enumerating
        // top-level windows instead costs about 8 s a call — it walks the desktop and filters by
        // process, so it grows with whatever else the machine has open — against about 15 ms
        // here.
        var inRootWindow = TryFindDialogRoot(
            RootElement, popupCondition, contentDialogCondition, buttonCondition);
        if (inRootWindow != null && !ReferenceEquals(inRootWindow, RootElement))
        {
            return new FlaUIMauiElement(inRootWindow, this);
        }

        return null;
    }

    private AutomationElement? TryFindDialogRoot(
        AutomationElement window,
        ConditionBase popupCondition,
        ConditionBase contentDialogCondition,
        ConditionBase buttonCondition)
    {
        try
        {
            var popup = window.FindAllDescendants(popupCondition)
                .LastOrDefault(candidate =>
                    !candidate.Properties.IsOffscreen.ValueOrDefault
                    && candidate.FindFirstDescendant(buttonCondition) != null);

            var dialog = popup
                ?? window.FindFirst(TreeScope.Element, contentDialogCondition)
                ?? window.FindFirstDescendant(contentDialogCondition);
            if (dialog != null)
                return dialog;

            return window.Properties.NativeWindowHandle.ValueOrDefault == _rootWindowHandle
                ? null
                : window;
        }
        catch (COMException)
        {
            return null;
        }
    }

    #endregion
    
    #region Scrolling

    /// <inheritdoc />
    /// <remarks>
    /// UIA keeps scrolled-off-screen elements in the tree with <c>IsOffscreen=true</c>, so
    /// scrolling reveals nothing a plain lookup missed. A virtualised list is the exception —
    /// there the answer is <c>VirtualizedItemPattern.Realize()</c>, not scrolling — and no list
    /// under test virtualises.
    /// </remarks>
    public IMauiElement? TryFindByScrollingWithin(IMauiElement? container, Locator locator) => null;

    #endregion
    
    #region IDisposable
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _watchdog?.Cancel();
            _application?.Close();
            _automation.Dispose();
            _disposed = true;
        }
    }
    
    #endregion
}
