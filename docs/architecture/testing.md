# Testing Conventions

**Analysis Date:** 2026-03-02

## Test Framework

- **xUnit** 2.9.3 (`xunit`, `xunit.runner.visualstudio` 3.1.5)
- **Moq** 4.20.70 — mocking in unit tests only
- **AutoFixture** 4.18.1 — test data generation
- **Bogus** 35.5.1 — fake data builder (persons, addresses, etc.)
- **coverlet.collector** 6.0.4 — code coverage
- **FluentAssertions** — **BANNED** compile-time via `CheckBannedPackages` MSBuild target; use `Assert.*` directly

## File Organization

```
testsnew/
  Directory.Build.props         # TargetFramework=net10.0, IsTestProject=true, shared refs
  Directory.Packages.props      # test-specific version pins

  Brinell.Core.Tests/           # unit tests for Core interfaces and abstractions
  Brinell.Maui.Tests/           # unit tests for Brinell.Maui (mock driver, no Appium)
  Brinell.Blazor.Tests/         # unit tests for Brinell.Blazor (mock driver)

  Brinell.Maui.UITests/         # end-to-end MAUI tests — requires Appium + real/emulated device
  Brinell.Blazor.UITests/       # end-to-end Blazor tests — requires Playwright + app host
  Brinell.Automation.Tests/     # unit tests for Brinell.Automation
```

All test projects target `net10.0` (single TFM from `testsnew/Directory.Build.props`).

## Run Commands

```bash
# All unit tests (no device required)
dotnet test testsnew/

# Specific project
dotnet test testsnew/Brinell.Maui.Tests/

# UITests — requires running Appium server and device/emulator
dotnet test testsnew/Brinell.Maui.UITests/

# Run with filter (skip UITests)
dotnet test testsnew/ --filter "Category!=UITest"
```

## Test Structure

### Unit test naming

```csharp
// File: {Feature/Class}Tests.cs
// Method: {Subject}_{WhenCondition}_{ExpectedResult}
[Fact]
public void FluentChaining_AfterClick_ReturnsSamePageInstance()
```

### Unit test arrangement

```csharp
public class ButtonFluentChainingTests
{
    [Fact]
    public void Button_WhenClicked_ReturnsScopeInstance()
    {
        // Arrange
        var mockContext = new Mock<IMauiTestContext>();
        mockContext.Setup(c => c.Timeouts)
                   .Returns(new TimeoutSettings { DefaultWait = 5000, PollingInterval = 100 });
        mockContext.Setup(c => c.DefaultLocatorStrategy)
                   .Returns(LocatorStrategy.AutomationId);

        var page = new FakeMainPage(mockContext.Object);

        // Act
        var result = page.LoginButton.Click();

        // Assert
        Assert.Same(page, result);
    }
}
```

**Rules:**
- Blank lines between Arrange / Act / Assert sections
- One logical assertion per test when possible
- Use `Assert.Same()` for reference equality; `Assert.Equal()` for value equality
- `Assert.Throws<TException>()` for expected exceptions
- No `FluentAssertions` — banned (compile error if referenced)

## UITest Fixture Pattern

UITests share a single driver session for performance:

```csharp
// Fixture (created once per collection):
public class AppiumFixture : MauiTestFixtureBase, IDisposable
{
    public MainPage MainPage { get; }

    public AppiumFixture()
    {
        MainPage = new MainPage(Context);
    }

    public void NavigateToMain()
    {
        // navigate to main tab
        Context.Shell.MainTab.Click();
        MainPage.WaitLoaded(true);
    }
}

// Collection marker:
[CollectionDefinition("Appium")]
public class AppiumCollection : ICollectionFixture<AppiumFixture> { }

// Test class:
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Page", "MainPage")]
public class MainPageTests
{
    private readonly AppiumFixture _fixture;
    private MainPage Page => _fixture.MainPage;

    public MainPageTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToMain(); // reset navigation in constructor
    }

    [Fact]
    public void MainPage_KeyControls_Exist()
    {
        Page.NameEntry.AssertExists();
        Page.SaveButton.AssertExists();
        Page.StatusToggle.AssertExists();
    }
}
```

## Mocking Patterns

Only unit tests use mocks. UITests use a real driver.

```csharp
// Set up a mock context:
var mockContext = new Mock<IMauiTestContext>();
mockContext.Setup(c => c.Timeouts)
           .Returns(TimeoutSettings.Default);
mockContext.Setup(c => c.DefaultLocatorStrategy)
           .Returns(LocatorStrategy.AutomationId);

// Set up element finding:
var mockElement = new Mock<AppiumElement>();
mockContext.Setup(c => c.TryFindElement(It.IsAny<Locator>()))
           .Returns(mockElement.Object);

// Verify interactions:
mockContext.Verify(c => c.TryFindElement(It.IsAny<Locator>()), Times.AtLeast(1));
```

## Tracing and Screenshot on Failure

`ScreenshotTestAttribute` captures a screenshot when an assertion fails:

```csharp
// Applied to UITest class:
[ScreenshotTest]
public class MainPageTests { ... }

// Screenshots go to: TestResults/Screenshots/{TestName}_{Timestamp}.png
```

## Test Categories and Traits

```csharp
[Trait("Category", "UITest")]         // Requires real device/browser
[Trait("Category", "Integration")]    // Requires external service (Appium, WireMock)
[Trait("Category", "Unit")]           // Pure unit test (default — no trait required)
[Trait("Page", "MainPage")]           // Which page the test covers
[Trait("Control", "DatePicker")]      // Which control the test covers
```

Filter examples:
```bash
dotnet test --filter "Category=UITest"
dotnet test --filter "Category!=UITest"
dotnet test --filter "Page=MainPage"
```

## MauiTestFixtureBase

All UITest fixtures inherit from `MauiTestFixtureBase` in `Brinell.Maui`:

- Reads environment variables for config:
  - `BRINELL_APP_PATH` — path to the `.apk`/`.app`/`.exe` under test
  - `BRINELL_APPIUM_SERVER` — Appium server URL (default `http://127.0.0.1:4723/`)
  - `BRINELL_PLATFORM` — `Android`, `iOS`, `Windows`
  - `BRINELL_DEVICE_UDID` — device UDID for targeting a specific device
- Constructs `MauiDriverOptions` and `MauiTestContext`
- Creates a `ScreenshotService` bound to the context
- Implements `IDisposable` — calls `Context.Quit()` on teardown

---

*Testing conventions analysis: 2026-03-02*
