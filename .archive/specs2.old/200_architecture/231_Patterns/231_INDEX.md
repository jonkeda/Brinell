# 231 Patterns Module Index

## Module Overview

| Property | Value |
|----------|-------|
| **Module Code** | PAT |
| **Module Name** | Patterns |
| **Purpose** | Design patterns used in the Brinell framework |
| **Scope** | Cross-cutting architectural patterns |
| **Updated** | Phase 5 - Generic Type Parameters |

---

## Description

The Patterns module documents the design patterns that form the foundation of the Brinell UI testing framework. These patterns provide consistent approaches to common problems in UI test automation.

### Generic Type Design

All patterns now use a single `TElement` type parameter in interfaces:

| Interface | Type Parameter | Purpose |
|-----------|---------------|---------|
| `IElementScope<TElement>` | `TElement` | Typed element finding |
| `ITestContext<TElement>` | `TElement` | Driver-level typed scope |
| `IPageObject<TElement>` | `TElement` | Page as typed scope |
| `IContainerControl<TElement>` | `TElement` | Container as typed scope |

Base classes add `TContext`/`TScope` for implementation:
```csharp
ControlBase<TElement, TScope>      // TScope is parent page/container
PageObjectBase<TElement, TContext> // TContext is test context
ContainerBase<TElement, TScope>    // TScope is parent page/container
```

---

## Documents

| Document | Title | Pattern Type | Description |
|----------|-------|--------------|-------------|
| [231_001](231_001_ControlObjectPattern.spx.md) | Control Object | Structural | Encapsulate UI element interactions |
| [231_002](231_002_PageObjectPattern.spx.md) | Page Object | Structural | Encapsulate page structure and navigation |
| [231_003](231_003_AdapterPattern.spx.md) | Adapter | Structural | Abstract automation driver details |
| [231_003b](231_003b_PlatformSpecificHierarchy.spx.md) | Platform-Specific Hierarchy | Structural | Generic interfaces with `TElement` type narrowing |
| [231_004](231_004_ContainerPattern.spx.md) | Container | Structural | Container as `IElementScope<TElement>` |
| [231_004b](231_004b_ContainerPatternV2.spx.md) | Container V2 | Structural | High-level container pattern overview |
| [231_005](231_005_BusyPagePattern.spx.md) | Busy Page | Behavioral | Track page loading/busy states |
| [231_006](231_006_TestBasePattern.spx.md) | Test Base | Structural | Platform-specific test infrastructure |
| [231_007](231_007_ScopedElementFinder.spx.md) | Scoped Element Finder | Structural | Typed element finding via `IElementScope<TElement>` |

---

## Pattern Relationships

```
┌─────────────────────────────────────────────────────────────┐
│                    Test Code Layer                          │
│                                                             │
│  [Test] ──uses──> [Page Object] ──creates──> [Controls]    │
│                        │                          │         │
│                        └── IElementScope<T> ──────┘         │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ page IS scope for controls
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   Framework Layer                           │
│                                                             │
│  [Control Object] ──uses──> [Scope.FindElement()] ──> [T]  │
│        │                                                    │
│        └──scoped by──> [Container : IElementScope<T>]      │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ interacts with
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                 Automation Layer                            │
│                                                             │
│  Appium (AppiumElement) │ Selenium (IWebElement)           │
└─────────────────────────────────────────────────────────────┘
```

---

## Pattern Summary

| Pattern | Problem | Solution | Key Benefit |
|---------|---------|----------|-------------|
| Control Object | Direct element interaction is brittle | Wrap elements in typed controls with `TElement` | Type-safe, reusable interactions |
| Page Object | Test code mixed with UI structure | Page implements `IElementScope<TElement>` | Maintainable, DRY test code |
| Adapter | Tight coupling to automation driver | Abstract driver behind interface | Platform portability |
| Platform-Specific Hierarchy | Parallel typed methods cause duplication | Interfaces narrow `TElement` via inheritance | Zero duplication, compile-time safety |
| Container | Global searches are slow and ambiguous | Container implements `IElementScope<TElement>` | Scoped search within container root |
| Busy Page | Tests proceed before async completes | Wait for busy indicators | Reliable async handling |
| Test Base | Generic context requires casting | Platform-specific typed base classes | Compile-time type safety |
| Scoped Element Finder | Controls find elements globally | Controls receive `IElementScope<TElement>` | Automatic scoping, cleaner locators |

---

## Generic Type Flow

```
ITestContext<AppiumElement>  ←── IMauiTestContext
        │
        │ creates
        ▼
IPageObject<AppiumElement>   ←── IMauiPageObject
        │
        │ page IS scope for controls
        ▼
IControlObject (with IElementScope<AppiumElement> scope)
        │
        │ scope.TryFindElement(locator) returns AppiumElement
        ▼
Control interactions use typed AppiumElement
```

**Constructor Pattern:**
```csharp
// OLD (3 params, page separate)
new ButtonControl(context, locator, page)

// NEW (2 params, scope IS parent)
new MauiButtonControl(scope, locator)  // scope is page or container
```

---

## Requirements Traceability

| Pattern | Requirement | Description |
|---------|-------------|-------------|
| Control Object | FR-100 | Control Object Model |
| Page Object | FR-101 | Page Object Model |
| Container | FR-102 | Container Object Model |
| Adapter | FR-103 | Interface Hierarchy |

---

## Related Documents

- [250 Foundation Specs](../../250_specifications/250_000_Foundation/250_000_INDEX.md) - Interface specifications
- [211 Modules](../211_Modules/211_INDEX.md) - Implementation modules
- [220 External](../220_External/220_INDEX.md) - External dependencies
- [221 Foundation](../221_Foundation/221_INDEX.md) - Cross-cutting concerns
