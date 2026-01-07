# PLAN-001: Architecture Creation Based on SPX V7

**Version:** 1.0  
**Created:** January 6, 2026  
**Status:** Draft

---

## 1. Overview

This plan defines how to create the complete architecture documentation for Brinell using SPX V7 architecture blocks. The architecture must be **complete from day one** to ensure that incremental implementation does not require refactoring.

### Key Principle

> **Architecture is complete. Implementation is incremental.**

The architecture documents define all layers, patterns, modules, and foundations upfront. Control object specifications are then added in levels, but they slot into the existing architecture without changing the base structure.

---

## 2. SPX V7 Architecture Blocks to Use

Based on [SPX V7 Block Index](../../SPX/Docs/V7/_Index2.md), we will use:

| Block | Code | Purpose for Brinell |
|-------|------|---------------------|
| 200 architecture | ARC | Overall pattern (Clean Architecture / Hexagonal) |
| 202 decision | ADR | Architecture Decision Records |
| 203 layers | LYR | Core, Platform, Technology, Test layers |
| 211 module | MOD | Logical module groupings |
| 214 parts | PRT | Categories within modules (interfaces, controls, base classes) |
| 220 external | EXT | External dependencies (Appium, Selenium, Playwright) |
| 221 foundation | FND | Cross-cutting concerns (logging, configuration, exceptions) |
| 231 pattern | PTN | Design patterns in use (Page Object, Control Object, Adapter) |

---

## 3. Architecture Documents to Create

### 3.1 Folder Structure

```
specs2/
├── 200_architecture/
│   ├── 200_INDEX.md              # Architecture overview index
│   ├── 200_000_Overview.spx.md   # Main architecture document
│   ├── 202_Decisions/            # ADR folder
│   │   ├── 202_INDEX.md
│   │   ├── 202_001_CleanArchitecture.spx.md
│   │   ├── 202_002_InterfaceFirst.spx.md
│   │   ├── 202_003_PlatformSeparation.spx.md
│   │   └── 202_004_ControlHierarchy.spx.md
│   ├── 203_Layers/
│   │   ├── 203_INDEX.md
│   │   ├── 203_001_CoreLayer.spx.md
│   │   ├── 203_002_PlatformLayer.spx.md
│   │   └── 203_003_TechnologyLayer.spx.md
│   ├── 211_Modules/
│   │   ├── 211_INDEX.md
│   │   ├── 211_001_Interfaces.spx.md
│   │   ├── 211_002_BaseClasses.spx.md
│   │   ├── 211_003_Controls.spx.md
│   │   └── 211_004_PageContext.spx.md
│   ├── 220_External/
│   │   ├── 220_INDEX.md
│   │   ├── 220_001_Appium.spx.md
│   │   ├── 220_002_Selenium.spx.md
│   │   └── 220_003_Playwright.spx.md
│   ├── 221_Foundation/
│   │   ├── 221_INDEX.md
│   │   ├── 221_001_Logging.spx.md
│   │   ├── 221_002_Configuration.spx.md
│   │   ├── 221_003_ExceptionHandling.spx.md
│   │   └── 221_004_Timeout.spx.md
│   └── 231_Patterns/
│       ├── 231_INDEX.md
│       ├── 231_001_ControlObjectPattern.spx.md
│       ├── 231_002_PageObjectPattern.spx.md
│       ├── 231_003_AdapterPattern.spx.md
│       └── 231_004_ContainerPattern.spx.md
```

### 3.2 Document Priorities

| Priority | Document | Reason |
|----------|----------|--------|
| P1 | 200_000_Overview | Must define overall structure first |
| P1 | 202_001-004 Decisions | Rationale must be captured upfront |
| P1 | 203_001-003 Layers | Layer boundaries are critical |
| P2 | 211_001-004 Modules | Module organization |
| P2 | 231_001-004 Patterns | Pattern definitions |
| P3 | 220_001-003 External | External system details |
| P3 | 221_001-004 Foundation | Cross-cutting concerns |

---

## 4. Key Architecture Decisions

These must be documented in the 202_Decisions folder:

### ADR-001: Clean Architecture / Interface-First

- Core defines interfaces only
- Technology packages provide implementations
- No direct dependencies on automation libraries in Core

### ADR-002: Control Interface Hierarchy

- Base interface: `IControlObject` (all controls)
- Capability interfaces: `IClickableControl`, `ITextControl`, etc.
- Single inheritance for concrete classes
- Multiple interface implementation for capabilities

### ADR-003: Platform Separation

- Separate packages: Core, MAUI, Blazor, WPF
- Each technology package has own class hierarchy
- Technology packages depend on Core only

### ADR-004: Control Object Composition

- Controls compose capabilities, not inherit multiple base classes
- Base classes implement common functionality
- Interfaces define contracts

---

## 5. Layer Definitions

### Core Layer (Brinell.Core)

```
Purpose: Platform-agnostic abstractions
Contains:
  - All interfaces (IControlObject, IPageObject, etc.)
  - Exception types
  - Configuration contracts
  - Logging contracts
Dependencies: None (pure abstractions)
```

### Platform Layer (Brinell.MAUI, Brinell.Blazor)

```
Purpose: Technology-specific implementations
Contains:
  - Base classes (ControlBase, PageBase)
  - Concrete control implementations
  - Driver adapters
Dependencies: Core + technology SDK (Appium, Selenium)
```

### Test Layer (*.UITests)

```
Purpose: Test projects
Contains:
  - Page objects
  - Test classes
Dependencies: Platform package
```

### Sample App Layer (*.Samples.*)

```
Purpose: Test targets for UI automation (FR-950)
Contains:
  - Sample applications per technology
  - All controls that have ControlObject implementations
  - Controls with unique AutomationIds
Dependencies: Technology SDK only (no Brinell dependency)
```

---

## 6. Interface and Base Class Hierarchies

> **Note:** Complete interface and base class hierarchy definitions are documented in [PLAN-002](PLAN-002-Specification-Levels.md) Level 0 Foundation section. Architecture documents the pattern and structure; specifications define the complete member lists.

Architecture documents (211_Modules) define:
- **Interface organization** — Capability-based interfaces (211_001_Interfaces)
- **Base class patterns** — Template method, inheritance structure (211_002_BaseClasses)
- **Control patterns** — How controls inherit and implement (211_003_Controls)

Specification documents (PLAN-002 Level 0) define:
- **Complete interface members** — All methods, properties, signatures
- **Complete base class members** — All methods, template methods, abstract methods
- **Behavior specifications** — What each method does, edge cases, assertions

---

## 7. Execution Steps

### Phase 1: Core Architecture (Week 1)

1. Create folder structure under `specs2/200_architecture/`
2. Write `200_000_Overview.spx.md`
3. Write all ADRs (202_001 through 202_004)
4. Write layer definitions (203_001 through 203_003)

### Phase 2: Module and Pattern Details (Week 1-2)

1. Write module definitions (211_001 through 211_004)
2. Write pattern definitions (231_001 through 231_004)

> **Note:** Complete interface and base class hierarchy definitions are created as part of PLAN-002 Level 0 Foundation, not Phase 2 of PLAN-001. See [PLAN-002](PLAN-002-Specification-Levels.md) for details.

### Phase 3: Foundation and External (Week 2)

1. Write foundation concerns (221_001 through 221_004)
2. Write external system definitions (220_001 through 220_003)

### Phase 4: Review and Validate

1. Review architecture against existing code
2. Validate all interfaces are documented
3. Validate all base classes are documented
4. Sign off architecture as complete

---

## 8. Validation Checklist

Before architecture is considered complete:

- [ ] All layers defined with clear boundaries
- [ ] All ADRs documented with rationale
- [ ] Interface organization patterns documented (complete members in PLAN-002)
- [ ] Base class organization patterns documented (complete members in PLAN-002)
- [ ] All patterns documented
- [ ] All cross-cutting concerns documented
- [ ] External dependencies documented
- [ ] No implementation details in architecture (those go in specifications)
- [ ] Sample app structure defined per technology (FR-950)
- [ ] Unit test project structure defined (FR-960)
- [ ] UI test project structure defined (FR-970)

---

## 9. Success Criteria

Architecture is complete when:

1. **Any control can be added** without changing architecture documents
2. **Layer boundaries are clear** - no ambiguity about where code belongs
3. **Interface contracts are stable** - adding controls doesn't change interfaces
4. **Base class hierarchy is stable** - adding controls extends, doesn't modify
5. **Patterns are defined** - implementation follows documented patterns

---

## Related Documents

- [PLAN-002-Specification-Levels](PLAN-002-Specification-Levels.md) — Incremental specification plan
- [SPX V7 Architecture Overview](../../SPX/Docs/V7/blocks2/200_architecture/20X_overview.md)
- [SPX V7 Block Index](../../SPX/Docs/V7/_Index2.md)

### Testing Infrastructure Requirements

- [FR-950 Sample Applications](../100_requirements/120_functional/120_950_SampleApplications.spx.md) — Sample apps per technology
- [FR-960 Unit Tests](../100_requirements/120_functional/120_960_UnitTests.spx.md) — Unit tests for ControlObjects with mocks
- [FR-961 Unit Tests Framework](../100_requirements/120_functional/120_961_UnitTestsFramework.spx.md) — Unit tests for framework infrastructure
- [FR-970 UI Tests](../100_requirements/120_functional/120_970_UITests.spx.md) — UI integration tests
