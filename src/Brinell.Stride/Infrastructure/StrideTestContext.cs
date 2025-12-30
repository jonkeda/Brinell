using System.Diagnostics;
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
            return response.Success ? response.Result?.ToString() : null;
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
    /// Get element bounds.
    /// </summary>
    public ElementBounds GetElementBounds(string automationId)
        => GetElementState(automationId).Bounds;

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

        _inputSimulator.Click(bounds.CenterX, bounds.CenterY);
    }

    /// <summary>
    /// Type text using keyboard simulation.
    /// </summary>
    public void TypeText(string text)
        => _inputSimulator.TypeText(text);

    /// <summary>
    /// Press a key.
    /// </summary>
    public void PressKey(VirtualKey key)
        => _inputSimulator.PressKey(key);

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
