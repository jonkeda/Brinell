# ADR-003: Platform Separation

**Block:** 202 decision
**Edition:** 🟡Ⅱ Core
**Version:** 1.0
**Created:** January 7, 2026

---

## decision ADR-003

- **title**: Separate NuGet Packages per Platform
- **status**: accepted
- **date**: 2026-01-07
- **context**: Framework supports multiple UI platforms with different automation drivers; users typically target one platform per test project.
- **decision**: Create separate NuGet packages per platform (Brinell.MAUI, Brinell.Blazor, etc.), each depending on Brinell.Core.
- **consequences**: Clean dependencies per platform, users install only what they need, requires maintaining multiple packages.

---

## 1. Context

The Brinell framework supports UI test automation across:

| Platform | Automation Driver                | Target                     |
| -------- | -------------------------------- | -------------------------- |
| MAUI     | Appium WebDriver                 | iOS, Android, Windows, Mac |
| Blazor   | Selenium WebDriver or Playwright | Web browsers               |
| WPF      | WinAppDriver                     | Windows desktop            |
| WinForms | WinAppDriver                     | Windows desktop            |
| Stride   | Custom (future)                  | Game engine                |

Each platform has:

- Different NuGet dependencies (Appium, Selenium, Playwright, WinAppDriver)
- Different locator strategies
- Platform-specific timing and behavior
- Unique control implementations

### Problem

How do we structure packages so that:

1. Users get only the dependencies they need
2. Platform code doesn't leak across boundaries
3. Maintenance is manageable
4. Core interfaces are shared

---

## 2. Decision

**Separate NuGet packages per platform**, all depending on a shared Brinell.Core:

```
                    ┌─────────────────┐
                    │  Brinell.Core   │
                    │  (no deps)      │
                    └────────┬────────┘
                             │
     ┌───────────┬───────────┼───────────┬───────────┐
     ▼           ▼           ▼           ▼           ▼
┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐
│ MAUI    │ │ Blazor  │ │ WPF     │ │WinForms │ │ Stride  │
│ +Appium │ │+Selenium│ │+WinAppDr│ │+WinAppDr│ │ +Custom │
└─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘
```

### Package Structure

| Package                   | Dependencies              | Size                    |
| ------------------------- | ------------------------- | ----------------------- |
| Brinell.Core              | None                      | Small (interfaces only) |
| Brinell.MAUI              | Core + Appium.WebDriver   | Medium                  |
| Brinell.Blazor.Selenium   | Core + Selenium.WebDriver | Medium                  |
| Brinell.Blazor.Playwright | Core + Playwright         | Medium                  |
| Brinell.WPF               | Core + WinAppDriver       | Medium                  |
| Brinell.WinForms          | Core + WinAppDriver       | Medium                  |
| Brinell.Stride            | Core + (TBD)              | Medium                  |

### User Installation

```powershell
# MAUI test project
dotnet add package Brinell.MAUI

# Blazor test project with Selenium
dotnet add package Brinell.Blazor

# Blazor test project with Playwright
dotnet add package Brinell.Blazor.Playwright
```

---

## 3. Consequences

### Positive

| Benefit                       | Description                                                    |
| ----------------------------- | -------------------------------------------------------------- |
| **Clean dependencies**  | MAUI tests don't pull Selenium; Blazor tests don't pull Appium |
| **Smaller packages**    | Users install only what they need                              |
| **Clear boundaries**    | Platform code is isolated                                      |
| **Independent updates** | Can update MAUI without affecting Blazor                       |
| **Focused testing**     | Each package has its own unit tests                            |

### Negative

| Trade-off                            | Mitigation                                                        |
| ------------------------------------ | ----------------------------------------------------------------- |
| **Multiple packages**          | Shared build scripts, consistent versioning                       |
| **Code duplication**           | Base classes in Core, platform-specific only in platform packages |
| **Version coordination**       | All packages released together with same version                  |
| **Documentation per platform** | Platform-specific guides, shared concepts in Core docs            |

### Neutral

| Aspect               | Notes                      |
| -------------------- | -------------------------- |
| **Build time** | Parallel builds possible   |
| **CI/CD**      | Matrix builds per platform |

---

## 4. Alternatives Considered

### Alternative 1: Single Package with All Platforms

```xml
<!-- NOT CHOSEN -->
<PackageReference Include="Brinell" Version="1.0.0" />
<!-- Pulls ALL drivers: Appium, Selenium, WinAppDriver... -->
```

**Rejected because:**

- Massive dependency tree
- Build failures if any driver has issues
- Unnecessary packages in every project
- Security surface area

### Alternative 2: Metapackage with Optional Dependencies

```xml
<!-- NOT CHOSEN -->
<PackageReference Include="Brinell" Version="1.0.0" />
<!-- Then conditional: -->
<PackageReference Include="Brinell.MAUI.Runtime" Condition="..." />
```

**Rejected because:**

- Complex installation
- Runtime discovery issues
- Confusing for users

### Alternative 3: Source-Only Core

```csharp
// NOT CHOSEN - Core distributed as source
// Each platform package includes Core source
```

**Rejected because:**

- Version conflicts possible
- No shared binary for Core
- Harder to update Core

---

## 5. Package Dependencies

### Brinell.Core

```xml
<Project>
  <!-- NO external dependencies -->
  <ItemGroup>
    <!-- Only .NET SDK references -->
  </ItemGroup>
</Project>
```

### Brinell.MAUI

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="Brinell.Core" Version="$(BrinellVersion)" />
    <PackageReference Include="Appium.WebDriver" Version="5.*" />
  </ItemGroup>
</Project>
```

### Brinell.Blazor.Selenium

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="Brinell.Core" Version="$(BrinellVersion)" />
    <PackageReference Include="Selenium.WebDriver" Version="4.*" />
  </ItemGroup>
</Project>
```

### Brinell.Blazor.Playwright

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="Brinell.Core" Version="$(BrinellVersion)" />
    <PackageReference Include="Microsoft.Playwright" Version="1.*" />
  </ItemGroup>
</Project>
```

---

## 6. Namespace Convention

Each platform package uses consistent namespaces:

```csharp
// Core (shared interfaces)
namespace Brinell.Core;
namespace Brinell.Core.Interfaces;
namespace Brinell.Core.Exceptions;

// MAUI platform
namespace Brinell.MAUI;
namespace Brinell.MAUI.Controls;
namespace Brinell.MAUI.Context;

// Blazor platform  
namespace Brinell.Blazor;
namespace Brinell.Blazor.Controls;
namespace Brinell.Blazor.Context;
```

---

## 7. Versioning Strategy

All packages are versioned together:

| Package        | Version |
| -------------- | ------- |
| Brinell.Core   | 1.0.0   |
| Brinell.MAUI   | 1.0.0   |
| Brinell.Blazor | 1.0.0   |
| Brinell.WPF    | 1.0.0   |

### Version Compatibility

- Same major.minor = compatible
- Platform packages depend on exact Core version
- Breaking changes = major version bump for all

---

## 8. Cross-Platform Usage

For projects testing multiple platforms:

```xml
<!-- Multi-platform test project -->
<ItemGroup>
  <PackageReference Include="Brinell.MAUI" Version="1.0.0" />
  <PackageReference Include="Brinell.Blazor" Version="1.0.0" />
</ItemGroup>
```

Both packages share the same Brinell.Core, so interfaces are compatible.

---

## 9. Validation

This decision is validated when:

- [ ] Installing Brinell.MAUI does not pull Selenium
- [ ] Installing Brinell.Blazor does not pull Appium
- [ ] Brinell.Core has no external dependencies
- [ ] All platform packages share same Core version
- [ ] Cross-platform projects work with multiple packages

---

## Related Decisions

- [ADR-001: Clean Architecture](202_001_CleanArchitecture.spx.md)
- [ADR-002: Interface-First Design](202_002_InterfaceFirst.spx.md)
- [ADR-004: Control Interface Hierarchy](202_004_ControlHierarchy.spx.md)

---

## Related Documents

- [203_002_PlatformLayer.spx.md](../203_Layers/203_002_PlatformLayer.spx.md) — Platform layer details
- [220_External/](../220_External/) — External dependency specifications
