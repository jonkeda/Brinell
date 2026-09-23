# Framework Overview

Brinell gives tests a consistent page-object and control-object model across
desktop, web, MAUI, Stride, native Android, UAT markdown scenarios, and the
Presenter shell.

## Layers

```text
Tests and UAT scenarios
  Page objects and fixture/runtime glue
    Brinell controls and platform contexts
      Brinell.Core interfaces, locators, waits, artifacts
        Platform drivers: FlaUI, Playwright, Selenium, Appium, Stride, Android
```

## Project Families

| Family | Purpose |
| --- | --- |
| `Brinell.Core` | Shared interfaces, locators, artifacts, wait helpers, assertions |
| `Brinell.Maui` | MAUI page objects and controls |
| `Brinell.Maui.Appium` | Appium driver adapter for MAUI/mobile |
| `Brinell.Maui.FlaUI` | Windows/FlaUI driver adapter for MAUI desktop |
| `Brinell.Wpf` | WPF automation through FlaUI |
| `Brinell.WinForms` | WinForms automation through FlaUI |
| `Brinell.Html` | HTML/web abstractions and Selenium support |
| `Brinell.Html.Playwright` | Playwright-backed web driver |
| `Brinell.Blazor` | Blazor-focused web testing helpers |
| `Brinell.Stride` and `Brinell.Automation` | Stride UI automation and in-game hooks |
| `Brinell.Mocking` | WireMock and mock sensor helpers |
| `Brinell.NativeAndroid` | Native Android automation helpers |
| `Brinell.Uat` | Markdown-driven UAT scenario runtime |
| `Brinell.Presenter` | Desktop presenter for UAT workspaces |

## Design Principles

- Tests express user intent.
- Page objects own screen structure.
- Controls own repeated interaction behavior.
- Platform drivers stay behind Brinell abstractions.
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
