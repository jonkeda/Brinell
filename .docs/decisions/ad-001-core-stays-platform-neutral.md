# AD-001: Core Stays Platform-Neutral

`Bruinet.Core` owns contracts and shared utilities. Platform element types stay
in platform projects.

**Ruled out:** platform element types (`IMauiElement`, FlaUI types, Playwright
handles) leaking into `Bruinet.Core`.

**Broken when:** `Bruinet.Core` references a platform driver package, or a Core
type names a platform element.
