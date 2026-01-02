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
            SetForegroundWindow(_gameWindowHandle);
            Thread.Sleep(50); // Give window time to receive focus

            // Verify focus was set
            currentForeground = GetForegroundWindow();
            if (currentForeground == _gameWindowHandle)
            {
                Log("Game window focus set successfully");
                return true;
            }

            Thread.Sleep(100); // Wait before retry
        }

        Log($"Failed to set game window focus after {timeoutMs}ms");
        return false;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

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
    /// Press a key.
    /// </summary>
    public void PressKey(VirtualKey key)
    {
        if (!EnsureGameHasFocus())
        {
            Log("Warning: Game may not have focus, key press might go to wrong window");
        }
        _inputSimulator.PressKey(key);
    }

    /// <summary>
    /// Hold a key for a duration.
    /// </summary>
    public void HoldKey(VirtualKey key, int durationMs)
        => _inputSimulator.HoldKey(key, durationMs);

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
