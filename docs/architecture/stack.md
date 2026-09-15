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
| `APPIUM_PLATFORM` | `windows`, `android`, or `ios`. `windows` selects the FlaUI driver; only `android` and `ios` use Appium |
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
| `BRINELL_BACKGROUND_MODE` | Physical input policy for WPF and WinForms - see [AD-005](decisions.md#ad-005-physical-input-is-opt-in). Unset or `0`: performed. `1`: refused. `audit`: performed and recorded. No effect on MAUI, which uses no physical input |
| `BRINELL_PHYSICAL_INPUT_LOG` | File that audited physical-input uses are appended to (WPF and WinForms) |
| `BRINELL_AUT_PLACE` | Where to put the MAUI app window on launch: `right`, `offscreen`, `secondary`. `offscreen` breaks visibility checks - UI Automation counts the monitor - so leave it unset unless you know you need it |
| `BRINELL_AUT_PLACEMENT_RESULT_FILE` | File the driver writes where the window was actually placed |
| `BRINELL_UIA_BRIDGE` | Gesture bridge. As an MSBuild constant, puts the bridge in the build (Debug only by default). As a variable set to `1`, turns it on at run time; the FlaUI driver sets it on the app it launches |
| `BRINELL_UIA_LOG` | File the app under test appends every bridge publish and verb call to. The first thing to read when a bridge verb "is not answered" |
| `BRINELL_APP_CRASH_LOG` | File the MAUI sample app writes unhandled exceptions to |
| `BRINELL_STRESS` | `1` runs the opt-in stress tests (`Pattern=Stress`) |

`BRINELL_ALLOW_POINTER_INPUT` and `BRINELL_WINDOWS_INTERACTION_MODE` were documented
here previously; no code reads either of them. Use `BRINELL_BACKGROUND_MODE`.
