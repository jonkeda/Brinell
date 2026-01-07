# 231 Patterns Module Index

## Module Overview

| Property | Value |
|----------|-------|
| **Module Code** | PAT |
| **Module Name** | Patterns |
| **Purpose** | Design patterns used in the Brinell framework |
| **Scope** | Cross-cutting architectural patterns |

---

## Description

The Patterns module documents the design patterns that form the foundation of the Brinell UI testing framework. These patterns provide consistent approaches to common problems in UI test automation.

Each pattern document describes:
- Intent and motivation
- Structure and participants
- Implementation guidelines
- Usage examples
- Anti-patterns to avoid

---

## Documents

| Document | Title | Pattern Type | Description |
|----------|-------|--------------|-------------|
| [231_001](231_001_ControlObjectPattern.spx.md) | Control Object | Structural | Encapsulate UI element interactions |
| [231_002](231_002_PageObjectPattern.spx.md) | Page Object | Structural | Encapsulate page structure and navigation |
| [231_003](231_003_AdapterPattern.spx.md) | Adapter | Structural | Abstract automation driver details |
| [231_004](231_004_ContainerPattern.spx.md) | Container | Structural | Scope element searches to UI regions |
| [231_005](231_005_BusyPagePattern.spx.md) | Busy Page | Behavioral | Track page loading/busy states |
| [231_006](231_006_TestBasePattern.spx.md) | Test Base | Structural | Platform-specific test infrastructure |

---

## Pattern Relationships

```
┌─────────────────────────────────────────────────────────────┐
│                    Test Code Layer                          │
│                                                             │
│  [Test] ──uses──> [Page Object] ──creates──> [Controls]    │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ delegates to
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   Framework Layer                           │
│                                                             │
│  [Control Object] ──uses──> [Adapter] ──wraps──> [Driver]  │
│        │                                                    │
│        └──scoped by──> [Container]                         │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ interacts with
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                 Automation Layer                            │
│                                                             │
│  Appium │ Selenium │ Playwright │ FlaUI                    │
└─────────────────────────────────────────────────────────────┘
```

---

## Pattern Summary

| Pattern | Problem | Solution | Key Benefit |
|---------|---------|----------|-------------|
| Control Object | Direct element interaction is brittle | Wrap elements in typed controls | Type-safe, reusable interactions |
| Page Object | Test code mixed with UI structure | Encapsulate page in class | Maintainable, DRY test code |
| Adapter | Tight coupling to automation driver | Abstract driver behind interface | Platform portability |
| Container | Global searches are slow and ambiguous | Scope searches to UI regions | Faster, more reliable element finding |
| Busy Page | Tests proceed before async completes | Wait for busy indicators | Reliable async handling |
| Test Base | Generic context requires casting | Platform-specific base classes | Compile-time type safety |

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

- [211 Modules](../211_Modules/211_INDEX.md) - Implementation modules
- [220 External](../220_External/220_INDEX.md) - External dependencies
- [221 Foundation](../221_Foundation/221_INDEX.md) - Cross-cutting concerns
