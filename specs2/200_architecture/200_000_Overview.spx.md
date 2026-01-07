# Brinell Framework Architecture

**Block:** 200 architecture  
**Edition:** 🟡Ⅱ Core  
**Version:** 1.0  
**Created:** January 7, 2026

---

## architecture

- **pattern**: Clean Architecture with Interface-First Design
- **description**: Layered architecture with platform-agnostic Core containing interfaces and abstractions, technology-specific Platform packages providing implementations, and Test/Sample layers for validation.

---

## 1. Overview

The Brinell UI Test Automation Framework follows **Clean Architecture** principles with an **Interface-First** approach. This architecture enables:

1. **Platform Independence** — Core interfaces work across MAUI, Blazor, WPF, WinForms, and Stride
2. **Testability** — Interfaces enable mocking and unit testing
3. **Extensibility** — New controls are added without modifying existing architecture
4. **Consistency** — Same patterns across all platform implementations

### Architecture Principle

> **Architecture is complete. Implementation is incremental.**

The architecture defines all layers, interfaces, base classes, and patterns upfront. Adding new controls means adding new specifications and implementations — never modifying the architecture.

---

## 2. Layer Model

```
┌─────────────────────────────────────────────────────────────┐
│                      Test Layer                              │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ *.UITests       │  │ *.UnitTests     │                   │
│  │ (UI Tests)      │  │ (Mocked Tests)  │                   │
│  └────────┬────────┘  └────────┬────────┘                   │
└───────────┼─────────────────────┼───────────────────────────┘
            │                     │
┌───────────┼─────────────────────┼───────────────────────────┐
│           ▼                     ▼      Platform Layer        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Brinell.MAUI    │  │ Brinell.Blazor  │  │ Brinell.WPF  │ │
│  │ (Appium)        │  │ (Selenium/PW)   │  │ (WinAppDriver)│ │
│  └────────┬────────┘  └────────┬────────┘  └──────┬───────┘ │
└───────────┼─────────────────────┼─────────────────┼─────────┘
            │                     │                 │
            └──────────┬──────────┴─────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                       Core Layer                             │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                    Brinell.Core                          ││
│  │  • Interfaces (IControlObject, IPageObject, etc.)       ││
│  │  • Exception types                                       ││
│  │  • Configuration contracts                               ││
│  │  • Logging contracts                                     ││
│  └─────────────────────────────────────────────────────────┘│
│                    ↓ No external dependencies ↓              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     Sample App Layer                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Samples.MAUI    │  │ Samples.Blazor  │  │ Samples.WPF  │ │
│  │ (Test Target)   │  │ (Test Target)   │  │ (Test Target)│ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
│              ↓ No Brinell dependency ↓                       │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Layer Definitions

### 3.1 Core Layer

| Aspect | Definition |
|--------|------------|
| **Package** | Brinell.Core |
| **Purpose** | Platform-agnostic abstractions |
| **Contains** | Interfaces, Exceptions, Configuration contracts, Logging contracts |
| **Dependencies** | None (pure abstractions) |

The Core layer defines **what** the framework does, not **how**. All interfaces live here. No technology-specific code is allowed.

### 3.2 Platform Layer

| Aspect | Definition |
|--------|------------|
| **Packages** | Brinell.MAUI, Brinell.Blazor, Brinell.WPF, Brinell.WinForms, Brinell.Stride |
| **Purpose** | Technology-specific implementations |
| **Contains** | Base classes, Control implementations, Driver adapters |
| **Dependencies** | Core + Automation SDK (Appium, Selenium, Playwright, WinAppDriver) |

Each platform package provides concrete implementations of Core interfaces using the appropriate automation technology.

### 3.3 Test Layer

| Aspect | Definition |
|--------|------------|
| **Packages** | *.UITests, *.UnitTests |
| **Purpose** | Framework validation and sample tests |
| **Contains** | Page objects, Test classes, Mocks |
| **Dependencies** | Platform package + Test framework (xUnit, FluentAssertions) |

### 3.4 Sample App Layer

| Aspect | Definition |
|--------|------------|
| **Packages** | Brinell.Samples.MAUI.App, Brinell.Samples.Blazor.App, etc. |
| **Purpose** | Test targets for UI automation |
| **Contains** | Sample applications with all supported controls |
| **Dependencies** | Technology SDK only (no Brinell dependency) |

Sample apps exist purely as automation targets. They contain every control that has a ControlObject implementation, with unique AutomationIds.

---

## 4. Interface Hierarchy

All control interfaces are defined in Core. The hierarchy follows **capability-based design** where interfaces represent what controls can do (click, edit text, toggle, select, etc.).

### Design Principles

1. **Single base interface** — IControlObject provides state checking and assertions for ALL controls
2. **Capability interfaces** — Each capability (clickable, editable, toggleable) is a separate interface
3. **Composition** — Controls implement multiple interfaces to express their capabilities
4. **Platform coverage** — Interfaces cover all standard controls in MAUI and Blazor

### Hierarchy Pattern

```
IControlObject                     # Base for ALL controls
│
├── Capability Interfaces          # What controls can DO
│   ├── IClickableControl          # Click, tap, press
│   ├── ITextControl               # Read text
│   │   └── IEditableTextControl   # Enter/clear text
│   ├── IToggleControl             # On/off state
│   ├── ISelectorControl           # Selection
│   ├── IRangeControl              # Numeric range
│   └── ... (derived from platform controls)
│
└── Structural Interfaces          # How controls are organized
    ├── IContainerControl          # Scoped child search
    ├── ICollectionControl         # Item enumeration
    └── IScrollableControl         # Scroll operations
```

### Scope

The **complete interface catalog** is defined in specifications, derived from:
- All standard MAUI controls (30+ control types)
- All standard Blazor/HTML controls
- Common patterns across both platforms

**Note:** This architecture defines the pattern. Specifications define the complete list.

### Interface Purpose Categories

| Category | Purpose | Example Interfaces |
|----------|---------|-------------------|
| Base | State and assertions for all controls | IControlObject |
| Interaction | User click/tap actions | IClickableControl, IFocusableControl |
| Text | Text content manipulation | ITextControl, IEditableTextControl |
| State | Binary and multi-state controls | IToggleControl, IExpandableControl |
| Selection | Choosing from options | ISelectorControl, ISelectableItemsControl |
| Range | Numeric values and progress | IRangeControl, IProgressControl |
| Structure | Element organization | IContainerControl, IItemsControl |
| Navigation | View and page control | INavigationControl, ITabControl |
| Scrolling | Scroll operations | IScrollableControl |
| Media | Rich content | IImageControl, IMediaControl, IWebViewControl |
| Platform-specific | Special capabilities | ISwipeableControl, IRefreshableControl |

---

## 5. Base Class Hierarchy

Each platform package implements base classes that correspond to capability interfaces:

### Pattern

```
ControlBase                        # Implements IControlObject
├── Capability Base Classes        # Implement capability interfaces
│   ├── ClickableControlBase       # Implements IClickableControl
│   ├── TextControlBase            # Implements ITextControl
│   │   └── EditableTextControlBase # Implements IEditableTextControl
│   ├── ToggleControlBase          # Implements IToggleControl
│   ├── SelectorControlBase        # Implements ISelectorControl
│   ├── RangeControlBase           # Implements IRangeControl
│   └── ... (matches interface hierarchy)
│
└── Structural Base Classes
    ├── ContainerControlBase       # Implements IContainerControl
    ├── CollectionControlBase      # Implements ICollectionControl
    └── ScrollableControlBase      # Implements IScrollableControl
```

### Base Class Principle

For each capability interface `IXxxControl`, there is a corresponding base class `XxxControlBase` that:
1. Implements the interface methods
2. Provides common functionality
3. Can be extended by concrete control classes

**Note:** The complete base class catalog is defined in specifications, mirroring the interface hierarchy.

### Base Class Responsibilities (Examples)

| Base Class | Provides |
|------------|----------|
| ControlBase | Element lookup, state checking, waiting, assertions |
| ClickableControlBase | Click, tap, long-press operations |
| TextControlBase | GetText(), text assertions |
| EditableTextControlBase | Enter(), Clear(), SetText() |
| ToggleControlBase | IsOn, Toggle(), SetState() |
| SelectorControlBase | Select(), GetSelectedItem() |
| RangeControlBase | GetValue(), SetValue(), GetRange() |
| ContainerControlBase | Scoped Find() methods |
| CollectionControlBase | GetItems(), GetItemCount() |

**Note:** This shows the pattern. Complete method signatures are in specifications.

---

## 6. Package Dependencies

```
┌──────────────────────────────────────────────────────────────┐
│ Brinell.Core                                                 │
│   Dependencies: None                                         │
└──────────────────────────────────────────────────────────────┘
           ▲                    ▲                    ▲
           │                    │                    │
┌──────────┴─────────┐ ┌───────┴────────┐ ┌────────┴─────────┐
│ Brinell.MAUI       │ │ Brinell.Blazor │ │ Brinell.WPF      │
│   + Appium.WebDrv  │ │ + Selenium.WD  │ │ + WinAppDriver   │
│   + OpenQA.Selenium│ │ or Playwright  │ │ + OpenQA.Selenium│
└────────────────────┘ └────────────────┘ └──────────────────┘
           ▲                    ▲                    ▲
           │                    │                    │
┌──────────┴─────────┐ ┌───────┴────────┐ ┌────────┴─────────┐
│ Samples.MAUI.Tests │ │ Samples.Blazor │ │ Samples.WPF.Tests│
│   + xUnit          │ │   .Tests       │ │   + xUnit        │
│   + FluentAssert   │ │   + xUnit      │ │   + FluentAssert │
└────────────────────┘ └────────────────┘ └──────────────────┘
```

---

## 7. Design Patterns

The framework uses these patterns consistently:

| Pattern | Purpose | Documentation |
|---------|---------|---------------|
| Control Object | Encapsulate control interactions | [231_001](231_Patterns/231_001_ControlObjectPattern.spx.md) |
| Page Object | Encapsulate page structure | [231_002](231_Patterns/231_002_PageObjectPattern.spx.md) |
| Adapter | Abstract automation driver | [231_003](231_Patterns/231_003_AdapterPattern.spx.md) |
| Container | Scope element searches | [231_004](231_Patterns/231_004_ContainerPattern.spx.md) |

---

## 8. Cross-Cutting Concerns

| Concern | Approach | Documentation |
|---------|----------|---------------|
| Logging | ILogger abstraction in Core | [221_001](221_Foundation/221_001_Logging.spx.md) |
| Configuration | IBrinellConfiguration in Core | [221_002](221_Foundation/221_002_Configuration.spx.md) |
| Exception Handling | Custom exception hierarchy | [221_003](221_Foundation/221_003_ExceptionHandling.spx.md) |
| Timeouts | Configurable wait strategies | [221_004](221_Foundation/221_004_Timeout.spx.md) |

---

## 9. Success Criteria

The architecture is considered successful when:

1. **Any control can be added** without modifying architecture documents
2. **Layer boundaries are clear** — no ambiguity about where code belongs
3. **Interface contracts are stable** — adding controls doesn't change interfaces
4. **Base class hierarchy is stable** — adding controls extends, doesn't modify
5. **Patterns are followed** — all implementations follow documented patterns

---

## Related Documents

- [202_Decisions/](202_Decisions/) — Architecture Decision Records
- [203_Layers/](203_Layers/) — Detailed layer specifications
- [211_Modules/](211_Modules/) — Module organization
- [231_Patterns/](231_Patterns/) — Design pattern documentation
