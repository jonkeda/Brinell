# Codebase Structure

**Analysis Date:** 2026-03-02

## Directory Layout

```
Brinell/
├── srcnew/                      # Active source (replaces legacy src/)
│   ├── Brinell.Core/            # Platform-agnostic contracts and base classes
│   ├── Brinell.Maui/            # MAUI platform controls, pages, test infra
│   ├── Brinell.Maui.Appium/     # Appium driver adapter for MAUI
│   ├── Brinell.Maui.FlaUI/      # FlaUI/UIA3 driver adapter (Windows MAUI)
│   ├── Brinell.Maui.CommunityToolkit/ # CommunityToolkit.Maui control wrappers
│   ├── Brinell.Html/            # HTML/web platform controls and pages
│   ├── Brinell.Html.Playwright/ # Playwright driver adapter for HTML
│   ├── Brinell.Blazor/          # Blazor-specific controls (extends Html)
│   ├── Brinell.Wpf/             # WPF platform controls (FlaUI, net-windows)
│   ├── Brinell.WinForms/        # WinForms platform controls (FlaUI, net-windows)
│   ├── Brinell.Stride/          # Stride 3D game test client
│   ├── Brinell.Automation/      # Stride in-game automation server
│   ├── Brinell.Mocking/         # WireMock-based API mock infrastructure
│   ├── explanation/             # Implementation deviation and design notes
│   └── testsnew/                # Symlink/alias for testsnew root (check)
├── testsnew/                    # Active tests (replaces legacy tests/)
│   ├── Brinell.Core.Tests/      # Unit tests for Core layer
│   ├── Brinell.Maui.Tests/      # Unit tests for Maui controls (Moq)
│   ├── Brinell.Maui.UITests/    # End-to-end UITests against sample MAUI app
│   ├── Brinell.Blazor.Tests/    # Unit tests for Blazor controls
│   ├── Brinell.Blazor.UITests/  # E2E Playwright tests for Blazor app
│   ├── Brinell.Html.Tests/      # Unit tests for Html controls
│   ├── Brinell.Html.UITests/    # E2E Selenium/Playwright UITests
│   ├── Brinell.Wpf.Tests/       # Unit tests for WPF controls
│   ├── Brinell.Wpf.UITests/     # E2E UITests for WPF sample app
│   ├── Brinell.WinForms.Tests/  # Unit tests for WinForms controls
│   ├── Brinell.WinForms.UITests/# E2E UITests for WinForms sample app
│   ├── Brinell.Stride.Tests/    # Unit tests for Stride controls
│   ├── Brinell.Stride.UITests/  # E2E UITests for Stride sample game
│   ├── Brinell.Mocking.Tests/   # Unit tests for mocking infrastructure
│   ├── Brinell.Automation.Tests/# Unit tests for Automation server
│   ├── Directory.Build.props    # Test-wide: TargetFramework=net10.0, xUnit packages, banned FluentAssertions
│   └── Directory.Packages.props # Imports root packages
├── samples/                     # Sample apps and sample tests per platform
│   ├── Brinell.Samples.Maui.App/
│   ├── Brinell.Samples.Maui.UITests/
│   ├── Brinell.Samples.Blazor.App/
│   ├── Brinell.Samples.Blazor.UITests/
│   ├── Brinell.Samples.Wpf.App/
│   ├── Brinell.Samples.Wpf.UITests/
│   └── ...
├── docs/                         # User and developer documentation (Markdown)
├── SPX/                          # Git submodule (SPX tooling)
├── .github/                      # Copilot instructions, skills, agents, prompts
├── .planning/                    # GSD project planning documents
├── Directory.Build.props         # Root build settings (LangVersion, Nullable, Version, SourceLink)
├── Directory.Packages.props      # Central NuGet package versions
├── global.json                   # .NET SDK pinning (10.0.100)
├── Brinell.sln                   # Solution file (includes srcnew + testsnew)
├── nuget.config                  # NuGet feed configuration
└── run-android-tests.ps1         # PowerShell test runner scripts
```

## Directory Purposes

**`srcnew/Brinell.Core/`:**
- Purpose: Platform-agnostic framework contracts — the only project with zero automation driver dependencies
- Contains: `Interfaces/`, `Abstractions/Controls/`, `Composition/`, `Locators/`, `Exceptions/`, `Configuration/`, `Logging/`, `Services/`, `Models/`, `Testing/`, `Utilities/`, `Attributes/`
- Key files: `Interfaces/IControlObject.cs`, `Interfaces/IPageObject.cs`, `Interfaces/IDriver.cs`, `Interfaces/ITestContext.cs`, `Locators/Locator.cs`, `Configuration/TimeoutSettings.cs`

**`srcnew/Brinell.Maui/`:**
- Purpose: MAUI platform implementation — controls, page bases, test context, driver factory
- Contains: `Controls/` (flat + subfolders per category), `Pages/PageObjectBase.cs`, `Context/`, `Interfaces/`, `Testing/`, `Enums/`, `Gestures/`
- Key files: `Controls/ControlBase.cs`, `Controls/ContainerBase.cs`, `Pages/PageObjectBase.cs`, `MauiDriverFactory.cs`, `Testing/MauiTestFixtureBase.cs`
- Control subfolders: `Buttons/`, `Text/`, `Toggle/`, `Display/`, `Range/`, `Selection/`, `DateTime/`, `Container/`, `Collection/`, `Navigation/`, `Media/`

**`srcnew/Brinell.Maui.Appium/`:**
- Purpose: Appium driver adapter — wraps `AppiumDriver` into `IDriver<AppiumElement>`
- Key files: `AppiumMauiDriver.cs`, `AppiumMauiElement.cs`, `LocatorExtensions.cs`

**`srcnew/Brinell.Html/` + `Brinell.Html.Playwright/` + `Brinell.Blazor/`:**
- Purpose: Layered HTML automation — Html = base controls/pages, Playwright = driver, Blazor = Blazor-specific extensions
- Pattern: `Brinell.Blazor` references both Html and Html.Playwright; `InternalsVisibleTo` from Playwright to Blazor

**`srcnew/Brinell.Mocking/`:**
- Purpose: WireMock-based API server for intercepting HTTP calls during UI tests
- Key files: `MockApiServer.cs` (stub, not yet fully implemented), `ApiStubBuilder.cs`

**`testsnew/Brinell.Maui.UITests/`:**
- Purpose: Integration/E2E tests against the sample MAUI app via Appium
- Contains: `AppiumFixture.cs`, `AppiumCollection.cs`, `Pages/`, `Tests/` (subfolders by feature), `Containers/`
- Key files: `AppiumFixture.cs` (test fixture + page object aggregation), `Tests/MainPageTests.cs`

## Key File Locations

**Entry Points:**
- `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` — MAUI UITest fixture; inherits `MauiTestFixtureBase`
- `srcnew/Brinell.Maui/Testing/MauiTestFixtureBase.cs` — base fixture; reads env vars, starts Appium session

**Configuration:**
- `global.json` — .NET SDK version pin
- `Directory.Build.props` — root build settings
- `Directory.Packages.props` — all package versions
- `testsnew/Directory.Build.props` — test-specific overrides + banned packages enforcement

**Core Contracts:**
- `srcnew/Brinell.Core/Interfaces/IControlObject.cs` — universal control interface
- `srcnew/Brinell.Core/Interfaces/IPageObject.cs` — page/screen interface
- `srcnew/Brinell.Core/Interfaces/IDriver.cs` — driver interface
- `srcnew/Brinell.Core/Interfaces/ITestContext.cs` — test session interface
- `srcnew/Brinell.Core/Locators/Locator.cs` — immutable locator value object
- `srcnew/Brinell.Core/Configuration/TimeoutSettings.cs` — structured timeout config

**MAUI Controls (representative):**
- `srcnew/Brinell.Maui/Controls/ControlBase.cs` — base for all MAUI controls
- `srcnew/Brinell.Maui/Controls/ContainerBase.cs` — base for scoped container controls
- `srcnew/Brinell.Maui/Controls/Buttons/Button.cs` — concrete control example
- `srcnew/Brinell.Maui/Pages/PageObjectBase.cs` — all page objects inherit this

**Testing:**
- `testsnew/Brinell.Maui.UITests/Tests/MainPageTests.cs` — canonical UITest example
- `testsnew/Brinell.Maui.Tests/FluentChainingTests.cs` — unit test using Moq

**Documentation:**
- `docs/` — user guides, migration guide, best practices
- `srcnew/explanation/DEVIATIONS-Phase1.md` — implementation decisions and deviations from spec

## Naming Conventions

**Files:**
- `PascalCase.cs` for all C# files matching the primary type name
- `GlobalUsings.cs` — global usings per project
- `*.csproj` — project file named by project root namespace

**Directories:**
- `PascalCase` for all directories — `Controls/`, `Interfaces/`, `Pages/`, `Testing/`
- Control category subfolders use plural (e.g., `Buttons/`, `Text/`, `Toggle/`, `Collection/`)

**Namespaces:**
- Mirror directory structure: `Brinell.Maui.Controls.Buttons`, `Brinell.Core.Interfaces`
- Test projects: `Brinell.Maui.UITests`, `Brinell.Maui.Tests`

## Where to Add New Code

**New MAUI control:**
- Implementation: `srcnew/Brinell.Maui/Controls/{Category}/{ControlName}.cs`
- Interface (if adds new capability): `srcnew/Brinell.Core/Interfaces/I{Capability}ControlObject.cs`
- Factory method: add to `srcnew/Brinell.Maui/Pages/PageObjectBase.cs` AND `srcnew/Brinell.Maui/Controls/ContainerBase.cs`
- Tests: `testsnew/Brinell.Maui.Tests/{ControlName}Tests.cs`

**New page object in UITests:**
- Implementation: `testsnew/Brinell.Maui.UITests/Pages/{PageName}Page.cs` — inherit `PageObjectBase<{PageName}Page>`
- Registration: add `[TestPage]`; fixture uses `[TestModuleScan]` and `TestComposition.ForFixture(...)`
- Tests: `testsnew/Brinell.Maui.UITests/Tests/{Feature}/{PageName}Tests.cs`

**New platform driver:**
- New project under `srcnew/Brinell.{Platform}.{Driver}/`
- Implement `IDriver<TElement>` and `IElement<TElement>`
- Reference in `testsnew/Directory.Build.props` or per test project

**New exception type:**
- Location: `srcnew/Brinell.Core/Exceptions/{ExceptionName}.cs`
- Inherit from `BrinellException`
