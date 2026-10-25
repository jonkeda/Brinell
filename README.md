# Bruinell

Bruinell is a cross-platform UI testing framework for .NET applications. It
provides shared page-object, control-object, synchronization, artifact, mocking,
and UAT patterns across desktop, web, MAUI, Stride, native Android, and the
Presenter shell.

## Repository

Bruinell lives in [exact-mobile-apps/test-tooling](https://github.com/exact-mobile-apps/test-tooling).
It was moved there from `jonkeda/Bruinell` with its full history.

```powershell
git clone https://github.com/exact-mobile-apps/test-tooling.git Bruinell
```

## Current Docs

- [Documentation index](docs/README.md)
- [Quick Start](docs/getting-started/quick-start.md)
- [Framework Overview](docs/getting-started/framework-overview.md)
- [Build And Test](docs/run/build-and-test.md)
- [Spec Status](docs/specs/README.md)

The previous documentation tree is preserved in [docs2](docs2/README.md) while
the active docs are rebuilt.

## Build

Working directory: Bruinell root.

```powershell
dotnet build Bruinell.sln -v:minimal /nr:false
```

`Bruinell.sln` holds every project in the repository, in folders by area. The two
Visual Studio extension projects are in it but not built by it: they need Visual Studio's
MSBuild, and build from `tools\Bruinell.Bdd.VisualStudio\Bruinell.Bdd.VisualStudio.slnx`.

## Packages

| Package | Purpose |
| --- | --- |
| `Bruinell.Core` | Core contracts, locators, waits, artifacts, and shared services |
| `Bruinell.Maui` | MAUI controls and page-object infrastructure |
| `Bruinell.Appium` | What the Appium stacks share: error mapping, emulator host address, Android screen recorder |
| `Bruinell.Maui.Appium` | Appium driver adapter for MAUI/mobile |
| `Bruinell.Maui.FlaUI` | FlaUI driver adapter for Windows MAUI |
| `Bruinell.Maui.CommunityToolkit` | MAUI Community Toolkit control support |
| `Bruinell.Wpf` | WPF automation through FlaUI |
| `Bruinell.WinForms` | WinForms automation through FlaUI |
| `Bruinell.Html` | HTML/web testing abstractions and Selenium support |
| `Bruinell.Html.Playwright` | Playwright-backed web automation |
| `Bruinell.Blazor` | Blazor-focused testing helpers |
| `Bruinell.Stride` | Stride UI testing integration |
| `Bruinell.Stride.Automation` | In-app automation hooks used by Stride-style tests |
| `Bruinell.Mocking` | WireMock and mock sensor helpers |
| `Bruinell.NativeAndroid` | Native Android apps through Appium: page objects and generated controls in MAUI's and Html's shape |
| `Bruinell.Uat` | Markdown-driven UAT scenario runtime |
| `Bruinell.Presenter` | Desktop presenter for UAT workspaces |

## Source Layout

| Path | Purpose |
| --- | --- |
| `srcnew/` | Active source projects |
| `testsnew/` | Active unit, UI, and UAT tests |
| `samples/` | Sample applications used by tests and demos |
| `tools/` | The BDD language server, IDE extensions, and scripts |
| `Scraper.Sites/` | What the Scraper makes of public sites, one page per technology |
| `docs/` | Active documentation |
| `docs2/` | Preserved previous documentation |
| `.my/` | Plans for work in flight, indexed in `.my/README.md` |

## Core Rules

- Tests express user intent.
- Page objects own screen structure.
- Controls own repeated interaction behavior.
- Wait for concrete UI state instead of adding arbitrary sleeps.
- Use xUnit `Assert`; do not add FluentAssertions.
- Route screenshots, logs, traces, and UAT output through `TestResults/<run-id>/`.

## License

Bruinell is licensed under the MIT License. See [LICENSE](LICENSE).
