# Technology Stack

**Analysis Date:** 2026-03-02

## Languages

**Primary:**
- C# (latest LangVersion, C# 12/13 features) — all framework and test code

**Secondary:**
- PowerShell — build and test runner scripts (`run-android-tests.ps1`, `start-appium.ps1`, `test-key.ps1`)

## Runtime

**Environment:**
- .NET 10 SDK 10.0.100 (rollForward: latestFeature) — default target for all framework projects
- Windows-only projects target `net8.0-windows;net9.0-windows;net10.0-windows` (WPF, WinForms)
- Stride targets `net10.0` only (engine constraint)

**Package Manager:**
- NuGet with Central Package Management (`Directory.Packages.props`) — all versions declared centrally, no per-project version pins allowed

## Frameworks

**Core (platform-agnostic):**
- `Brinell.Core` — no framework dependencies; xunit.extensibility.core (PrivateAssets=all) for xUnit attribute base classes only

**Mobile/MAUI:**
- `Microsoft.Maui.Controls` 10.0.1 — MAUI app controls (referenced in `Brinell.Maui.csproj`)
- `Appium.WebDriver` 8.0.1 — driver for Android/iOS and Windows MAUI automation
- `CommunityToolkit.Maui` 9.0.0 — MAUI community toolkit controls (`Brinell.Maui.CommunityToolkit`)

**WPF/WinForms:**
- `FlaUI.Core` 5.0.0 + `FlaUI.UIA3` 5.0.0 — UIA3-based automation for WPF and WinForms on Windows

**Web/Blazor:**
- `Microsoft.Playwright` 1.50.0 — browser automation for Blazor Server and WASM
- `Selenium.WebDriver` 4.29.0 + `Selenium.Support` 4.29.0 — Selenium for HTML/web (alternative driver)
- `WebDriverManager` 2.17.5 — automatic browser driver management for Selenium

**3D/Game:**
- `Stride.Engine` 4.3.0.2507 + `Stride.UI` — Stride game engine integration
- `Stride.CommunityToolkit.Windows` 1.0.0-preview.62 + `Stride.CommunityToolkit.Bepu`

**API Mocking:**
- `WireMock.Net` 1.6.10 — HTTP mock server for simulating backend during UI tests

**Testing:**
- xUnit 2.9.3 — test runner (unit and UI tests)
- Moq 4.20.70 — mocking for unit tests
- AutoFixture 4.18.1 + AutoFixture.Xunit2 — test data generation
- Bogus 35.5.1 — fake data generation
- coverlet.collector 6.0.4 — code coverage
- Microsoft.NET.Test.Sdk 17.14.0

> **Important:** FluentAssertions is **banned** (wrong license). Use xUnit `Assert` only. A compile-time `CheckBannedPackages` target enforces this in `testsnew/Directory.Build.props`.

## Key Dependencies

**Critical:**
- `Appium.WebDriver` 8.0.1 — MAUI mobile/Windows automation driver; referenced in `Brinell.Maui` and `Brinell.Maui.Appium`
- `Microsoft.Playwright` 1.50.0 — Blazor browser automation; referenced in `Brinell.Html.Playwright` and `Brinell.Blazor`
- `FlaUI.Core` + `FlaUI.UIA3` 5.0.0 — WPF/WinForms desktop automation; referenced in `Brinell.Wpf` and `Brinell.WinForms`
- `WireMock.Net` 1.6.10 — API mocking in `Brinell.Mocking`; has a known suppressed vulnerability (NU1903) in transitive `System.Linq.Dynamic.Core`

**Infrastructure:**
- `Microsoft.SourceLink.GitHub` 8.0.0 — source-linked PDB packages for debugging NuGet packages
- `Microsoft.EntityFrameworkCore` 10.0.0 + SQLite — declared in packages but not yet actively used in srcnew
- `Serilog` 4.1.0 + `Serilog.Sinks.Console` — declared in packages, available for logging implementations

## Configuration

**Environment (UI tests):**
- `APPIUM_SERVER_URI` — Appium server URL (default: `http://127.0.0.1:4723`)
- `APPIUM_PLATFORM` — `windows`, `android`, or `ios` (default: `windows`)
- `APPIUM_APP_PATH` — path to app executable/package
- `APPIUM_DEVICE_NAME` — device/emulator name (Android/iOS)
- `APPIUM_PLATFORM_VERSION` — iOS platform version
- `APPIUM_ATTACH_TO_RUNNING` — attach to already-running app
- `APPIUM_PROCESS_NAME` / `APPIUM_WINDOW_HANDLE` — process/window targeting

**Build:**
- `Directory.Build.props` — root: LangVersion=latest, Nullable=enable, ImplicitUsings=enable, TreatWarningsAsErrors=true, Version=0.1.0
- `Directory.Packages.props` — central package version management
- `global.json` — .NET SDK 10.0.100, rollForward: latestFeature
- `testsnew/Directory.Build.props` — adds TargetFramework=net10.0, IsTestProject=true, common test package references, banned-package check

## Platform Requirements

**Development:**
- Windows required for WPF/WinForms tests (net-windows TFMs)
- Appium server (`start-appium.ps1`) must be running for MAUI UITests
- Android emulator or physical device for Android tests
- iOS simulator or physical device for iOS tests (macOS only)

**Distribution:**
- All `srcnew/` projects produce NuGet packages (`GeneratePackageOnBuild=true`)
- Symbol packages in `.snupkg` format (`IncludeSymbols=true`, `SymbolPackageFormat=snupkg`)
- Source Link enabled for GitHub repository

---

*Stack analysis: 2026-03-02*
