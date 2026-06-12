# Architecture

**Analysis Date:** 2026-03-02

## Pattern Overview

**Overall:** Multi-platform UI Automation Framework using Page Object Model (POM) with Curiously Recurring Template Pattern (CRTP) for fluent method chaining

**Key Characteristics:**
- Platform-agnostic Core defines contracts (interfaces); platform projects implement them
- CRTP (`TScope` / `TSelf`) enables strongly-typed fluent chaining without casting
- Is/Wait/Assert triple pattern on every control: `IsExists()` → `WaitExists(bool?)` → `AssertExists(bool?)`
- Immutable `Locator` value objects decouple element finding from platform driver details
- Stopwatch-based polling in `ControlBase.Poll()` — no `Thread.Sleep`, no `Task.Delay`

## Layers

**Layer 1 — Core Contracts (`srcnew/Brinell.Core/`):**
- Purpose: Platform-agnostic interfaces and base classes; no automation driver dependencies
- Contains: `IControlObject<TScope>`, `IPageObject`, `IDriver<TElement>`, `ITestContext`, `Locator`, `LocatorStrategy`, exception types, `TimeoutSettings`, `ITestLogger`, `ScreenshotService`
- Depends on: xunit.extensibility.core (PrivateAssets=all, for attribute base classes only)
- Used by: all platform projects

**Layer 2 — Platform Implementations (`srcnew/Brinell.Maui/`, `srcnew/Brinell.Html/`, `srcnew/Brinell.Wpf/`, `srcnew/Brinell.WinForms/`, `srcnew/Brinell.Blazor/`, `srcnew/Brinell.Stride/`):**
- Purpose: Platform-specific controls, page bases, context objects implementing Core interfaces
- Contains: `ControlBase<TScope>`, `PageObjectBase<TSelf>`, platform-specific element types, driver wrappers
- Depends on: `Brinell.Core` + platform drivers (Appium, FlaUI, Playwright, Selenium, Stride)
- Used by: driver-specific projects and test projects

**Layer 3 — Driver Adapters (`srcnew/Brinell.Maui.Appium/`, `srcnew/Brinell.Maui.FlaUI/`, `srcnew/Brinell.Html.Playwright/`, `srcnew/Brinell.Maui.CommunityToolkit/`):**
- Purpose: Concrete driver wrappers that adapt third-party automation libraries to Brinell's `IDriver<TElement>`
- Contains: `AppiumMauiDriver`, `AppiumMauiElement`, `PlaywrightHtmlElement`, `PlaywrightTestContext`
- Depends on: platform project + specific automation SDK
- Used by: test projects (choose which driver to use)

**Layer 4 — Test Infrastructure (`srcnew/Brinell.Mocking/`, `srcnew/Brinell.Core/Testing/`, `srcnew/Brinell.Maui/Testing/`):**
- Purpose: Test fixture base classes, screenshot service, WireMock API mocking infrastructure
- Contains: `MauiTestFixtureBase`, `ScreenshotTestAttribute`, `MockApiServer`
- Depends on: platform project + WireMock.Net
- Used by: test projects

**Layer 5 — Test Projects (`testsnew/`, `samples/*/UITests/`):**
- Purpose: Actual test suites using the framework
- Contains: `AppiumFixture`, `PageObjectBase` subclasses, `[Fact]` / `[Collection]` xUnit tests
- Depends on: platform implementation package + driver adapter

## Data Flow

**MAUI UITest Execution Flow:**

1. xUnit starts test collection `[Collection("Appium")]` → creates `AppiumFixture` once
2. `AppiumFixture` calls `MauiTestFixtureBase()` → creates `MauiTestContext` with `MauiDriverOptions`
3. `MauiDriverOptions` resolves platform from `APPIUM_PLATFORM` env var (windows/android/ios)
4. `MauiTestFixtureBase.CreateTestContextOptions()` builds capabilities → `AppiumMauiDriver` connects to Appium server
5. Test constructor receives `AppiumFixture`, calls `fixture.NavigateToMain()` → page navigation
6. Test body uses page object: `Page.NameEntry.Enter("text").GreetButton.Click()`
7. `Enter()` calls `ControlBase.Poll()` with Stopwatch → `context.TryFindElement(locator)` → `AppiumElement.SendKeys()`
8. `Click()` returns `TScope` (the page) enabling fluent chaining
9. `Page.GreetingLabel.AssertText("Hello, text!")` → throws `AssertionException` on mismatch

**Wait/Poll Flow:**

1. `control.WaitExists(true, timeoutMs: 5000)` called
2. `ControlBase.Poll(condition, timeoutMs)` starts `Stopwatch`
3. Loop: calls `context.TryFindElement(locator)` every `PollingInterval` (100ms default)
4. Returns `true` when condition met, `false` on timeout
5. `AssertExists(true)` wraps `WaitExists` and throws `AssertionException` if it returns `false`

**State Management:**
- No global singleton state — each `ITestContext` owns the driver session
- xUnit `ICollectionFixture<AppiumFixture>` provides single driver instance across a test collection
- `TimeoutSettings` injected per context; can be overridden per-call with `timeoutMs` parameter

## Key Abstractions

**`IControlObject<TScope>`:**
- Purpose: Universal control contract with Is/Wait/Assert triple for exists, visible, enabled, text
- Pattern: All action methods return `TScope` for fluent chaining
- Location: `srcnew/Brinell.Core/Interfaces/IControlObject.cs`

**`ControlObjectBase<TScope>`:**
- Purpose: Abstract base giving platform controls access to `Locator` and `IElementScope`
- Pattern: Protected members only; no automation code here
- Location: `srcnew/Brinell.Core/Abstractions/Controls/ControlObjectBase.cs`

**`ControlBase<TScope>` (per platform, e.g., MAUI):**
- Purpose: Implements `IControlObject<TScope>` with `Poll()` loop + platform element calls
- Pattern: All methods that interact with UI funnel through `Poll(condition, timeoutMs)`
- Location: `srcnew/Brinell.Maui/Controls/ControlBase.cs`

**`PageObjectBase<TSelf>` (CRTP):**
- Purpose: Base for all page objects; `TSelf` is the concrete page type for fluent returns
- Pattern: `public TSelf Self => (TSelf)this;` — controls return `Self` to enable chaining
- Factory methods: `Button(locator)`, `Entry(locator)`, etc. — control constructors take `this` as scope
- Location: `srcnew/Brinell.Maui/Pages/PageObjectBase.cs`

**`Locator`:**
- Purpose: Immutable value object; holds `LocatorStrategy` enum + `string Value` + optional `Parent`
- Pattern: Factory methods — `Locator.ByAutomationId("id")`, `Locator.ByCss(".class")`, `Locator.ByXPath("//x")`
- Location: `srcnew/Brinell.Core/Locators/Locator.cs`

**`ContainerBase<TParent, TSelf>` (MAUI):**
- Purpose: Container controls (Grid, ScrollView, Expander, etc.) that act as scopes for child controls
- Pattern: Inherits from `ControlBase<TParent>` and implements `IMauiScope<TSelf>` — can scope both to parent for chaining and to self for child finding
- Location: `srcnew/Brinell.Maui/Controls/ContainerBase.cs`

## Entry Points

**MAUI UITest:**
- Location: `testsnew/Brinell.Maui.UITests/AppiumFixture.cs`
- Triggers: xUnit test runner creates `IClassFixture<AppiumFixture>` or `[Collection("Appium")]`
- Responsibilities: instantiate `MauiTestContext`, create page objects, expose navigation helpers

**Test context bootstrap:**
- Location: `srcnew/Brinell.Maui/Testing/MauiTestFixtureBase.cs`
- Triggers: `new AppiumFixture()` from xUnit
- Responsibilities: read env vars, build `MauiDriverOptions`, create `MauiTestContext` (which starts Appium session)

**Sample App page objects:**
- Location: `testsnew/Brinell.Maui.UITests/Pages/`
- Triggers: test constructor, e.g., `MainPage Page => _fixture.MainPage;`

## Error Handling

**Strategy:** Exceptions propagate; no empty catches; only `WebDriverException` caught for transient Appium flakiness cases

**Exception Types (all in `srcnew/Brinell.Core/Exceptions/`):**
- `BrinellException` — base
- `ElementNotFoundException` — element not found within timeout
- `AssertionException` — assertion failed (thrown by `Assert*` methods)
- `WaitTimeoutException` — wait exceeded timeout
- `LocatorNotSupportedException` — strategy not supported by current driver
- `PageLoadException` — page/screen did not load in time

**Polling behavior:**
- Loop body catches transient failures silently (stale element, element not yet present)
- Final check after timeout allows exception to propagate with full context
- `ContainerBase` catches only `ElementNotFoundException` for second-try stale recovery

## Dependencies Between Components

```
Brinell.Core  (no automation deps)
  └── Brinell.Maui  (Appium.WebDriver for exception types only)
        ├── Brinell.Maui.Appium  (Appium.WebDriver full driver)
        ├── Brinell.Maui.FlaUI   (FlaUI for Windows)
        └── Brinell.Maui.CommunityToolkit

Brinell.Core
  └── Brinell.Html
        ├── Brinell.Html.Playwright  (Microsoft.Playwright)
        └── Brinell.Blazor  (Html + Html.Playwright)

Brinell.Core
  ├── Brinell.Wpf           (FlaUI, Windows only)
  ├── Brinell.WinForms      (FlaUI, Windows only)
  └── Brinell.Mocking       (WireMock.Net)

Brinell.Automation  (Stride.Engine, net10.0 only)
Brinell.Stride      (Brinell.Core + Stride)
```

---

*Architecture analysis: 2026-03-02*
