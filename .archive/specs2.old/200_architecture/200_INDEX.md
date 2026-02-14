# Architecture Index

**Version:** 1.0  
**Created:** January 7, 2026  
**Status:** Draft

---

## Overview

This folder contains the complete architecture documentation for the Brinell UI Test Automation Framework. The architecture is designed to be **complete from day one** — control specifications are added incrementally, but they slot into this existing structure without modification.

### Key Principle

> **Architecture is complete. Implementation is incremental.**

---

## Document Structure

| Document | Purpose |
|----------|---------|
| [200_000_Overview.spx.md](200_000_Overview.spx.md) | Main architecture document |
| [202_Decisions/](202_Decisions/) | Architecture Decision Records |
| [203_Layers/](203_Layers/) | Layer definitions and boundaries |
| [211_Modules/](211_Modules/) | Module organization |
| [220_External/](220_External/) | External dependencies |
| [221_Foundation/](221_Foundation/) | Cross-cutting concerns |
| [231_Patterns/](231_Patterns/) | Design patterns in use |

---

## Architecture Decisions (ADRs)

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-001](202_Decisions/202_001_CleanArchitecture.spx.md) | Clean Architecture / Interface-First | accepted |
| [ADR-002](202_Decisions/202_002_InterfaceFirst.spx.md) | Interface-First Design | accepted |
| [ADR-003](202_Decisions/202_003_PlatformSeparation.spx.md) | Platform Separation | accepted |
| [ADR-004](202_Decisions/202_004_ControlHierarchy.spx.md) | Control Interface Hierarchy | accepted |

---

## Layer Overview

| Layer | Package | Purpose |
|-------|---------|---------|
| Core | Brinell.Core | Platform-agnostic abstractions |
| Platform | Brinell.MAUI, Brinell.Blazor | Technology-specific implementations |
| Test | *.UITests | Test projects using Brinell |
| Sample | *.Samples.* | Test target applications |

---

## Quick Reference

### Interface Hierarchy

> **📋 Complete interface definitions:** See [250_005_InterfaceHierarchy.spx.md](../250_specifications/250_000_Foundation/250_005_InterfaceHierarchy.spx.md)

```
IControlObject                     # Base for all controls
├── IClickableControl              # Click capability
├── ITextControl                   # Text display
│   └── IEditableTextControl       # Text input
├── IToggleControl                 # Toggle state
├── ISelectorControl               # Selection from list
│   └── IMultiSelectorControl      # Multi-selection
├── IRangeControl                  # Numeric range
├── IContainerControl              # Child scoping
└── ICollectionControl             # Item enumeration
```

### Base Class Hierarchy

```
ControlBase                        # Implements IControlObject
├── ClickableControlBase           # Implements IClickableControl
├── TextControlBase                # Implements ITextControl
│   └── EditableTextControlBase    # Implements IEditableTextControl
├── ToggleControlBase              # Implements IToggleControl
├── SelectorControlBase            # Implements ISelectorControl
├── RangeControlBase               # Implements IRangeControl
└── ContainerControlBase           # Implements IContainerControl
```

---

## Cross-Layer References

Architecture documents (200_*) answer **WHY** and **HOW**. Specifications (250_*) answer **WHAT** and **WHEN**.

| Topic | Architecture (WHY/HOW) | Specification (WHAT/WHEN) |
|-------|------------------------|---------------------------|
| Interface Design | [ADR-002 Interface-First](202_Decisions/202_002_InterfaceFirst.spx.md) | [250_001 IControlObject](../250_specifications/250_000_Foundation/250_001_IControlObject.spx.md) |
| Control Hierarchy | [ADR-004 ControlHierarchy](202_Decisions/202_004_ControlHierarchy.spx.md) | [250_005 InterfaceHierarchy](../250_specifications/250_000_Foundation/250_005_InterfaceHierarchy.spx.md) |
| Patterns | [231_001 ControlObjectPattern](231_Patterns/231_001_ControlObjectPattern.spx.md) | [250_001 IControlObject](../250_specifications/250_000_Foundation/250_001_IControlObject.spx.md) |
| External Drivers | [220_000 ExternalDependencies](220_External/220_000_ExternalDependencies.spx.md) | [250_009 PlatformContexts](../250_specifications/250_000_Foundation/250_009_PlatformContexts.spx.md) |

---

## Related Documents

- [PLAN-001-Architecture-Creation](../Plan/PLAN-001-Architecture-Creation.md) — Creation plan
- [PLAN-002-Specification-Levels](../Plan/PLAN-002-Specification-Levels.md) — Incremental specification plan
- [250_specifications/](../250_specifications/) — Control specifications (by level)
- [Questions-to-Blocks.md](../../SPX/Docs/V7/Overview/Questions-to-Blocks.md) — SPX block guide by question type
