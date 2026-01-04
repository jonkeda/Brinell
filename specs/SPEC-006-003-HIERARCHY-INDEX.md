# SPEC-006-003: Control Hierarchy Design

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026

---

## Overview

This document defines the control class hierarchy for MAUI and Blazor implementations. The hierarchy provides:

1. **Base classes with virtual methods** - for platform-specific overrides
2. **String constructors** - for convenient PageObject usage
3. **Logging integration** - for test debugging
4. **Consistent patterns** - across MAUI and Blazor

---

## Documents

| Document | Description |
|----------|-------------|
| [SPEC-006-003-HIERARCHY-MAUI](SPEC-006-003-HIERARCHY-MAUI.md) | MAUI class hierarchy |
| [SPEC-006-003-HIERARCHY-BLAZOR](SPEC-006-003-HIERARCHY-BLAZOR.md) | Blazor async hierarchy |

---

## Design Principles

### 1. Virtual Methods for Override

All action methods (Click, Enter, etc.) are `virtual` to allow:
- Platform-specific customization
- Control-specific behavior overrides
- Test framework extensions

### 2. String Constructor Convenience

All controls support simple string construction:
```csharp
// Simple - uses AutomationId (MAUI) or TestId (Blazor)
var button = new ButtonControl(context, "SubmitButton", page);

// Full - explicit locator
var button = new ButtonControl(context, By.XPath("//button[@type='submit']"), page);
```

### 3. Logging at Every Level

All operations log via `Log()`:
```csharp
public virtual void Click(int? timeoutMs = null)
{
    Log("Click()");  // Always log
    CheckVisible(true, timeoutMs);
    CheckEnabled(true, timeoutMs);
    FindElementRequired(timeoutMs).Click();
}
```

### 4. No Factory Pattern

Controls are instantiated with `new`:
```csharp
// PageObject pattern
public class LoginPage : PageObjectBase
{
    public ButtonControl SubmitButton => new(Context, "SubmitBtn", this);
    public EntryControl UsernameEntry => new(Context, "Username", this);
}
```

---

## Class Hierarchy Summary

### MAUI (Sync)

```
ControlObjectBase
├── IsExists, IsVisible, IsEnabled, GetText
├── WaitExists, WaitVisible, WaitEnabled
├── CheckExists, CheckVisible, CheckEnabled
├── AssertExists, AssertVisible, AssertEnabled
└── Log()

ClickableControlBase : ControlObjectBase
├── virtual Click()
├── virtual DoubleClick()
├── virtual RightClick()
├── virtual Hover()
└── virtual LongPress()

TextControlBase : ClickableControlBase
├── virtual IsFocused(), Focus(), Blur()
├── virtual Enter()
├── virtual Clear()
├── virtual ClearAndEnter()
├── virtual Append()
└── virtual IsReadOnly(), GetTextLength()

ButtonControl : ClickableControlBase
LabelControl : ControlObjectBase
EntryControl : TextControlBase
EditorControl : TextControlBase
```

### Blazor (Async)

```
AsyncControlObjectBase
├── IsExistsAsync, IsVisibleAsync, IsEnabledAsync, GetTextAsync
├── WaitExistsAsync, WaitVisibleAsync, WaitEnabledAsync
├── CheckExistsAsync, CheckVisibleAsync, CheckEnabledAsync
├── AssertExistsAsync, AssertVisibleAsync, AssertEnabledAsync
└── Log()

AsyncClickableControlBase : AsyncControlObjectBase
├── virtual ClickAsync()
├── virtual DoubleClickAsync()
├── virtual RightClickAsync()
└── virtual HoverAsync()

AsyncTextControlBase : AsyncClickableControlBase
├── virtual IsFocusedAsync(), FocusAsync(), BlurAsync()
├── virtual EnterAsync()
├── virtual ClearAsync()
├── virtual ClearAndEnterAsync()
└── virtual AppendAsync()

ButtonControl : AsyncClickableControlBase
LabelControl : AsyncControlObjectBase
InputControl : AsyncTextControlBase
TextAreaControl : AsyncTextControlBase
```

---

## Interface to Class Mapping

| Interface | MAUI Class | Blazor Class |
|-----------|------------|--------------|
| IControlObject | ControlObjectBase | AsyncControlObjectBase |
| IInteractiveControlObject | ControlObjectBase | AsyncControlObjectBase |
| IClickableControlObject | ClickableControlBase | AsyncClickableControlBase |
| ITextControlObject | TextControlBase | AsyncTextControlBase |

---

## Constructor Patterns

### Pattern 1: Full Locator (Primary)

```csharp
public ButtonControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
    : base(context, locator, page)
{
}
```

### Pattern 2: String Shorthand (Convenience)

```csharp
public ButtonControl(MauiTestContext context, string automationId, IPageObject? page = null)
    : this(context, By.AutomationId(automationId), page)
{
}
```

### Default Locator Strategy

| Platform | Default Strategy |
|----------|-----------------|
| MAUI | `By.AutomationId(string)` |
| Blazor | `By.TestId(string)` |

---

## Next Steps

1. Review hierarchy documents for MAUI and Blazor
2. Implement changes per PLAN-SPEC-006c-POC
3. Update POC code with new base classes
