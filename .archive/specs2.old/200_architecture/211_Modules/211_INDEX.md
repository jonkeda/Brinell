# 211 Modules Index

**Block Type:** MOD (Modules)  
**Parent:** [200_INDEX](../200_INDEX.md)

---

## Overview

This folder defines the logical module groupings within the Brinell framework. Modules represent cohesive units of functionality that work together to provide the framework's capabilities.

## Module Organization

```
Brinell Framework
├── Interfaces Module          # Contracts for all controls and pages
├── Base Classes Module        # Reusable base implementations
├── Controls Module            # Concrete control implementations
└── Page/Context Module        # Test context and page object support
```

## Documents

| ID | Document | Purpose |
|----|----------|---------|
| 211.001 | [Interfaces](211_001_Interfaces.spx.md) | Control and page interface definitions |
| 211.002 | [BaseClasses](211_002_BaseClasses.spx.md) | Abstract base class implementations |
| 211.003 | [Controls](211_003_Controls.spx.md) | Concrete control implementations |
| 211.004 | [PageContext](211_004_PageContext.spx.md) | Page objects and test context |

## Module Dependencies

```
┌─────────────────┐
│   Interfaces    │  ← No dependencies (Core)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Base Classes   │  ← Depends on Interfaces
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Controls     │  ← Depends on Base Classes
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Page/Context   │  ← Depends on Controls
└─────────────────┘
```

## Key Principles

1. **Interface Segregation** — Interfaces are capability-based, not control-based
2. **Base Class Reuse** — Common functionality in base classes, platform-specific in controls
3. **Control Composition** — Controls inherit from one base class, implement multiple interfaces
4. **Context Isolation** — Test context manages driver lifecycle, pages organize controls

## Related Documents

- [Core Layer](../203_Layers/203_001_CoreLayer.spx.md)
- [Platform Layer](../203_Layers/203_002_PlatformLayer.spx.md)
- [ADR-002 Interface-First](../202_Decisions/202_002_InterfaceFirst.spx.md)
- [ADR-004 Control Hierarchy](../202_Decisions/202_004_ControlHierarchy.spx.md)
