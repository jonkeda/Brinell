# Brinell

Brinell is a cross-platform UI testing framework for .NET applications. It
provides shared page-object, control-object, synchronization, artifact, mocking,
and UAT patterns across desktop, web, MAUI, Stride, native Android, and the
Presenter shell.

## Current Docs

- [Documentation index](docs/README.md)
- [Quick Start](docs/getting-started/quick-start.md)
- [Framework Overview](docs/getting-started/framework-overview.md)
- [Build And Test](docs/run/build-and-test.md)
- [Spec Status](docs/specs/README.md)

The previous documentation tree is preserved in [docs2](docs2/README.md) while
the active docs are rebuilt.

## Build

Working directory: Brinell root.

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
```

Use `srcnew\Brinell.sln` as the broad active compile check. It covers many
source, sample, and test projects, but it is not a complete project inventory.
The top-level `Brinell.sln` includes a different slice plus tools and may fail
for tool-specific restore policies even when the framework projects build.

## Packages

| Package | Purpose |
| --- | --- |
| `Brinell.Core` | Core contracts, locators, waits, artifacts, and shared services |
| `Brinell.Maui` | MAUI controls and page-object infrastructure |
| `Brinell.Maui.Appium` | Appium driver adapter for MAUI/mobile |
| `Brinell.Maui.FlaUI` | FlaUI driver adapter for Windows MAUI |
| `Brinell.Maui.CommunityToolkit` | MAUI Community Toolkit control support |
| `Brinell.Wpf` | WPF automation through FlaUI |
| `Brinell.WinForms` | WinForms automation through FlaUI |
| `Brinell.Html` | HTML/web testing abstractions and Selenium support |
| `Brinell.Html.Playwright` | Playwright-backed web automation |
| `Brinell.Blazor` | Blazor-focused testing helpers |
| `Brinell.Stride` | Stride UI testing integration |
| `Brinell.Automation` | In-app automation hooks used by Stride-style tests |
| `Brinell.Mocking` | WireMock and mock sensor helpers |
| `Brinell.NativeAndroid` | Native Android automation helpers |
| `Brinell.Uat` | Markdown-driven UAT scenario runtime |
| `Brinell.Presenter` | Desktop presenter for UAT workspaces |

## Source Layout

| Path | Purpose |
| --- | --- |
| `srcnew/` | Active source projects |
| `testsnew/` | Active unit, UI, and UAT tests |
| `samples/` | Sample applications used by tests and demos |
| `docs/` | Active documentation |
| `docs2/` | Preserved previous documentation |
| `.my/reports/` | Planning and research notes |

## Core Rules

- Tests express user intent.
- Page objects own screen structure.
- Controls own repeated interaction behavior.
- Wait for concrete UI state instead of adding arbitrary sleeps.
- Use xUnit `Assert`; do not add FluentAssertions.
- Route screenshots, logs, traces, and UAT output through `TestResults/<run-id>/`.

## License

Brinell is licensed under the MIT License. See [LICENSE](LICENSE).
