# 203.002b Platform Layer

**Block Type:** LYR (Layer)  
**Edition:** 🟢Ⅰ Lite

---

## Overview

The Platform layer contains **technology-specific implementations** of the Core interfaces. Each platform package (MAUI, Blazor, WPF) provides concrete control objects that work with a specific UI technology.

## Purpose

- Provide concrete control implementations for each platform
- Implement Core interfaces with platform-specific behavior
- Wrap automation libraries (Appium, Selenium, Playwright)
- Provide base classes for code reuse across controls

## Packages

### Brinell.MAUI

For .NET MAUI applications on Android, iOS, Windows, and Mac.

```
Brinell.MAUI/
├── Controls/     # Concrete control implementations (Button, Entry, etc.)
├── Base/         # Capability base classes (Clickable, Text, Toggle, etc.)
├── Context/      # TestContext and PageBase implementations
└── Utilities/    # Internal helpers (element finding, waiting)
```

**Automation SDK:** Appium.WebDriver

### Brinell.Blazor

For Blazor applications (Server and WebAssembly).

```
Brinell.Blazor/
├── Controls/     # Concrete control implementations (Button, Input, Select, etc.)
├── Base/         # Capability base classes
├── Context/      # TestContext and PageBase implementations
└── Utilities/    # Internal helpers
```

**Automation SDK:** Selenium.WebDriver (or Playwright)

### Brinell.WPF

For WPF desktop applications.

```
Brinell.WPF/
├── Controls/     # Concrete control implementations
├── Context/      # TestContext implementations
└── Utilities/    # Internal helpers
```

**Automation SDK:** Appium.WebDriver (WinAppDriver)

## Dependencies

- Brinell.Core (required)
- Platform-specific automation SDK

## Dependents

- Test projects using this platform

## Design Rules

1. All controls must implement interfaces from Core
2. Platform packages cannot depend on each other
3. Automation library types must not leak into public API
4. Base class hierarchy mirrors Core interface hierarchy
5. Control naming follows platform conventions

## Validation

- [ ] Implements all required Core interfaces
- [ ] Does not reference other platform packages
- [ ] Automation library types are internal only
- [ ] Has complete base class hierarchy

---

## Related Documents

- [Core Layer](203_001b_CoreLayer.spx.md)
- [Technology Layer](203_003b_TechnologyLayer.spx.md)
- [ADR-003 Platform Separation](../202_Decisions/202_003_PlatformSeparation.spx.md)
