# Requirement Changes from SPEC-006

**Version:** 1.0 | **Status:** Active

## Change 1: Nullable Expected Parameters

All `Wait*`, `Check*`, and `Assert*` methods accept nullable expected values. When `null` is passed, the method is a no-op (skips the check).

```csharp
// null expected = skip assertion
control.AssertVisible(null);        // no-op
control.AssertText(null);           // no-op
control.WaitChecked(null);          // returns true immediately
```

**Rationale:** Enables conditional assertions driven by data without branching in test code.

## Change 2: Locator System

Replaced `string automationId` constructor parameter with `Locator` value object supporting 14 strategies.

| Component | Purpose |
|-----------|---------|
| `Locator` | Immutable value object: Strategy + Value + optional Parent |
| `LocatorStrategy` | Enum: AutomationId, Id, Name, XPath, Css, Text, AccessibilityId, etc. |
| Factory methods | `Locator.ByAutomationId()`, `Locator.ByXPath()`, `Locator.ByCss()`, etc. |

**Platform mapping:**

| Strategy | Appium | FlaUI | Playwright |
|----------|--------|-------|------------|
| AutomationId | AccessibilityId | AutomationId | data-testid |
| XPath | XPath | XPath | XPath |
| Css | n/a | n/a | CSS |
| Name | Name | Name | text |

**Backward compatibility:** Implicit `string → Locator` conversion treats strings as AutomationId.

## Change 3: Parameter Order Convention

All methods follow: `required params` → `nullable expected` → `message?` → `timeoutMs?`

```csharp
AssertText(string? expected, string? message = null, int? timeoutMs = null)
AssertValue(double? expected, double tolerance, string? message = null, int? timeoutMs = null)
```
