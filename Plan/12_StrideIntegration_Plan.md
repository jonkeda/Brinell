# 
    Plan 12: Stride 3D Engine UI Testing Integration

**Status:** ✅ Complete
**Date:** January 2025
**Depends On:** Plan 08 (Playwright Infrastructure)
**Framework:** Brinell UI Test Framework
**Implementation Date:** December 2025

### Completion Summary

| Phase                              | Status | Description                                           |
| ---------------------------------- | ------ | ----------------------------------------------------- |
| Phase 1: Core Infrastructure       | ✅     | Named pipes, input simulation, game driver            |
| Phase 2: In-Game Automation        | ✅     | AutomationService, StrideUIHandler, element registry  |
| Phase 3: Control Objects           | ✅     | All Stride controls with Wait/Check/Is/Assert pattern |
| Phase 4: Page Objects & Sample App | ✅     | StridePageBase, sample game with menus                |
| Phase 5: Test Suite                | ✅     | Test infrastructure and sample tests                  |
| Phase 6: Documentation             | ✅     | Platform guide, Copilot instructions, README updates  |

---

## 1. Executive Summary

This plan integrates Stride 3D game engine UI testing into the Brinell framework. Unlike traditional UI frameworks (WPF, MAUI, HTML), Stride renders directly to GPU without OS-level accessibility APIs. This requires a hybrid approach combining:

1. **Internal Automation Layer** - In-game hooks exposing UI element state
2. **Input Simulation** - OS-level keyboard/mouse simulation for user actions
3. **Named Pipe Communication** - Inter-process communication between test runner and game

### Key Deliverables

| Deliverable                        | Description                                  |
| ---------------------------------- | -------------------------------------------- |
| `Brinell.Stride`                 | Core framework library for Stride UI testing |
| `Brinell.Stride.Automation`      | In-game automation hooks package             |
| `Brinell.Samples.Stride.App`     | Sample Stride application with testable UI   |
| `Brinell.Samples.Stride.UITests` | Sample tests demonstrating the framework     |

---

## 2. Architecture Overview

### 2.1 Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Test Process (xUnit)                           │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Brinell.Samples.Stride.UITests                                    │ │
│  │   └─ PageObjects/ (MainMenuPage, SettingsPage, etc.)             │ │
│  │   └─ Tests/ (MainMenuTests, NavigationTests, etc.)               │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│         │ references                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Brinell.Stride                                                    │ │
│  │   └─ Infrastructure/ (StrideTestContext, StrideGameDriver)        │ │
│  │   └─ Controls/ (StrideButtonControl, StrideTextBoxControl, etc.)  │ │
│  │   └─ Communication/ (NamedPipeChannel, AutomationCommands)        │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│         │ references                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Brinell.Core (existing)                                           │ │
│  │   └─ ITestContext, IPageObject, IControlObject, IButton, etc.    │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                              │ Named Pipe
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    Game Process (Stride)                                 │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Brinell.Samples.Stride.App                                        │ │
│  │   └─ UI/ (MainMenuUI, SettingsUI, etc.)                           │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│         │ references                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ Brinell.Stride.Automation (conditionally compiled)                │ │
│  │   └─ AutomationService (element registry)                         │ │
│  │   └─ AutomationHost (pipe server)                                 │ │
│  │   └─ UIElementProvider (state queries)                            │ │
│  │   └─ AutomationExtensions (SetAutomationId)                       │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Project Structure

```
Brinell/
├── src/
│   ├── Brinell.Core/                    # Existing - shared interfaces
│   ├── Brinell.Stride/                  # NEW - Stride test framework
│   │   ├── Brinell.Stride.csproj
│   │   ├── Infrastructure/
│   │   │   ├── StrideTestContext.cs
│   │   │   ├── StrideGameDriver.cs
│   │   │   ├── StrideInputSimulator.cs
│   │   │   └── StrideTestOptions.cs
│   │   ├── Controls/
│   │   │   ├── Base/
│   │   │   │   ├── StrideControlBase.cs
│   │   │   │   ├── StridePageBase.cs
│   │   │   │   ├── StrideContentControlBase.cs
│   │   │   │   └── StrideTextControlBase.cs
│   │   │   ├── StrideButtonControl.cs
│   │   │   ├── StrideTextBlockControl.cs
│   │   │   ├── StrideEditTextControl.cs
│   │   │   ├── StrideCheckBoxControl.cs
│   │   │   ├── StrideSliderControl.cs
│   │   │   └── StrideListBoxControl.cs
│   │   ├── Communication/
│   │   │   ├── IAutomationChannel.cs
│   │   │   ├── NamedPipeChannel.cs
│   │   │   ├── AutomationCommand.cs
│   │   │   └── AutomationResponse.cs
│   │   └── Testing/
│   │       ├── StrideTestFixture.cs
│   │       └── StrideTestBase.cs
│   │
│   └── Brinell.Stride.Automation/       # NEW - In-game automation hooks
│       ├── Brinell.Stride.Automation.csproj
│       ├── AutomationService.cs
│       ├── AutomationHost.cs
│       ├── UIElementProvider.cs
│       ├── AutomationExtensions.cs
│       ├── ElementState.cs
│       └── Commands/
│           ├── QueryCommand.cs
│           ├── ActionCommand.cs
│           └── WaitCommand.cs
│
├── samples/
│   ├── Brinell.Samples.Stride.App/      # NEW - Sample Stride game
│   │   ├── Brinell.Samples.Stride.App.csproj
│   │   ├── Program.cs
│   │   ├── SampleGame.cs
│   │   └── UI/
│   │       ├── MainMenuUI.cs
│   │       ├── SettingsUI.cs
│   │       └── GameplayUI.cs
│   │
│   └── Brinell.Samples.Stride.UITests/  # NEW - Sample tests
│       ├── Brinell.Samples.Stride.UITests.csproj
│       ├── PageObjects/
│       │   ├── MainMenuPage.cs
│       │   ├── SettingsPage.cs
│       │   └── GameplayPage.cs
│       ├── Tests/
│       │   ├── MainMenuTests.cs
│       │   ├── NavigationTests.cs
│       │   └── SettingsTests.cs
│       └── Fixtures/
│           └── StrideGameFixture.cs
│
└── docs/
    └── 05-stride-testing.md             # NEW - Stride platform guide
```

---

## 3. Platform Enum Update

Add Stride to the existing Platform enum in `Brinell.Core`:

```csharp
// In Brinell.Core/Abstractions/ITestContext.cs
public enum Platform
{
    Windows,        // WPF desktop using FlaUI
    WindowsMaui,    // MAUI on Windows using Appium
    Android,        // Android using Appium
    iOS,            // iOS using Appium
    Web,            // Web browser using Selenium/Playwright
    Stride          // NEW: Stride game engine
}
```

---

## 4. Implementation Phases

### Phase 1: Core Infrastructure (Week 1-2)

#### 4.1.1 Goals

- Create `Brinell.Stride` project structure
- Implement communication channel
- Implement input simulation
- Create game lifecycle management

#### 4.1.2 Deliverables

| File                        | Description               | Priority |
| --------------------------- | ------------------------- | -------- |
| `Brinell.Stride.csproj`   | Project with dependencies | High     |
| `IAutomationChannel.cs`   | Channel abstraction       | High     |
| `NamedPipeChannel.cs`     | Named pipe implementation | High     |
| `AutomationCommand.cs`    | Command serialization     | High     |
| `AutomationResponse.cs`   | Response serialization    | High     |
| `StrideInputSimulator.cs` | Keyboard/mouse simulation | High     |
| `StrideTestOptions.cs`    | Configuration class       | High     |
| `StrideGameDriver.cs`     | Game lifecycle management | High     |

#### 4.1.3 Acceptance Criteria

- [X] Can connect to a running game via named pipe
- [X] Can send commands and receive JSON responses
- [X] Can simulate mouse clicks at screen coordinates
- [X] Can simulate keyboard input
- [X] Can start and stop game process

---

### Phase 2: In-Game Automation Hooks (Week 2-3)

#### 4.2.1 Goals

- Create `Brinell.Stride.Automation` package
- Implement UI element registry
- Implement command processing
- Support conditional compilation

#### 4.2.2 Deliverables

| File                                 | Description                    | Priority |
| ------------------------------------ | ------------------------------ | -------- |
| `Brinell.Stride.Automation.csproj` | Project with Stride dependency | High     |
| `AutomationService.cs`             | Element registry               | High     |
| `AutomationHost.cs`                | Named pipe server              | High     |
| `UIElementProvider.cs`             | Element state queries          | High     |
| `AutomationExtensions.cs`          | SetAutomationId helper         | High     |
| `ElementState.cs`                  | State DTO                      | High     |

#### 4.2.3 Acceptance Criteria

- [X] Game can start with automation enabled
- [X] Game accepts automation connections
- [X] Can query element state via named pipe
- [X] Element registration works with Stride UI
- [X] Commands execute on game thread

---

### Phase 3: Control Objects (Week 3-4)

#### 4.3.1 Goals

- Implement `StrideTestContext`
- Implement all control wrappers
- Follow Wait/Check/Is/Assert pattern

#### 4.3.2 Deliverables

| File                            | Description                 | Priority |
| ------------------------------- | --------------------------- | -------- |
| `StrideTestContext.cs`        | ITestContext implementation | High     |
| `StrideControlBase.cs`        | Base control class          | High     |
| `StrideContentControlBase.cs` | Clickable controls base     | High     |
| `StrideTextControlBase.cs`    | Text controls base          | High     |
| `StrideButtonControl.cs`      | Button wrapper              | High     |
| `StrideTextBlockControl.cs`   | TextBlock wrapper           | High     |
| `StrideEditTextControl.cs`    | EditText wrapper            | High     |
| `StrideCheckBoxControl.cs`    | CheckBox wrapper            | Medium   |
| `StrideSliderControl.cs`      | Slider wrapper              | Medium   |
| `StrideListBoxControl.cs`     | ListBox wrapper             | Medium   |

#### 4.3.3 Control Type Mapping

| Stride UI Class  | Control Wrapper               | Interface            |
| ---------------- | ----------------------------- | -------------------- |
| `Button`       | `StrideButtonControl`       | `IButton`          |
| `TextBlock`    | `StrideTextBlockControl`    | `ILabel`           |
| `EditText`     | `StrideEditTextControl`     | `ITextBox`         |
| `CheckBox`     | `StrideCheckBoxControl`     | `ICheckBox`        |
| `ToggleButton` | `StrideToggleButtonControl` | `IToggleControl`   |
| `Slider`       | `StrideSliderControl`       | `IRangeControl`    |
| `ListBox`      | `StrideListBoxControl`      | `ISelectorControl` |

#### 4.3.4 Acceptance Criteria

- [X] All controls implement appropriate interfaces
- [X] Wait/Check/Is/Assert pattern works correctly
- [X] Input simulation clicks buttons correctly
- [X] Text input works with EditText

---

### Phase 4: Page Objects & Sample App (Week 4-5)

#### 4.4.1 Goals

- Create `StridePageBase`
- Create sample Stride application
- Create sample page objects

#### 4.4.2 Sample App UI Screens

| Screen              | Class           | Controls                                      |
| ------------------- | --------------- | --------------------------------------------- |
| **Main Menu** | `MainMenuUI`  | Start, Settings, Exit buttons                 |
| **Settings**  | `SettingsUI`  | Volume slider, Fullscreen toggle, Back button |
| **Gameplay**  | `GameplayUI`  | Pause button, Score label                     |
| **Pause**     | `PauseMenuUI` | Resume, Settings, Main Menu buttons           |

#### 4.4.3 Deliverables

| File                  | Description     | Priority |
| --------------------- | --------------- | -------- |
| `StridePageBase.cs` | Base page class | High     |
| `SampleGame.cs`     | Main game class | High     |
| `MainMenuUI.cs`     | Main menu UI    | High     |
| `SettingsUI.cs`     | Settings UI     | High     |
| `GameplayUI.cs`     | In-game HUD     | Medium   |
| `MainMenuPage.cs`   | Page object     | High     |
| `SettingsPage.cs`   | Page object     | High     |
| `GameplayPage.cs`   | Page object     | Medium   |

#### 4.4.4 Acceptance Criteria

- [X] Sample app runs and displays main menu
- [X] All UI elements have automation IDs
- [X] Page objects correctly detect display state
- [X] Navigation between screens works

---

### Phase 5: Test Suite (Week 5-6)

#### 4.5.1 Goals

- Create test infrastructure
- Implement smoke tests
- Implement navigation tests
- Document patterns

#### 4.5.2 Deliverables

| File                     | Description            | Priority |
| ------------------------ | ---------------------- | -------- |
| `StrideGameFixture.cs` | Game lifecycle fixture | High     |
| `StrideTestBase.cs`    | Base test class        | High     |
| `MainMenuTests.cs`     | Main menu tests        | High     |
| `NavigationTests.cs`   | Navigation tests       | High     |
| `SettingsTests.cs`     | Settings tests         | Medium   |

#### 4.5.3 Test Categories

| Category          | Description                      |
| ----------------- | -------------------------------- |
| `Smoke`         | Critical path tests (always run) |
| `UI.MainMenu`   | Main menu functionality          |
| `UI.Settings`   | Settings functionality           |
| `UI.Navigation` | Screen navigation                |
| `UI.Gameplay`   | In-game UI                       |

#### 4.5.4 Acceptance Criteria

- [X] All smoke tests pass
- [X] Tests run reliably in isolation
- [X] Tests clean up after themselves
- [X] CSV logging works correctly

---

### Phase 6: Documentation (Week 6)

#### 4.6.1 Deliverables

| File                                            | Description                 |
| ----------------------------------------------- | --------------------------- |
| `docs/05-stride-testing.md`                   | Stride platform guide       |
| `.github/instructions/stride.instructions.md` | Copilot instructions        |
| Updated README                                  | Stride platform in overview |
| Updated framework-overview                      | Architecture diagram        |

---

## 5. Technical Specifications

### 5.1 Named Pipe Protocol

#### 5.1.1 Command Structure

```csharp
public class AutomationCommand
{
    public string Type { get; set; }      // "Query", "Action", "Wait"
    public string? Target { get; set; }    // AutomationId
    public string Method { get; set; }     // "IsVisible", "Click", etc.
    public object[]? Args { get; set; }    // Method arguments
    public int TimeoutMs { get; set; }     // Timeout for waits
}
```

#### 5.1.2 Response Structure

```csharp
public class AutomationResponse
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
}
```

#### 5.1.3 Supported Commands

| Type   | Method         | Description               |
| ------ | -------------- | ------------------------- |
| Query  | GetState       | Get full element state    |
| Query  | IsGameReady    | Check if game initialized |
| Query  | GetAllElements | List registered elements  |
| Action | Click          | Trigger click on element  |
| Action | SetText        | Set text content          |
| Action | SetChecked     | Set toggle state          |
| Action | SetValue       | Set range value           |
| Action | TakeScreenshot | Capture screenshot        |
| Wait   | WaitVisible    | Wait for visibility       |
| Wait   | WaitEnabled    | Wait for enabled          |

### 5.2 Element State DTO

```csharp
public class ElementState
{
    public bool Exists { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsFocused { get; set; }
    public string? Text { get; set; }
    public string? AutomationId { get; set; }
    public Rectangle Bounds { get; set; }
  
    // Toggle control state
    public bool? IsChecked { get; set; }
  
    // Selector control state
    public int SelectedIndex { get; set; }
    public List<string>? Items { get; set; }
  
    // Range control state
    public double? Value { get; set; }
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }
}
```

### 5.3 Dependencies

#### Brinell.Stride.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <Description>Stride 3D game engine UI testing for Brinell framework.</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="InputSimulatorStandard" Version="1.*" />
    <PackageReference Include="xunit" />
  </ItemGroup>
</Project>
```

#### Brinell.Stride.Automation.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <Description>In-game automation hooks for Stride UI testing.</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Stride.Engine" Version="4.2.*" />
    <PackageReference Include="System.Text.Json" />
  </ItemGroup>
</Project>
```

---

## 6. Sample Code Examples

### 6.1 Sample Game Setup

```csharp
// SampleGame.cs
public class SampleGame : Game
{
    private AutomationService? _automationService;
  
    protected override Task LoadContent()
    {
        // Initialize automation if enabled
        if (IsAutomationEnabled)
        {
            _automationService = new AutomationService(this);
            _automationService.Start();
        }
      
        // Create main menu
        ShowMainMenu();
      
        return base.LoadContent();
    }
  
    private void ShowMainMenu()
    {
        var menu = new MainMenuUI(this, _automationService);
        menu.Initialize();
    }
}
```

### 6.2 UI with Automation IDs

```csharp
// MainMenuUI.cs
public class MainMenuUI
{
    public void Initialize()
    {
        var startButton = new Button
        {
            Content = new TextBlock { Text = "Start Game" }
        }.WithAutomationId("MainMenu.Start");
      
        var settingsButton = new Button
        {
            Content = new TextBlock { Text = "Settings" }
        }.WithAutomationId("MainMenu.Settings");
      
        var exitButton = new Button
        {
            Content = new TextBlock { Text = "Exit" }
        }.WithAutomationId("MainMenu.Exit");
    }
}
```

### 6.3 Page Object

```csharp
// MainMenuPage.cs
public class MainMenuPage : StridePageBase
{
    public StrideButtonControl StartButton { get; }
    public StrideButtonControl SettingsButton { get; }
    public StrideButtonControl ExitButton { get; }
  
    protected override StrideControlBase KeyControl => StartButton;
  
    public MainMenuPage(StrideTestContext context) 
        : base(context, "MainMenu", "MainMenu.Root")
    {
        StartButton = new StrideButtonControl(context, this, "MainMenu.Start");
        SettingsButton = new StrideButtonControl(context, this, "MainMenu.Settings");
        ExitButton = new StrideButtonControl(context, this, "MainMenu.Exit");
    }
  
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

### 6.4 Test Example

```csharp
// MainMenuTests.cs
[Collection("Stride UI Tests")]
[Trait("Category", "Smoke")]
public class MainMenuTests : StrideTestBase
{
    public MainMenuTests(StrideGameFixture fixture) : base(fixture) { }
  
    [Fact]
    public void MainMenu_AllButtons_AreVisible()
    {
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForReady();
      
        mainMenu.StartButton.AssertVisible();
        mainMenu.SettingsButton.AssertVisible();
        mainMenu.ExitButton.AssertVisible();
    }
  
    [Fact]
    public void Settings_Navigate_ShowsSettingsPage()
    {
        var mainMenu = new MainMenuPage(Context);
        mainMenu.WaitForReady();
      
        mainMenu.NavigateToSettings();
      
        var settings = new SettingsPage(Context);
        settings.WaitForReady();
        settings.AssertDisplayed();
    }
}
```

---

## 7. Risk Assessment

### 7.1 Technical Risks

| Risk                          | Probability | Impact | Mitigation                            |
| ----------------------------- | ----------- | ------ | ------------------------------------- |
| Named pipe reliability        | Medium      | High   | Retry logic, fallback to in-process   |
| Input simulation accuracy     | Medium      | Medium | Calibrate delays, configurable timing |
| Game thread synchronization   | High        | High   | Use Stride's Script.Scheduler         |
| Screen resolution differences | Medium      | Medium | Use relative coordinates              |
| Test flakiness                | High        | High   | Robust waits, retry mechanism         |

### 7.2 Dependencies

- Stride Engine 4.2+ required
- Windows-only for input simulation (initial version)
- .NET 8.0+ required

---

## 8. Success Metrics

| Metric                     | Target                       |
| -------------------------- | ---------------------------- |
| Test reliability           | > 95% pass rate on same code |
| Test execution time        | < 30 seconds for smoke tests |
| Sample app coverage        | All UI paths tested          |
| Documentation completeness | All features documented      |

---

## 9. Timeline Summary

| Week | Phase   | Key Milestone                     |
| ---- | ------- | --------------------------------- |
| 1-2  | Phase 1 | Named pipe connection working     |
| 2-3  | Phase 2 | Can query element state from game |
| 3-4  | Phase 3 | All control wrappers functional   |
| 4-5  | Phase 4 | Sample app with page objects      |
| 5-6  | Phase 5 | All tests passing                 |
| 6    | Phase 6 | Documentation complete            |

**Total Duration:** 6 weeks

---

## 10. Appendix: Stride UI Control Reference

### 10.1 Stride.UI.Controls

| Class            | Description          | Wrapper                       |
| ---------------- | -------------------- | ----------------------------- |
| `Button`       | Clickable button     | `StrideButtonControl`       |
| `TextBlock`    | Display text         | `StrideTextBlockControl`    |
| `EditText`     | Text input           | `StrideEditTextControl`     |
| `CheckBox`     | Toggle checkbox      | `StrideCheckBoxControl`     |
| `ToggleButton` | Toggle button        | `StrideToggleButtonControl` |
| `Slider`       | Value slider         | `StrideSliderControl`       |
| `ScrollViewer` | Scrollable container | `StrideScrollViewerControl` |

### 10.2 Stride.UI.Panels

| Class           | Description          |
| --------------- | -------------------- |
| `StackPanel`  | Stack layout         |
| `Grid`        | Grid layout          |
| `Canvas`      | Absolute positioning |
| `UniformGrid` | Uniform grid layout  |

---

## 11. Oravey Game Integration (Extended Plan)

This section covers the Oravey-specific extensions that live **outside** of Brinell.

### 11.1 Separation of Concerns

| Component                     | Location     | Responsibility                       |
| ----------------------------- | ------------ | ------------------------------------ |
| `Brinell.Stride`            | Brinell repo | Generic Stride UI testing            |
| `Brinell.Stride.Automation` | Brinell repo | Generic in-game UI hooks             |
| `Oravey.Game.Automation`    | Oravey repo  | **Oravey-specific game state** |
| `Oravey.Game.StrideUITests` | Oravey repo  | **Oravey-specific tests**      |

### 11.2 Architecture with Oravey Extension

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Brinell Repository (Generic/Reusable)                │
├─────────────────────────────────────────────────────────────────────────┤
│  Brinell.Stride                                                         │
│    ├─ StrideTestContext          (ITestContext)                        │
│    ├─ StrideControlBase          (buttons, textboxes, sliders)         │
│    ├─ StridePageBase             (UI screen abstraction)               │
│    ├─ NamedPipeChannel           (communication)                       │
│    └─ StrideInputSimulator       (keyboard/mouse)                      │
├─────────────────────────────────────────────────────────────────────────┤
│  Brinell.Stride.Automation                                              │
│    ├─ AutomationService          (UI element registry)                 │
│    ├─ AutomationHost             (pipe server)                         │
│    ├─ UIElementProvider          (UI state queries)                    │
│    └─ AutomationExtensions       (.WithAutomationId())                 │
└─────────────────────────────────────────────────────────────────────────┘
                              │ references
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    Oravey Repository (Game-Specific)                    │
├─────────────────────────────────────────────────────────────────────────┤
│  Oravey.Game.Automation (extends Brinell.Stride.Automation)             │
│    ├─ OraveyAutomationService    (extends AutomationService)           │
│    ├─ PlayerStateProvider        (position, health, inventory)         │
│    ├─ WorldStateProvider         (current area, entities, time)        │
│    ├─ CameraStateProvider        (view frustum, target)                │
│    └─ OraveyCommands             (game-specific commands)              │
├─────────────────────────────────────────────────────────────────────────┤
│  Oravey.Game                                                            │
│    └─ References Oravey.Game.Automation (conditional)                  │
├─────────────────────────────────────────────────────────────────────────┤
│  Oravey.Game.StrideUITests                                              │
│    ├─ OraveyTestContext          (extends StrideTestContext)           │
│    ├─ PageObjects/                                                      │
│    │   ├─ MainMenuPage                                                 │
│    │   ├─ NameInputPage                                                │
│    │   ├─ LoadGamePage                                                 │
│    │   ├─ PauseMenuPage                                                │
│    │   └─ GameplayPage           (HUD + game state access)             │
│    └─ Tests/                                                            │
│        ├─ MainMenuTests                                                │
│        ├─ NavigationTests                                              │
│        ├─ GameplayTests          (movement, combat, etc.)              │
│        └─ WorldTests             (area transitions, entities)          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 11.3 Game State Extension API

#### 11.3.1 Oravey.Game.Automation

```csharp
// OraveyAutomationService.cs - Extends base automation
public class OraveyAutomationService : AutomationService
{
    private readonly PlayerStateProvider _playerState;
    private readonly WorldStateProvider _worldState;
    private readonly CameraStateProvider _cameraState;
  
    public OraveyAutomationService(OraveyGame game) : base(game)
    {
        _playerState = new PlayerStateProvider(game.Player);
        _worldState = new WorldStateProvider(game.WorldManager);
        _cameraState = new CameraStateProvider(game.Camera);
      
        // Register Oravey-specific command handlers
        RegisterCommandHandler("GetPlayerPosition", _playerState.GetPosition);
        RegisterCommandHandler("GetPlayerHealth", _playerState.GetHealth);
        RegisterCommandHandler("GetInventory", _playerState.GetInventory);
        RegisterCommandHandler("GetCurrentArea", _worldState.GetCurrentArea);
        RegisterCommandHandler("GetVisibleEntities", _worldState.GetVisibleEntities);
        RegisterCommandHandler("GetTimeOfDay", _worldState.GetTimeOfDay);
        RegisterCommandHandler("IsEntityInView", _cameraState.IsEntityInView);
    }
}
```

```csharp
// PlayerStateProvider.cs
public class PlayerStateProvider
{
    private readonly Player _player;
  
    public PlayerStateProvider(Player player) => _player = player;
  
    public Vector3 GetPosition() => _player.Transform.Position;
    public float GetHealth() => _player.Health;
    public float GetMaxHealth() => _player.MaxHealth;
    public string[] GetInventory() => _player.Inventory.Items.Select(i => i.Name).ToArray();
    public string GetEquippedWeapon() => _player.EquippedWeapon?.Name ?? "None";
    public bool IsMoving() => _player.IsMoving;
    public string GetFacingDirection() => _player.FacingDirection.ToString();
}
```

```csharp
// WorldStateProvider.cs
public class WorldStateProvider
{
    private readonly WorldManager _world;
  
    public WorldStateProvider(WorldManager world) => _world = world;
  
    public string GetCurrentArea() => _world.CurrentArea?.Name ?? "Unknown";
    public string GetCurrentMap() => _world.CurrentMap?.Name ?? "Unknown";
    public Vector2 GetMapDimensions() => _world.CurrentMap?.Dimensions ?? Vector2.Zero;
    public float GetTimeOfDay() => _world.TimeOfDay;
    public bool IsNight() => _world.IsNight;
  
    public EntityInfo[] GetVisibleEntities() => 
        _world.VisibleEntities.Select(e => new EntityInfo
        {
            Id = e.Id,
            Name = e.Name,
            Type = e.EntityType.ToString(),
            Position = e.Transform.Position
        }).ToArray();
  
    public EntityInfo? GetEntity(string entityId) =>
        _world.FindEntity(entityId) is Entity e 
            ? new EntityInfo { Id = e.Id, Name = e.Name, Position = e.Transform.Position }
            : null;
}
```

#### 11.3.2 OraveyTestContext Extension

```csharp
// In Oravey.Game.StrideUITests
public class OraveyTestContext : StrideTestContext
{
    public OraveyTestContext(IAutomationChannel channel, StrideTestOptions options) 
        : base(channel, options) { }
  
    #region Player State
  
    public Vector3 GetPlayerPosition()
        => Query<Vector3>("GetPlayerPosition");
  
    public float GetPlayerHealth()
        => Query<float>("GetPlayerHealth");
  
    public string[] GetInventory()
        => Query<string[]>("GetInventory");
  
    public bool WaitForPlayerPosition(Func<Vector3, bool> condition, int? timeoutMs = null)
        => WaitFor(() => condition(GetPlayerPosition()), timeoutMs, "player position");
  
    #endregion
  
    #region World State
  
    public string GetCurrentArea()
        => Query<string>("GetCurrentArea");
  
    public bool WaitForArea(string areaName, int? timeoutMs = null)
        => WaitFor(() => GetCurrentArea() == areaName, timeoutMs, $"area '{areaName}'");
  
    public EntityInfo[] GetVisibleEntities()
        => Query<EntityInfo[]>("GetVisibleEntities");
  
    public bool IsEntityVisible(string entityId)
        => Query<bool>("IsEntityInView", entityId);
  
    #endregion
  
    #region Player Actions (Input Helpers)
  
    public void MoveNorth(int durationMs = 500) => HoldKey(VirtualKeyCode.W, durationMs);
    public void MoveSouth(int durationMs = 500) => HoldKey(VirtualKeyCode.S, durationMs);
    public void MoveEast(int durationMs = 500) => HoldKey(VirtualKeyCode.D, durationMs);
    public void MoveWest(int durationMs = 500) => HoldKey(VirtualKeyCode.A, durationMs);
  
    public void Attack() => PressKey(VirtualKeyCode.SPACE);
    public void Interact() => PressKey(VirtualKeyCode.E);
    public void OpenInventory() => PressKey(VirtualKeyCode.I);
  
    #endregion
}
```

### 11.4 Oravey Test Examples

#### 11.4.1 Gameplay Movement Tests

```csharp
[Collection("Oravey UI Tests")]
[Trait("Category", "Gameplay.Movement")]
public class MovementTests : OraveyTestBase
{
    public MovementTests(OraveyGameFixture fixture) : base(fixture) { }
  
    [Fact]
    public void Player_MoveNorth_PositionIncreases()
    {
        // Arrange - start a new game and get to gameplay
        StartNewGameAndWaitForGameplay();
      
        var startPos = Context.GetPlayerPosition();
      
        // Act
        Context.MoveNorth(durationMs: 1000);
      
        // Assert
        var endPos = Context.GetPlayerPosition();
        Assert.True(endPos.Z > startPos.Z, 
            $"Player should move north. Start: {startPos.Z}, End: {endPos.Z}");
    }
  
    [Fact]
    public void Player_CannotWalkThroughWalls()
    {
        StartNewGameAndWaitForGameplay();
      
        // Move toward known wall position
        var startPos = Context.GetPlayerPosition();
      
        // Try to move into wall for 2 seconds
        Context.MoveNorth(durationMs: 2000);
      
        var endPos = Context.GetPlayerPosition();
      
        // Should have stopped at wall
        Assert.True(endPos.Z < 100f, "Player should be blocked by wall");
    }
}
```

#### 11.4.2 Area Transition Tests

```csharp
[Trait("Category", "Gameplay.World")]
public class AreaTransitionTests : OraveyTestBase
{
    [Fact]
    public void Player_EntersForest_ShowsAreaName()
    {
        StartNewGameAndWaitForGameplay();
      
        var gameplay = new GameplayPage(Context);
      
        // Move to forest entrance (known position)
        Context.MoveNorth(durationMs: 3000);
      
        // Assert area name popup appears
        gameplay.AreaNamePopup.WaitVisible();
        gameplay.AreaNamePopup.AssertTextContains("Forest");
      
        // Assert world state updated
        Assert.Equal("Dark Forest", Context.GetCurrentArea());
    }
  
    [Fact]
    public void AreaTransition_UpdatesMiniMap()
    {
        StartNewGameAndWaitForGameplay();
      
        var gameplay = new GameplayPage(Context);
        var beforeScreenshot = gameplay.MiniMap.TakeScreenshot("before-transition");
      
        // Trigger area transition
        Context.MoveNorth(durationMs: 3000);
        Context.WaitForArea("Dark Forest");
      
        var afterScreenshot = gameplay.MiniMap.TakeScreenshot("after-transition");
      
        // Screenshots should differ (minimap updated)
        Assert.NotEqual(beforeScreenshot, afterScreenshot);
    }
}
```

#### 11.4.3 Combat Tests

```csharp
[Trait("Category", "Gameplay.Combat")]
public class CombatTests : OraveyTestBase
{
    [Fact]
    public void Player_AttacksEnemy_DealsDamage()
    {
        StartNewGameAndWaitForGameplay();
      
        // Get visible enemy
        var enemies = Context.GetVisibleEntities()
            .Where(e => e.Type == "Enemy")
            .ToList();
      
        if (enemies.Count == 0)
        {
            // Move to area with enemies
            Context.MoveNorth(durationMs: 2000);
            enemies = Context.GetVisibleEntities()
                .Where(e => e.Type == "Enemy")
                .ToList();
        }
      
        Assert.NotEmpty(enemies);
      
        // Attack
        Context.Attack();
      
        // Could verify via damage numbers UI or enemy health query
        var gameplay = new GameplayPage(Context);
        gameplay.DamageNumber.WaitVisible(timeoutMs: 1000);
    }
  
    [Fact]
    public void Player_TakesDamage_HealthBarUpdates()
    {
        StartNewGameAndWaitForGameplay();
      
        var startHealth = Context.GetPlayerHealth();
        var gameplay = new GameplayPage(Context);
      
        // Get hit by standing near enemy
        Context.WaitFor(() => Context.GetPlayerHealth() < startHealth, 
            timeoutMs: 5000, 
            description: "take damage");
      
        // Verify health bar UI reflects damage
        var healthPercent = startHealth > 0 
            ? Context.GetPlayerHealth() / startHealth 
            : 0;
      
        gameplay.HealthBar.AssertValueLessThan(startHealth);
    }
}
```

### 11.5 Oravey Project Structure

```
Oravey/Sources/
├── Game/
│   └── Oravey.Game/
│       └── (references Oravey.Game.Automation conditionally)
│
├── GameAutomation/                      # NEW PROJECT
│   └── Oravey.Game.Automation/
│       ├── Oravey.Game.Automation.csproj
│       ├── OraveyAutomationService.cs
│       ├── Providers/
│       │   ├── PlayerStateProvider.cs
│       │   ├── WorldStateProvider.cs
│       │   ├── CameraStateProvider.cs
│       │   └── EntityInfo.cs
│       └── Commands/
│           └── OraveyCommands.cs
│
└── Tests/
    └── Oravey.Game.StrideUITests/       # NEW PROJECT
        ├── Oravey.Game.StrideUITests.csproj
        ├── OraveyTestContext.cs
        ├── OraveyTestBase.cs
        ├── OraveyGameFixture.cs
        ├── PageObjects/
        │   ├── MainMenuPage.cs
        │   ├── NameInputPage.cs
        │   ├── LoadGamePage.cs
        │   ├── PauseMenuPage.cs
        │   ├── GameplayPage.cs
        │   └── InventoryPage.cs
        └── Tests/
            ├── SmokeTests/
            │   └── ApplicationStartupTests.cs
            ├── MainMenu/
            │   ├── MainMenuDisplayTests.cs
            │   └── MainMenuNavigationTests.cs
            ├── NewGame/
            │   └── NewGameFlowTests.cs
            ├── Gameplay/
            │   ├── MovementTests.cs
            │   ├── CombatTests.cs
            │   └── InventoryTests.cs
            └── World/
                ├── AreaTransitionTests.cs
                └── MiniMapTests.cs
```

### 11.6 Oravey Phase Timeline

| Phase | Focus                                        | Duration  | Depends On      |
| ----- | -------------------------------------------- | --------- | --------------- |
| 7A    | Create `Oravey.Game.Automation` project    | 1 week    | Plan 12 Phase 2 |
| 7B    | Implement state providers                    | 1 week    | Phase 7A        |
| 7C    | Create `Oravey.Game.StrideUITests` project | 1 week    | Plan 12 Phase 5 |
| 7D    | Implement Oravey page objects                | 1-2 weeks | Phase 7C        |
| 7E    | Implement gameplay tests                     | 2 weeks   | Phase 7D        |

**Total Oravey Extension: 6-7 weeks** (can run in parallel with Brinell phases 3-6)

### 11.7 Command Summary

#### Generic Commands (Brinell.Stride.Automation)

| Command            | Description            |
| ------------------ | ---------------------- |
| `GetState`       | Get UI element state   |
| `IsGameReady`    | Check game initialized |
| `Click`          | Click UI element       |
| `SetText`        | Set text content       |
| `TakeScreenshot` | Capture screenshot     |

#### Oravey Commands (Oravey.Game.Automation)

| Command                | Description               |
| ---------------------- | ------------------------- |
| `GetPlayerPosition`  | Get player world position |
| `GetPlayerHealth`    | Get current health        |
| `GetInventory`       | Get inventory items       |
| `GetCurrentArea`     | Get area name             |
| `GetVisibleEntities` | Get entities in view      |
| `GetTimeOfDay`       | Get world time            |
| `IsEntityInView`     | Check if entity visible   |

---

*Document Version: 1.1*
*Last Updated: December 2025*
