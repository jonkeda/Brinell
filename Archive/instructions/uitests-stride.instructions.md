````instructions
---
applyTo: "**/UITests/**/*.cs"
description: "Brinell Stride 3D game engine UI testing framework guidelines"
---

# Brinell Stride UI Testing Guidelines

## Framework Overview
- Use Brinell.Stride for Stride 3D game engine automation
- Base class for tests: `StrideTestBase`
- Base class for page objects: `StridePageBase`
- Test context: `StrideTestContext`
- Communication: Named pipes with `NamedPipeChannel`
- In-game automation: `Brinell.Stride.Automation` (must be added to game)

## Architecture
Unlike WPF/MAUI, Stride renders to GPU without OS accessibility APIs. Testing uses:
1. **Named Pipe IPC** - Communication between test runner and game
2. **In-Game Hooks** - `Brinell.Stride.Automation` exposes UI element state
3. **Win32 Input** - OS-level keyboard/mouse simulation

## Page Object Structure
```csharp
using Brinell.Stride.Controls;
using Brinell.Stride.Pages;
using Brinell.Stride.Infrastructure;

public class MainMenuPage : StridePageBase
{
    // Controls - initialized in constructor
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
    
    // Navigation methods
    public void NavigateToSettings()
    {
        Log("NavigateToSettings()");
        SettingsButton.Click();
    }
    
    public void StartGame()
    {
        Log("StartGame()");
        StartButton.Click();
    }
}
```

## Test Class Structure
```csharp
using Brinell.Stride.Communication;
using Brinell.Stride.Infrastructure;
using Brinell.Stride.Testing;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

[Collection("StrideUITests")]
public class MainMenuTests : StrideTestBase, IAsyncLifetime
{
    protected StrideTestContext Context { get; private set; } = null!;
    protected StrideGameDriver GameDriver { get; private set; } = null!;
    
    protected virtual string GameExecutablePath => 
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", 
            "MyGame", "bin", "Debug", "net8.0-windows", "MyGame.exe");
    
    public async Task InitializeAsync()
    {
        GameDriver = new StrideGameDriver(new StrideTestOptions
        {
            ExecutablePath = GameExecutablePath,
            PipeName = "MyGame.Automation",
            StartupTimeoutMs = 10000
        });
        
        await GameDriver.StartAsync();
        
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
}
```

## Stride Control Types
- `StrideButtonControl` - Buttons
- `StrideTextBlockControl` - Labels/TextBlocks
- `StrideEditTextControl` - Text inputs (EditText)
- `StrideCheckBoxControl` - CheckBoxes
- `StrideToggleButtonControl` - Toggle buttons
- `StrideSliderControl` - Sliders
- `StrideListBoxControl` - ListBoxes

## Control Methods

### State Checks
```csharp
bool visible = control.IsVisible();
bool enabled = control.IsEnabled();
bool exists = control.IsExists();
bool clickable = control.IsClickable();
```

### Wait Methods
```csharp
bool appeared = control.WaitVisible(expected: true, timeoutMs: 5000);
bool enabled = control.WaitEnabled(expected: true);
bool clickable = control.WaitClickable();
```

### Check Methods (throw on failure)
```csharp
control.CheckVisible();   // Throws if not visible
control.CheckEnabled();   // Throws if disabled
control.CheckClickable(); // Throws if not clickable
```

### Assert Methods (test assertions with logging)
```csharp
control.AssertVisible("Button should be visible");
control.AssertEnabled("Button should be enabled");
control.AssertText("Expected Value", "Label text mismatch");
control.AssertTextContains("partial", "Should contain text");
```

## Input Simulation
```csharp
// Keyboard
Context.InputSimulator.KeyPress(VirtualKey.ESCAPE);
Context.InputSimulator.KeyPress(VirtualKey.RETURN);
Context.InputSimulator.HotKey(VirtualKey.S, VirtualKey.CONTROL); // Ctrl+S
Context.InputSimulator.TypeText("Hello World");

// Mouse
Context.InputSimulator.Click(100, 200);
Context.InputSimulator.DoubleClick(100, 200);
Context.InputSimulator.RightClick(100, 200);
Context.InputSimulator.MoveTo(100, 200);
```

## Automation ID Convention
Use hierarchical, descriptive IDs:
```csharp
// Good
element.SetAutomationId("MainMenu.StartButton");
element.SetAutomationId("Settings.Audio.VolumeSlider");
element.SetAutomationId("Gameplay.Inventory.SlotGrid");

// Avoid
element.SetAutomationId("btn1");
element.SetAutomationId("slider");
```

## Game Setup (In Game Project)
Add automation hooks to your game:

### 1. Add Package Reference
```xml
<PackageReference Include="Brinell.Stride.Automation" 
                  Condition="'$(Configuration)' == 'Debug'" />
```

### 2. Register Automation Service
```csharp
using Brinell.Stride.Automation;

public class MyGame : Game
{
    protected override void BeginRun()
    {
        base.BeginRun();
        
#if AUTOMATION_ENABLED
        GameSystems.Add(new AutomationGameSystem(Services));
#endif
    }
}
```

### 3. Register UI Elements
```csharp
using Brinell.Stride.Automation;

public class MainMenuUI
{
    public void Initialize(UIPage page)
    {
        var startButton = page.RootElement.FindName("StartButton") as Button;
        
#if AUTOMATION_ENABLED
        startButton?.SetAutomationId("MainMenu.StartButton");
#endif
    }
}
```

## Test Categories
```csharp
[Trait("Category", "Smoke")]        // Critical path tests
[Trait("Category", "UI.MainMenu")]  // Main menu tests
[Trait("Category", "Navigation")]   // Screen navigation
[Trait("Category", "Gameplay")]     // In-game tests
[Trait("Category", "Settings")]     // Settings tests
```

## Best Practices
- Use conditional compilation `#if AUTOMATION_ENABLED` for automation code
- Controls are instantiated in constructor, not as properties with factory methods
- Use `Log()` method to record actions for debugging
- Always `WaitForDisplayed()` after navigation before interacting
- Keep tests focused on single behaviors
- Tests should be independent and not rely on order
- Use test collections to prevent parallel execution issues
- Use hierarchical automation IDs (e.g., "MainMenu.StartButton")

## Navigation Pattern
```csharp
// Void return - test creates target page object
public void NavigateToSettings()
{
    Log("NavigateToSettings()");
    SettingsButton.Click();
}

// Usage in test
mainMenu.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.WaitForDisplayed();
```

## Comparison with WPF
| Aspect | Stride | WPF |
|--------|--------|-----|
| Element Access | Named Pipe IPC | UI Automation API |
| Input Simulation | Win32 SendInput | UI Automation |
| Game Integration | Required (Automation hooks) | None |
| Conditional Build | Recommended | Not needed |
| Context Class | `StrideTestContext` | `FlaUITestContext` |
| Page Base | `StridePageBase` | `PageBase` |

````
