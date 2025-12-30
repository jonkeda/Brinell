# Framework Overview

**Version:** 3.0  
**Architecture:** Multi-platform test automation with interface-based design

---

## Framework Stack

| Component | Version | Purpose | Platform |
|-----------|---------|---------|----------|
| **FlaUI** | 4.0.0 | UI Automation 3 (UIA3) | Windows WPF |
| **Appium.WebDriver** | 8.0.0 | W3C WebDriver protocol | Windows MAUI, Android, iOS |
| **Selenium.WebDriver** | 4.27.0 | Browser automation | Chrome, Firefox, Edge, Safari |
| **xUnit** | 2.9.x | Test framework | All platforms |
| **FluentAssertions** | 6.x | Assertion library | All platforms |

---

## Architecture Layers

### Four-Layer Design

```
┌─────────────────────────────────────────────────────────────┐
│ Application Tests (*.UITests)                               │
│ • Page Objects                                              │
│ • Test Classes                                              │
│ • Test Configuration                                        │
└──────────────────────────┬──────────────────────────────────┘
                           │ references
┌──────────────────────────▼──────────────────────────────────┐
│ Platform Implementations                                    │
│ • Oravey.UITestFramework.Wpf     (FlaUI)                   │
│ • Oravey.UITestFramework.Maui    (Appium)                  │
│ • Oravey.UITestFramework.Html    (Selenium)                │
│                                                              │
│ Each platform provides:                                     │
│ • TestContext with native driver access                     │
│ • Complete base class hierarchy                             │
│ • Platform-specific controls                                │
└──────────────────────────┬──────────────────────────────────┘
                           │ references
┌──────────────────────────▼──────────────────────────────────┐
│ Core (Oravey.UITestFramework.Core)                         │
│ • Interfaces only: ITestContext, IPageObject, IControlObject│
│ • Platform enum with extension methods                      │
│ • Structured CSV logging                                    │
│ • Exception types                                           │
└─────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

| Layer | Responsibility | Examples |
|-------|----------------|----------|
| **Application Tests** | Application-specific tests and page objects | `MainWindowPage`, `LoginTests` |
| **Platform** | Platform-specific implementations with native drivers | `FlaUITestContext`, `ButtonControl` |
| **Core** | Interface contracts and utilities | `IControlObject`, `Platform enum` |

---

## Key Design Principles

### 1. Core = Interfaces Only (v3 Change)

**Previous (v1-v2):** Core contained base classes and adapter abstractions.

**Current (v3):** Core contains **only interfaces**, each platform provides its own implementations.

**Benefits:**
- Simpler Core project
- Native performance (no adapter overhead)
- Platform-specific optimizations possible
- Each platform fully self-contained

### 2. Native Driver Access

Platforms use automation libraries directly:
- **WPF:** Direct `FlaUI.Core.AutomationElement` access
- **MAUI:** Direct `OpenQA.Selenium.Appium.AppiumElement` access
- **Web:** Direct `OpenQA.Selenium.IWebElement` access

No adapter or wrapper layer between framework and automation library.

### 3. Navigation Returns Void

**Previous (v1-v2):** Navigation methods returned target page object.

```csharp
// OLD PATTERN
var settingsPage = homePage.NavigateToSettings();
```

**Current (v3):** Navigation returns void, tests create page objects.

```csharp
// NEW PATTERN
homePage.NavigateToSettings();
var settingsPage = new SettingsPage(Context);
settingsPage.WaitForPageReady();
```

**Benefits:**
- Tests own page object lifecycle
- Explicit wait strategies
- Clearer separation of concerns
- Better testability

### 4. Always Check Before Action

Every action method verifies preconditions before acting:

```
Button.Click()
    ↓
CheckClickable()
    ↓
WaitVisible(true) + WaitEnabled(true)
    ↓
If not clickable after timeout → throw
    ↓
Perform native driver click
    ↓
Log action result
```

### 5. Virtual Methods for Extensibility

All base class methods are `virtual` for:
- Platform-specific overrides
- Custom logging/retry logic
- Test-specific behavior

### 6. Structured CSV Logging

All test actions logged in machine-parseable CSV format:

```csv
Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
2025-12-30T10:15:30;LoginTest;LoginPage;UsernameInput;EnterText;admin;;;Success;
2025-12-30T10:15:31;LoginTest;LoginPage;LoginButton;Click;;;Success;
2025-12-30T10:15:32;LoginTest;MainPage;WelcomeLabel;AssertText;Welcome;Welcome;Pass;
```

---

## Platform Comparison

| Feature | WPF (FlaUI) | MAUI (Appium) | Web (Selenium) |
|---------|-------------|---------------|----------------|
| **Automation** | UI Automation 3 | W3C WebDriver | W3C WebDriver |
| **Element Type** | `AutomationElement` | `AppiumElement` | `IWebElement` |
| **AutomationId** | `AutomationProperties.AutomationId` | `AutomationId` | `data-automation-id` or `id` |
| **Context Class** | `FlaUITestContext` | `AppiumTestContext` | `SeleniumTestContext` |
| **Base Hierarchy** | WPF-specific | MAUI-specific | HTML-specific |

---

## Project Structure

### Typical Test Project

```
YourApp.UITests/
├── PageObjects/
│   ├── MainWindowPage.cs
│   ├── SettingsPage.cs
│   └── LoginDialog.cs
├── Tests/
│   ├── NavigationTests.cs
│   ├── SettingsTests.cs
│   └── LoginTests.cs
├── appsettings.json
└── YourApp.UITests.csproj
```

### Platform Project Structure

```
Oravey.UITestFramework.Wpf/
├── Infrastructure/
│   ├── FlaUITestContext.cs     # Implements ITestContext
│   └── FlaUIDriverAdapter.cs   # App lifecycle
├── Controls/
│   ├── Base/
│   │   ├── ControlBase.cs
│   │   ├── PageBase.cs
│   │   ├── BusyPageBase.cs
│   │   ├── ContentControlBase.cs
│   │   ├── TextControlBase.cs
│   │   ├── ToggleControlBase.cs
│   │   └── ... (capability bases)
│   └── Specific Controls/
│       ├── ButtonControl.cs
│       ├── TextBoxControl.cs
│       ├── CheckBoxControl.cs
│       └── ... (concrete controls)
└── Testing/
    └── WpfUITestBase.cs
```

---

## Control Object Pattern

Controls encapsulate element interaction with built-in waits and state verification.

### Hierarchy

```
IControlObject (Core interface)
    ↓
ControlBase (Platform-specific)
    ↓
├── ContentControlBase → ButtonControl, LabelControl
├── TextControlBase → TextBoxControl, EntryControl
├── ToggleControlBase → CheckBoxControl, SwitchControl
├── SelectorControlBase → ComboBoxControl, PickerControl
├── RangeControlBase → SliderControl, ProgressControl
└── ItemsControlBase → ListBoxControl, CollectionViewControl
```

### Example

```csharp
// Button control with built-in checks
public class ButtonControl : ContentControlBase
{
    public virtual void Click()
    {
        CheckClickable();  // Wait for visible + enabled
        
        // Perform click via native driver
        var element = GetNativeElement();
        element.Click();
        
        // Log success
        Logger.LogAction(...);
    }
}
```

---

## Page Object Pattern

Pages encapsulate UI structure and navigation workflows.

### Hierarchy

```
IPageObject (Core interface)
    ↓
PageBase (Platform-specific)
    ↓
├── Simple pages (no loading indicators)
└── BusyPageBase / LoadingPageBase (with IsBusy tracking)
```

### Example

```csharp
public class SettingsPage : BusyPageBase
{
    protected override string BusyIndicatorId => "SettingsPageBusyIndicator";
    
    // Controls
    public TextBoxControl UsernameInput { get; }
    public ButtonControl SaveButton { get; }
    
    public SettingsPage(FlaUITestContext context) 
        : base(context, "Settings")
    {
        UsernameInput = new TextBoxControl(context, this, "UsernameInput");
        SaveButton = new ButtonControl(context, this, "SaveButton");
    }
    
    // Actions
    public void SaveSettings()
    {
        SaveButton.Click();
        WaitForNotBusy();  // Wait for save to complete
    }
}
```

---

## Wait/Check/Is/Assert Pattern

Four-tier state verification:

| Method | Returns | On Failure | Logging | Use Case |
|--------|---------|------------|---------|----------|
| `Is*()` | `bool` | Returns current state | None | Conditional logic |
| `Wait*()` | `bool` | Returns `false` | Minimal | Async operations |
| `Check*()` | `void` | Throws `AssertionException` | Error only | Preconditions |
| `Assert*()` | `void` | Throws `AssertionException` | Full CSV | Test assertions |

### Example

```csharp
// Is* - Immediate check
if (button.IsVisible()) 
{
    // Conditional logic
}

// Wait* - Poll until condition
bool ready = page.WaitForNotBusy(timeout: 5000);

// Check* - Precondition (used internally by actions)
button.CheckClickable();  // Called automatically by Click()

// Assert* - Test assertion
label.AssertText("Expected Value");
```

---

## IsBusy State Tracking

Pages indicate loading state via standardized busy indicator:

```csharp
public class DashboardPage : BusyPageBase
{
    protected override string BusyIndicatorId => "DashboardBusyIndicator";
    
    // WaitForPageReady() automatically:
    // 1. Waits for page displayed
    // 2. Waits for IsBusy = false
}

// In test
homePage.NavigateToDashboard();
var dashboard = new DashboardPage(Context);
dashboard.WaitForPageReady();  // Safe - waits for page ready

dashboard.RefreshButton.Click();  // Now safe to interact
```

---

## Testing Pyramid Guidance

```
         /\
        /  \      UI Tests (< 5%)
       /────\     • Smoke tests
      /      \    • Navigation works
     /        \   • Critical paths
    /──────────\ 
   / Integration\ Integration Tests (10-20%)
  /    Tests     \• API contracts
 /────────────────\• Database operations
/                  \
/    Unit Tests     \ Unit Tests (75-85%)
/____________________\• Business logic
                      • ViewModels
```

### What to UI Test

✅ Application launches  
✅ Navigation between views  
✅ Critical controls visible  
✅ Happy path user flows

### What NOT to UI Test

❌ Business logic (unit tests)  
❌ Form validation (ViewModel tests)  
❌ Data persistence (integration tests)  
❌ Edge cases (unit tests)  
❌ Complex workflows (integration tests)

---

## Key Interfaces (Core)

### ITestContext

```csharp
public interface ITestContext
{
    string TestName { get; }
    Platform Platform { get; }
    ITestLogger? Logger { get; }
    int DefaultTimeoutMs { get; }
    int ShortTimeoutMs { get; }
    int PollingIntervalMs { get; }
    
    void SetLogger(ITestLogger logger);
    void Log(string message);
    void LogError(Exception ex, string context);
    bool WaitFor(Func<bool> condition, int? timeoutMs, string description);
    string? TakeScreenshot(string name);
}
```

### IControlObject

```csharp
public interface IControlObject
{
    string AutomationId { get; }
    ITestContext? Context { get; }
    IPageObject? Page { get; }
    
    // State checks
    bool IsExists();
    bool IsVisible();
    bool IsEnabled();
    bool IsClickable();
    string GetText();
    
    // Waits
    bool WaitExists(bool expected, int? timeoutMs = null);
    bool WaitVisible(bool expected, int? timeoutMs = null);
    bool WaitEnabled(bool expected, int? timeoutMs = null);
    bool WaitClickable(int? timeoutMs = null);
    
    // Checks (throw on failure)
    void CheckExists(bool expected, int? timeoutMs = null);
    void CheckVisible(bool expected, int? timeoutMs = null);
    void CheckEnabled(bool expected, int? timeoutMs = null);
    void CheckClickable(int? timeoutMs = null);
    
    // Assertions
    void AssertExists(string? message = null);
    void AssertVisible(string? message = null);
    void AssertNotVisible(string? message = null);
    void AssertEnabled(string? message = null);
    void AssertDisabled(string? message = null);
    void AssertText(string expected, string? message = null);
    void AssertTextContains(string substring, string? message = null);
}
```

### IPageObject

```csharp
public interface IPageObject
{
    ITestContext Context { get; }
    string PageName { get; }
    
    bool IsDisplayed();
    void WaitForDisplayed(int? timeoutMs = null);
    void CheckDisplayed(int? timeoutMs = null);
    void AssertDisplayed(string? message = null);
}
```

---

## Platform Enum

Type-safe platform identification:

```csharp
public enum Platform
{
    Windows,      // WPF desktop
    WindowsMaui,  // MAUI on Windows
    Android,      // MAUI on Android
    iOS,          // MAUI on iOS
    Web           // Browser (Chrome, Firefox, Edge, Safari)
}

// Extension methods
platform.IsMobile()        // true for Android/iOS
platform.IsDesktop()       // true for Windows/WindowsMaui
platform.IsWeb()           // true for Web
platform.SupportsGestures() // true for mobile platforms
```

---

## Next Steps

- **[Architecture](03-architecture.md)** - Deep dive into component relationships
- **[Control Objects](04-control-objects.md)** - Master control patterns
- **[Page Objects](05-page-objects.md)** - Learn page encapsulation
- **[WPF Platform](07-wpf-platform.md)** - Windows desktop specifics

---

*Next: [Architecture](03-architecture.md)*
