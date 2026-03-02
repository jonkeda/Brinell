# External Integrations

**Analysis Date:** 2026-03-02

## Test Automation Drivers

**Appium (Mobile/MAUI on Windows):**
- Purpose: drives MAUI apps on Android, iOS, and Windows via WebDriver protocol
- SDK/Client: `Appium.WebDriver` 8.0.1 — `AppiumDriver`, `AppiumElement`, `IOSDriver`, `AndroidDriver`
- Entry: `srcnew/Brinell.Maui.Appium/AppiumMauiDriver.cs` wraps `AppiumDriver`
- Config: `MauiDriverOptions` holds server URI, platform, app path, device name, capabilities
- Auth: no — local server connection only
- Server: requires Appium server running at `APPIUM_SERVER_URI` (default `http://127.0.0.1:4723`)

**FlaUI / UIA3 (WPF, WinForms):**
- Purpose: Windows UI Automation 3 for WPF and WinForms desktop apps
- SDK/Client: `FlaUI.Core` 5.0.0 + `FlaUI.UIA3` 5.0.0
- Entry: `srcnew/Brinell.Wpf/` and `srcnew/Brinell.WinForms/FlaUI/` — wrap FlaUI Application/AutomationElement
- Auth: no — in-process Windows automation
- Platform: Windows only (`net8.0-windows;net9.0-windows;net10.0-windows`)

**Microsoft Playwright (Blazor/HTML):**
- Purpose: drives Chromium, Firefox, or WebKit for Blazor Server/WASM and general HTML
- SDK/Client: `Microsoft.Playwright` 1.50.0 — `IPlaywright`, `IBrowser`, `IPage`
- Entry: `srcnew/Brinell.Html.Playwright/` — `PlaywrightHtmlElement`, `PlaywrightTestContext`
- Auth: no — local browser launch or connect
- Node: Playwright bundles its own Node.js runtime — see `bin/.playwright/node/`

**Selenium WebDriver (HTML):**
- Purpose: alternative web driver for HTML/web testing (non-Blazor or when Selenium preferred)
- SDK/Client: `Selenium.WebDriver` 4.29.0 + `Selenium.Support` 4.29.0 + `WebDriverManager` 2.17.5
- Entry: `srcnew/Brinell.Html/`
- Auth: no — local browser + WebDriverManager auto-downloads browser drivers

## API Mocking

**WireMock.Net:**
- Purpose: HTTP mock server to simulate backend APIs during UI tests without real network calls
- SDK/Client: `WireMock.Net` 1.6.10
- Entry: `srcnew/Brinell.Mocking/MockApiServer.cs` (stub — not yet fully implemented)
- Binding: listens on a configured port; test sets up stubs, app under test hits the mock
- Known issue: transitive dependency `System.Linq.Dynamic.Core` has a vulnerability (NU1903 suppressed in `Brinell.Mocking.csproj`)

## 3D/Game Engine

**Stride Engine:**
- Purpose: in-game automation for Stride 3D game applications
- SDK/Client: `Stride.Engine` 4.3.0.2507 + `Stride.UI`
- Entry: `srcnew/Brinell.Automation/` (game-side server) + `srcnew/Brinell.Stride/` (test client)
- Architecture: two-part — `Brinell.Automation` embeds in the game process; `Brinell.Stride` sends commands from the test process
- Platform: net10.0 only (Stride package constraint)

## CI/CD & Deployment

**GitHub Actions:**
- Repository: `https://github.com/jonkeda/Brinell` (per `Directory.Build.props`)
- Source Link: `Microsoft.SourceLink.GitHub` 8.0.0 activated in CI (`GITHUB_ACTIONS=true`)
- Deterministic builds enabled in CI via `ContinuousIntegrationBuild` property

**NuGet Publishing:**
- All `srcnew/` projects produce NuGet packages (`.nupkg` + `.snupkg` symbols)
- Feed: `nuget.config` at root — likely nuget.org or private feed (check file for specifics)
- Version: `0.1.0` currently (pre-release)

## Monitoring & Observability

**Screenshot Service:**
- Built-in: `srcnew/Brinell.Core/Services/ScreenshotService.cs` — captures screenshots on test failure
- Output: `TestResults/Screenshots/` directory at solution root
- Configuration: `ScreenshotSettings` (output directory, `CaptureOnFailure`, timestamp, format)
- xUnit integration: `ScreenshotTestAttribute` (in `Brinell.Maui`) hooks into xUnit's `BeforeAfterTestAttribute`

**Test Logging:**
- `ITestLogger` interface: `ConsoleTestLogger`, `CsvTestLogger`, `NullTestLogger` implementations in `srcnew/Brinell.Core/Logging/`
- Structured: log entry/exit/assertion/wait/navigation/error events
- CSV logger: produces machine-readable test execution logs

**Serilog:**
- Declared in `Directory.Packages.props` (`Serilog` 4.1.0, `Serilog.Sinks.Console` 6.0.0)
- Not yet actively wired in `srcnew/` — available for implementation

---

*Integrations analysis: 2026-03-02*
