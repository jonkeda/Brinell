# 203 Layers Index

**Block Type:** LYR (Layers)  
**Parent:** [200_INDEX](../200_INDEX.md)

---

## Overview

This folder defines the architectural layers of the Brinell framework. The layering follows Clean Architecture principles where dependencies flow inward — outer layers depend on inner layers, never the reverse.

## Layer Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│                    Test Projects Layer                       │
│  (*.UITests, *.Tests — consumes framework)                  │
├─────────────────────────────────────────────────────────────┤
│                    Technology Layer                          │
│  (Appium, Selenium, Playwright — automation drivers)        │
├─────────────────────────────────────────────────────────────┤
│                    Platform Layer                            │
│  (Brinell.MAUI, Brinell.Blazor — implementations)          │
├─────────────────────────────────────────────────────────────┤
│                    Core Layer                                │
│  (Brinell.Core — interfaces, contracts, abstractions)       │
└─────────────────────────────────────────────────────────────┘
         ↑ Dependencies flow INWARD (up in this diagram)
```

## Documents

| ID | Document | Purpose |
|----|----------|---------|
| 203.001 | [CoreLayer](203_001_CoreLayer.spx.md) | Core abstractions layer — interfaces, contracts, exceptions |
| 203.002 | [PlatformLayer](203_002_PlatformLayer.spx.md) | Platform implementations — MAUI, Blazor, WPF packages |
| 203.003 | [TechnologyLayer](203_003_TechnologyLayer.spx.md) | Automation technology integration — Appium, Selenium, Playwright |

## Key Principles

1. **Dependency Direction** — All dependencies point inward toward Core
2. **Core Has No Dependencies** — Core layer contains only abstractions
3. **Platform Implements Core** — Platform packages implement Core interfaces
4. **Technology Is Isolated** — Automation libraries are wrapped, not exposed
5. **Tests Consume Platform** — Test projects depend on platform packages only

## Related Documents

- [ADR-001 Clean Architecture](../202_Decisions/202_001_CleanArchitecture.spx.md)
- [ADR-003 Platform Separation](../202_Decisions/202_003_PlatformSeparation.spx.md)
- [Architecture Overview](../200_000_Overview.spx.md)
