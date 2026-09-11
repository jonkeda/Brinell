using System.Globalization;
using System.Runtime.InteropServices;
using Brinell.Uia;
using Brinell.Uia.Provider;

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
/// </remarks>
internal static class Program
{
    private const string WindowClassName = "BrinellUiaTestHostWindow";

    private static HostNative.WndProc? _windowProcedure;
    private static BrinellUiaBridge? _bridge;

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

        _bridge = BrinellUiaBridge.Attach(hwnd);
        _bridge.Register(new RecordingTarget(HostTargets.Primary));
        _bridge.Register(new RecordingTarget(HostTargets.Secondary));

        Report(readyFile, hwnd);

        RunMessageLoop();

        _bridge.Dispose();
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

        var report = string.Join(
            Environment.NewLine,
            $"HWND={hwnd.ToInt64().ToString(CultureInfo.InvariantCulture)}",
            $"PID={Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}",
            $"PATTERNID={registration.PatternId.ToString(CultureInfo.InvariantCulture)}",
            $"AVAILABLEPROPID={registration.IsPatternAvailablePropertyId.ToString(CultureInfo.InvariantCulture)}",
            $"BRIDGEHWND={_bridge!.Handle.ToInt64().ToString(CultureInfo.InvariantCulture)}");

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

        return HostNative.DefWindowProc(hwnd, message, wParam, lParam);
    }
}
