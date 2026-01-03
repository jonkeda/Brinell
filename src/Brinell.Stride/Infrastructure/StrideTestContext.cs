using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Brinell.Core.Abstractions;
using Brinell.Core.Logging;
using Brinell.Stride.Communication;

namespace Brinell.Stride.Infrastructure;

/// <summary>
/// Test context for Stride game engine UI testing.
/// </summary>
public class StrideTestContext : ITestContext, IDisposable
{
    private readonly IAutomationChannel _channel;
    private readonly StrideInputSimulator _inputSimulator;
    private readonly StrideTestOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;
    private WindowInfo? _cachedWindowInfo;
    private IntPtr _gameWindowHandle;

    /// <inheritdoc />
    public string TestName { get; set; } = string.Empty;

    /// <inheritdoc />
    public Platform Platform => Platform.Stride;

    /// <inheritdoc />
    public int DefaultTimeoutMs => _options.DefaultTimeoutMs;

    /// <inheritdoc />
    public int ShortTimeoutMs => _options.ShortTimeoutMs;

    /// <inheritdoc />
    public int PollingIntervalMs => _options.PollingIntervalMs;

    /// <inheritdoc />
    public ITestLogger? Logger { get; private set; }

    /// <summary>
    /// The input simulator for this context.
    /// </summary>
    public StrideInputSimulator Input => _inputSimulator;

    /// <summary>
    /// Create a new Stride test context.
    /// </summary>
    public StrideTestContext(IAutomationChannel channel, StrideTestOptions? options = null)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _options = options ?? new StrideTestOptions();
        _inputSimulator = new StrideInputSimulator(_options);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Set the game window handle for focus management.
    /// </summary>
    public void SetGameWindowHandle(IntPtr handle)
    {
        _gameWindowHandle = handle;
        Log($"Game window handle set: {handle:X}");
    }

    /// <inheritdoc />
    public void SetLogger(ITestLogger logger) => Logger = logger;

    /// <inheritdoc />
    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] [{TestName}] {message}");
    }

    /// <inheritdoc />
    public void LogError(Exception ex, string context)
    {
        Log($"ERROR in {context}: {ex.Message}");
        Logger?.LogError(TestName, "", "", context, ex);
    }

    /// <inheritdoc />
    public bool WaitFor(Func<bool> condition, int? timeoutMs = null, string description = "condition")
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            try
            {
                if (condition())
                    return true;
            }
            catch
            {
                // Ignore exceptions during polling
            }

            Thread.Sleep(PollingIntervalMs);
        }

        Log($"WaitFor '{description}' timed out after {timeout}ms");
        return false;
    }

    /// <inheritdoc />
    public string? TakeScreenshot(string name)
    {
        try
        {
            var response = SendCommand(AutomationCommand.Action("TakeScreenshot", null, name));
            if (response.Success)
            {
                return response.Result?.ToString();
            }
            else
            {
                Log($"Screenshot failed: {response.Error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to take screenshot: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public string CaptureFailureScreenshot(string suffix = "failure")
    {
        var name = $"{TestName}_{suffix}_{DateTime.Now:HHmmss}";
        return TakeScreenshot(name) ?? string.Empty;
    }

    #region Element Operations

    /// <summary>
    /// Get element state from the game.
    /// </summary>
    public ElementState GetElementState(string automationId)
    {
        var response = SendCommand(AutomationCommand.Query("GetState", automationId));

        if (!response.Success || response.Result == null)
        {
            return new ElementState { Exists = false };
        }

        try
        {
            var json = response.Result.ToString()!;
            return JsonSerializer.Deserialize<ElementState>(json, _jsonOptions)
                ?? new ElementState { Exists = false };
        }
        catch
        {
            return new ElementState { Exists = false };
        }
    }

    /// <summary>
    /// Check if an element exists.
    /// </summary>
    public bool ElementExists(string automationId)
        => GetElementState(automationId).Exists;

    /// <summary>
    /// Check if an element is visible.
    /// </summary>
    public bool ElementIsVisible(string automationId)
    {
        var state = GetElementState(automationId);
        return state.Exists && state.IsVisible;
    }

    /// <summary>
    /// Check if an element is enabled.
    /// </summary>
    public bool ElementIsEnabled(string automationId)
    {
        var state = GetElementState(automationId);
        return state.Exists && state.IsEnabled;
    }

    /// <summary>
    /// Get element text.
    /// </summary>
    public string GetElementText(string automationId)
        => GetElementState(automationId).Text ?? string.Empty;

    /// <summary>
    /// Set element text directly via server-side command (more reliable than keyboard simulation).
    /// </summary>
    public bool SetElementText(string automationId, string text)
    {
        var response = SendCommand(AutomationCommand.Action("SetElementText", automationId, text));
        return response.Success;
    }

    /// <summary>
    /// Set slider value directly via server-side command (more reliable than mouse clicks).
    /// </summary>
    public bool SetSliderValue(string automationId, double value)
    {
        var response = SendCommand(AutomationCommand.Action("SetSliderValue", automationId, value));
        return response.Success;
    }

    /// <summary>
    /// Get element bounds.
    /// </summary>
    public ElementBounds GetElementBounds(string automationId)
        => GetElementState(automationId).Bounds;

    #endregion

    #region Window Focus Management

    /// <summary>
    /// Ensure the game window has focus before input operations.
    /// Uses aggressive focus-stealing techniques to work around Windows restrictions.
    /// </summary>
    public bool EnsureGameHasFocus(int timeoutMs = 5000)
    {
        if (_gameWindowHandle == IntPtr.Zero)
        {
            Log("Warning: Game window handle not set, cannot ensure focus");
            return false;
        }

        if (!OperatingSystem.IsWindows())
        {
            Log("Window focus management is only supported on Windows");
            return true; // Don't fail on other platforms
        }

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            var currentForeground = GetForegroundWindow();
            if (currentForeground == _gameWindowHandle)
            {
                Log("Game window already has focus");
                return true;
            }

            Log($"Setting game window to foreground (current: {currentForeground:X}, target: {_gameWindowHandle:X})");
            
            // Use aggressive focus-stealing technique
            ForceForegroundWindow(_gameWindowHandle);
            Thread.Sleep(100); // Give window time to receive focus

            // Verify focus was set
            currentForeground = GetForegroundWindow();
            if (currentForeground == _gameWindowHandle)
            {
                Log("Game window focus set successfully");
                return true;
            }

            Thread.Sleep(150); // Wait before retry
        }

        Log($"Failed to set game window focus after {timeoutMs}ms");
        return false;
    }

    /// <summary>
    /// Forcefully set a window to foreground, bypassing Windows restrictions.
    /// </summary>
    private void ForceForegroundWindow(IntPtr targetWindow)
    {
        var currentThread = GetCurrentThreadId();
        var foregroundWindow = GetForegroundWindow();
        var foregroundThread = GetWindowThreadProcessId(foregroundWindow, out _);
        
        // Attach input threads to allow focus stealing
        var attached = false;
        if (foregroundThread != currentThread)
        {
            attached = AttachThreadInput(currentThread, foregroundThread, true);
        }

        try
        {
            // Try multiple methods to set foreground
            
            // Method 1: Standard SetForegroundWindow
            SetForegroundWindow(targetWindow);
            
            // Method 2: Show and activate the window
            ShowWindow(targetWindow, SW_RESTORE);
            
            // Method 3: Bring to top and activate
            BringWindowToTop(targetWindow);
            
            // Method 4: Set focus explicitly
            SetFocus(targetWindow);
            
            // Method 5: Simulate Alt key to unlock foreground locking
            // Windows allows focus changes immediately after Alt is pressed
            var inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].ki.wVk = VK_MENU; // Alt key
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].ki.wVk = VK_MENU;
            inputs[1].ki.dwFlags = KEYEVENTF_KEYUP;
            SendInput(2, inputs, Marshal.SizeOf<INPUT>());
            
            // Try SetForegroundWindow again after Alt
            SetForegroundWindow(targetWindow);
        }
        finally
        {
            // Detach input threads
            if (attached)
            {
                AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }

    // Window focus P/Invoke declarations
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const int SW_RESTORE = 9;
    private const int INPUT_KEYBOARD = 1;
    private const ushort VK_MENU = 0x12; // Alt key
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // Win32 GetWindowRect for fallback window position
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// Get window rectangle using Win32 API as fallback.
    /// </summary>
    private (int x, int y, int width, int height)? GetWindowRectFallback()
    {
        if (_gameWindowHandle == IntPtr.Zero)
        {
            Log("GetWindowRectFallback: No window handle");
            return null;
        }

        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        if (GetWindowRect(_gameWindowHandle, out RECT rect))
        {
            var x = rect.Left;
            var y = rect.Top;
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            Log($"GetWindowRectFallback: ({x}, {y}) {width}x{height}");
            return (x, y, width, height);
        }

        Log("GetWindowRectFallback: GetWindowRect failed");
        return null;
    }

    /// <summary>
    /// Get window rectangle, trying pipe query first, then Win32 fallback.
    /// </summary>
    private (int x, int y, int width, int height) GetWindowRectWithFallback()
    {
        // Try pipe query first
        var windowInfo = GetWindowInfo();
        if (windowInfo != null && windowInfo.WindowWidth > 0)
        {
            Log($"GetWindowRectWithFallback: Via pipe - ({windowInfo.WindowX}, {windowInfo.WindowY}) {windowInfo.WindowWidth}x{windowInfo.WindowHeight}");
            return (windowInfo.WindowX, windowInfo.WindowY,
                    windowInfo.WindowWidth, windowInfo.WindowHeight);
        }

        // Fallback to Win32 API
        var fallback = GetWindowRectFallback();
        if (fallback.HasValue)
        {
            return fallback.Value;
        }

        Log("GetWindowRectWithFallback: No window info available");
        return (0, 0, 0, 0);
    }

    #endregion

    #region Input Actions

    /// <summary>
    /// Click an element by automation ID.
    /// </summary>
    public void ClickElement(string automationId)
    {
        var bounds = GetElementBounds(automationId);
        if (bounds.IsEmpty)
        {
            throw new InvalidOperationException($"Cannot click element '{automationId}' - not found or has no bounds");
        }

        // Transform UI-local coordinates to screen coordinates
        var (screenX, screenY) = TransformToScreenCoordinates(bounds.CenterX, bounds.CenterY);
        Log($"Clicking '{automationId}' at UI({bounds.CenterX}, {bounds.CenterY}) -> Screen({screenX}, {screenY})");
        _inputSimulator.Click(screenX, screenY);
    }

    /// <summary>
    /// Transform UI-local coordinates to screen coordinates.
    /// </summary>
    private (int screenX, int screenY) TransformToScreenCoordinates(int uiX, int uiY)
    {
        var windowInfo = GetWindowInfo();
        if (windowInfo == null)
        {
            Log("Warning: Could not get window info, using UI coordinates as screen coordinates");
            return (uiX, uiY);
        }

        // Add window position offset to convert UI-local to screen coordinates
        var screenX = windowInfo.WindowX + uiX;
        var screenY = windowInfo.WindowY + uiY;

        return (screenX, screenY);
    }

    /// <summary>
    /// Get current window information from the game.
    /// </summary>
    private WindowInfo? GetWindowInfo()
    {
        // Refresh window info each time since window might move
        try
        {
            var response = SendCommand(AutomationCommand.GameQuery("GetWindowInfo"));
            if (response.Success && response.Result != null)
            {
                var json = response.Result.ToString()!;
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions);
                if (dict != null)
                {
                    _cachedWindowInfo = new WindowInfo
                    {
                        WindowX = dict.TryGetValue("windowX", out var x) ? x.GetInt32() : 0,
                        WindowY = dict.TryGetValue("windowY", out var y) ? y.GetInt32() : 0,
                        WindowWidth = dict.TryGetValue("windowWidth", out var w) ? w.GetInt32() : 1280,
                        WindowHeight = dict.TryGetValue("windowHeight", out var h) ? h.GetInt32() : 720
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to get window info: {ex.Message}");
        }

        return _cachedWindowInfo;
    }

    /// <summary>
    /// Type text using keyboard simulation.
    /// </summary>
    public void TypeText(string text)
    {
        if (!EnsureGameHasFocus())
        {
            Log("Warning: Game may not have focus, text input might go to wrong window");
        }
        _inputSimulator.TypeText(text);
    }

    /// <summary>
    /// Press a key using Windows SendInput.
    /// </summary>
    public void PressKey(VirtualKey key)
    {
        EnsureGameHasKeyboardFocus();
        Thread.Sleep(50); // Small delay after focus
        _inputSimulator.PressKey(key);
        Thread.Sleep(50); // Small delay for game to process
    }

    /// <summary>
    /// Hold a key for a duration using Windows SendInput.
    /// </summary>
    public void HoldKey(VirtualKey key, int durationMs)
    {
        EnsureGameHasKeyboardFocus();
        Thread.Sleep(100); // Give game time to process focus change
        Log($"HoldKey: Sending {key} for {durationMs}ms");
        _inputSimulator.HoldKey(key, durationMs);
        Log($"HoldKey: Released {key}");
        Thread.Sleep(100); // Give game time to process key release
    }

    /// <summary>
    /// Ensure the game has TRUE keyboard focus by clicking on the window.
    /// Windows SendInput goes to the window with keyboard focus, which isn't always
    /// the same as the foreground window. The ONLY reliable way to get keyboard
    /// focus is to physically click the window.
    /// </summary>
    private void EnsureGameHasKeyboardFocus()
    {
        // First, try to ensure the window is foreground
        EnsureGameHasFocus();

        // Get window rectangle using pipe query or Win32 fallback
        var (x, y, width, height) = GetWindowRectWithFallback();

        if (width > 0 && height > 0)
        {
            // Click in the center of the game window
            var centerX = x + width / 2;
            var centerY = y + height / 2;
            Log($"Clicking center of game window at ({centerX}, {centerY}) to ensure keyboard focus");
            _inputSimulator.Click(centerX, centerY);
            Thread.Sleep(200); // Wait for focus to be established (at least 2-3 game frames)
        }
        else
        {
            Log("ERROR: Cannot determine window position for focus click - keyboard input may fail!");
        }
    }

    /// <summary>
    /// Move mouse to position.
    /// </summary>
    public void MoveMouse(int x, int y)
        => _inputSimulator.MoveTo(x, y);

    #endregion

    #region Game State

    /// <summary>
    /// Check if game is ready for testing.
    /// </summary>
    public bool IsGameReady()
    {
        try
        {
            var response = SendCommand(AutomationCommand.Query("IsGameReady"));
            return response.Success && response.Result is bool ready && ready;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Wait for game to be ready.
    /// </summary>
    public bool WaitForGameReady(int? timeoutMs = null)
        => WaitFor(IsGameReady, timeoutMs ?? _options.StartupTimeoutMs, "game ready");

    /// <summary>
    /// Check if game is busy (loading, etc.).
    /// </summary>
    public bool IsGameBusy()
    {
        try
        {
            var response = SendCommand(AutomationCommand.Query("IsBusy"));
            return response.Success && response.Result is bool busy && busy;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Wait for game to not be busy.
    /// </summary>
    public bool WaitForNotBusy(int? timeoutMs = null)
        => WaitFor(() => !IsGameBusy(), timeoutMs, "not busy");

    #endregion

    #region Generic Query/Command

    /// <summary>
    /// Send a command to the game synchronously.
    /// </summary>
    public AutomationResponse SendCommand(AutomationCommand command)
        => SendCommandAsync(command).GetAwaiter().GetResult();

    /// <summary>
    /// Send a command to the game asynchronously.
    /// </summary>
    public Task<AutomationResponse> SendCommandAsync(AutomationCommand command, CancellationToken cancellationToken = default)
        => _channel.SendCommandAsync(command, cancellationToken);

    /// <summary>
    /// Query a value from the game.
    /// </summary>
    public T? Query<T>(string method, params object[] args)
    {
        var response = SendCommand(AutomationCommand.Query(method, null, args));
        if (!response.Success || response.Result == null)
        {
            return default;
        }

        try
        {
            var json = JsonSerializer.Serialize(response.Result);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            return default;
        }
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _channel.Dispose();
        GC.SuppressFinalize(this);
    }
}
