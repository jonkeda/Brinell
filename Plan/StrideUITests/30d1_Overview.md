# 1. Overview & Testing Approaches

**Parent:** [Documentation Index](30d0_StrideUITestFramework_Index.md)  
**Next:** [Architecture](30d2_Architecture.md)  
**Version:** 1.0 (Proposal - January 2025)

---

## 1.1 Testing Scope

### 1.1.1 UI Form Elements Testing

Testing individual Stride UI controls in isolation or small groupings:

| Control Type | Stride Class | Test Focus |
|--------------|--------------|------------|
| **Buttons** | `Button` | Click behavior, enabled state, visual feedback |
| **Text Display** | `TextBlock` | Text content, visibility, formatting |
| **Text Input** | `EditText` | Text entry, selection, validation |
| **Toggles** | `ToggleButton`, `CheckBox` | Checked state, toggle behavior |
| **Sliders** | `Slider` | Value changes, range validation |
| **Lists** | `ListBox` | Item selection, scrolling |
| **Grids** | `UniformGrid`, `Grid` | Layout, child arrangement |
| **Panels** | `StackPanel`, `Canvas` | Child management, layout |

### 1.1.2 Game UI Testing

Testing complete game screens and user workflows:

| Screen | Current Class | Test Focus |
|--------|---------------|------------|
| **Main Menu** | `MainMenuUI` | Button states, navigation, keyboard support |
| **Name Input** | `NameInputDialog` | Text entry, validation, confirmation |
| **Load Game** | `LoadGameUI` | Save list population, selection, loading |
| **Pause Menu** | `PauseMenuUI` | Show/hide, resume, save, exit |
| **Loading Screen** | `LoadingScreenUI` | Progress display, completion |
| **Mini Map** | `MiniMapUI` | Rendering, player position, area display |
| **Debug Overlay** | `DebugOverlay` | FPS display, debug info toggle |

---

## 1.2 Stride UI Architecture Overview

### 1.2.1 Stride UI Components

Stride uses a component-based UI system:

```csharp
// UI is attached to entities via UIComponent
var uiEntity = new Entity("MainMenuUI");
uiEntity.Add(new UIComponent
{
    Page = new UIPage { RootElement = rootElement },
    Resolution = new Vector3(1920, 1080, 1000),
    IsFullScreen = true
});
```

### 1.2.2 UI Element Hierarchy

```
UIComponent (Entity Component)
??? UIPage
    ??? RootElement (UIElement)
        ??? Canvas / Grid / StackPanel
        ?   ??? Button
        ?   ?   ??? TextBlock (Content)
        ?   ??? EditText
        ?   ??? TextBlock
        ?   ??? ...
        ??? ...
```

### 1.2.3 Key Stride UI Classes

| Class | Namespace | Description |
|-------|-----------|-------------|
| `UIComponent` | `Stride.Engine` | Attaches UI to entity |
| `UIPage` | `Stride.UI` | Contains root element |
| `UIElement` | `Stride.UI` | Base class for all UI elements |
| `ContentControl` | `Stride.UI.Controls` | Base for controls with content |
| `Button` | `Stride.UI.Controls` | Clickable button |
| `TextBlock` | `Stride.UI.Controls` | Display text |
| `EditText` | `Stride.UI.Controls` | Text input field |
| `Slider` | `Stride.UI.Controls` | Value slider |
| `StackPanel` | `Stride.UI.Panels` | Stack layout |
| `Canvas` | `Stride.UI.Panels` | Absolute positioning |
| `Grid` | `Stride.UI.Panels` | Grid layout |

---

## 1.3 Testing Approach Analysis

### 1.3.1 Approach A: Internal Automation Layer

**Description:** Inject test automation hooks directly into the game, exposing the UI element tree and state to external test code.

**Implementation:**
```csharp
// In-game AutomationService
public class AutomationService : ScriptComponent
{
    private Dictionary<string, UIElement> _registeredElements = new();
    
    public void RegisterElement(string automationId, UIElement element)
    {
        _registeredElements[automationId] = element;
    }
    
    public bool IsVisible(string automationId)
    {
        return _registeredElements.TryGetValue(automationId, out var element)
            && element.IsVisible;
    }
    
    public void Click(string automationId)
    {
        if (_registeredElements.TryGetValue(automationId, out var element)
            && element is ButtonBase button)
        {
            // Simulate click via Stride API
            button.RaiseClick();
        }
    }
}
```

**Pros:**
- Direct access to UI state
- Reliable element identification
- Fast execution
- Access to internal state (IsBusy, loading, etc.)

**Cons:**
- Requires modifying game code
- Test automation code ships with game (can be conditionally compiled)
- Tight coupling between test framework and game

**Complexity:** Medium  
**Reliability:** High  
**Recommendation:** ? **Primary approach for state verification**

---

### 1.3.2 Approach B: Visual Recognition

**Description:** Use screenshots and image/OCR recognition to find and interact with UI elements.

**Implementation:**
```csharp
// Screenshot-based testing
public class VisualTestContext
{
    public bool FindButton(string buttonText)
    {
        var screenshot = CaptureScreen();
        var locations = OcrEngine.FindText(screenshot, buttonText);
        return locations.Any();
    }
    
    public void ClickButton(string buttonText)
    {
        var screenshot = CaptureScreen();
        var location = OcrEngine.FindText(screenshot, buttonText).First();
        SimulateClick(location.Center);
    }
}
```

**Pros:**
- Tests exactly what users see
- No game code modification needed
- Works with any rendering technology

**Cons:**
- Slow (screenshot + OCR overhead)
- Brittle (visual changes break tests)
- Difficult to verify non-visual state
- OCR accuracy issues with game fonts
- Resolution/scaling dependencies

**Complexity:** High  
**Reliability:** Medium  
**Recommendation:** ?? **Only for visual regression testing, not primary automation**

---

### 1.3.3 Approach C: Input Simulation

**Description:** Simulate mouse and keyboard input at the OS level, letting the game process inputs naturally.

**Implementation:**
```csharp
// Windows API input simulation
public class InputSimulator
{
    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint data, int extra);
    
    public void Click(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }
    
    public void TypeText(string text)
    {
        foreach (var c in text)
        {
            keybd_event(VkKeyScan(c), 0, 0, 0);
            keybd_event(VkKeyScan(c), 0, KEYEVENTF_KEYUP, 0);
        }
    }
}
```

**Pros:**
- Tests actual input pipeline
- No game code modification
- Simple implementation

**Cons:**
- Requires knowing screen coordinates
- Cannot query UI state
- Affected by window position/focus
- Timing-sensitive
- No access to internal state

**Complexity:** Low  
**Reliability:** Low  
**Recommendation:** ?? **Use only for input actions in hybrid approach**

---

### 1.3.4 Approach D: Hybrid Approach (Recommended)

**Description:** Combine Internal Automation Layer for state queries with Input Simulation for user actions.

**Architecture:**
```
???????????????????????????????????????????????????????????????????????????
? Test Code                                                               ?
?   ??? Query State ? Internal Automation Layer ? Reliable state info   ?
?   ??? Perform Actions ? Input Simulation ? Realistic user interaction ?
?   ??? Assert Results ? Internal Automation Layer ? Accurate validation?
???????????????????????????????????????????????????????????????????????????
```

**Implementation Pattern:**
```csharp
[Fact]
public void MainMenu_NewGame_NavigatesToNameInput()
{
    // Arrange - use automation layer for state
    var mainMenu = new MainMenuPage(Context);
    mainMenu.WaitForPageReady();
    mainMenu.NewGameButton.AssertVisible();
    mainMenu.NewGameButton.AssertEnabled();
    
    // Act - use input simulation for realistic interaction
    var buttonBounds = mainMenu.NewGameButton.GetBounds();
    InputSimulator.Click(buttonBounds.Center);
    
    // Assert - use automation layer for verification
    var nameInput = new NameInputPage(Context);
    nameInput.WaitForPageReady();
    nameInput.AssertDisplayed();
}
```

**Pros:**
- Best of both worlds
- Reliable state verification
- Realistic user interactions
- Tests actual input handling

**Cons:**
- More complex implementation
- Requires game code hooks

**Complexity:** Medium  
**Reliability:** High  
**Recommendation:** ? **Recommended approach**

---

## 1.4 Communication Architecture

### 1.4.1 Named Pipe Communication

For cross-process communication between test runner and game:

```
??????????????????????                    ??????????????????????
? Test Process       ?                    ? Game Process       ?
? (xUnit)            ?                    ? (Oravey.Game)      ?
?                    ?  Named Pipe        ?                    ?
? StrideTestContext  ?????????????????????? AutomationHost     ?
?                    ?  JSON Commands     ?                    ?
? - SendCommand()    ?                    ? - ProcessCommand() ?
? - QueryState()     ?                    ? - GetUIState()     ?
??????????????????????                    ??????????????????????
```

### 1.4.2 Command Protocol

```csharp
// Command structure
public class AutomationCommand
{
    public string Type { get; set; }      // "Query", "Action", "Wait"
    public string Target { get; set; }     // AutomationId
    public string Method { get; set; }     // "IsVisible", "Click", etc.
    public object[] Args { get; set; }     // Method arguments
    public int TimeoutMs { get; set; }     // Timeout for waits
}

// Response structure
public class AutomationResponse
{
    public bool Success { get; set; }
    public object Result { get; set; }
    public string Error { get; set; }
}
```

### 1.4.3 In-Process Testing Alternative

For simpler tests, run tests in the same process as the game:

```csharp
public class InProcessStrideTest : IDisposable
{
    private OraveyGame _game;
    private Thread _gameThread;
    
    public InProcessStrideTest()
    {
        _gameThread = new Thread(() =>
        {
            _game = new OraveyGame();
            _game.Run();
        });
        _gameThread.SetApartmentState(ApartmentState.STA);
        _gameThread.Start();
        
        // Wait for game to initialize
        WaitForGameReady();
    }
    
    protected void ExecuteOnGameThread(Action action)
    {
        _game.Script.Scheduler.Add(() =>
        {
            action();
            return Task.CompletedTask;
        });
    }
}
```

---

## 1.5 Stride UI Testing Specifics

### 1.5.1 Element Identification Strategy

Since Stride UI doesn't have a built-in automation ID system, we need to add one:

**Option 1: Name Property**
```csharp
var button = new Button { Name = "NewGameButton" };
```

**Option 2: Custom Extension Property** (Recommended)
```csharp
public static class UIAutomationExtensions
{
    private static readonly Dictionary<UIElement, string> AutomationIds = new();
    
    public static void SetAutomationId(this UIElement element, string id)
    {
        AutomationIds[element] = id;
    }
    
    public static string GetAutomationId(this UIElement element)
    {
        return AutomationIds.TryGetValue(element, out var id) ? id : element.Name;
    }
}

// Usage in UI creation
var button = new Button { Name = "NewGameButton" };
button.SetAutomationId("MainMenu.NewGame");
```

**Option 3: Attribute-Based Registration**
```csharp
[AutomationId("MainMenu.NewGame")]
public Button NewGameButton { get; private set; }
```

### 1.5.2 Screen Coordinate Calculation

For input simulation, we need to convert UI coordinates to screen coordinates:

```csharp
public Vector2 GetScreenPosition(UIElement element)
{
    // Get element bounds in UI space
    var bounds = element.RenderSizeInternal;
    var localCenter = new Vector2(bounds.X / 2, bounds.Y / 2);
    
    // Transform to screen space
    var worldMatrix = element.WorldMatrix;
    var screenPos = Vector3.Transform(new Vector3(localCenter, 0), worldMatrix);
    
    // Apply window position offset
    var windowPos = GetWindowPosition();
    return new Vector2(
        windowPos.X + screenPos.X,
        windowPos.Y + screenPos.Y
    );
}
```

---

## 1.6 Test Categories

Following the existing Oravey test category structure:

| Category | Description | Scope |
|----------|-------------|-------|
| `Smoke` | Critical path tests | Always run |
| `UI.Controls` | Individual control tests | Fast, isolated |
| `UI.Navigation` | Page navigation tests | Medium, multi-screen |
| `UI.Workflows` | Complete user workflows | Slow, end-to-end |
| `Visual` | Visual regression tests | Slow, screenshot-based |

---

## 1.7 Framework Stack (Proposed)

| Component | Version | Purpose |
|-----------|---------|---------|
| **Stride.Engine** | 4.2+ | Game engine with UI system |
| **Oravey.UITestFramework.Core** | 3.0 | Shared interfaces and patterns |
| **Oravey.UITestFramework.Stride** | 1.0 | Stride-specific implementation |
| **xUnit** | 2.9.x | Test framework |
| **FluentAssertions** | 6.x | Readable assertions |
| **InputSimulatorStandard** | 1.x | Cross-platform input simulation |

---

*Document Version: 1.0*  
*Last Updated: January 2025*
