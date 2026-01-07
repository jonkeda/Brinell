# ADR-001: Clean Architecture

**Block:** 202 decision  
**Edition:** 🟡Ⅱ Core  
**Version:** 1.0  
**Created:** January 7, 2026

---

## decision ADR-001

- **title**: Clean Architecture for Framework Structure
- **status**: accepted
- **date**: 2026-01-07
- **context**: Need an architecture that supports multiple UI platforms (MAUI, Blazor, WPF, WinForms, Stride) while maintaining consistent patterns and enabling unit testing with mocks.
- **decision**: Adopt Clean Architecture with domain-centric layers where Core contains pure abstractions with no external dependencies.
- **consequences**: Clear separation of concerns, testable code, requires discipline to maintain layer boundaries.

---

## 1. Context

The Brinell framework must support UI test automation across multiple platforms:

- **MAUI** (iOS, Android, Windows, Mac) — using Appium
- **Blazor** (Web) — using Selenium or Playwright
- **WPF** (Windows Desktop) — using WinAppDriver
- **WinForms** (Windows Desktop) — using WinAppDriver
- **Stride** (Game Engine) — using custom automation

Each platform has different:
- Automation drivers
- Element locator strategies
- Platform-specific behaviors
- Timing characteristics

The framework needs a structure that:
1. Shares common logic across platforms
2. Allows platform-specific implementations
3. Enables unit testing without running applications
4. Supports adding new platforms without restructuring

---

## 2. Decision

Adopt **Clean Architecture** with the following layer structure:

```
┌─────────────────────────────────────────────────────────┐
│                    Test Layer                           │
│  (UITests, UnitTests - depends on Platform)             │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                  Platform Layer                         │
│  (Brinell.MAUI, Brinell.Blazor, etc.)                   │
│  Contains: Base classes, implementations                │
│  Depends on: Core + Automation SDK                      │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    Core Layer                           │
│  (Brinell.Core)                                         │
│  Contains: Interfaces, Exceptions, Contracts            │
│  Depends on: Nothing                                    │
└─────────────────────────────────────────────────────────┘
```

### Key Principles

1. **Core has no dependencies** — Pure C# interfaces and types only
2. **Platform depends on Core** — Never the reverse
3. **Test depends on Platform** — Uses concrete implementations
4. **Dependencies point inward** — Outer layers depend on inner layers

---

## 3. Consequences

### Positive

| Benefit | Description |
|---------|-------------|
| **Testability** | Core interfaces can be mocked; unit tests don't need running apps |
| **Platform independence** | Business logic (wait strategies, assertions) defined once in Core |
| **Extensibility** | New platforms only require new Platform packages |
| **Clear boundaries** | Obvious where each type of code belongs |
| **Consistency** | Same patterns and interfaces across all platforms |

### Negative

| Trade-off | Mitigation |
|-----------|------------|
| **More projects** | Clear naming conventions, organized folder structure |
| **Indirection** | Well-documented interfaces, consistent patterns |
| **Discipline required** | Code reviews, architecture documentation |
| **Initial complexity** | Comprehensive architecture documentation (this folder) |

### Neutral

| Aspect | Notes |
|--------|-------|
| **Package structure** | Separate NuGet packages per platform |
| **Build time** | Multiple projects, but parallel builds possible |

---

## 4. Alternatives Considered

### Alternative 1: Single Package with Conditionals

```csharp
// NOT CHOSEN
#if MAUI
    // MAUI-specific code
#elif BLAZOR
    // Blazor-specific code
#endif
```

**Rejected because:**
- Compile-time conditionals are hard to maintain
- All platform SDKs would be required for builds
- Cannot have different behavior per platform at runtime
- Testing requires building all variants

### Alternative 2: Plugin Architecture

```
Core -> loads -> Platform plugins at runtime
```

**Rejected because:**
- Over-engineered for current needs
- Runtime discovery adds complexity
- Most users use one platform per test project
- Can be added later if needed

### Alternative 3: No Core, Direct Platform Packages

```
Brinell.MAUI (standalone)
Brinell.Blazor (standalone)
// No shared code
```

**Rejected because:**
- Duplicates interfaces across packages
- No guarantee of API consistency
- Updates must be made in multiple places
- Cannot share wait strategies, assertion logic

---

## 5. Implementation

### Package Structure

```
Brinell.Core           # Interfaces and abstractions
Brinell.MAUI           # MAUI implementation (depends on Core)
Brinell.Blazor         # Blazor implementation (depends on Core)
Brinell.WPF            # WPF implementation (depends on Core)
Brinell.WinForms       # WinForms implementation (depends on Core)
Brinell.Stride         # Stride implementation (depends on Core)
```

### Core Contents

```csharp
// Brinell.Core
namespace Brinell.Core
{
    public interface IControlObject { ... }
    public interface IPageObject { ... }
    public interface IClickableControl : IControlObject { ... }
    // etc.
    
    public class ControlNotFoundException : Exception { ... }
    public class TimeoutException : Exception { ... }
    // etc.
}
```

### Platform Implementation

```csharp
// Brinell.MAUI
namespace Brinell.MAUI
{
    public abstract class ControlBase : IControlObject { ... }
    public class ButtonControl : ClickableControlBase { ... }
    // etc.
}
```

---

## 6. Validation

This decision is validated when:

- [ ] Brinell.Core compiles with no external dependencies
- [ ] Platform packages only reference Brinell.Core (not each other)
- [ ] Unit tests can mock IControlObject and run without apps
- [ ] New platform can be added without modifying Core
- [ ] Same interface works identically across platforms

---

## Related Decisions

- [ADR-002: Interface-First Design](202_002_InterfaceFirst.spx.md)
- [ADR-003: Platform Separation](202_003_PlatformSeparation.spx.md)
- [ADR-004: Control Interface Hierarchy](202_004_ControlHierarchy.spx.md)

---

## Related Documents

- [200_000_Overview.spx.md](../200_000_Overview.spx.md) — Architecture overview
- [203_Layers/](../203_Layers/) — Detailed layer specifications
