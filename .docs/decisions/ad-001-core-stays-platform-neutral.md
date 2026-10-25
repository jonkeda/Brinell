# AD-001: Core Stays Platform-Neutral

`Bruinell.Core` owns contracts and shared utilities. Platform element types stay
in platform projects.

**Ruled out:** platform element types (`IMauiElement`, FlaUI types, Playwright
handles) leaking into `Bruinell.Core`.

**Broken when:** `Bruinell.Core` references a platform driver package, or a Core
type names a platform element.
