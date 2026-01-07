# 211.001 Interfaces Module

**Block Type:** MOD (Module)
**ID:** 211.001
**Title:** Interfaces Module Definition
**Status:** Draft
**Version:** 1.0

---

## 1. Overview

The Interfaces module contains all control and page interfaces defined in `Brinell.Core`. These interfaces are the **contracts** that all platform implementations must fulfill. They define what controls can do, not how they do it.

> **Note:** Code snippets in this document are illustrative examples showing the intended patterns and API design. Final implementations may vary in details.

### Module Identity

- **Package:** `Brinell.Core`
- **Namespace:** `Brinell.Core.Interfaces`
- **Dependencies:** None
- **Consumers:** All platform packages

---

## 2. Purpose

The Interfaces module provides:

1. **Contracts** — Define what each control capability provides
2. **Abstraction** — Hide platform-specific implementation details
3. **Testability** — Enable mocking and unit testing
4. **Documentation** — Interfaces serve as living API documentation

---

## 3. Interface Categories

### 3.1 Base Interface

The foundation for all controls. Every control in the framework implements `IControlObject`, which provides identity, state checking, waiting, assertions, and basic text retrieval.

```csharp
public interface IControlObject
{
    // Identity - how to find and reference the control
    Locator Locator { get; }      // The locator used to find this control
    IPageObject? Page { get; }    // The page containing this control (optional)
  
    // State - query current control state (immediate, no waiting)
    bool IsExists();              // Returns true if element is in DOM/visual tree
    bool IsVisible();             // Returns true if visible
    bool IsEnabled();             // Returns true if enabled
  
    // Waiting - poll until condition or timeout; null expected = skip
    bool WaitExists(bool? expected, int? timeoutMs = null);   // Wait for existence
    bool WaitVisible(bool? expected, int? timeoutMs = null);  // Wait for visibility
    bool WaitEnabled(bool? expected, int? timeoutMs = null);  // Wait for enabled state
  
    // Assertions - throw if condition not met; null expected = skip
    void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
    void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
  
    // Text - retrieve and verify content; null expected = skip
    string GetText(int? timeoutMs = null);
    void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    string? GetAttribute(string name);
}
```

### 3.2 Capability Interfaces

Capability interfaces extend `IControlObject` to add specific functionality. Each interface represents a single capability (click, text input, toggle, etc.) that a control may have. Controls implement multiple interfaces to express all their capabilities.

| Interface                  | Capability      | Key Methods                                    |
| -------------------------- | --------------- | ---------------------------------------------- |
| IClickableControlObject    | Click actions   | Click(), DoubleClick(), RightClick()           |
| ITextControlObject         | Text display    | GetText(), AssertText(), AssertTextContains()  |
| IEditableTextControlObject | Text input      | Enter(), Clear(), SetText()                    |
| IToggleControlObject       | On/off state    | IsChecked(), Toggle(), SetChecked()            |
| ISelectorControlObject     | Selection       | SelectByText(), SelectByIndex(), GetSelectedText() |
| IRangeControlObject        | Numeric range   | GetValue(), SetValue()                         |
| IItemsControlObject        | Item collection | GetItems(), GetItemCount()                     |
| IContainerControlObject    | Child scoping   | FindChild(), FindChildren()                    |
| IScrollableControlObject   | Scrolling       | ScrollTo(), ScrollToEnd()                      |

### 3.3 Structural Interfaces

Structural interfaces organize the test environment. `IPageObject` represents a screen or page containing controls, while `ITestContext` manages the driver lifecycle and provides access to configuration.

```csharp
/// <summary>
/// Represents a page or screen in the application under test.
/// Controls are created via 'new' pattern, not factory methods.
/// </summary>
public interface IPageObject
{
    string Name { get; }          // Page name for logging and identification
  
    // Page state - nullable expected for skip-on-null pattern
    bool IsLoaded(int? timeoutMs = null);        // Check if page is loaded
    bool WaitLoaded(bool? expected, int? timeoutMs = null);   // Wait for loaded state
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
  
    // Title verification
    string GetTitle(int? timeoutMs = null);      // Get page title
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
  
    // Control existence helpers (for page-level queries)
    bool ControlExists(Locator locator, int? timeoutMs = null);
    bool WaitControlExists(Locator locator, bool? expected, int? timeoutMs = null);
  
    // Page operations
    void TakeScreenshot(string? filename, int? timeoutMs = null);
    void ScrollToControl(Locator? locator, int? timeoutMs = null);
}

/// <summary>
/// Manages the test execution context including driver, configuration, and state.
/// Implements IDisposable for proper cleanup.
/// Does NOT track current page - controls receive page via constructor.
/// </summary>
public interface ITestContext : IDisposable
{
    // Configuration - settings for timeouts, logging, etc.
    TimeoutSettings Timeouts { get; }  // Timeout configuration
    ITestLogger Logger { get; }        // Test logger for diagnostics
  
    // Navigation - move between pages/URLs
    void NavigateTo(string destination);  // Navigate to destination
    void NavigateBack();                  // Go back in history
    void Refresh();                       // Refresh current page
  
    // Screenshots - capture visual state
    byte[] TakeScreenshot();              // Capture as byte array
    void SaveScreenshot(string path);     // Save to file
  
    // App state
    void ResetAppState();                 // Reset application to initial state
}
```

### 3.4 Platform-Specific Test Context Interfaces

Each platform extends `ITestContext` with technology-specific capabilities. This allows base classes to use the interface type instead of casting to concrete classes, improving testability and following the Interface Segregation Principle.

```csharp
// MAUI/Appium - defined in Brinell.Maui
public interface IMauiTestContext : ITestContext
{
    // Driver access for advanced scenarios
    AppiumDriver Driver { get; }
    
    // Appium-specific element finding
    AppiumElement FindElement(Locator locator);
    AppiumElement? TryFindElement(Locator locator);
    IReadOnlyList<AppiumElement> FindElements(Locator locator);
}

// Blazor/Selenium - defined in Brinell.Blazor
public interface IBlazorTestContext : ITestContext
{
    // Driver access for advanced scenarios
    IWebDriver Driver { get; }
    
    // Base URL for relative navigation
    string BaseUrl { get; }
    
    // Selenium-specific element finding
    IWebElement FindElement(Locator locator);
    IWebElement? TryFindElement(Locator locator);
    IReadOnlyList<IWebElement> FindElements(Locator locator);
}

// WPF - defined in Brinell.Wpf
public interface IWpfTestContext : ITestContext
{
    // WPF-specific element finding
    AutomationElement FindElement(Locator locator);
    AutomationElement? TryFindElement(Locator locator);
    IReadOnlyList<AutomationElement> FindElements(Locator locator);
}
```

**Design Note:** Platform-specific interfaces are defined in their respective platform packages (`Brinell.MAUI`, `Brinell.Blazor`, `Brinell.WPF`), not in `Brinell.Core`. This keeps the Core package free of technology dependencies while still enabling interface-based programming in platform code.

---

## 4. Interface Design Patterns

Consistent patterns make the API predictable and easy to learn. All interfaces follow these conventions for method naming, parameters, and return types.

### 4.1 Method Naming Convention

Method prefixes indicate the operation type and expected behavior:

| Pattern | Example                         | Purpose                   |
| ------- | ------------------------------- | ------------------------- |
| Is*     | IsExists(), IsVisible()         | State query, returns bool or bool? |
| Get*    | GetText(), GetValue()           | Value retrieval, may return null |
| Set*    | SetText(), SetValue()           | Value assignment          |
| Wait*   | WaitExists(), WaitVisible()     | Polling with timeout, returns success |
| Assert* | AssertExists(), AssertVisible() | Throws if condition not met |

### 4.2 Parameter Patterns

Parameters use nullable types for optional values and the skip-on-null pattern. This allows callers to use simple invocations while having full control when needed.

```csharp
// Nullable expected: null means skip the operation entirely
bool WaitExists(bool? expected, int? timeoutMs = null);
void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
void AssertText(string? expected, string? message = null, int? timeoutMs = null);

// Timeout: null means use default from TimeoutSettings
bool WaitVisible(bool? expected, int? timeoutMs = null);

// Input values: null means skip the action
void Enter(string? text, int? timeoutMs = null);
void SetValue(double? value, int? timeoutMs = null);
void SelectByText(string? text, int? timeoutMs = null);
```

**Skip-on-null behavior:**
- When `expected` or input value is null, method returns immediately
- No action performed, no logging, no exceptions
- Enables conditional operations without explicit null checks

### 4.3 Return Type Patterns

Return types indicate what the caller receives and how to handle missing values:

| Return Type          | Usage                               |
| -------------------- | ----------------------------------- |
| bool                 | Definitive state (IsExists)         |
| bool?                | State that depends on existence (IsVisible returns null if not exists) |
| string?              | Text content (null if not available) |
| void                 | Actions and assertions              |
| T                    | Generic control retrieval           |
| IReadOnlyList`<T>` | Collections (never null, may be empty) |

---

## 5. Interface Hierarchy

The interface hierarchy follows a capability-based design. `IControlObject` is the root, and each capability interface adds specific methods. Some interfaces extend others (e.g., `IEditableTextControlObject` extends `ITextControlObject`).

```
IControlObject (base)
│
├── IClickableControlObject
│   └── Click, DoubleClick, RightClick, LongPress
│
├── ITextControlObject
│   ├── GetText, AssertText, AssertTextContains
│   └── IEditableTextControlObject
│       └── Enter, Clear, SetText
│
├── IToggleControlObject
│   └── IsChecked, Toggle, SetChecked, Check, Uncheck
│
├── ISelectorControlObject
│   └── SelectByText, SelectByIndex, GetSelectedText, GetSelectedIndex, GetItemTexts
│
├── IRangeControlObject
│   └── GetValue, SetValue, GetMinimum, GetMaximum
│
├── IItemsControlObject
│   └── GetItems, GetItemCount, GetItemAt, FindItem
│
├── IContainerControlObject
│   └── FindChild, FindChildren, GetChildCount
│
└── IScrollableControlObject
    └── ScrollTo, ScrollToTop, ScrollToEnd
```

**Note:** The complete interface catalog with all methods is defined in specifications.

---

## 6. Namespace Organization

All interfaces reside in `Brinell.Core.Interfaces`. Each interface has its own file for clarity and to enable fine-grained source control history.

```
Brinell.Core.Interfaces/
├── IControlObject.cs
├── IClickableControlObject.cs
├── ITextControlObject.cs
├── IEditableTextControlObject.cs
├── IToggleControlObject.cs
├── ISelectorControlObject.cs
├── IRangeControlObject.cs
├── IItemsControlObject.cs
├── IContainerControlObject.cs
├── IScrollableControlObject.cs
├── IPageObject.cs
└── ITestContext.cs
```

---

## 7. Validation Rules

The Interfaces module is valid when:

- [ ] All interfaces are in `Brinell.Core.Interfaces` namespace
- [ ] No interface references platform-specific types
- [ ] All capability interfaces extend IControlObject
- [ ] Method signatures follow naming conventions
- [ ] Optional parameters use nullable types
- [ ] No implementation code in interfaces

---

## Related Documents

- [Core Layer](../203_Layers/203_001_CoreLayer.spx.md)
- [ADR-002 Interface-First](../202_Decisions/202_002_InterfaceFirst.spx.md)
- [FR-103 Interface Hierarchy](../../100_requirements/120_functional/120_103_InterfaceHierarchy.spx.md)
- [Base Classes Module](211_002_BaseClasses.spx.md)
