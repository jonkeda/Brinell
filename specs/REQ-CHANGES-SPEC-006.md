# REQ-CHANGES: SPEC-006 Requirement Updates

**Version:** 1.0  
**Status:** Draft  
**Date:** January 4, 2026  
**Source:** SPEC-006

---

## Overview

This document captures requirement changes introduced in SPEC-006 that affect existing specifications and implementation plans.

---

## 1. Nullable Expected Parameters

### Change Description

All `Wait`, `Check`, and `Assert` methods now accept nullable expected values. When the expected value is `null`, the operation is skipped entirely.

### Affected Methods

| Method Pattern | Old Signature | New Signature |
|----------------|---------------|---------------|
| WaitExists | `bool WaitExists(bool expected, int? timeoutMs)` | `bool WaitExists(bool? expected, int? timeoutMs)` |
| WaitVisible | `bool WaitVisible(bool expected, int? timeoutMs)` | `bool WaitVisible(bool? expected, int? timeoutMs)` |
| WaitEnabled | `bool WaitEnabled(bool expected, int? timeoutMs)` | `bool WaitEnabled(bool? expected, int? timeoutMs)` |
| WaitChecked | `bool WaitChecked(bool expected, int? timeoutMs)` | `bool WaitChecked(bool? expected, int? timeoutMs)` |
| WaitValue | `bool WaitValue(double expected, ...)` | `bool WaitValue(double? expected, ...)` |
| CheckExists | `void CheckExists(bool expected, int? timeoutMs)` | `void CheckExists(bool? expected, int? timeoutMs)` |
| CheckVisible | `void CheckVisible(bool expected, int? timeoutMs)` | `void CheckVisible(bool? expected, int? timeoutMs)` |
| CheckEnabled | `void CheckEnabled(bool expected, int? timeoutMs)` | `void CheckEnabled(bool? expected, int? timeoutMs)` |
| AssertExists | `void AssertExists(bool expected, ...)` | `void AssertExists(bool? expected, ...)` |
| AssertVisible | `void AssertVisible(bool expected, ...)` | `void AssertVisible(bool? expected, ...)` |
| AssertEnabled | `void AssertEnabled(bool expected, ...)` | `void AssertEnabled(bool? expected, ...)` |
| AssertChecked | `void AssertChecked(bool expected, ...)` | `void AssertChecked(bool? expected, ...)` |
| AssertValue | `void AssertValue(double expected, ...)` | `void AssertValue(double? expected, ...)` |
| AssertText | `void AssertText(string expected, ...)` | `void AssertText(string? expected, ...)` |
| AssertSelectedIndex | `void AssertSelectedIndex(int expected, ...)` | `void AssertSelectedIndex(int? expected, ...)` |
| AssertItemCount | `void AssertItemCount(int expected, ...)` | `void AssertItemCount(int? expected, ...)` |

### Behavioral Change

```csharp
// Old behavior - must provide expected value
control.WaitVisible(true);
control.AssertEnabled(false);

// New behavior - null skips the operation
bool? shouldBeVisible = config.CheckVisibility ? true : null;
control.WaitVisible(shouldBeVisible);  // Skipped if null

int? expectedCount = config.ValidateCount ? 5 : null;
list.AssertItemCount(expectedCount);   // Skipped if null
```

### Requirements Impact

- **REQ-WAIT-001**: Update to allow nullable expected parameters
- **REQ-CHECK-001**: Update to allow nullable expected parameters
- **REQ-ASSERT-001**: Update to allow nullable expected parameters
- **REQ-SKIP-001**: New requirement for skip-on-null behavior

---

## 2. Locator Strategy System

### Change Description

Replaced `string automationId` parameter with flexible `ControlLocator` class supporting multiple locator strategies.

### New Types

#### ControlLocator Class

```csharp
public class ControlLocator
{
    public LocatorStrategy Strategy { get; }
    public string Value { get; }
    public ControlLocator? Parent { get; }
    
    public ControlLocator Then(ControlLocator child);
    public ControlLocator WithIndex(int index);
    public ControlLocator First();
    public ControlLocator Last();
    public ControlLocator Nth(int n);
    
    public static implicit operator ControlLocator(string automationId);
}
```

#### LocatorStrategy Enum

```csharp
public enum LocatorStrategy
{
    AutomationId,      // MAUI: AutomationId, Blazor: data-automation-id
    Name,              // MAUI: Name property, Blazor: name attribute
    Id,                // MAUI: N/A, Blazor: id attribute
    ClassName,         // MAUI: ClassName, Blazor: class attribute
    XPath,             // MAUI: XPath, Blazor: XPath
    Css,               // MAUI: N/A, Blazor: CSS selector
    Text,              // MAUI: Text/Label, Blazor: text content
    PartialText,       // MAUI: Contains text, Blazor: contains text
    AccessibilityId,   // MAUI: AccessibilityId, Blazor: aria-label
    TagName,           // MAUI: ControlType, Blazor: tag name
    Label,             // MAUI: Label, Blazor: label association
    Placeholder,       // MAUI: Placeholder, Blazor: placeholder
    Title,             // MAUI: Title, Blazor: title attribute
    Role,              // MAUI: AutomationControlType, Blazor: role
    TestId,            // MAUI: AutomationId, Blazor: data-testid
    DataAttribute,     // MAUI: N/A, Blazor: data-* attributes
    Chained            // Parent-child relationship
}
```

#### By Static Factory

```csharp
public static class By
{
    public static ControlLocator AutomationId(string value);
    public static ControlLocator Name(string value);
    public static ControlLocator Id(string value);
    public static ControlLocator ClassName(string value);
    public static ControlLocator XPath(string value);
    public static ControlLocator Css(string value);
    public static ControlLocator Text(string value);
    public static ControlLocator PartialText(string value);
    public static ControlLocator AccessibilityId(string value);
    public static ControlLocator TagName(string value);
    public static ControlLocator Label(string value);
    public static ControlLocator Placeholder(string value);
    public static ControlLocator Title(string value);
    public static ControlLocator Role(string value);
    public static ControlLocator TestId(string value);
    public static ControlLocator DataAttribute(string name, string value);
}
```

### Usage Examples

```csharp
// Old approach - AutomationId only
var button = page.GetControl<IButtonControl>("submitButton");

// New approach - Multiple strategies
var button = page.GetControl<IButtonControl>(By.AutomationId("submitButton"));
var link = page.GetControl<ILinkControl>(By.Text("Click here"));
var input = page.GetControl<ITextControl>(By.Css("input.form-control"));
var item = page.GetControl<IControlObject>(By.XPath("//div[@class='item']"));

// Chained locators
var cell = page.GetControl<IControlObject>(
    By.AutomationId("dataGrid")
      .Then(By.ClassName("row"))
      .Then(By.Css("td:first-child"))
);

// Implicit conversion preserves backward compatibility
var button = page.GetControl<IButtonControl>("submitButton");  // Still works
```

### Platform-Specific Mapping

| Strategy | MAUI (Appium) | Blazor (Playwright) |
|----------|---------------|---------------------|
| AutomationId | `MobileBy.AccessibilityId()` | `[data-automation-id="value"]` |
| Name | `MobileBy.Name()` | `[name="value"]` |
| Id | Not supported | `#value` |
| ClassName | `MobileBy.ClassName()` | `.value` |
| XPath | `By.XPath()` | `xpath=value` |
| Css | Not supported | `value` |
| Text | `MobileBy.AndroidUIAutomator()` / iOS predicate | `text=value` |
| PartialText | Contains text predicate | `text=*value*` |
| AccessibilityId | `MobileBy.AccessibilityId()` | `[aria-label="value"]` |
| TagName | Control type | `value` tag selector |
| TestId | `MobileBy.AccessibilityId()` | `[data-testid="value"]` |
| DataAttribute | Not supported | `[data-name="value"]` |

### Requirements Impact

- **REQ-LOCATOR-001**: New requirement for ControlLocator class
- **REQ-LOCATOR-002**: New requirement for LocatorStrategy enum
- **REQ-LOCATOR-003**: New requirement for By static factory
- **REQ-LOCATOR-004**: New requirement for chained locators
- **REQ-LOCATOR-005**: New requirement for implicit string conversion
- **REQ-LOCATOR-006**: New requirement for platform-specific mapping
- **REQ-CONTROL-001**: Update IControlObject.Locator property type
- **REQ-PAGE-001**: Update IPageObject.GetControl parameter type
- **REQ-CONTAINER-001**: Update container GetChild parameter type

---

## 3. Interface Property Changes

### IControlObject.Locator

| Aspect | Old | New |
|--------|-----|-----|
| Property Type | `string AutomationId { get; }` | `ControlLocator Locator { get; }` |

### Constructor Changes

All control classes must accept `ControlLocator` instead of `string automationId`:

```csharp
// Old
public ButtonControl(string automationId, IPageObject page)

// New
public ButtonControl(ControlLocator locator, IPageObject page)
```

---

## 4. New Exception Types

### LocatorNotFoundException

```csharp
public class LocatorNotFoundException : ControlObjectException
{
    public LocatorStrategy Strategy { get; }
}
```

Thrown when:
- Using unsupported locator strategy for platform (e.g., CSS on MAUI)
- Locator syntax is invalid
- Chained locator parent not found

### Requirements Impact

- **REQ-EXCEPTION-001**: Update exception hierarchy
- **REQ-EXCEPTION-002**: New LocatorNotFoundException requirement

---

## 5. Backward Compatibility

### Preserved

1. **Implicit string conversion**: `"automationId"` automatically converts to `By.AutomationId("automationId")`
2. **Existing method signatures**: All methods retain same names and parameter order
3. **Return types**: No changes to return types

### Breaking Changes

1. **Locator property type**: Changed from `string` to `ControlLocator`
2. **Constructor parameters**: Changed from `string` to `ControlLocator`

### Migration Path

```csharp
// No change needed for simple cases
page.GetControl<IButtonControl>("myButton");  // Works via implicit conversion

// For property access, use Locator.Value
string id = control.Locator.Value;  // Instead of control.AutomationId
```

---

## 6. Summary of Affected Requirements

### New Requirements

| Requirement ID | Description |
|----------------|-------------|
| REQ-SKIP-001 | Nullable expected parameters skip operations when null |
| REQ-LOCATOR-001 | ControlLocator class definition |
| REQ-LOCATOR-002 | LocatorStrategy enum values |
| REQ-LOCATOR-003 | By static factory methods |
| REQ-LOCATOR-004 | Chained locator support |
| REQ-LOCATOR-005 | Implicit string to ControlLocator conversion |
| REQ-LOCATOR-006 | Platform-specific locator mapping |

### Updated Requirements

| Requirement ID | Change Description |
|----------------|-------------------|
| REQ-WAIT-001 | Nullable expected parameters |
| REQ-CHECK-001 | Nullable expected parameters |
| REQ-ASSERT-001 | Nullable expected parameters |
| REQ-CONTROL-001 | Locator property type change |
| REQ-PAGE-001 | GetControl parameter type change |
| REQ-CONTAINER-001 | GetChild parameter type change |
| REQ-EXCEPTION-001 | New exception type |

---

## 7. Implementation Priority

1. **High Priority**
   - ControlLocator class
   - By static factory
   - LocatorStrategy enum
   - Platform mapping (MAUI/Appium, Blazor/Playwright)

2. **Medium Priority**
   - Nullable expected parameter handling
   - Chained locator support
   - Index/First/Last/Nth methods

3. **Low Priority**
   - Less common locator strategies (DataAttribute, Role, etc.)
   - Advanced chaining scenarios

---

**Related Documents:**
- [SPEC-006-INDEX](SPEC-006-INDEX.md)
- [SPEC-006-001-INTERFACES](SPEC-006-001-INTERFACES.md)
- [SPEC-006-002-CLASSES](SPEC-006-002-CLASSES.md)
