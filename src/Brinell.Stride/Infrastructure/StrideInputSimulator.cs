using System.Runtime.InteropServices;

namespace Brinell.Stride.Infrastructure;

/// <summary>
/// Simulates keyboard and mouse input for Stride game testing.
/// Uses Windows API for reliable input injection.
/// </summary>
public class StrideInputSimulator
{
    private readonly StrideTestOptions _options;

    /// <summary>
    /// Create a new input simulator.
    /// </summary>
    public StrideInputSimulator(StrideTestOptions? options = null)
    {
        _options = options ?? new StrideTestOptions();
    }

    #region Mouse Operations

    /// <summary>
    /// Move mouse to screen position.
    /// </summary>
    public void MoveTo(int x, int y)
    {
        // Convert to absolute coordinates (0-65535 range)
        var screenWidth = GetSystemMetrics(SM_CXSCREEN);
        var screenHeight = GetSystemMetrics(SM_CYSCREEN);

        var absoluteX = (x * 65535) / screenWidth;
        var absoluteY = (y * 65535) / screenHeight;

        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].mi.dx = absoluteX;
        inputs[0].mi.dy = absoluteY;
        inputs[0].mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;

        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    /// <summary>
    /// Click at screen position.
    /// </summary>
    public void Click(int x, int y)
    {
        MoveTo(x, y);
        Thread.Sleep(_options.ClickDelayMs);

        var inputs = new INPUT[2];

        // Mouse down
        inputs[0].type = INPUT_MOUSE;
        inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

        // Mouse up
        inputs[1].type = INPUT_MOUSE;
        inputs[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        Thread.Sleep(_options.PostClickDelayMs);
    }

    /// <summary>
    /// Double-click at screen position.
    /// </summary>
    public void DoubleClick(int x, int y)
    {
        Click(x, y);
        Thread.Sleep(50);
        Click(x, y);
    }

    /// <summary>
    /// Right-click at screen position.
    /// </summary>
    public void RightClick(int x, int y)
    {
        MoveTo(x, y);
        Thread.Sleep(_options.ClickDelayMs);

        var inputs = new INPUT[2];

        inputs[0].type = INPUT_MOUSE;
        inputs[0].mi.dwFlags = MOUSEEVENTF_RIGHTDOWN;

        inputs[1].type = INPUT_MOUSE;
        inputs[1].mi.dwFlags = MOUSEEVENTF_RIGHTUP;

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        Thread.Sleep(_options.PostClickDelayMs);
    }

    /// <summary>
    /// Scroll mouse wheel.
    /// </summary>
    public void Scroll(int clicks)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].mi.dwFlags = MOUSEEVENTF_WHEEL;
        inputs[0].mi.mouseData = (uint)(clicks * 120); // 120 = one notch

        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    #endregion

    #region Keyboard Operations

    /// <summary>
    /// Type a string of text.
    /// </summary>
    public void TypeText(string text)
    {
        foreach (var c in text)
        {
            TypeChar(c);
            Thread.Sleep(_options.KeyPressDelayMs);
        }
    }

    /// <summary>
    /// Type a single character.
    /// </summary>
    public void TypeChar(char c)
    {
        var inputs = new INPUT[2];

        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].mi.wScan = c;
        inputs[0].mi.dwFlags = KEYEVENTF_UNICODE;

        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].mi.wScan = c;
        inputs[1].mi.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    /// <summary>
    /// Press and release a key.
    /// </summary>
    public void PressKey(VirtualKey key)
    {
        KeyDown(key);
        Thread.Sleep(10);
        KeyUp(key);
    }

    /// <summary>
    /// Press a key down (hold).
    /// </summary>
    public void KeyDown(VirtualKey key)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].mi.wVk = (ushort)key;

        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    /// <summary>
    /// Release a key.
    /// </summary>
    public void KeyUp(VirtualKey key)
    {
        var inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].mi.wVk = (ushort)key;
        inputs[0].mi.dwFlags = KEYEVENTF_KEYUP;

        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    /// <summary>
    /// Hold a key for a duration.
    /// </summary>
    public void HoldKey(VirtualKey key, int durationMs)
    {
        KeyDown(key);
        Thread.Sleep(durationMs);
        KeyUp(key);
    }

    /// <summary>
    /// Press a key combination (e.g., Ctrl+A).
    /// </summary>
    public void PressKeyCombination(params VirtualKey[] keys)
    {
        // Press all keys down
        foreach (var key in keys)
        {
            KeyDown(key);
            Thread.Sleep(10);
        }

        // Release in reverse order
        for (var i = keys.Length - 1; i >= 0; i--)
        {
            KeyUp(keys[i]);
            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Press a hotkey combination (key with modifier).
    /// </summary>
    public void HotKey(VirtualKey key, VirtualKey modifier)
    {
        KeyDown(modifier);
        Thread.Sleep(10);
        PressKey(key);
        Thread.Sleep(10);
        KeyUp(modifier);
    }

    /// <summary>
    /// Press a hotkey combination with multiple modifiers.
    /// </summary>
    public void HotKey(VirtualKey key, params VirtualKey[] modifiers)
    {
        // Press modifiers down
        foreach (var mod in modifiers)
        {
            KeyDown(mod);
            Thread.Sleep(10);
        }

        // Press the key
        PressKey(key);

        // Release modifiers in reverse
        for (var i = modifiers.Length - 1; i >= 0; i--)
        {
            KeyUp(modifiers[i]);
            Thread.Sleep(10);
        }
    }

    #endregion

    #region Windows API

    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public MOUSEKEYBDINPUT mi;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct MOUSEKEYBDINPUT
    {
        // Mouse input
        [FieldOffset(0)] public int dx;
        [FieldOffset(4)] public int dy;
        [FieldOffset(8)] public uint mouseData;
        [FieldOffset(12)] public uint dwFlags;
        [FieldOffset(16)] public uint time;
        [FieldOffset(20)] public IntPtr dwExtraInfo;

        // Keyboard input (overlapping fields)
        [FieldOffset(0)] public ushort wVk;
        [FieldOffset(2)] public ushort wScan;
    }

    #endregion
}

/// <summary>
/// Virtual key codes for keyboard input.
/// </summary>
public enum VirtualKey : ushort
{
    // Letters
    A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45,
    F = 0x46, G = 0x47, H = 0x48, I = 0x49, J = 0x4A,
    K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E, O = 0x4F,
    P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54,
    U = 0x55, V = 0x56, W = 0x57, X = 0x58, Y = 0x59, Z = 0x5A,

    // Numbers
    D0 = 0x30, D1 = 0x31, D2 = 0x32, D3 = 0x33, D4 = 0x34,
    D5 = 0x35, D6 = 0x36, D7 = 0x37, D8 = 0x38, D9 = 0x39,

    // Function keys
    F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74,
    F6 = 0x75, F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79,
    F11 = 0x7A, F12 = 0x7B,

    // Special keys
    Escape = 0x1B,
    Tab = 0x09,
    CapsLock = 0x14,
    Shift = 0x10,
    Control = 0x11,
    Alt = 0x12,
    Space = 0x20,
    Enter = 0x0D,
    Backspace = 0x08,
    Delete = 0x2E,
    Insert = 0x2D,
    Home = 0x24,
    End = 0x23,
    PageUp = 0x21,
    PageDown = 0x22,

    // Arrow keys
    Left = 0x25,
    Up = 0x26,
    Right = 0x27,
    Down = 0x28,

    // Numpad
    NumPad0 = 0x60, NumPad1 = 0x61, NumPad2 = 0x62, NumPad3 = 0x63,
    NumPad4 = 0x64, NumPad5 = 0x65, NumPad6 = 0x66, NumPad7 = 0x67,
    NumPad8 = 0x68, NumPad9 = 0x69,
    Multiply = 0x6A, Add = 0x6B, Subtract = 0x6D, Decimal = 0x6E, Divide = 0x6F
}
