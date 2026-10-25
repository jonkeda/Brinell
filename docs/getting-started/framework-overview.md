# Framework Overview

Bruinell gives tests a consistent page-object and control-object model across
desktop, web, MAUI, Stride, native Android, UAT markdown scenarios, and the
Presenter shell.

## Layers

```text
Tests and UAT scenarios
  Page objects and fixture/runtime glue
    Bruinell controls and platform contexts
      Bruinell.Core interfaces, locators, waits, artifacts
        Platform drivers: FlaUI, Playwright, Selenium, Appium, Stride, Android
```

## Project Families

| Family | Purpose |
| --- | --- |
| `Bruinell.Core` | Shared interfaces, locators, artifacts, wait helpers, assertions |
| `Bruinell.Maui` | MAUI page objects and controls |
| `Bruinell.Maui.Appium` | Appium driver adapter for MAUI/mobile |
| `Bruinell.Maui.FlaUI` | Windows/FlaUI driver adapter for MAUI desktop |
| `Bruinell.Wpf` | WPF automation through FlaUI |
| `Bruinell.WinForms` | WinForms automation through FlaUI |
| `Bruinell.Html` | HTML/web abstractions, control objects, pages, containers and collections |
| `Bruinell.Html.Playwright` | Playwright-backed web driver |
| `Bruinell.Blazor` | Blazor-focused web testing helpers |
| `Bruinell.Stride` and `Bruinell.Stride.Automation` | Stride UI automation and in-game hooks |
| `Bruinell.Mocking` | WireMock and mock sensor helpers |
| `Bruinell.NativeAndroid` | Native Android automation helpers |
| `Bruinell.Uat` | Markdown-driven UAT scenario runtime |
| `Bruinell.Presenter` | Desktop presenter for UAT workspaces |

## Design Principles

- Tests express user intent.
- Page objects own screen structure.
- Controls own repeated interaction behavior.
- Platform drivers stay behind Bruinell abstractions.
- Synchronization waits for observable state.
- Test artifacts use the shared `TestResults/<run-id>/...` layout.
- Test settings load from `TestSettings/*.json` with default, local, scenario,
  and explicit layers.
- Public APIs prefer semantic operations over pointer or coordinate actions.

## Where To Go Next

- [Codebase Structure](../architecture/structure.md)
- [Control Object Index](../controls/index.md)
- [Testing](../architecture/testing.md)
- [Reporting And Artifacts](../guides/reporting-artifacts.md)
- [Test Settings](../guides/settings.md)
