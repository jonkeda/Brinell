# Bruinet

Bruinet is a cross-platform UI testing framework for .NET applications. It
provides shared page-object, control-object, synchronization, artifact, mocking,
and UAT patterns across desktop, web, MAUI, Stride, native Android, and the
Presenter shell.

## Repository

Bruinet lives in [exact-mobile-apps/test-tooling](https://github.com/exact-mobile-apps/test-tooling).
It was moved there from `jonkeda/Bruinet` with its full history.

```powershell
git clone https://github.com/exact-mobile-apps/test-tooling.git Bruinet
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

Working directory: Bruinet root.

```powershell
dotnet build Bruinet.sln -v:minimal /nr:false
```

`Bruinet.sln` holds every project in the repository, in folders by area. The two
Visual Studio extension projects are in it but not built by it: they need Visual Studio's
MSBuild, and build from `tools\Bruinet.Bdd.VisualStudio\Bruinet.Bdd.VisualStudio.slnx`.

## Packages

| Package | Purpose |
| --- | --- |
| `Bruinet.Core` | Core contracts, locators, waits, artifacts, and shared services |
| `Bruinet.Maui` | MAUI controls and page-object infrastructure |
| `Bruinet.Appium` | What the Appium stacks share: error mapping, emulator host address, Android screen recorder |
| `Bruinet.Maui.Appium` | Appium driver adapter for MAUI/mobile |
| `Bruinet.Maui.FlaUI` | FlaUI driver adapter for Windows MAUI |
| `Bruinet.Maui.CommunityToolkit` | MAUI Community Toolkit control support |
| `Bruinet.Wpf` | WPF automation through FlaUI |
| `Bruinet.WinForms` | WinForms automation through FlaUI |
| `Bruinet.Html` | HTML/web testing abstractions and Selenium support |
| `Bruinet.Html.Playwright` | Playwright-backed web automation |
| `Bruinet.Blazor` | Blazor-focused testing helpers |
| `Bruinet.Stride` | Stride UI testing integration |
| `Bruinet.Stride.Automation` | In-app automation hooks used by Stride-style tests |
| `Bruinet.Mocking` | WireMock and mock sensor helpers |
| `Bruinet.NativeAndroid` | Native Android apps through Appium: page objects and generated controls in MAUI's and Html's shape |
| `Bruinet.Uat` | Markdown-driven UAT scenario runtime |
| `Bruinet.Presenter` | Desktop presenter for UAT workspaces |

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

Bruinet is licensed under the MIT License. See [LICENSE](LICENSE).
