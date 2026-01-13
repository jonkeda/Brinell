# 203.001b Core Layer

**Block Type:** LYR (Layer)  
**Edition:** 🟢Ⅰ Lite

---

## Overview

The Core layer is the innermost layer of the Brinell architecture. It contains **abstractions and cross-cutting concerns** — interfaces, contracts, exception types, configuration definitions, and platform-agnostic utilities. It has **zero dependencies** on external packages.

## Purpose

- Define contracts (interfaces) for all platform implementations
- Provide cross-cutting utilities (logging, timeout, retry)
- Ensure compile-time safety through interface-based design
- Serve as stable API that all platforms must implement

## Contents

```
Brinell.Core/
├── Interfaces/       # Control and page interface definitions
├── Exceptions/       # Framework exception types (NotFound, NotVisible, etc.)
├── Configuration/    # Configuration contracts (timeout, retry, logging)
├── Logging/          # Logging abstractions and default console implementation
├── Timeout/          # Timeout settings and platform-agnostic wait logic
├── Retry/            # Retry policies and execution utilities
└── Assertions/       # Common assertion helper utilities
```

## Dependencies

- None (pure .NET)

## Dependents

- All platform packages (Brinell.Maui, Brinell.Blazor, Brinell.Wpf)
- All test projects

## Design Rules

1. No references to automation libraries (Appium, Selenium, Playwright)
2. No platform-specific types or code
3. Target .NET Standard 2.0 for maximum compatibility
4. Interfaces are stable — breaking changes require major version bump
5. Cross-cutting implementations use only .NET types

## Validation

- [ ] No external package dependencies
- [ ] Compiles against .NET Standard 2.0
- [ ] No platform-specific code
- [ ] No automation library references

---

## Related Documents

- [ADR-001 Clean Architecture](../202_Decisions/202_001_CleanArchitecture.spx.md)
- [ADR-002 Interface-First](../202_Decisions/202_002_InterfaceFirst.spx.md)
- [Platform Layer](203_002b_PlatformLayer.spx.md)
