# 3. Core Framework Interfaces

**Parent:** [Documentation Index](30d0_StrideUITestFramework_Index.md)  
**Previous:** [Architecture](30d2_Architecture.md)  
**Next:** [Control Objects](30d4_ControlObjects.md)  
**Version:** 1.0 (Proposal - January 2025)

---

## 3.1 Overview

The Stride UI Test Framework reuses the interfaces defined in `Oravey.UITestFramework.Core` and provides Stride-specific implementations. This document describes how these interfaces map to Stride concepts.

---

## 3.2 Platform Enum Extension

Add Stride platform to the existing enum:

```csharp
// In Oravey.UITestFramework.Core.Abstractions.Platform
public enum Platform
{
    Windows,        // WPF desktop using FlaUI
    WindowsMaui,    // MAUI on Windows using Appium
    Android,        // Android using Appium
    iOS,            // iOS using Appium
    Web,            // Web browser using Selenium
    Stride          // NEW: Stride game engine
}

// Extension methods
public static class PlatformExtensions
{
    // ... existing methods ...
    
    public static bool IsGameEngine(this Platform platform)
        => platform == Platform.Stride;
    
    public static bool RequiresGameLoop(this Platform platform)
        => platform == Platform.Stride;
    
    public static string GetAutomationLibrary(this Platform platform)
        => platform switch
        {
            Platform.Windows => "FlaUI",
            Platform.WindowsMaui => "Appium",
            Platform.Android => "Appium",
            Platform.iOS => "Appium",
            Platform.Web => "Selenium",
            Platform.Stride => "Stride.Automation",
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };
}
```

---

## 3.3 StrideTestContext

### 3.3.1 Interface Implementation

```csharp
/// <summary>
/// Test context for Stride game engine UI testing.
/// </summary>
public class StrideTestContext : ITestContext, IDisposable
{
    private readonly IAutomationChannel _channel;
    private readonly StrideInputSimulator _inputSimulator;
    private readonly StrideTestOptions _options;
    
    // ITestContext implementation
    public string TestName { get; set; } = string.Empty;
    public Platform Platform => Platform.Stride;
    public ITestLogger? Logger { get; private set; }
    public int DefaultTimeoutMs { get; }
    public int ShortTimeoutMs { get; }
    public int PollingIntervalMs { get; }
    
    public StrideTestContext(IAutomationChannel channel, StrideTestOptions? options = null)
    {
        _channel = channel;
        _options = options ?? new StrideTestOptions();
        _inputSimulator = new StrideInputSimulator(_options);
        
        DefaultTimeoutMs = _options.DefaultTimeoutMs;
        ShortTimeoutMs = _options.ShortTimeoutMs;
        PollingIntervalMs = _options.PollingIntervalMs;
    }
    
    public void SetLogger(ITestLogger logger) => Logger = logger;
    
    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] [{TestName}] {message}");
    }
    
    public void LogError(Exception ex, string context)
    {
        Log($"ERROR in {context}: {ex.Message}");
        Logger?.LogError(TestName, "", "", context, ex.Message);
    }
    
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
}
```

### 3.3.2 Stride-Specific Methods

```csharp
public class StrideTestContext : ITestContext, IDisposable
{
    // ... ITestContext implementation ...
    
    #region Element Operations
    
    /// <summary>
    /// Query element state from the game.
    /// </summary>
    public async Task<ElementState> GetElementStateAsync(string automationId)
    {
        var response = await _channel.SendCommandAsync(new AutomationCommand
        {
            Type = "Query",
            Target = automationId,
            Method = "GetState"
        });
        
        if (!response.Success)
            return new ElementState { Exists = false };
        
        return JsonSerializer.Deserialize<ElementState>(response.Result.ToString()!)!;
    }
    
    /// <summary>
    /// Synchronous element state query.
    /// </summary>
    public ElementState GetElementState(string automationId)
        => GetElementStateAsync(automationId).GetAwaiter().GetResult();
    
    /// <summary>
    /// Check if element exists.
    /// </summary>
    public bool ElementExists(string automationId)
        => GetElementState(automationId).Exists;
    
    /// <summary>
    /// Check if element is visible.
    /// </summary>
    public bool ElementIsVisible(string automationId)
    {
        var state = GetElementState(automationId);
        return state.Exists && state.IsVisible;
    }
    
    /// <summary>
    /// Check if element is enabled.
    /// </summary>
    public bool ElementIsEnabled(string automationId)
    {
        var state = GetElementState(automationId);
        return state.Exists && state.IsEnabled;
    }
    
    /// <summary>
    /// Get element text content.
    /// </summary>
    public string GetElementText(string automationId)
    {
        var state = GetElementState(automationId);
        return state.Text ?? string.Empty;
    }
    
    /// <summary>
    /// Get element screen bounds for input simulation.
    /// </summary>
    public Rectangle GetElementBounds(string automationId)
    {
        var state = GetElementState(automationId);
        return state.Bounds;
    }
    
    #endregion
    
    #region Input Simulation
    
    /// <summary>
    /// Click at element center.
    /// </summary>
    public void ClickElement(string automationId)
    {
        var bounds = GetElementBounds(automationId);
        if (bounds.IsEmpty)
            throw new InvalidOperationException($"Cannot click element '{automationId}' - not found or has no bounds");
        
        _inputSimulator.Click(bounds.Center());
    }
    
    /// <summary>
    /// Type text using keyboard simulation.
    /// </summary>
    public void TypeText(string text)
    {
        _inputSimulator.TypeText(text);
    }
    
    /// <summary>
    /// Press a specific key.
    /// </summary>
    public void PressKey(VirtualKeyCode key)
    {
        _inputSimulator.PressKey(key);
    }
    
    /// <summary>
    /// Move mouse to position.
    /// </summary>
    public void MoveMouse(Point position)
    {
        _inputSimulator.MoveTo(position);
    }
    
    #endregion
    
    #region Game-Specific Operations
    
    /// <summary>
    /// Wait for game to be fully initialized.
    /// </summary>
    public bool WaitForGameReady(int? timeoutMs = null)
    {
        return WaitFor(
            () => IsGameReady(),
            timeoutMs ?? DefaultTimeoutMs * 2,
            "game ready");
    }
    
    /// <summary>
    /// Check if game is ready for testing.
    /// </summary>
    public bool IsGameReady()
    {
        try
        {
            var response = _channel.SendCommandAsync(new AutomationCommand
            {
                Type = "Query",
                Method = "IsGameReady"
            }).GetAwaiter().GetResult();
            
            return response.Success && (bool)response.Result;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Wait for any loading to complete.
    /// </summary>
    public bool WaitForNotBusy(int? timeoutMs = null)
    {
        return WaitFor(
            () => !IsGameBusy(),
            timeoutMs,
            "not busy");
    }
    
    /// <summary>
    /// Check if game is currently busy (loading, transitioning, etc.).
    /// </summary>
    public bool IsGameBusy()
    {
        try
        {
            var response = _channel.SendCommandAsync(new AutomationCommand
            {
                Type = "Query",
                Method = "IsBusy"
            }).GetAwaiter().GetResult();
            
            return response.Success && (bool)response.Result;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Take a screenshot of the current game window.
    /// </summary>
    public string? TakeScreenshot(string name)
    {
        var response = _channel.SendCommandAsync(new AutomationCommand
        {
            Type = "Action",
            Method = "TakeScreenshot",
            Args = new object[] { name }
        }).GetAwaiter().GetResult();
        
        return response.Success ? response.Result?.ToString() : null;
    }
    
    #endregion
    
    public void Dispose()
    {
        _channel?.Dispose();
    }
}
```

---

## 3.4 Element State Data Transfer

### 3.4.1 ElementState Class

```csharp
/// <summary>
/// Serializable state information for a UI element.
/// </summary>
public class ElementState
{
    public bool Exists { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsHitTestVisible { get; set; }
    public bool IsFocused { get; set; }
    public string? Text { get; set; }
    public string? Name { get; set; }
    public string? AutomationId { get; set; }
    public string? ControlType { get; set; }
    public Rectangle Bounds { get; set; }
    public float Opacity { get; set; }
    
    // Toggle control state
    public bool? IsChecked { get; set; }
    
    // Selector control state
    public int SelectedIndex { get; set; } = -1;
    public string? SelectedText { get; set; }
    public List<string>? Items { get; set; }
    
    // Range control state
    public double? Value { get; set; }
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }
}
```

### 3.4.2 Rectangle Helper

```csharp
/// <summary>
/// Screen rectangle for UI element bounds.
/// </summary>
public struct Rectangle
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    
    public bool IsEmpty => Width == 0 || Height == 0;
    
    public Point Center() => new Point(X + Width / 2, Y + Height / 2);
    
    public bool Contains(Point point)
        => point.X >= X && point.X < X + Width
        && point.Y >= Y && point.Y < Y + Height;
}
```

---

## 3.5 Automation Commands

### 3.5.1 Command Structure

```csharp
/// <summary>
/// Command sent from test to game for automation.
/// </summary>
public class AutomationCommand
{
    /// <summary>
    /// Command type: "Query", "Action", "Wait".
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Target element automation ID (null for global commands).
    /// </summary>
    public string? Target { get; set; }
    
    /// <summary>
    /// Method to invoke.
    /// </summary>
    public string Method { get; set; } = string.Empty;
    
    /// <summary>
    /// Method arguments.
    /// </summary>
    public object[]? Args { get; set; }
    
    /// <summary>
    /// Timeout for wait commands (milliseconds).
    /// </summary>
    public int TimeoutMs { get; set; } = 10000;
}

/// <summary>
/// Response from game to test.
/// </summary>
public class AutomationResponse
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public string? StackTrace { get; set; }
}
```

### 3.5.2 Supported Commands

| Type | Method | Target | Description |
|------|--------|--------|-------------|
| Query | GetState | automationId | Get full element state |
| Query | IsGameReady | - | Check if game initialized |
| Query | IsBusy | - | Check if game loading |
| Query | GetAllElements | - | List all registered elements |
| Query | GetPageName | - | Get current active page |
| Action | Click | automationId | Trigger click on element |
| Action | SetText | automationId | Set text content |
| Action | SetChecked | automationId | Set toggle state |
| Action | SetValue | automationId | Set range value |
| Action | SelectIndex | automationId | Select item by index |
| Action | TakeScreenshot | - | Capture screenshot |
| Action | Exit | - | Close game |
| Wait | WaitVisible | automationId | Wait for visibility |
| Wait | WaitEnabled | automationId | Wait for enabled |
| Wait | WaitText | automationId | Wait for text value |

---

## 3.6 Input Simulator

### 3.6.1 StrideInputSimulator

```csharp
/// <summary>
/// Simulates keyboard and mouse input for Stride game testing.
/// Uses Windows API for reliable input injection.
/// </summary>
public class StrideInputSimulator
{
    private readonly StrideTestOptions _options;
    private readonly InputSimulator _simulator; // From InputSimulatorStandard
    
    public StrideInputSimulator(StrideTestOptions options)
    {
        _options = options;
        _simulator = new InputSimulator();
    }
    
    /// <summary>
    /// Click at screen position.
    /// </summary>
    public void Click(Point position)
    {
        MoveTo(position);
        Thread.Sleep(_options.ClickDelayMs);
        _simulator.Mouse.LeftButtonClick();
        Thread.Sleep(_options.PostClickDelayMs);
    }
    
    /// <summary>
    /// Double-click at screen position.
    /// </summary>
    public void DoubleClick(Point position)
    {
        MoveTo(position);
        Thread.Sleep(_options.ClickDelayMs);
        _simulator.Mouse.LeftButtonDoubleClick();
        Thread.Sleep(_options.PostClickDelayMs);
    }
    
    /// <summary>
    /// Right-click at screen position.
    /// </summary>
    public void RightClick(Point position)
    {
        MoveTo(position);
        Thread.Sleep(_options.ClickDelayMs);
        _simulator.Mouse.RightButtonClick();
        Thread.Sleep(_options.PostClickDelayMs);
    }
    
    /// <summary>
    /// Move mouse to screen position.
    /// </summary>
    public void MoveTo(Point position)
    {
        // Convert to absolute coordinates (0-65535 range)
        var screenWidth = GetSystemMetrics(SM_CXSCREEN);
        var screenHeight = GetSystemMetrics(SM_CYSCREEN);
        
        var absoluteX = (position.X * 65535) / screenWidth;
        var absoluteY = (position.Y * 65535) / screenHeight;
        
        _simulator.Mouse.MoveMouseTo(absoluteX, absoluteY);
    }
    
    /// <summary>
    /// Type text string.
    /// </summary>
    public void TypeText(string text)
    {
        foreach (var c in text)
        {
            _simulator.Keyboard.TextEntry(c);
            Thread.Sleep(_options.KeyPressDelayMs);
        }
    }
    
    /// <summary>
    /// Press a key.
    /// </summary>
    public void PressKey(VirtualKeyCode key)
    {
        _simulator.Keyboard.KeyPress(key);
    }
    
    /// <summary>
    /// Press key combination (e.g., Ctrl+S).
    /// </summary>
    public void PressKeys(params VirtualKeyCode[] keys)
    {
        // Press all modifier keys
        for (int i = 0; i < keys.Length - 1; i++)
        {
            _simulator.Keyboard.KeyDown(keys[i]);
        }
        
        // Press final key
        _simulator.Keyboard.KeyPress(keys[^1]);
        
        // Release modifier keys in reverse
        for (int i = keys.Length - 2; i >= 0; i--)
        {
            _simulator.Keyboard.KeyUp(keys[i]);
        }
    }
    
    /// <summary>
    /// Scroll mouse wheel.
    /// </summary>
    public void Scroll(int clicks)
    {
        _simulator.Mouse.VerticalScroll(clicks);
    }
    
    // Windows API imports for screen metrics
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
}
```

---

## 3.7 Test Options

```csharp
/// <summary>
/// Configuration options for Stride UI tests.
/// </summary>
public class StrideTestOptions
{
    // Timeouts
    public int DefaultTimeoutMs { get; set; } = 10000;
    public int ShortTimeoutMs { get; set; } = 3000;
    public int PollingIntervalMs { get; set; } = 250;
    public int StartupTimeoutMs { get; set; } = 30000;
    public int ConnectionTimeoutMs { get; set; } = 10000;
    
    // Input simulation
    public int ClickDelayMs { get; set; } = 50;
    public int PostClickDelayMs { get; set; } = 100;
    public int KeyPressDelayMs { get; set; } = 20;
    
    // Game configuration
    public string? GameExecutablePath { get; set; }
    public string[] GameArguments { get; set; } = Array.Empty<string>();
    public bool AttachToExisting { get; set; } = false;
    public string PipeName { get; set; } = "Oravey.Automation";
    
    // Screenshots
    public string ScreenshotDirectory { get; set; } = "TestResults/Screenshots";
    public bool CaptureScreenshotOnFailure { get; set; } = true;
    
    // Logging
    public string LogDirectory { get; set; } = "TestResults/Logs";
    public bool EnableCsvLogging { get; set; } = true;
}
```

---

## 3.8 Test Fixture Base

```csharp
/// <summary>
/// Base fixture for Stride UI tests. Manages game lifecycle.
/// </summary>
public class StrideTestFixture : IAsyncLifetime
{
    private StrideGameDriver? _gameDriver;
    
    public StrideTestContext Context { get; private set; } = null!;
    public StrideTestOptions Options { get; }
    
    public StrideTestFixture()
    {
        Options = new StrideTestOptions();
    }
    
    public async Task InitializeAsync()
    {
        _gameDriver = new StrideGameDriver();
        await _gameDriver.StartGameAsync(Options);
        
        Context = new StrideTestContext(_gameDriver.Channel, Options);
        
        // Wait for game to be ready
        if (!Context.WaitForGameReady(Options.StartupTimeoutMs))
        {
            throw new InvalidOperationException("Game did not become ready in time");
        }
    }
    
    public async Task DisposeAsync()
    {
        Context?.Dispose();
        
        if (_gameDriver != null)
        {
            await _gameDriver.StopGameAsync();
            _gameDriver.Dispose();
        }
    }
}

/// <summary>
/// xUnit collection for sharing game instance across tests.
/// </summary>
[CollectionDefinition("Stride UI Tests")]
public class StrideTestCollection : ICollectionFixture<StrideTestFixture>
{
}
```

---

*Document Version: 1.0*  
*Last Updated: January 2025*
