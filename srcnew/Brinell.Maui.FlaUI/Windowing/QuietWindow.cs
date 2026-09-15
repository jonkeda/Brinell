using static Brinell.Maui.FlaUI.Windowing.NativeMethods;

namespace Brinell.Maui.FlaUI.Windowing;

/// <summary>
/// Keeps the app under test behind whatever the person at the machine is using.
/// </summary>
/// <remarks>
/// <para>
/// Three measures, always active. They used to switch off when physical input was allowed,
/// because a run that clicks at coordinates needs the window in front; this driver no longer
/// clicks or types at all, so nothing needs the window in front:
/// </para>
/// <list type="bullet">
/// <item>a <b>watchdog</b> that pushes the app back whenever any of its windows takes the foreground;</item>
/// <item><b><c>WS_EX_NOACTIVATE</c></b> on the main window, so a pattern call does not raise it;</item>
/// <item>sending the app <b>behind</b> once it has launched, and handing the foreground back.</item>
/// </list>
/// <para>
/// Step 104 moved these out of <c>FlaUIMauiDriver</c>. See AD-005 and the MAUI platform guide.
/// </para>
/// </remarks>
internal sealed class QuietWindow : IDisposable
{
    private readonly int _processId;
    private readonly IntPtr _previousForeground;
    private readonly CancellationTokenSource? _watchdog;
    private int _foregroundGrabs;

    private QuietWindow(int processId, IntPtr previousForeground)
    {
        _processId = processId;
        _previousForeground = previousForeground;

        _watchdog = new CancellationTokenSource();
        var token = _watchdog.Token;
        new Thread(() => WatchTheForeground(token))
        {
            IsBackground = true,
            Name = "Brinell foreground watchdog",
        }.Start();
    }

    /// <summary>
    /// Starts watching a freshly launched app. Call it before anything waits on the app.
    /// </summary>
    /// <remarks>
    /// Launch is when the app is certain to grab the foreground, and waiting for its main window
    /// takes seconds. Started any later, the watchdog only cleans up after the flash the person at
    /// the machine has already seen.
    /// </remarks>
    /// <param name="processId">The app under test, whose windows are watched.</param>
    /// <param name="previousForeground">The window the person was using, to hand back.</param>
    internal static QuietWindow ForLaunch(int processId, IntPtr previousForeground)
        => new(processId, previousForeground);

    /// <summary>
    /// How many times the app under test took the foreground and had to be put back.
    /// </summary>
    /// <remarks>
    /// <b>Zero is the claim this framework makes.</b> A run that never takes the keyboard or the
    /// pointer but puts its window over what somebody is reading has still taken the machine.
    /// </remarks>
    internal int ForegroundGrabs => Volatile.Read(ref _foregroundGrabs);

    /// <summary>
    /// Settles the main window once it is known: refuses activation, sends it behind, and hands
    /// the foreground back.
    /// </summary>
    internal void Settle(IntPtr mainWindow)
    {
        RefuseActivation(mainWindow);
        SendBehind(mainWindow);
        RestorePreviousForeground();
    }

    /// <summary>
    /// Marks the app's window as one that does not become the foreground window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Measured, not supposed: every button click raised the app over the person's work.</b>
    /// The watchdog counted two foreground grabs per <c>Click</c>, navigating or not, and none for
    /// a read. <c>Click</c> is <c>InvokePattern.Invoke</c> - not physical input, and never refused
    /// as such - and WinUI answers it by activating the window. Refusing input could never catch
    /// it, because no input was ever sent.
    /// </para>
    /// <para>
    /// <b><c>WS_EX_NOACTIVATE</c> refuses it at the source.</b> The window keeps working and keeps
    /// answering UI Automation; it simply is not made the foreground window when something inside
    /// it asks. Set on the app's own handle, so it needs nothing from the app under test.
    /// </para>
    /// </remarks>
    private static void RefuseActivation(IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var style = GetWindowLongPtr(window, GwlExStyle);
            SetWindowLongPtr(window, GwlExStyle, style | WsExNoActivate);
        }
        catch
        {
            // Out of the way is a courtesy; the watchdog still pushes the window back if this
            // could not be applied.
        }
    }

    /// <summary>
    /// Puts a window at the bottom of the z-order without activating it.
    /// </summary>
    /// <remarks>
    /// Behind, not minimized and not hidden. A minimized WinUI window can stop laying out and may
    /// never realize virtualized content, so the suite would start failing on elements that
    /// genuinely are not there. Behind is invisible to the person using the machine and fully
    /// composed to UI Automation.
    /// </remarks>
    private static void SendBehind(IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            return;
        }

        try
        {
            SetWindowPos(window, HwndBottom, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
        }
        catch
        {
            // Out of the way is a courtesy, not a requirement. It changes no test outcome.
        }
    }

    /// <summary>
    /// Hands the foreground back to the window that had it before the app was launched.
    /// </summary>
    /// <remarks>
    /// <b>Asking usually does not work, which is why <see cref="SendBehind"/> comes first.</b>
    /// Windows only permits a foreground change from the process that holds it, so this request is
    /// normally refused - silently. Pushing the app down the z-order needs no such permission.
    /// </remarks>
    private void RestorePreviousForeground()
    {
        if (_previousForeground == IntPtr.Zero)
        {
            return;
        }

        try
        {
            SetForegroundWindow(_previousForeground);
        }
        catch
        {
            // Losing this race only means the app keeps focus; it changes no test outcome.
        }
    }

    /// <remarks>
    /// <para>
    /// <b>A polling thread rather than <c>SetWinEventHook</c>.</b> The hook delivers through a
    /// message queue; a test runner's threads do not pump one, so the events would arrive when
    /// nobody is listening.
    /// </para>
    /// <para>
    /// <b>By process, not by window handle.</b> The handle is not known until the main window has
    /// been found, seconds after launch - which is precisely the stretch in which the app takes
    /// the foreground. Eager for as long as launch can take, then settled.
    /// </para>
    /// </remarks>
    private void WatchTheForeground(CancellationToken cancellation)
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
                    && owner == _processId)
                {
                    Interlocked.Increment(ref _foregroundGrabs);
                    SendBehind(foreground);
                    RestorePreviousForeground();
                }
            }
            catch
            {
                // The app has gone, or the handle is stale. Neither is worth taking a test run
                // down over: this is a courtesy thread.
            }

            Thread.Sleep(clock.ElapsedMilliseconds < eagerForMs ? eagerMs : settledMs);
        }
    }

    public void Dispose() => _watchdog?.Cancel();
}
