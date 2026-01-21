# SPEC-027: Unified Driver Abstraction - Requirements

**Spec ID:** 027  
**Feature:** unified-driver-abstraction  
**Status:** Draft  
**Created:** January 21, 2026

---

## Introduction

This specification defines a unified driver abstraction layer that enables the Brinell MAUI test framework to support multiple underlying automation frameworks while sharing the same ControlObject implementations across platforms.

### Problem Statement

Currently, `srcnew/Brinell.Maui` is tightly coupled to Appium via:
- `IMauiElement` - wraps `AppiumElement` with `UnwrapElement()` escape hatch
- `IMauiDriver` - wraps `AppiumDriver` with `UnwrapDriver()` escape hatch

This creates issues for MAUI testing across platforms:

| Platform | Current Driver | Recommended Driver | Issue |
|----------|----------------|-------------------|-------|
| Windows | Appium + WinAppDriver | **FlaUI** | Appium on Windows is slower, less reliable |
| Android | Appium | Appium | ✓ Works |
| iOS | Appium | Appium | ✓ Works |

**FlaUI Benefits for Windows:**
- Native Windows UI Automation API access
- Faster element finding and interactions
- Better stability for Windows desktop apps
- No need for WinAppDriver server process

### Current Code Analysis (srcnew)

The existing implementation in `srcnew` is well-designed and should be **reused**:

**Excellent Patterns to Preserve:**
1. **Interface Hierarchy**: `IElementScope<TElement>` → `IMauiElementScope` → `IMauiScope<TScope>` provides clean abstraction layers
2. **Scope-based Element Finding**: `TryFindElement()`, `FindElement()`, `FindElements()` work within scopes
3. **Fluent Chaining**: CRTP pattern with `TSelf`/`TScope` enables elegant fluent APIs
4. **ControlObject Base Classes**: `MauiControlBase<TScope>`, `MauiClickableControlBase<TScope>` are well-factored
5. **Logging Infrastructure**: `Run()`, `RunWithElement()`, `RunAssert()` provide consistent logging
6. **Poll Pattern**: `Poll()`, `PollWithElement()` for wait conditions without arbitrary sleeps
7. **Locator System**: `Locator` class with factory methods and `LocatorStrategy` enum

**Code to Refactor:**
1. `IMauiElement.UnwrapElement()` - Escape hatch that couples to Appium
2. `IMauiDriver.UnwrapDriver()` - Escape hatch that couples to Appium
3. `MauiClickableControlBase.RightClickCore()` - Uses `UnwrapElement()` and Selenium Actions
4. `MauiClickableControlBase.HoverCore()` - Uses `UnwrapElement()` and Selenium Actions
5. `MauiClickableControlBase.LongPressCore()` - Uses `UnwrapElement()` and Selenium Actions
6. `MauiElement.ScrollIntoView()` - Uses `UnwrapDriver()` for gesture execution

### Solution

Create a **layered interface design** with generics:

```
Brinell.Core:
  IElement                    - Complete element operations (including gestures)
  IDriver<TElement>           - Generic driver with typed element returns

Brinell.Maui:
  IMauiElement : IElement     - Adds DOM access for hybrid apps
  IMauiDriver : IDriver<IMauiElement>  - Adds platform, contexts, windows
```

**Key Design Decisions:**

1. **Gestures in IElement** - `DoubleClick`, `RightClick`, `Hover`, `LongPress`, `ScrollIntoView` are universal across all UI tech (WPF, WinForms, HTML, MAUI, Stride)

2. **Generic IDriver<TElement>** - Consistent with existing `IElementScope<TElement>` pattern. Enables compile-time type safety without `new` keyword overrides

3. **No PageSource in IDriver** - Diagnostic feature moved to optional `IDiagnosticDriver` interface

4. **Locator Translation Internal** - Each driver internally translates `Locator` → framework-specific. No extension methods exposing framework types

5. **No Escape Hatches** - `IElement` and `IDriver` are complete. No `Unwrap()` methods

---

## Alignment with Product Vision

This feature supports the Brinell framework's core goals:
- **Cross-Platform Testing** - Single test code runs on Windows, iOS, and Android
- **Framework Independence** - ControlObjects don't depend on specific drivers
- **Performance** - Use the best driver for each platform (FlaUI for Windows)
- **Maintainability** - One ControlObject implementation, multiple drivers
- **No Escape Hatches** - Interfaces provide complete functionality

---

## Requirements

### REQ-027.1: Core Element Interface (IElement)

**User Story:** As a test framework developer, I want a complete element interface so that all UI operations work across all platforms (MAUI, WPF, WinForms, Blazor, Stride) without escape hatches.

#### Design Rationale

All gesture methods (`DoubleClick`, `RightClick`, `Hover`, `LongPress`, `ScrollIntoView`) belong in `IElement` because:
- They are **universal** across all UI technologies
- WPF, WinForms, HTML, MAUI, Stride all support these interactions
- Putting them in the base interface ensures consistent behavior
- Platform implementations handle the specifics internally

#### Acceptance Criteria

1. WHEN `IElement<TSelf>` is defined in `Brinell.Core` THEN it SHALL NOT reference Appium, FlaUI, Playwright, or any specific framework
2. WHEN `IElement<TSelf>` is used THEN it SHALL provide state properties: `Visible`, `Enabled`, `Selected`, `Text?`, `TagName?`
3. WHEN `IElement<TSelf>` is used THEN it SHALL provide location properties: `Location` (Point), `Size` (Size)
4. WHEN `IElement<TSelf>` is used THEN it SHALL provide all interaction actions including gestures
5. WHEN `IElement<TSelf>` is used THEN it SHALL provide attribute access: `GetAttribute(string)`
6. WHEN `IElement<TSelf>.FindElement()` is called THEN it SHALL return `TSelf` (the same element type)
7. WHEN `IElement<TSelf>.SendKeys()` is called THEN it SHALL accept `TextInputMethod` enum (Keys, Paste, SetValue)
8. `IElement<TSelf>` SHALL NOT have any `Unwrap()` or escape hatch methods

#### TextInputMethod Enum

```csharp
public enum TextInputMethod
{
    Keys,      // Type each character as keyboard events
    Paste,     // Paste from clipboard (faster)
    SetValue   // Directly set element value property (fastest)
}
```

#### IElement Interface Definition

```csharp
public interface IElement<TSelf>
    where TSelf : IElement<TSelf>
{
    // State
    bool Visible { get; }
    bool Enabled { get; }
    bool Selected { get; }
    string? Text { get; }
    string? TagName { get; }
    
    // Location
    Point Location { get; }
    Size Size { get; }
    
    // Basic Actions
    void Click();
    void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys);
    void Clear();
    
    // Gesture Actions (universal across all UI tech)
    void DoubleClick();
    void RightClick();
    void Hover();
    void LongPress(int durationMs = 1000);
    void ScrollIntoView(int timeoutMs = 5000);
    
    // Attributes
    string? GetAttribute(string name);
    
    // Child Finding (returns same element type)
    TSelf FindElement(Locator locator, int timeoutMs = 5000);
    IReadOnlyList<TSelf> FindElements(Locator locator, int timeoutMs = 0);
    bool TryFindElement(Locator locator, out TSelf? element, int timeoutMs = 0);
}
```

---

### REQ-027.2: MAUI Element Interface (IMauiElement)

**User Story:** As a MAUI test developer, I want MAUI-specific element capabilities for hybrid app scenarios and DOM access.

#### Design Rationale

`IMauiElement` extends `IElement` with MAUI/mobile-specific features:
- DOM attribute access (for hybrid WebView apps)
- Any future MAUI-specific properties

Note: Gesture methods are now in `IElement` since they're universal.

#### Acceptance Criteria

1. WHEN `IMauiElement` extends `IElement` THEN it SHALL add DOM-related methods for hybrid apps
2. WHEN `IMauiElement` is used with WebView content THEN it SHALL provide `GetDomAttribute()`, `GetDomProperty()`, `GetCssValue()`
3. `IMauiElement` SHALL NOT have any `Unwrap()` or escape hatch methods

#### IMauiElement Interface Definition

```csharp
public interface IMauiElement : IElement<IMauiElement>
{
    // DOM Access (for hybrid apps with WebView)
    string? GetDomAttribute(string name);
    string? GetDomProperty(string name);
    string? GetCssValue(string name);
}
```

---

### REQ-027.3: Core Driver Interface (IDriver<TElement>)

**User Story:** As a test framework developer, I want a generic driver interface so that element finding returns the correct typed elements without casting.

#### Design Rationale

**Why Generic `IDriver<TElement>`?**

The existing codebase already uses this pattern:
- `IElementScope<TElement>` - generic element scope
- `ITestContext<TElement> : IElementScope<TElement>` - generic test context
- `IMauiElementScope : IElementScope<IMauiElement>` - fixes the type

Using `IDriver<TElement>` is consistent with existing design:
- `IDriver<IElement>` for base usage
- `IMauiDriver : IDriver<IMauiElement>` - no `new` keyword overrides needed
- Compile-time type safety, no runtime casting

**Why Remove PageSource?**

`PageSource` is a debugging/diagnostic feature, not a core driver operation:
- Not all platforms support it equally (FlaUI would need to serialize UI tree)
- It's never used in normal test flows
- Move to a separate `IDiagnosticDriver` interface or make optional

#### Acceptance Criteria

1. WHEN `IDriver<TElement>` is defined THEN it SHALL be generic over the element type
2. WHEN `IDriver<TElement>` is used THEN `FindElement()` SHALL return `TElement`
3. WHEN `IDriver<TElement>` is used THEN it SHALL provide session management: `Quit()`, `Close()`
4. WHEN `IDriver<TElement>` is used THEN it SHALL provide screenshots: `GetScreenshot()` returning `byte[]`
5. `IDriver<TElement>` SHALL NOT include `PageSource` (moved to diagnostics)
6. `IDriver<TElement>` SHALL NOT have any `Unwrap()` or escape hatch methods

#### IDriver Interface Definition

```csharp
public interface IDriver<TElement> : IDisposable
    where TElement : IElement<TElement>
{
    // Element Finding (returns typed elements)
    TElement FindElement(Locator locator, int timeoutMs = 5000);
    IReadOnlyList<TElement> FindElements(Locator locator, int timeoutMs = 0);
    bool TryFindElement(Locator locator, out TElement? element, int timeoutMs = 0);
    
    // Session
    void Quit();
    void Close();
    
    // Screenshots
    byte[] GetScreenshot();
}

// Optional diagnostic interface for debugging
public interface IDiagnosticDriver
{
    string GetPageSource();
    string GetAutomationTree();
}
```

---

### REQ-027.4: MAUI Driver Interface (IMauiDriver)

**User Story:** As a MAUI test developer, I want MAUI-specific driver capabilities for platform detection and hybrid app context switching.

#### Acceptance Criteria

1. WHEN `IMauiDriver` extends `IDriver<IMauiElement>` THEN `FindElement()` naturally returns `IMauiElement`
2. WHEN `IMauiDriver` is used THEN it SHALL expose `Platform` (MauiPlatform enum)
3. WHEN `IMauiDriver` is used THEN it SHALL provide context switching: `Context`, `Contexts` for hybrid apps
4. WHEN `IMauiDriver` is used THEN it SHALL provide window management: `CurrentWindowHandle`, `WindowHandles`
5. `IMauiDriver` MAY optionally implement `IDiagnosticDriver` for debugging

#### IMauiDriver Interface Definition

```csharp
public interface IMauiDriver : IDriver<IMauiElement>
{
    // Platform
    MauiPlatform Platform { get; }
    
    // Context Switching (hybrid apps)
    string Context { get; set; }
    IReadOnlyCollection<string> Contexts { get; }
    
    // Window Management
    string CurrentWindowHandle { get; }
    IReadOnlyCollection<string> WindowHandles { get; }
}
```

---

### REQ-027.5: Locator Translation (Internal to Driver)

**User Story:** As a test framework developer, I want locator translation to be encapsulated inside driver implementations so that the `Locator` class remains framework-agnostic.

#### Design Rationale

**Problem with Current Design:**
The current `LocatorExtensions.ToBy()` method:
- Is in `Brinell.Maui.Extensions` but references Selenium/Appium `By` class
- Exposes framework-specific types (`By`) to callers
- Requires callers to know about platform differences

**Best Long-Term Solution:**
Locator translation should be **internal to each driver implementation**:

```
Locator (Brinell.Core) → IDriver.FindElement(Locator) → [internal translation] → Framework-specific search
```

- `Locator` stays a pure value object in `Brinell.Core`
- Each driver internally translates `Locator` to its framework's locator type
- No extension methods needed - translation is encapsulated
- Different drivers can optimize translation differently

#### Acceptance Criteria

1. WHEN `Locator` is defined in `Brinell.Core` THEN it SHALL remain framework-agnostic (no Selenium/Appium/FlaUI refs)
2. WHEN `IDriver.FindElement(Locator)` is called THEN the driver implementation SHALL internally translate to framework-specific locator
3. WHEN `AppiumMauiDriver` translates a locator THEN it SHALL use `By.Id()`, `MobileBy.AccessibilityId()`, etc. internally
4. WHEN `FlaUIDriver` translates a locator THEN it SHALL use FlaUI's `ConditionFactory` internally
5. The `LocatorExtensions.ToBy()` method SHALL be removed or made internal to Appium implementation
6. IF a locator strategy is not supported THEN the driver SHALL throw `LocatorNotSupportedException`

#### Implementation Pattern

```csharp
// Brinell.Core - Locator remains pure value object
public sealed class Locator
{
    public LocatorStrategy Strategy { get; }
    public string Value { get; }
    // No ToBy() or framework-specific methods
}

// Brinell.Maui.Appium - Internal translation
internal sealed class AppiumMauiDriver : IMauiDriver
{
    private readonly AppiumDriver _driver;
    private readonly MauiPlatform _platform;
    
    public IMauiElement FindElement(Locator locator)
    {
        var by = TranslateLocator(locator);
        var element = _driver.FindElement(by);
        return new AppiumMauiElement(element, this);
    }
    
    private By TranslateLocator(Locator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => _platform switch
            {
                MauiPlatform.Android => By.Id(locator.Value),
                _ => MobileBy.AccessibilityId(locator.Value)
            },
            LocatorStrategy.XPath => By.XPath(locator.Value),
            LocatorStrategy.Name => By.Name(locator.Value),
            // ... etc
            _ => throw new LocatorNotSupportedException(locator.Strategy, "Appium")
        };
    }
}

// Brinell.Maui.FlaUI - Different internal translation
internal sealed class FlaUIDriver : IMauiDriver
{
    private readonly Application _app;
    private readonly ConditionFactory _cf;
    
    public IMauiElement FindElement(Locator locator)
    {
        var condition = TranslateLocator(locator);
        var element = _app.GetMainWindow(_automation).FindFirstDescendant(condition);
        return new FlaUIElement(element, this);
    }
    
    private ConditionBase TranslateLocator(Locator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => _cf.ByAutomationId(locator.Value),
            LocatorStrategy.Name => _cf.ByName(locator.Value),
            LocatorStrategy.ClassName => _cf.ByClassName(locator.Value),
            // ... etc
            _ => throw new LocatorNotSupportedException(locator.Strategy, "FlaUI")
        };
    }
}
```

---

### REQ-027.6: FlaUI Driver Implementation

**User Story:** As a Windows test runner, I want a FlaUI driver implementation so that Windows MAUI tests run faster and more reliably.

#### Acceptance Criteria

1. WHEN `FlaUIDriver` is created THEN it SHALL connect to a Windows application via FlaUI.UIA3
2. WHEN `FlaUIDriver.FindElement()` is called THEN it SHALL use FlaUI's `AutomationElement` internally
3. WHEN `FlaUIElement.Click()` is called THEN it SHALL use FlaUI's `InvokePattern` or `Click()` method
4. WHEN `FlaUIElement.SendKeys()` is called THEN it SHALL use FlaUI's `Keyboard.Type()` method
5. WHEN `FlaUIElement.ScrollIntoView()` is called THEN it SHALL use FlaUI's `ScrollPattern`
6. WHEN `FlaUIElement.RightClick()` is called THEN it SHALL use FlaUI's `RightClick()` method
7. `FlaUIDriver` and `FlaUIElement` SHALL implement `IMauiDriver` and `IMauiElement` respectively

---

### REQ-027.7: Appium Driver Implementation

**User Story:** As an iOS/Android test runner, I want an Appium driver implementation that implements the new interfaces.

#### Acceptance Criteria

1. WHEN `AppiumMauiDriver` is created THEN it SHALL connect to an Appium server
2. WHEN `AppiumMauiElement.ScrollIntoView()` is called on Android THEN it SHALL use `mobile: scrollGesture`
3. WHEN `AppiumMauiElement.ScrollIntoView()` is called on iOS THEN it SHALL use `mobile: scroll`
4. WHEN `AppiumMauiElement.LongPress()` is called THEN it SHALL use Appium Actions API internally
5. WHEN `AppiumMauiElement.RightClick()` is called THEN it SHALL use Appium Actions API for context click
6. `AppiumMauiDriver` and `AppiumMauiElement` SHALL implement `IMauiDriver` and `IMauiElement` respectively

---

### REQ-027.8: Unified ControlObject Base (Refactor Existing)

**User Story:** As a test framework developer, I want the existing ControlObject base classes to use the new interfaces so that they work with both FlaUI and Appium.

#### Acceptance Criteria

1. WHEN `MauiControlBase<TScope>` is refactored THEN it SHALL use `IMauiElement` instead of accessing unwrapped elements
2. WHEN `MauiClickableControlBase<TScope>.RightClickCore()` is called THEN it SHALL use `IMauiElement.RightClick()` not Selenium Actions
3. WHEN `MauiClickableControlBase<TScope>.HoverCore()` is called THEN it SHALL use `IMauiElement.Hover()` not Selenium Actions
4. WHEN `MauiClickableControlBase<TScope>.LongPressCore()` is called THEN it SHALL use `IMauiElement.LongPress()` not Selenium Actions
5. ALL existing control classes (`MauiButtonControl`, `MauiEntryControl`, etc.) SHALL continue to work without modification

---

### REQ-027.9: Test Context Platform Selection

**User Story:** As a test runner, I want the test context to automatically select the right driver based on platform.

#### Acceptance Criteria

1. WHEN `MauiTestContext` is created with `MauiPlatform.Windows` THEN the system SHALL instantiate `FlaUIDriver`
2. WHEN `MauiTestContext` is created with `MauiPlatform.Android` THEN the system SHALL instantiate `AppiumMauiDriver`
3. WHEN `MauiTestContext` is created with `MauiPlatform.iOS` THEN the system SHALL instantiate `AppiumMauiDriver`
4. WHEN driver selection is needed THEN the system SHALL support explicit driver configuration via options

---

## Non-Functional Requirements

### NFR-027.1: Code Architecture and Modularity

- **Layered Interfaces**: `IElement` → `IMauiElement`, `IDriver` → `IMauiDriver`
- **No Escape Hatches**: Interfaces provide complete functionality without `Unwrap()` methods
- **Reuse Existing Code**: Preserve `MauiControlBase`, `MauiPageObjectBase`, scope interfaces
- **Package Structure**:
  - `Brinell.Core` - `IElement`, `IDriver`, `Locator` (no framework refs)
  - `Brinell.Maui` - `IMauiElement`, `IMauiDriver`, ControlObjects, PageObjects
  - `Brinell.Maui.Appium` - `AppiumMauiDriver`, `AppiumMauiElement`
  - `Brinell.Maui.FlaUI` - `FlaUIDriver`, `FlaUIElement`

### NFR-027.2: Performance

- FlaUI driver on Windows SHALL be at least as fast as current Appium implementation
- No runtime type checking or casting in hot paths
- Driver initialization SHALL complete within 5 seconds for local apps

### NFR-027.3: Logging

- All driver operations SHALL integrate with existing `ITestLogger`
- Driver selection and initialization SHALL be logged

### NFR-027.4: Testing

- Unit tests SHALL mock `IMauiElement` and `IMauiDriver` (no real automation)
- Integration tests in `testsnew/Brinell.Maui.UITests` SHALL work with both drivers
- Platform-specific tests SHALL be tagged appropriately

---

## Scope

### In Scope

- `IElement` interface in `Brinell.Core`
- `IDriver` interface in `Brinell.Core`
- `IMauiElement` interface extending `IElement` in `Brinell.Maui`
- `IMauiDriver` interface extending `IDriver` in `Brinell.Maui`
- FlaUI implementation for Windows (`Brinell.Maui.FlaUI` - new project)
- Appium implementation refactored (`Brinell.Maui.Appium` - new project or inline)
- Refactoring `srcnew/Brinell.Maui` to remove `Unwrap()` usage
- Updating `testsnew/Brinell.Maui.UITests` to verify both drivers

### Out of Scope

- Backward compatibility with old interfaces (clean break)
- Blazor driver abstraction (already uses Playwright)
- WinForms/WPF driver abstraction (separate specification)
- Stride driver abstraction (game engine specific)
- New control implementations (covered by SPEC-024)

---

## Dependencies

- SPEC-024: MAUI Control Objects (provides controls to refactor)
- SPEC-015: Scope-aware fluent chaining (must continue to work)
- FlaUI NuGet packages: `FlaUI.Core`, `FlaUI.UIA3`
- Appium NuGet packages: `Appium.WebDriver`

---

## Implementation Strategy

### Phase 1: Interface Definition

1. Define `IElement` in `Brinell.Core.Interfaces` with all gestures
2. Define `IDriver<TElement>` in `Brinell.Core.Interfaces` (generic)
3. Define `IDiagnosticDriver` for optional debugging features
4. Update `IMauiElement` to extend `IElement` (add only DOM methods)
5. Update `IMauiDriver` to extend `IDriver<IMauiElement>` 
6. Remove `UnwrapElement()` and `UnwrapDriver()` from all interfaces

### Phase 2: Appium Implementation Update

1. Move gesture implementations (`RightClick`, `Hover`, `LongPress`, `ScrollIntoView`) to `AppiumMauiElement`
2. Make locator translation internal to `AppiumMauiDriver.TranslateLocator()`
3. Remove or make internal `LocatorExtensions.ToBy()` 
4. Move Selenium Actions code from `MauiClickableControlBase` to `AppiumMauiElement`
5. Verify all `testsnew/Brinell.Maui.UITests` pass with Android

### Phase 3: FlaUI Implementation

1. Create `Brinell.Maui.FlaUI` project
2. Implement `FlaUIDriver : IMauiDriver` with internal locator translation
3. Implement `FlaUIElement : IMauiElement` with FlaUI-native gestures
4. Add Windows-specific tests

### Phase 4: ControlObject Refactoring

1. Update `MauiClickableControlBase` to use `IMauiElement.RightClick()` etc.
2. Simplify - ControlObjects just call interface methods, no framework knowledge
3. Verify all controls work with both drivers

### Phase 5: Test Context Factory

1. Update `MauiTestContext` to accept driver factory/instance
2. Implement platform-based auto-selection (Windows→FlaUI, Mobile→Appium)
3. Update `MauiTestFixtureBase` to support both drivers

---

## Code Locations

| Component | Current Location | Action |
|-----------|-----------------|--------|
| `IElement` | New | Create in `Brinell.Core/Interfaces/` |
| `IDriver` | New | Create in `Brinell.Core/Interfaces/` |
| `IMauiElement` | `Brinell.Maui/Interfaces/` | Refactor to extend `IElement` |
| `IMauiDriver` | `Brinell.Maui/Interfaces/` | Refactor to extend `IDriver` |
| `MauiElement` | `Brinell.Maui/Wrappers/` | Refactor, add gesture methods |
| `MauiDriver` | `Brinell.Maui/Wrappers/` | Refactor |
| `FlaUIDriver` | New | Create in `Brinell.Maui.FlaUI/` |
| `FlaUIElement` | New | Create in `Brinell.Maui.FlaUI/` |
| `MauiControlBase` | `Brinell.Maui/Controls/` | Update to use new interfaces |
| `MauiClickableControlBase` | `Brinell.Maui/Controls/` | Remove `Unwrap()` calls |

---

## References

- [SPEC-024-maui-control-objects](./024-maui-control-objects/requirements.spc.spx.md) - Control implementations
- [FlaUI Documentation](https://github.com/FlaUI/FlaUI) - Windows UI Automation
- [Appium Documentation](https://appium.io/) - Mobile automation
