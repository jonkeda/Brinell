using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Core.Utilities;
using Brinell.Maui.Configuration;
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
    private const int SwRestore = 9;

    private readonly UIA3Automation _automation;
    private readonly Application? _application;
    private readonly AutomationElement _rootElement;
    private readonly ConditionFactory _conditionFactory;
    private readonly nint _rootWindowHandle;
    private bool _disposed;
    
    /// <summary>
    /// Creates a new FlaUIMauiDriver for an existing window.
    /// </summary>
    /// <param name="windowHandle">The window handle to attach to.</param>
    public FlaUIMauiDriver(IntPtr windowHandle)
    {
        _automation = new UIA3Automation();
        _rootElement = _automation.FromHandle(windowHandle);
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
            UseShellExecute = true
        };

        // Whoever the user was working in before the run. Windows hands a freshly launched
        // process the foreground, so without this the app steals focus and keeps it for the
        // whole run - see RestoreForegroundWindow.
        var previousForeground = GetForegroundWindow();

        var process = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {executablePath}");
        
        // Give the process time to initialize before attaching
        process.WaitForInputIdle();
        
        _application = Application.Attach(process);
        
        // Wait for main window
        var window = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(30));
        _rootElement = window ?? throw new InvalidOperationException("Failed to get main window");
        _conditionFactory = new ConditionFactory(_automation.PropertyLibrary);
        _rootWindowHandle = _rootElement.Properties.NativeWindowHandle.ValueOrDefault;
        TryApplyRequestedWindowPlacement();
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

    /// <summary>
    /// Gets the Windows interaction policy for this driver session.
    /// </summary>
    /// <summary>
    /// Checks whether a screen point falls within the root window bounds.
    /// </summary>
    /// <param name="point">Screen point to validate.</param>
    /// <param name="padding">Optional inset padding in pixels.</param>
    /// <returns>True when point is within the root window rectangle.</returns>
    internal bool IsPointInsideRootWindow(Point point, int padding = 0)
    {
        try
        {
            var rect = _rootElement.BoundingRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return false;
            }

            var left = rect.Left + padding;
            var right = rect.Right - padding;
            var top = rect.Top + padding;
            var bottom = rect.Bottom - padding;

            return point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures the root window is focused and activated before physical input.
    /// </summary>
    internal void EnsureRootWindowFocused(string action = "physical input")
    {
        BringRootWindowToForeground();
    }

    internal void PointerClick(Point point, string action)
    {
        EnsureRootWindowFocused(action);
        Mouse.MoveTo(point);
        Mouse.Down(MouseButton.Left);
        try
        {
            WaitHelper.Pause(120);
        }
        finally
        {
            Mouse.Up(MouseButton.Left);
        }
    }

    internal void PointerDoubleClick(AutomationElement element, string action)
    {
        EnsureRootWindowFocused(action);
        element.DoubleClick();
    }

    internal void PointerRightClick(AutomationElement element, string action)
    {
        EnsureRootWindowFocused(action);
        element.RightClick();
    }

    internal void PointerHover(Point point, string action)
    {
        EnsureRootWindowFocused(action);
        Mouse.MoveTo(point);
    }

    internal void PointerLongPress(Point point, int durationMs, string action)
    {
        EnsureRootWindowFocused(action);
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

    internal void PointerScroll(Point point, int wheelClicks, string action)
    {
        EnsureRootWindowFocused(action);
        Mouse.MoveTo(point);
        Mouse.Scroll(wheelClicks);
    }

    internal void PointerDrag(
        Point start,
        Point end,
        int durationMs,
        string action)
    {
        EnsureRootWindowFocused(action);
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

    internal void FocusForGlobalKeyboardInput(AutomationElement element, string action)
    {
        EnsureRootWindowFocused(action);
        element.Focus();
    }

    internal void GlobalType(string text, string action)
    {
        Keyboard.Type(text);
    }

    internal void GlobalType(VirtualKeyShort key, string action)
    {
        Keyboard.Type(key);
    }

    internal void GlobalTypeSimultaneously(
        string action,
        params VirtualKeyShort[] keys)
    {
        Keyboard.TypeSimultaneously(keys);
    }

    internal void SetClipboardTextForInput(string text, string action)
    {
        System.Windows.Forms.Clipboard.SetText(text);
    }

    private void BringRootWindowToForeground()
    {
        var nativeWindowHandle = _rootElement.Properties.NativeWindowHandle.ValueOrDefault;
        if (nativeWindowHandle != 0)
        {
            var handle = new IntPtr(nativeWindowHandle);
            ShowWindow(handle, SwRestore);
            SetForegroundWindow(handle);
            WaitHelper.Pause(100);
        }

        try
        {
            if (_rootElement.Patterns.Window.IsSupported)
            {
                var windowPattern = _rootElement.Patterns.Window.Pattern;
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

        try
        {
            _rootElement.SetForeground();
        }
        catch
        {
            // SetForeground can fail if the window is not top-level; fall back to Focus.
            try
            {
                _rootElement.Focus();
            }
            catch
            {
                // Ignore focus failures; interaction will proceed regardless.
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

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
    /// Best effort by nature: Windows only permits a foreground change from a process that
    /// currently holds it, so this can legitimately fail and is not worth failing a run over.
    /// </para>
    /// </remarks>
    private void RestoreForegroundWindow(IntPtr previousForeground)
    {
        if (previousForeground == IntPtr.Zero)
        {
            return;
        }

        try
        {
            SetForegroundWindow(previousForeground);
        }
        catch
        {
            // Losing this race only means the app keeps focus; it changes no test outcome.
        }
    }

    private void TryApplyRequestedWindowPlacement()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("BRINELL_AUT_PLACE_RIGHT"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var workArea = GetPrimaryWorkArea();
        var presenterWidth = Math.Max(320, workArea.Width / 4);
        var gap = 20;
        var requestedLeft = Math.Min(workArea.Right - 320, workArea.Left + presenterWidth + gap);
        var requestedWidth = Math.Max(320, workArea.Right - requestedLeft);
        var requested = new Rectangle(requestedLeft, workArea.Top, requestedWidth, workArea.Height);
        var presenter = new Rectangle(workArea.Left, workArea.Top, presenterWidth, workArea.Height);

        try
        {
            if (!_rootElement.Patterns.Transform.IsSupported)
            {
                WriteAutPlacementReport(presenter, requested, "not supported", "Transform pattern is not supported.");
                return;
            }

            var transform = _rootElement.Patterns.Transform.Pattern;
            if (!transform.CanMove.Value)
            {
                WriteAutPlacementReport(presenter, requested, "not supported", "Window cannot be moved.");
                return;
            }

            if (transform.CanResize.Value)
            {
                transform.Resize(requested.Width, requested.Height);
            }

            transform.Move(requested.Left, requested.Top);
            WriteAutPlacementReport(presenter, requested, "moved", actual: _rootElement.BoundingRectangle);
        }
        catch (Exception ex)
        {
            WriteAutPlacementReport(presenter, requested, $"failed: {ex.Message}");
        }
    }

    private static Rectangle GetPrimaryWorkArea()
    {
        if (TryGetPrimaryWorkArea(out var workArea))
        {
            return workArea;
        }

        return new Rectangle(0, 0, GetSystemMetrics(0), GetSystemMetrics(1));
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

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

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
            var found = _rootElement.FindFirstDescendant(condition);
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
                var found = _rootElement.FindAllDescendants(condition);
                if (found.Length > 0)
                {
                    return found.Select(e => new FlaUIMauiElement(e, this)).ToList();
                }
                WaitHelper.Pause(100);
            }
        }
        
        var elements = _rootElement.FindAllDescendants(condition);
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
    public string CurrentWindowHandle => _rootElement.Properties.NativeWindowHandle.Value.ToString();
    
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
        if (_rootElement.Patterns.Window.IsSupported)
        {
            _rootElement.Patterns.Window.Pattern.Close();
        }
    }
    
    #endregion
    
    #region Screenshots
    
    /// <inheritdoc />
    public byte[] GetScreenshot()
    {
        var capture = Capture.Element(_rootElement);
        using var ms = new MemoryStream();
        capture.Bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
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
        return BuildAutomationTree(_rootElement);
    }
    
    /// <inheritdoc />
    public string GetAutomationTree()
    {
        return BuildAutomationTree(_rootElement);
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
    
    #region Navigation
    
    /// <inheritdoc />
    public void NavigateTo(string destination)
    {
        // FlaUI desktop apps don't support URL navigation
        throw new NotSupportedException("URL navigation is not supported by FlaUI driver for desktop apps");
    }
    
    /// <inheritdoc />
    public void NavigateBack()
    {
        if (TryInvokeBackButton())
            return;

        try
        {
            FocusForGlobalKeyboardInput(_rootElement, nameof(NavigateBack));
            GlobalTypeSimultaneously(
                nameof(NavigateBack),
                VirtualKeyShort.ALT,
                VirtualKeyShort.LEFT);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Some locked-down desktops deny SendInput. Keep recovery best-effort
            // and avoid turning a completed test into a teardown failure.
        }
    }
    
    /// <inheritdoc />
    public void Refresh()
    {
        // Try F5 refresh for desktop apps
        FocusForGlobalKeyboardInput(_rootElement, nameof(Refresh));
        GlobalType(VirtualKeyShort.F5, nameof(Refresh));
    }
    
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
            var rootBounds = _rootElement.BoundingRectangle;
            var buttons = _rootElement.FindAllDescendants(_conditionFactory.ByControlType(ControlType.Button));

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
            _rootElement, popupCondition, contentDialogCondition, buttonCondition);
        if (inRootWindow != null && !ReferenceEquals(inRootWindow, _rootElement))
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
            _application?.Close();
            _automation.Dispose();
            _disposed = true;
        }
    }
    
    #endregion
}
