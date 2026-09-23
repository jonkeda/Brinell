using System.Globalization;
using System.Runtime.InteropServices;
using Brinell.Uia;
#if BRINELL_UIA_BRIDGE
using Brinell.Uia.Provider;
#endif

namespace Brinell.Uia.TestHost;

/// <summary>
/// A window with a Brinell bridge on it, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// Launched by <c>Brinell.Uia.Tests</c>. Deliberately not a MAUI app: the spikes it serves ask
/// whether UI Automation will carry a custom pattern on a child window at all, and the answer
/// must not depend on WinUI, on XAML islands, or on a two-minute build.
/// </para>
/// <para>
/// Two targets are published. <c>SpikeTarget</c> answers verbs; <c>SpikeSecondTarget</c> exists
/// so that sibling navigation has something to navigate to, which is the part of the fragment
/// contract a single child cannot exercise.
/// </para>
/// <para>
/// <b>Built without <c>BRINELL_UIA_BRIDGE</c> it still runs, and shows a window with nothing
/// on it.</b> That is step 27's negative case and it needs a live process to be worth anything:
/// a test proving the fragment root is absent has to be able to tell "gated" from "the client
/// could not see the app at all", and the only way to do that is to find the window and fail to
/// find the bridge under it.
/// </para>
/// </remarks>
internal static class Program
{
    private const string WindowClassName = "BrinellUiaTestHostWindow";

    private static HostNative.WndProc? _windowProcedure;
    private static System.Threading.Timer? _deadline;
#if BRINELL_UIA_BRIDGE
    private static BrinellUiaBridge? _bridge;

    /// <summary>
    /// How many posted commands have run. The harness waits on this rather than on a reply.
    /// </summary>
    /// <remarks>
    /// Only ever touched on the message-loop thread, so it needs no synchronisation of its own;
    /// the SendMessage that reads it is dispatched on that same thread.
    /// </remarks>
    private static int _commandsHandled;
#endif

    /// <param name="args">
    /// <c>args[0]</c> is the window title, unique per run so concurrent runs cannot find each
    /// other's window. <c>args[1]</c> is a file to write once the bridge is up - a file rather
    /// than a line on stdout because a pipe can sit in a buffer, and the harness needs to know
    /// the window is ready rather than that it probably is.
    /// </param>
    /// <returns>Zero on a clean exit.</returns>
    [STAThread]
    private static int Main(string[] args)
    {
        var title = args.Length > 0 ? args[0] : "Brinell UIA Test Host";
        var readyFile = args.Length > 1 ? args[1] : null;

        var hwnd = CreateHostWindow(title);

#if BRINELL_UIA_BRIDGE
        // The same gate the app under test runs, from the same lines - this is why step 27's
        // tests can claim anything about the app. An app calls it through
        // UseBrinellGestureBridge(); here there is no builder to hang it off, so it is read
        // directly, and the decision is identical either way.
        if (BrinellBridgeGate.IsOpen)
        {
            _bridge = AttachBridge(hwnd);
        }
#endif

        Console.Out.WriteLine(BrinellBridgeGate.Explain());
        Report(readyFile, hwnd);

        StopEventually();

        RunMessageLoop();

#if BRINELL_UIA_BRIDGE
        _bridge?.Dispose();
#endif
        return 0;
    }

    private static IntPtr CreateHostWindow(string title)
    {
        _windowProcedure = WindowProcedure;

        var windowClass = new HostNative.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<HostNative.WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_windowProcedure),
            hInstance = HostNative.GetModuleHandle(null),
            hCursor = HostNative.LoadCursor(IntPtr.Zero, HostNative.IDC_ARROW),
            lpszClassName = WindowClassName,
        };

        if (HostNative.RegisterClassEx(ref windowClass) == 0)
        {
            throw new InvalidOperationException(
                $"RegisterClassEx failed ({Marshal.GetLastWin32Error()}).");
        }

        var hwnd = HostNative.CreateWindowEx(
            exStyle: 0,
            className: WindowClassName,
            windowName: title,
            style: HostNative.WS_OVERLAPPEDWINDOW | HostNative.WS_VISIBLE,
            x: 40,
            y: 40,
            width: 420,
            height: 260,
            parent: IntPtr.Zero,
            menu: IntPtr.Zero,
            instance: HostNative.GetModuleHandle(null),
            param: IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"CreateWindowEx failed ({Marshal.GetLastWin32Error()}).");
        }

        HostNative.ShowWindow(hwnd, HostNative.SW_SHOWNORMAL);
        return hwnd;
    }

    /// <summary>
    /// Writes what the harness needs to know, including this process's pattern id.
    /// </summary>
    /// <remarks>
    /// The pattern id is reported so the tests can compare it with their own. Registration
    /// hands out ids per process, and showing that the two differ while the calls still land is
    /// what proves the GUID rather than the id is the contract.
    /// </remarks>
    private static void Report(string? readyFile, IntPtr hwnd)
    {
        var registration = BrinellPatternRegistration.Current;

        var lines = new List<string>
        {
            $"HWND={hwnd.ToInt64().ToString(CultureInfo.InvariantCulture)}",
            $"PID={Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}",
            $"PATTERNID={registration.PatternId.ToString(CultureInfo.InvariantCulture)}",
            $"AVAILABLEPROPID={registration.IsPatternAvailablePropertyId.ToString(CultureInfo.InvariantCulture)}",
        };

#if BRINELL_UIA_BRIDGE
        if (_bridge is not null)
        {
            lines.Add("BRIDGE=on");
            lines.Add(
                $"BRIDGEHWND={_bridge.Handle.ToInt64().ToString(CultureInfo.InvariantCulture)}");
        }
        else
#endif
        {
            // No BRIDGEHWND, because there is no bridge window - and reporting a zero would let
            // a test read "gated" as "the handle was not filled in".
            lines.Add("BRIDGE=off");
            lines.Add($"BRIDGEWHY={BrinellBridgeGate.Explain()}");
        }

        var report = string.Join(Environment.NewLine, lines);

        Console.Out.WriteLine(report);
        Console.Out.Flush();

        if (readyFile is not null)
        {
            // Written last, and written whole: the harness waits on this file existing, so it
            // must never appear before the content it is announcing.
            var temporary = readyFile + ".partial";
            File.WriteAllText(temporary, report);
            File.Move(temporary, readyFile, overwrite: true);
        }
    }

#if BRINELL_UIA_BRIDGE
    /// <summary>
    /// One bridge with the host's two targets on it.
    /// </summary>
    /// <remarks>
    /// Shared between startup and <see cref="HostCommands.CycleBridge"/> so a soak cycle
    /// rebuilds exactly what it tore down. A cycle that produced a different bridge would make
    /// "the client can still use it afterwards" a weaker claim than it looks.
    /// </remarks>
    private static BrinellUiaBridge AttachBridge(IntPtr hwnd)
    {
        var bridge = BrinellUiaBridge.Attach(hwnd);
        bridge.Register(new RecordingTarget(HostTargets.Primary));
        bridge.Register(new RecordingTarget(HostTargets.Secondary));
        return bridge;
    }
#endif

    /// <summary>
    /// Ends the process if nobody has, well after any plausible test has finished.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A stray host does not just linger, it hangs the run that started it.</b> The harness
    /// kills its host in <c>Dispose</c>, but a test run that is itself killed never gets there -
    /// and a surviving child holds the inherited stdout pipe open, so the shell waits on a
    /// handle nobody is going to write to. That is a ten-minute silence that looks like a hung
    /// test suite and is not one; it happened while step 28 was being written.
    /// </para>
    /// <para>
    /// <b>Long enough not to be a timeout.</b> Five minutes is far past the slowest thing any
    /// test here does, so this can never end a run that is still going - it only cleans up after
    /// one that has already stopped.
    /// </para>
    /// </remarks>
    private static void StopEventually()
    {
        var deadline = new System.Threading.Timer(
            _ => Environment.Exit(0),
            state: null,
            dueTime: TimeSpan.FromMinutes(5),
            period: Timeout.InfiniteTimeSpan);

        // Rooted for the life of the process, because a collected timer never fires.
        GC.KeepAlive(deadline);
        _deadline = deadline;
    }

    private static void RunMessageLoop()
    {
        while (HostNative.GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            HostNative.TranslateMessage(ref message);
            HostNative.DispatchMessage(ref message);
        }
    }

    private static IntPtr WindowProcedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == HostNative.WM_DESTROY)
        {
            HostNative.PostQuitMessage(0);
            return IntPtr.Zero;
        }

#if BRINELL_UIA_BRIDGE
        // Step 28's lifetime commands. On this thread because it owns the window, and
        // synchronously because the caller is about to measure the result - see HostCommands.
        switch (message)
        {
            // Queries. Safe to answer inside a cross-process SendMessage, because none of them
            // calls out of the process.
            case HostCommands.CountBridges:
                return BrinellUiaBridge.ActiveCount;

            case HostCommands.CountDisconnectFailures:
                return BrinellUiaBridge.DisconnectFailures;

            case HostCommands.LastDisconnectHResult:
                return BrinellUiaBridge.LastDisconnectHResult;

            case HostCommands.CommandsHandled:
                return _commandsHandled;

            // Commands. Posted by the caller, never sent - UiaDisconnectProvider has to call out
            // to the client, and COM refuses an outgoing call while an input-synchronous one is
            // being dispatched. See HostCommands.
            case HostCommands.CycleBridge:
                _bridge?.Dispose();
                _bridge = AttachBridge(hwnd);
                _commandsHandled++;
                return IntPtr.Zero;

            case HostCommands.DestroyBridgeWindow:
                if (_bridge is not null)
                {
                    // Deliberately not Dispose. The bridge has to notice its own window going
                    // away, because in a real app that is how it usually goes: the parent closes
                    // and Windows destroys the children without telling anybody in managed code.
                    HostNative.DestroyWindow(_bridge.Handle);
                    _bridge = null;
                }

                _commandsHandled++;
                return IntPtr.Zero;
        }
#endif

        return HostNative.DefWindowProc(hwnd, message, wParam, lParam);
    }
}
