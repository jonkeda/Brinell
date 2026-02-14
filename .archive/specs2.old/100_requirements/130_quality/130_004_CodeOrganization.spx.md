# 130_004 Code Organization

## quality CodeOrganization

- **attribute**: Maintainability
- **requirement**: Framework code is well-organized with clear separation of concerns
- **priority**: high

---

## Description

This requirement ensures the framework codebase is maintainable through proper organization, clear dependencies, and separation between core abstractions and platform implementations.

---

## Sub-Requirements

### NFR-MAINT-001.1: Separation of Concerns

- Core interfaces MUST be separate from platform implementations
- Each platform implementation MUST be self-contained
- Test code MUST be separate from framework code

### NFR-MAINT-001.2: Clear Dependencies

- Framework dependencies MUST be explicitly declared
- Platform-specific dependencies MUST be isolated to platform projects
- Core project MUST have minimal dependencies

---

## Project Structure

```
src/
├── Brinell.Core/              # Interfaces + cross-cutting concerns
│   ├── Interfaces/            # Control and page interfaces
│   ├── Exceptions/            # Exception types
│   ├── Configuration/         # Configuration contracts
│   ├── Logging/               # Logging (contracts + default impl)
│   ├── Timeout/               # Timeout and wait utilities
│   ├── Retry/                 # Retry policies
│   └── Assertions/            # Common assertion logic
├── Brinell.Maui/              # MAUI + Appium implementation
├── Brinell.Blazor/            # Blazor + Selenium implementation
├── Brinell.Wpf/               # WPF + FlaUI implementation
└── Brinell.Testing/           # Test utilities
```

**Core Contains:**
- Interfaces (contracts for all controls and pages)
- Exceptions (framework exception types)
- Cross-cutting concerns (logging, timeout, retry, assertions) using only .NET types
- **NO** technology-specific code (no Appium, Selenium, Playwright references)

---

## Dependency Rules

| Project | May Reference | Must NOT Reference |
|---------|---------------|-------------------|
| Core | .NET Standard only | Platform projects, automation libraries |
| Maui | Core, Appium | Blazor, Wpf |
| Blazor | Core, Selenium/Playwright | Maui, Wpf |
| Wpf | Core, FlaUI/WinAppDriver | Maui, Blazor |
| Testing | Core | Platform projects |

---

## Acceptance Criteria

- Architecture review validates separation
- No circular dependencies
- Core project compiles without platform SDKs

---

## Related

- [NFR-MAINT-002 Code Quality](130_005_CodeQuality.spx.md)
- [FR-008 Extensibility](../120_functional/120_008_Extensibility.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-MAINT-001
