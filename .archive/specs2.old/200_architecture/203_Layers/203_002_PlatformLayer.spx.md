# 203.002 Platform Layer

**Block Type:** LYR (Layer)  
**ID:** 203.002  
**Title:** Platform Layer Definition  
**Status:** Draft  
**Version:** 1.0

---

## 1. Overview

The Platform layer contains **technology-specific implementations** of the Core interfaces. Each platform package (MAUI, Blazor, WPF) provides concrete control objects that work with a specific UI technology.

### Layer Identity

- **Packages:** `Brinell.MAUI`, `Brinell.Blazor`, `Brinell.WPF`
- **Dependencies:** `Brinell.Core` + automation SDK
- **Dependents:** Test projects

---

## 2. Purpose

The Platform layer provides:

1. **Concrete Implementations** — Real control objects that interact with UI elements
2. **Base Classes** — Reusable functionality for control implementations
3. **Driver Adapters** — Wrappers around automation library specifics
4. **Platform Utilities** — Helpers specific to each technology

---

## 3. Platform Packages

### 3.1 Brinell.MAUI

For .NET MAUI applications on all platforms (Android, iOS, Windows, Mac).

```
Brinell.MAUI/
├── Controls/
│   ├── ControlBase.cs              # Base for all MAUI controls
│   ├── ButtonControl.cs
│   ├── EntryControl.cs
│   ├── LabelControl.cs
│   ├── CheckBoxControl.cs
│   └── ... (all MAUI controls)
├── Base/
│   ├── ClickableControlBase.cs
│   ├── TextControlBase.cs
│   ├── EditableTextControlBase.cs
│   ├── ToggleControlBase.cs
│   └── ... (capability base classes)
├── Context/
│   ├── IMauiTestContext.cs         # Platform interface extending ITestContext
│   ├── AppiumTestContext.cs        # Implements IMauiTestContext
│   └── MauiPageBase.cs
└── Utilities/
    ├── ElementFinder.cs
    └── WaitHelper.cs
```

**Automation SDK:** Appium.WebDriver

### 3.2 Brinell.Blazor

For Blazor applications (Server and WebAssembly).

```
Brinell.Blazor/
├── Controls/
│   ├── ControlBase.cs              # Base for all Blazor controls
│   ├── ButtonControl.cs
│   ├── InputControl.cs
│   ├── SelectControl.cs
│   └── ... (all HTML/Blazor controls)
├── Base/
│   ├── ClickableControlBase.cs
│   ├── TextControlBase.cs
│   └── ... (capability base classes)
├── Context/
│   ├── IBlazorTestContext.cs       # Platform interface extending ITestContext
│   ├── SeleniumTestContext.cs      # Implements IBlazorTestContext
│   └── BlazorPageBase.cs
└── Utilities/
    ├── ElementFinder.cs
    └── WaitHelper.cs
```

**Automation SDK:** Selenium.WebDriver (or Playwright)

### 3.3 Brinell.WPF

For WPF desktop applications.

```
Brinell.WPF/
├── Controls/
│   ├── ControlBase.cs
│   └── ... (WPF controls)
├── Context/
│   ├── IWpfTestContext.cs          # Platform interface extending ITestContext
│   └── WpfTestContext.cs           # Implements IWpfTestContext
└── ...
```

**Automation SDK:** Appium.WebDriver (WinAppDriver)

---

## 4. Base Class Hierarchy

Each platform implements a parallel base class hierarchy:

```
ControlBase                         # Implements IControlObject
├── ClickableControlBase            # Implements IClickableControlObject
├── TextControlBase                 # Implements ITextControlObject
│   └── EditableTextControlBase     # Implements IEditableTextControlObject
├── ToggleControlBase               # Implements IToggleControlObject
├── SelectorControlBase             # Implements ISelectorControlObject
├── RangeControlBase                # Implements IRangeControlObject
├── ContainerControlBase            # Implements IContainerControlObject
├── ItemsControlBase                # Implements IItemsControlObject
└── ScrollableControlBase           # Implements IScrollableControlObject
```

**Note:** The complete base class hierarchy mirrors the interface hierarchy defined in specifications.

---

## 5. Design Rules

### 5.1 Implement Core Interfaces

All control classes must implement interfaces from Core:

```csharp
// ✓ Correct: Implements Core interface
public class ButtonControl : ClickableControlBase, IClickableControlObject
{
    // Implementation
}
```

### 5.2 No Cross-Platform Dependencies

Platform packages cannot depend on each other:

```
✓ Brinell.MAUI → Brinell.Core
✓ Brinell.Blazor → Brinell.Core
✗ Brinell.MAUI → Brinell.Blazor  // FORBIDDEN
```

### 5.3 Wrap Automation Libraries

Automation library types should not leak into public API:

```csharp
// ✗ Bad: Exposes Appium type
public AppiumElement GetElement();

// ✓ Good: Internal only
internal AppiumElement GetElement();
public bool IsExists();  // Public API uses primitives
```

### 5.4 Consistent Naming

Control names should match platform conventions:

| MAUI Control | MAUI Class | Blazor Control | Blazor Class |
|--------------|------------|----------------|--------------|
| Button | ButtonControl | button | ButtonControl |
| Entry | EntryControl | input[type=text] | InputControl |
| Label | LabelControl | span/label | LabelControl |
| CheckBox | CheckBoxControl | input[type=checkbox] | CheckBoxControl |
| Picker | PickerControl | select | SelectControl |
| Slider | SliderControl | input[type=range] | RangeControl |

---

## 6. Package Dependencies

### Brinell.MAUI

```
Brinell.MAUI
├── Brinell.Core
└── Appium.WebDriver
```

### Brinell.Blazor

```
Brinell.Blazor
├── Brinell.Core
└── Selenium.WebDriver (or Microsoft.Playwright)
```

---

## 7. Namespace Structure

### MAUI

```
Brinell.MAUI
├── Brinell.MAUI.Controls         # Concrete control implementations
├── Brinell.MAUI.Base             # Base classes
├── Brinell.MAUI.Context          # IMauiTestContext, TestContext, PageBase
└── Brinell.MAUI.Utilities        # Internal helpers
```

### Blazor

```
Brinell.Blazor
├── Brinell.Blazor.Controls       # Concrete control implementations
├── Brinell.Blazor.Base           # Base classes
├── Brinell.Blazor.Context        # IBlazorTestContext, TestContext, PageBase
└── Brinell.Blazor.Utilities      # Internal helpers
```

---

## 8. Control Implementation Pattern

All controls follow this pattern. Base classes take the platform-specific interface (e.g., `IMauiTestContext`) rather than casting to concrete types:

```csharp
public class EntryControl : EditableTextControlBase, IEditableTextControlObject
{
    public EntryControl(IMauiTestContext context, Locator locator) 
        : base(context, locator)
    {
    }

    // Interface implementation uses base class element finding
    public override void Enter(string text)
    {
        var element = (AppiumElement)FindElement();
        element.Clear();
        element.SendKeys(text);
    }

    public override void Clear()
    {
        var element = (AppiumElement)FindElement();
        element.Clear();
    }

    public override string GetText()
    {
        var element = (AppiumElement)FindElement();
        return element.Text;
    }
}
```

---

## 9. Validation Rules

A platform package is valid when:

- [ ] Implements all required Core interfaces
- [ ] Defines a platform-specific TestContext interface (e.g., IMauiTestContext)
- [ ] TestContext implementation provides type-safe element finding
- [ ] Base classes use the platform-specific interface, not concrete casts
- [ ] Does not reference other platform packages
- [ ] Does not expose automation library types in public API
- [ ] Has parallel base class hierarchy matching Core interfaces
- [ ] All controls have consistent naming

---

## Related Documents

- [Core Layer](203_001_CoreLayer.spx.md)
- [Technology Layer](203_003_TechnologyLayer.spx.md)
- [ADR-003 Platform Separation](../202_Decisions/202_003_PlatformSeparation.spx.md)
- [ADR-004 Control Hierarchy](../202_Decisions/202_004_ControlHierarchy.spx.md)
