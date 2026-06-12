# Technology Stack

Brinell uses .NET with central package management.

## Runtime

- SDK: controlled by `global.json`.
- Source projects: default `net8.0;net9.0;net10.0`.
- Windows source projects: `net8.0-windows;net9.0-windows;net10.0-windows`.
- Tests: default `net10.0`, with Windows UI/UAT tests using Windows TFMs where
  required.

## Major Dependencies

| Area | Dependencies |
| --- | --- |
| Tests | xUnit, Microsoft.NET.Test.Sdk, coverlet |
| Mocking | Moq, NSubstitute, AutoFixture, Bogus |
| Desktop UI | FlaUI.Core, FlaUI.UIA3 |
| Web | Selenium.WebDriver, Selenium.Support, WebDriverManager, Microsoft.Playwright |
| MAUI | Microsoft.Maui.Controls, Appium.WebDriver, CommunityToolkit.Maui |
| Backend mocking | WireMock.Net |
| Stride | Stride.Engine, Stride.UI, Stride.Graphics, Stride.Rendering |
| Presenter | WebView2, SQLite, Roslyn, Copilot SDK dependencies |

Package versions live in `Directory.Packages.props` and
`srcnew/Directory.Packages.props`. Do not pin package versions in individual
project files unless there is a deliberate exception.

## Assertion Policy

Use xUnit `Assert`. FluentAssertions is banned in test projects by
`testsnew/Directory.Build.props`.

## Common Environment Variables

| Variable | Purpose |
| --- | --- |
| `APPIUM_SERVER_URI` | Appium server URL |
| `APPIUM_PLATFORM` | `windows`, `android`, or `ios` |
| `APPIUM_APP_PATH` | App executable or package path |
| `APPIUM_DEVICE_NAME` | Device/emulator name |
| `WPF_APP_PATH`, `WINFORMS_APP_PATH` | Desktop sample/app executable paths |
| `BLAZOR_APP_URL`, `BLAZOR_APP_PATH`, `HTML_APP_PATH` | Web and Blazor test host inputs |
| `HEADLESS`, `BROWSER_TYPE` | Browser run mode |
| `STRIDE_APP_PATH` | Stride app executable path |
| `BRINELL_AUTOMATION` | Enables Brinell automation hooks where supported |
| `BRINELL_PRESENTER_SETTINGS_PATH` | Presenter settings file path |
| `BRINELL_TEST_RESULTS_DIR` | Overrides the `TestResults` root |
| `BRINELL_TEST_RUN_ID` | Reuses a run folder across projects |
| `BRINELL_TEST_SUITE` | Overrides the artifact suite name |
| `BRINELL_ALLOW_POINTER_INPUT` | Enables opt-in pointer actions for gesture-only cases |
| `BRINELL_WINDOWS_INTERACTION_MODE` | Windows interaction mode: `semantic` or `interactive` |

Some older code paths may still mention `BRINELL_WINDOWS_ALLOW_*` variables.
Prefer the current `BRINELL_ALLOW_*` names in new docs and examples.
