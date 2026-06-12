# Locator Classes

**Source of truth:** `srcnew/Brinell.Core/Locators/`

## Locator

Immutable value object representing how to find an element.

| Property | Type | Purpose |
|----------|------|---------|
| `Strategy` | `LocatorStrategy` | How to find (AutomationId, XPath, etc.) |
| `Value` | `string` | The search value |
| `Parent` | `Locator?` | Optional parent scope |

### Factory Methods

`Locator.ByAutomationId("id")`, `Locator.ByXPath("//...")`, `Locator.ByCss(".class")`, `Locator.ByName("name")`, `Locator.ByText("text")`, `Locator.ByAccessibilityId("aid")`, `Locator.ByDataTestId("tid")`, etc.

### Composition

- `locator.ScopedTo(parent)` — Creates nested locator
- `locator.WithStrategy(newStrategy)` — Changes strategy, keeps value
- Implicit `string → Locator` conversion (treated as AutomationId)

## LocatorStrategy Enum

14 values covering all platforms. See [001-INTERFACES.md](../001-INTERFACES.md#locator-system) for the full list.
