using System.Drawing;
using System.Runtime.InteropServices;

namespace Brinell.Presenter.Services;

/// <summary>
/// Docks the Presenter window, and places the app under test beside it by its top-level window:
/// the one way that works for every target, since a fixture starts its app (or its browser) in a
/// process of its own. The MAUI driver can also place its app itself (<c>BRINELL_AUT_PLACE</c>).
/// </summary>
/// <param name="window">The Presenter's window, once it exists.</param>
public sealed class PresenterWindowService(Func<Window?> window) : IPresenterWindowService
{
    private const int MinimumWidth = 200;
    private const int MinimumHeight = 150;

    /// <inheritdoc />
    public void Dock(PresenterDock dock)
    {
#if WINDOWS
        if (Handle() is not { } handle || handle == IntPtr.Zero || WorkArea(handle) is not { } workArea)
        {
            return;
        }

        var bar = PresenterLayout.Bar(workArea, dock);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle));
        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(bar.X, bar.Y, bar.Width, bar.Height));
#endif
    }

    /// <inheritdoc />
    public object Snapshot() => new HashSet<IntPtr>(TopLevelWindows().Select(entry => entry.Handle));

    /// <inheritdoc />
    public string? PlaceApp(object before, PresenterDock dock)
    {
#if WINDOWS
        if (Handle() is not { } own || own == IntPtr.Zero || WorkArea(own) is not { } workArea)
        {
            return "AUT placement:" + Environment.NewLine + "Result: not supported";
        }

        var known = before as HashSet<IntPtr> ?? [];
        var ownProcess = Environment.ProcessId;
        var candidate = TopLevelWindows()
            .Where(entry => !known.Contains(entry.Handle) && entry.ProcessId != ownProcess)
            .Where(entry => entry.Bounds.Width >= MinimumWidth && entry.Bounds.Height >= MinimumHeight)
            .OrderByDescending(entry => entry.Bounds.Width * entry.Bounds.Height)
            .FirstOrDefault();
        if (candidate.Handle == IntPtr.Zero)
        {
            return null;
        }

        GetWindowRect(own, out var ownRect);
        var bar = Rectangle.FromLTRB(ownRect.Left, ownRect.Top, ownRect.Right, ownRect.Bottom);
        var target = PresenterLayout.App(workArea, bar, dock);
        if (IsZoomed(candidate.Handle))
        {
            ShowWindow(candidate.Handle, SwRestore);
        }

        var moved = SetWindowPos(candidate.Handle, IntPtr.Zero, target.X, target.Y, target.Width, target.Height, SwpNoZOrder | SwpNoActivate);
        return string.Join(
            Environment.NewLine,
            "AUT placement:",
            $"Placement: {(dock == PresenterDock.Left ? "Right" : "Left")} of the Presenter",
            $"Presenter bounds: {Format(bar)}",
            $"Requested AUT bounds: {Format(target)}",
            $"Result: {(moved ? "moved" : "failed")}",
            $"Window: {candidate.Title}");
#else
        return "AUT placement:" + Environment.NewLine + "Result: not supported";
#endif
    }

    private static string Format(Rectangle rectangle)
        => $"x={rectangle.X} y={rectangle.Y} w={rectangle.Width} h={rectangle.Height}";

#if WINDOWS
    private const int SwRestore = 9;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint GwOwner = 4;
    private const uint MonitorDefaultToNearest = 2;

    private IntPtr? Handle()
        => window()?.Handler?.PlatformView is Microsoft.UI.Xaml.Window platform
            ? WinRT.Interop.WindowNative.GetWindowHandle(platform)
            : null;

    private static Rectangle? WorkArea(IntPtr handle)
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        return GetMonitorInfo(MonitorFromWindow(handle, MonitorDefaultToNearest), ref info)
            ? Rectangle.FromLTRB(info.WorkArea.Left, info.WorkArea.Top, info.WorkArea.Right, info.WorkArea.Bottom)
            : null;
    }

    // Visible, unowned, titled windows: an app's main window, not its tooltips or popups.
    private static List<(IntPtr Handle, int ProcessId, Rectangle Bounds, string Title)> TopLevelWindows()
    {
        List<(IntPtr, int, Rectangle, string)> windows = [];
        EnumWindows((handle, _) =>
        {
            var length = GetWindowTextLength(handle);
            if (IsWindowVisible(handle) && GetWindow(handle, GwOwner) == IntPtr.Zero && length > 0 && GetWindowRect(handle, out var rect))
            {
                var title = new char[length + 1];
                var read = GetWindowText(handle, title, title.Length);
                GetWindowThreadProcessId(handle, out var processId);
                windows.Add((handle, (int)processId, Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom), new string(title, 0, read)));
            }

            return true;
        }, IntPtr.Zero);
        return windows;
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr handle, uint command);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
    private static extern int GetWindowTextLength(IntPtr handle);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr handle, [Out] char[] text, int maxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr handle, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr handle, IntPtr after, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
#else
    private static List<(IntPtr Handle, int ProcessId, Rectangle Bounds, string Title)> TopLevelWindows() => [];
#endif
}
