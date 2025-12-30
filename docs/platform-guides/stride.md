````markdown
# Stride 3D Engine Testing Guide

## Overview

Brinell.Stride provides UI testing support for Stride 3D game engine applications. Unlike traditional UI frameworks (WPF, MAUI, HTML), Stride renders directly to GPU without OS-level accessibility APIs. This requires a hybrid approach:

1. **In-Game Automation Layer** - Hooks inside the game process exposing UI element state
2. **Named Pipe Communication** - IPC between test runner and game process
3. **Input Simulation** - OS-level keyboard/mouse simulation via Windows API

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Test Process (xUnit)                           │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Your.Game.UITests                                                  │ │
│  │   └─ PageObjects/ (MainMenuPage, SettingsPage, etc.)              │ │
│  │   └─ Tests/ (MainMenuTests, NavigationTests, etc.)                │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│         │ references                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Brinell.Stride                                                     │ │
│  │   └─ Infrastructure/ (StrideTestContext, StrideGameDriver)         │ │
│  │   └─ Controls/ (StrideButtonControl, StrideTextBoxControl, etc.)   │ │
│  │   └─ Communication/ (NamedPipeChannel, AutomationCommands)         │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│         │ references                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Brinell.Core (interfaces)                                          │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                              │ Named Pipe IPC
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    Game Process (Stride)                                 │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Your.Game                                                          │ │
│  │   └─ UI/ (MainMenuUI, SettingsUI, etc.)                           │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│         │ references                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Brinell.Stride.Automation                                          │ │
│  │   └─ AutomationService (element registry)                          │ │
│  │   └─ AutomationServer (pipe server)                                │ │
│  │   └─ StrideUIHandler (element queries)                             │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

## Installation

### 1. Test Project

Add Brinell.Stride to your test project:

```xml
<PackageReference Include="Brinell.Stride" />
<PackageReference Include="xunit" />
<PackageReference Include="FluentAssertions" />
```

### 2. Game Project

Add Brinell.Stride.Automation to your game project:

```xml
<PackageReference Include="Brinell.Stride.Automation" />
```

Use conditional compilation to exclude automation in release builds:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DefineConstants>$(DefineConstants);AUTOMATION_ENABLED</DefineConstants>
</PropertyGroup>
```

## Game Setup

### 1. Register Automation Service

In your game's initialization, add the automation service:

```csharp
using Brinell.Stride.Automation;

public class MyGame : Game
{
    protected override void BeginRun()
    {
        base.BeginRun();
        
#if AUTOMATION_ENABLED
        // Add automation hooks
        GameSystems.Add(new AutomationGameSystem(Services));
#endif
    }
}
```

### 2. Assign Automation IDs

All testable UI elements need automation IDs:

```csharp
using Brinell.Stride.Automation;

public class MainMenuUI
{
    public Button StartButton { get; private set; }
    public Button SettingsButton { get; private set; }
    public Button ExitButton { get; private set; }
    
    public void Initialize(UIPage page)
    {
        StartButton = page.RootElement.FindName("StartButton") as Button;
        SettingsButton = page.RootElement.FindName("SettingsButton") as Button;
        ExitButton = page.RootElement.FindName("ExitButton") as Button;
        
#if AUTOMATION_ENABLED
        // Register elements with automation IDs
        StartButton?.SetAutomationId("MainMenu.StartButton");
        SettingsButton?.SetAutomationId("MainMenu.SettingsButton");
        ExitButton?.SetAutomationId("MainMenu.ExitButton");
#endif
    }
}
```

## Test Setup

### 1. Create Test Base Class

```csharp
using Brinell.Stride.Communication;
using Brinell.Stride.Infrastructure;
using Brinell.Stride.Testing;
using Xunit;

public abstract class MyGameTestBase : StrideTestBase, IAsyncLifetime
{
    protected StrideTestContext Context { get; private set; } = null!;
    protected StrideGameDriver GameDriver { get; private set; } = null!;
    
    protected virtual string GameExecutablePath => 
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", 
            "MyGame", "bin", "Debug", "net8.0-windows", "MyGame.exe");
    
    public async Task InitializeAsync()
    {
        // Start the game
        GameDriver = new StrideGameDriver(new StrideTestOptions
        {
            ExecutablePath = GameExecutablePath,
            PipeName = "MyGame.Automation",
            StartupTimeoutMs = 10000
        });
        
        await GameDriver.StartAsync();
        
        // Connect to automation channel
        var channel = new NamedPipeChannel("MyGame.Automation");
        await channel.ConnectAsync(TimeSpan.FromSeconds(5));
        
        Context = new StrideTestContext(channel, new StrideTestOptions
        {
            DefaultTimeoutMs = 5000,
            PollingIntervalMs = 100
        });
    }
    
    public async Task DisposeAsync()
    {
        await GameDriver.StopAsync();
    }
}
```

### 2. Create Page Objects

```csharp
using Brinell.Stride.Controls;
using Brinell.Stride.Pages;
using Brinell.Stride.Infrastructure;

public class MainMenuPage : StridePageBase
{
    public StrideButtonControl StartButton { get; }
    public StrideButtonControl SettingsButton { get; }
    public StrideButtonControl ExitButton { get; }
    
    public MainMenuPage(StrideTestContext context) 
        : base(context, "MainMenu")
    {
        StartButton = new StrideButtonControl(context, this, "MainMenu.StartButton");
        SettingsButton = new StrideButtonControl(context, this, "MainMenu.SettingsButton");
        ExitButton = new StrideButtonControl(context, this, "MainMenu.ExitButton");
    }
    
    public override string AutomationId => "MainMenu.Page";
    
    public void NavigateToSettings()
    {
        SettingsButton.Click();
    }
    
    public void StartGame()
    {
        StartButton.Click();
    }
}
```

### 3. Write Tests

```csharp
public class MainMenuTests : MyGameTestBase
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void MainMenu_WhenGameStarts_IsDisplayed()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        
        // Assert
        mainMenu.AssertDisplayed();
        mainMenu.StartButton.AssertVisible();
        mainMenu.SettingsButton.AssertVisible();
        mainMenu.ExitButton.AssertVisible();
    }
    
    [Fact]
    [Trait("Category", "Navigation")]
    public void MainMenu_ClickSettings_OpensSettingsPage()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForDisplayed();
        
        // Act
        mainMenu.NavigateToSettings();
        
        // Assert
        var settings = new SettingsPage(Context);
        settings.AssertDisplayed();
    }
    
    [Fact]
    [Trait("Category", "Navigation")]
    public void MainMenu_ClickStart_BeginsGameplay()
    {
        // Arrange
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForDisplayed();
        
        // Act
        mainMenu.StartGame();
        
        // Assert
        var gameplay = new GameplayPage(Context);
        gameplay.AssertDisplayed();
    }
}
```

## Available Controls

| Control | Stride Elements | Key Features |
|---------|-----------------|--------------|
| `StrideButtonControl` | `Button` | Click, WaitClickable, AssertEnabled |
| `StrideTextBlockControl` | `TextBlock` | GetText, AssertText, AssertTextContains |
| `StrideEditTextControl` | `EditText` | EnterText, ClearText, GetValue |
| `StrideCheckBoxControl` | `CheckBox` | Check, Uncheck, Toggle, IsChecked |
| `StrideSliderControl` | `Slider` | GetValue, SetValue, Increment, Decrement |
| `StrideListBoxControl` | `ListBox` | SelectItem, GetSelectedIndex, GetItems |
| `StrideToggleButtonControl` | `ToggleButton` | Toggle, IsOn, TurnOn, TurnOff |

## Control Patterns

All controls follow the **Is/Wait/Check/Assert** pattern:

### Is Methods (Immediate state check)
```csharp
bool visible = button.IsVisible();
bool enabled = button.IsEnabled();
bool checked = checkBox.IsChecked();
```

### Wait Methods (Poll until condition or timeout)
```csharp
bool appeared = button.WaitVisible(expected: true, timeoutMs: 5000);
bool enabledNow = button.WaitEnabled(expected: true);
```

### Check Methods (Throw if not met)
```csharp
button.CheckVisible();   // Throws CheckFailedException if not visible
button.CheckClickable(); // Throws if not visible AND enabled
```

### Assert Methods (Test assertions with logging)
```csharp
button.AssertVisible("Button should be visible after dialog opens");
label.AssertText("Expected Text");
slider.AssertValue(50, tolerance: 0.5);
```

## Input Simulation

### Keyboard Input

```csharp
// Single key press
Context.InputSimulator.KeyPress(VirtualKey.ESCAPE);
Context.InputSimulator.KeyPress(VirtualKey.RETURN);

// Key combination
Context.InputSimulator.HotKey(VirtualKey.S, VirtualKey.CONTROL); // Ctrl+S

// Multiple modifiers
Context.InputSimulator.HotKey(VirtualKey.S, VirtualKey.CONTROL, VirtualKey.SHIFT); // Ctrl+Shift+S

// Type text
Context.InputSimulator.TypeText("Hello World");
```

### Mouse Input

```csharp
// Click at coordinates
Context.InputSimulator.Click(100, 200);

// Double-click
Context.InputSimulator.DoubleClick(100, 200);

// Right-click
Context.InputSimulator.RightClick(100, 200);

// Move mouse
Context.InputSimulator.MoveTo(100, 200);
```

### Control-Level Input

Controls calculate their screen position automatically:

```csharp
button.Click();           // Clicks center of button
textBox.EnterText("Hi");  // Focuses and types
slider.DragTo(75);        // Drags slider handle
```

## Named Pipe Protocol

The framework uses a simple JSON protocol over named pipes.

### Command Structure

```csharp
public class AutomationCommand
{
    public string Type { get; set; }      // "Query", "Action", "Wait"
    public string? Target { get; set; }   // AutomationId
    public string Method { get; set; }    // "IsVisible", "Click", etc.
    public object[]? Args { get; set; }   // Method arguments
    public int TimeoutMs { get; set; }    // Timeout for waits
}
```

### Response Structure

```csharp
public class AutomationResponse
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
}
```

### Custom Queries

Extend the automation handler for game-specific queries:

```csharp
// In game project
public class MyGameUIHandler : StrideUIHandler
{
    public MyGameUIHandler(IServiceRegistry services) : base(services) { }
    
    public override AutomationResponse HandleQuery(AutomationCommand command)
    {
        switch (command.Method)
        {
            case "GetPlayerHealth":
                var health = GetPlayerHealth();
                return AutomationResponse.Ok(health);
                
            case "GetCurrentArea":
                var area = GetCurrentAreaName();
                return AutomationResponse.Ok(area);
                
            default:
                return base.HandleQuery(command);
        }
    }
}
```

## Best Practices

### 1. Use Descriptive Automation IDs

```csharp
// Good - Hierarchical, clear
button.SetAutomationId("MainMenu.StartButton");
button.SetAutomationId("Settings.Audio.VolumeSlider");

// Avoid - Ambiguous, non-descriptive
button.SetAutomationId("btn1");
button.SetAutomationId("slider");
```

### 2. Conditional Compilation

Only include automation code in debug/test builds:

```csharp
#if AUTOMATION_ENABLED
    element.SetAutomationId("MyElement");
    GameSystems.Add(new AutomationGameSystem(Services));
#endif
```

### 3. Wait for Page Ready

Always wait for UI to stabilize before interacting:

```csharp
var settings = new SettingsPage(Context);
settings.WaitForDisplayed();  // Don't skip this
settings.VolumeSlider.SetValue(80);
```

### 4. Use Page Objects

Encapsulate UI structure and navigation:

```csharp
// Good - Encapsulated
var mainMenu = new MainMenuPage(Context);
mainMenu.NavigateToSettings();

// Avoid - Directly finding elements in tests
var button = Context.FindControl("Settings.BackButton");
button.Click();
```

### 5. Test Categories

Organize tests with traits:

```csharp
[Trait("Category", "Smoke")]      // Critical path tests
[Trait("Category", "UI.MainMenu")] // Main menu tests
[Trait("Category", "Navigation")]  // Screen navigation
[Trait("Category", "Gameplay")]    // In-game tests
```

## Troubleshooting

### Connection Failed

```
Error: Could not connect to named pipe
```

- Verify game is running with automation enabled
- Check pipe name matches in both game and tests
- Ensure game fully initialized before connecting

### Element Not Found

- Verify automation ID is registered with `SetAutomationId()`
- Check ID spelling matches exactly
- Ensure element exists in current UI state
- Use `WaitExists()` for dynamic elements

### Input Not Working

- Verify game window has focus
- Some games capture input differently - adjust timing
- Check if game is running in fullscreen/borderless mode

### Timing Issues

- Increase timeouts for slow operations
- Use `WaitFor()` instead of `Thread.Sleep()`
- Add explicit waits after screen transitions

### Debug Mode

Enable detailed logging:

```csharp
var options = new StrideTestOptions
{
    EnableDetailedLogging = true,
    LogFilePath = "stride-tests.log"
};
```

## Comparison with Other Platforms

| Feature | Stride | WPF (FlaUI) | Web (Playwright) |
|---------|--------|-------------|------------------|
| **Element Access** | Named Pipe IPC | UI Automation API | CDP/WebDriver |
| **Input Method** | Win32 SendInput | UI Automation | Browser API |
| **Game Integration** | Required (Brinell.Stride.Automation) | None | None |
| **Conditional Build** | Recommended | Not needed | Not needed |
| **Performance** | Fast (in-process queries) | Fast | Moderate |

## Known Limitations

1. **Windows Only** - Input simulation uses Win32 APIs
2. **Requires Game Modification** - Must add Brinell.Stride.Automation to game
3. **No Visual Validation** - Screenshot comparison not yet implemented
4. **Single Game Instance** - Tests run against one game process at a time

## Next Steps

- **[Framework Overview](../02-framework-overview.md)** - Core concepts
- **[Control Objects](../04-control-objects.md)** - Master control patterns
- **[Page Objects](../05-page-objects.md)** - Learn page encapsulation
- **[Best Practices](../12-best-practices.md)** - Testing guidelines

````
