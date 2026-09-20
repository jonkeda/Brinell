# AD-001: Core Stays Platform-Neutral

`Brinell.Core` owns contracts and shared utilities. Platform element types stay
in platform projects.

**Ruled out:** platform element types (`IMauiElement`, FlaUI types, Playwright
handles) leaking into `Brinell.Core`.

**Broken when:** `Brinell.Core` references a platform driver package, or a Core
type names a platform element.
