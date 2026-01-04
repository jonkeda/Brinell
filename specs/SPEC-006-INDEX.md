# SPEC-006: ControlObject Framework - Complete Specification

**Version:** 1.1  
**Status:** Final  
**Date:** January 2026

---

## Documents

| Document | Description |
|----------|-------------|
| [SPEC-006-001](SPEC-006-001-INTERFACES.md) | Interface Definitions |
| [SPEC-006-002](SPEC-006-002-CLASSES.md) | Class Definitions |

---

## Key Changes from SPEC-005

1. **Nullable expected on Wait/Check/Assert** - If null, skip the operation
2. **Locator Strategy** - Find elements by AutomationId, Name, Id, XPath, CSS, Text, etc.
3. **IBusyPageObject** - Page interface for busy/loading state tracking (v1.1)

---

## Design Rules

### Rule 1: Nullable Expected Parameter

All Wait, Check, and Assert methods have nullable expected:
```csharp
bool WaitVisible(bool? expected, int? timeoutMs = null);
// If expected is null, method returns immediately (true for Wait, no-op for Check/Assert)
```

### Rule 2: Locator Strategy

Elements can be found using multiple strategies via `ControlLocator`:

```csharp
// By AutomationId (default)
var button = page.GetControl<IClickableControlObject>("SubmitButton");

// By explicit locator
var button = page.GetControl<IClickableControlObject>(By.AutomationId("SubmitButton"));
var button = page.GetControl<IClickableControlObject>(By.Name("Submit"));
var button = page.GetControl<IClickableControlObject>(By.Id("submit-btn"));
var button = page.GetControl<IClickableControlObject>(By.XPath("//button[@type='submit']"));
var button = page.GetControl<IClickableControlObject>(By.Css("button.submit"));
var button = page.GetControl<IClickableControlObject>(By.Text("Submit"));
var button = page.GetControl<IClickableControlObject>(By.PartialText("Sub"));

// Chained locators
var button = page.GetControl<IClickableControlObject>(
    By.AutomationId("Form").Then(By.Name("SubmitButton"))
);
```

### Rule 3: Parameter Order

1. Required value parameters first
2. Optional nullable parameters (including expected)
3. `string? message = null` for Assert methods
4. `int? timeoutMs = null` always last
