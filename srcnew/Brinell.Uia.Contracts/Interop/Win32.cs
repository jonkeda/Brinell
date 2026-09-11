using System.Runtime.InteropServices;

namespace Brinell.Uia.Interop;

/// <summary>The window-management calls the bridge needs to own an HWND.</summary>
/// <remarks>
/// A custom UI Automation pattern can only be answered by a native provider, and the only
/// supported way to put a native provider into the tree is to return it from
/// <c>WM_GETOBJECT</c> on a window. Hence a window, and hence this file.
/// </remarks>
internal static class Win32
{
    internal const int WM_GETOBJECT = 0x003D;
    internal const int WM_DESTROY = 0x0002;
    internal const int WM_NCDESTROY = 0x0082;

    /// <summary>
    /// The object id UI Automation asks for. Not <c>OBJID_CLIENT</c>.
    /// </summary>
    /// <remarks>
    /// <c>WM_GETOBJECT</c> is shared with MSAA, which asks for <c>OBJID_CLIENT</c> (0) on the
    /// same message. Answering both with the same provider is how you end up with two
    /// disagreeing trees; answering only this one is correct.
    /// </remarks>
    internal const int UiaRootObjectId = -25;

    internal const int WS_CHILD = 0x40000000;
    internal const int WS_VISIBLE = 0x10000000;
    internal const int WS_DISABLED = 0x08000000;

    /// <summary>Excludes the window from the parent's tab order and from Alt+Tab.</summary>
    internal const int WS_EX_NOACTIVATE = 0x08000000;
    internal const int WS_EX_TRANSPARENT = 0x00000020;

    internal const int ERROR_CLASS_ALREADY_EXISTS = 1410;

    internal delegate IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;

        public IntPtr hIconSm;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "RegisterClassExW")]
    internal static extern ushort RegisterClassEx(ref WNDCLASSEX windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateWindowExW")]
    internal static extern IntPtr CreateWindowEx(
        int exStyle,
        [MarshalAs(UnmanagedType.LPWStr)] string className,
        [MarshalAs(UnmanagedType.LPWStr)] string? windowName,
        int style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr param);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "DefWindowProcW")]
    internal static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(IntPtr hwnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetModuleHandleW")]
    internal static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string? moduleName);

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("kernel32.dll")]
    internal static extern uint GetCurrentThreadId();
}
