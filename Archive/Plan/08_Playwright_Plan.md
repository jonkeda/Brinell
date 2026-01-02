# Plan 08: Add Playwright Support to Brinell.Html

## Overview
Add Microsoft Playwright as an alternative browser automation driver alongside Selenium in the Brinell.Html package. This provides users with a choice of automation engines based on their needs.

## Why Playwright?
| Feature | Selenium | Playwright |
|---------|----------|------------|
| Browser Support | Chrome, Firefox, Edge, Safari | Chromium, Firefox, WebKit |
| Auto-wait | Manual waits required | Built-in auto-waiting |
| Speed | Moderate | Fast (headless optimized) |
| Driver Management | WebDriverManager needed | Built-in browser install |
| Network Interception | Limited | First-class support |
| Mobile Emulation | Through DevTools | Native support |
| Parallel Execution | Manual setup | Built-in isolation |
| Tracing/Debugging | Screenshots only | Video, trace, console logs |
| .NET Support | Mature | Modern async API |

## Goals
1. Add Playwright as optional driver in Brinell.Html
2. Share page object and control abstractions
3. Allow runtime driver selection (Selenium or Playwright)
4. Maintain backward compatibility with existing Selenium tests
5. Provide Playwright-specific features (tracing, network mocking)

---

## Phase 1: Architecture Design (1 day)

### 1.1 Package Structure Options

**Option A: Single Package with Optional Dependency**
```
Brinell.Html/
├── Abstractions/          # Shared interfaces
├── Controls/              # Platform-agnostic controls
├── Selenium/              # Selenium implementation
└── Playwright/            # Playwright implementation (optional)
```
- Pros: Single package, simpler for users
- Cons: Optional dependency complexity

**Option B: Separate Packages (Recommended)**
```
Brinell.Html.Core/         # Abstractions and shared code
Brinell.Html.Selenium/     # Selenium implementation
Brinell.Html.Playwright/   # Playwright implementation
```
- Pros: Clean separation, no unused dependencies
- Cons: More packages to manage

### 1.2 Abstraction Layer

**IWebDriverAdapter** (shared interface)
```csharp
namespace Brinell.Html.Core.Abstractions;

public interface IWebDriverAdapter : IDisposable
{
    // Navigation
    Task NavigateToAsync(string url);
    Task<string> GetCurrentUrlAsync();
    Task<string> GetTitleAsync();
    Task RefreshAsync();
    Task GoBackAsync();
    Task GoForwardAsync();
    
    // Element Finding
    Task<IWebElementAdapter?> FindElementAsync(string selector);
    Task<IReadOnlyList<IWebElementAdapter>> FindElementsAsync(string selector);
    
    // JavaScript
    Task<object?> ExecuteScriptAsync(string script, params object[] args);
    
    // Screenshots
    Task<byte[]> TakeScreenshotAsync();
    
    // Waiting
    Task WaitForSelectorAsync(string selector, int timeoutMs);
    Task WaitForNavigationAsync(int timeoutMs);
}

public interface IWebElementAdapter
{
    Task ClickAsync();
    Task FillAsync(string text);
    Task ClearAsync();
    Task<string> GetTextAsync();
    Task<string?> GetAttributeAsync(string name);
    Task<bool> IsVisibleAsync();
    Task<bool> IsEnabledAsync();
    Task<bool> IsCheckedAsync();
}
```

### 1.3 Test Context Abstraction
```csharp
public interface IHtmlTestContext : ITestContext
{
    IWebDriverAdapter Driver { get; }
    
    Task NavigateToAsync(string url);
    Task<bool> WaitForAsync(Func<Task<bool>> condition, int timeoutMs, string description);
}
```

---

## Phase 2: Refactor Brinell.Html for Abstraction (2 days)

### 2.1 Create Brinell.Html.Core
New project with shared abstractions:

**Brinell.Html.Core.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <RootNamespace>Brinell.Html.Core</RootNamespace>
    <Description>Core abstractions for Brinell HTML/web UI testing</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
  </ItemGroup>
</Project>
```

**Files to create:**
```
Brinell.Html.Core/
├── Abstractions/
│   ├── IWebDriverAdapter.cs
│   ├── IWebElementAdapter.cs
│   └── IHtmlTestContext.cs
├── Controls/
│   └── Base/
│       ├── HtmlControlBase.cs      # Abstract base
│       └── HtmlPageBase.cs         # Abstract base
└── Brinell.Html.Core.csproj
```

### 2.2 Rename Brinell.Html → Brinell.Html.Selenium
Or keep Brinell.Html as Selenium implementation with refactoring:

**Move to implementation namespace:**
```
Brinell.Html/
├── Infrastructure/
│   ├── SeleniumDriverAdapter.cs    # Implements IWebDriverAdapter
│   ├── SeleniumElementAdapter.cs   # Implements IWebElementAdapter
│   └── SeleniumTestContext.cs      # Implements IHtmlTestContext
├── Controls/
│   ├── ButtonControl.cs            # Selenium-specific
│   ├── TextInputControl.cs
│   └── ...
└── Testing/
    └── HtmlUITestBase.cs           # Selenium test base
```

### 2.3 Backward Compatibility
- Keep existing public API surface
- Add `[Obsolete]` to methods being replaced with async versions
- Provide sync wrappers over async for existing Selenium users

---

## Phase 3: Implement Playwright Support (3 days)

### 3.1 Create Brinell.Html.Playwright

**Brinell.Html.Playwright.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <RootNamespace>Brinell.Html.Playwright</RootNamespace>
    <Description>Playwright-based HTML/web UI testing for Brinell</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Brinell.Html.Core\Brinell.Html.Core.csproj" />
  </ItemGroup>
</Project>
```

### 3.2 Playwright Driver Adapter
```csharp
using Microsoft.Playwright;
using Brinell.Html.Core.Abstractions;

namespace Brinell.Html.Playwright.Infrastructure;

public class PlaywrightDriverAdapter : IWebDriverAdapter
{
    private readonly IPage _page;
    private readonly IBrowser _browser;
    private readonly IBrowserContext _context;

    public PlaywrightDriverAdapter(IPage page, IBrowser browser, IBrowserContext context)
    {
        _page = page;
        _browser = browser;
        _context = context;
    }

    public async Task NavigateToAsync(string url)
    {
        await _page.GotoAsync(url);
    }

    public async Task<IWebElementAdapter?> FindElementAsync(string selector)
    {
        var locator = _page.Locator(selector);
        if (await locator.CountAsync() > 0)
        {
            return new PlaywrightElementAdapter(locator.First);
        }
        return null;
    }

    public async Task<byte[]> TakeScreenshotAsync()
    {
        return await _page.ScreenshotAsync();
    }

    public async Task WaitForSelectorAsync(string selector, int timeoutMs)
    {
        await _page.WaitForSelectorAsync(selector, new() { Timeout = timeoutMs });
    }

    // ... other implementations
}
```

### 3.3 Playwright Element Adapter
```csharp
public class PlaywrightElementAdapter : IWebElementAdapter
{
    private readonly ILocator _locator;

    public PlaywrightElementAdapter(ILocator locator)
    {
        _locator = locator;
    }

    public async Task ClickAsync()
    {
        await _locator.ClickAsync();
    }

    public async Task FillAsync(string text)
    {
        await _locator.FillAsync(text);
    }

    public async Task<string> GetTextAsync()
    {
        return await _locator.TextContentAsync() ?? string.Empty;
    }

    public async Task<bool> IsVisibleAsync()
    {
        return await _locator.IsVisibleAsync();
    }

    // ... other implementations
}
```

### 3.4 Playwright Test Base
```csharp
public abstract class PlaywrightUITestBase : UITestBase<PlaywrightTestContext>
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private IPage? _page;

    protected virtual BrowserType BrowserType => BrowserType.Chromium;
    protected virtual bool Headless => true;
    protected abstract string BaseUrl { get; }

    protected PlaywrightUITestBase(Action<string>? outputWriter = null)
        : base(outputWriter)
    {
    }

    protected async Task LaunchBrowserAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        var browserType = BrowserType switch
        {
            BrowserType.Chromium => _playwright.Chromium,
            BrowserType.Firefox => _playwright.Firefox,
            BrowserType.WebKit => _playwright.Webkit,
            _ => _playwright.Chromium
        };

        _browser = await browserType.LaunchAsync(new()
        {
            Headless = Headless
        });

        _browserContext = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1920, Height = 1080 }
        });

        _page = await _browserContext.NewPageAsync();
        
        var driver = new PlaywrightDriverAdapter(_page, _browser, _browserContext);
        var logger = CsvTestLogger.CreateDefault(TestName);
        InitializeContext(new PlaywrightTestContext(driver, Log), logger);

        await _page.GotoAsync(BaseUrl);
    }

    protected async Task CloseBrowserAsync()
    {
        await _browserContext?.CloseAsync()!;
        await _browser?.CloseAsync()!;
        _playwright?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CloseBrowserAsync().GetAwaiter().GetResult();
        }
        base.Dispose(disposing);
    }
}
```

### 3.5 Playwright-Specific Features
```csharp
public class PlaywrightTestContext : IHtmlTestContext
{
    // Tracing support
    public async Task StartTracingAsync(string name)
    {
        await _browserContext.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true
        });
    }

    public async Task StopTracingAsync(string path)
    {
        await _browserContext.Tracing.StopAsync(new() { Path = path });
    }

    // Network mocking
    public async Task MockRouteAsync(string url, Func<IRoute, Task> handler)
    {
        await _page.RouteAsync(url, handler);
    }

    // Video recording
    public async Task StartVideoAsync()
    {
        // Configure in browser context options
    }
}
```

---

## Phase 4: Unified Controls (1 day)

### 4.1 Abstract Control Base
```csharp
namespace Brinell.Html.Core.Controls.Base;

public abstract class HtmlControlBase<TContext> : IControlBase
    where TContext : IHtmlTestContext
{
    protected readonly TContext _context;
    protected readonly IPageObject _parent;
    
    public string AutomationId { get; }
    
    protected HtmlControlBase(TContext context, IPageObject parent, string automationId)
    {
        _context = context;
        _parent = parent;
        AutomationId = automationId;
    }

    public abstract Task<bool> IsVisibleAsync();
    public abstract Task ClickAsync();
    public abstract Task<string> GetTextAsync();
    
    // Sync wrappers for backward compatibility
    public bool IsVisible() => IsVisibleAsync().GetAwaiter().GetResult();
    public void Click() => ClickAsync().GetAwaiter().GetResult();
    public string GetText() => GetTextAsync().GetAwaiter().GetResult();
}
```

### 4.2 Selenium Control Implementation
```csharp
namespace Brinell.Html.Controls;

public class ButtonControl : HtmlControlBase<SeleniumTestContext>
{
    public ButtonControl(SeleniumTestContext context, IPageObject parent, string automationId)
        : base(context, parent, automationId)
    {
    }

    public override async Task<bool> IsVisibleAsync()
    {
        return await Task.FromResult(_context.ElementIsVisible(AutomationId));
    }

    public override async Task ClickAsync()
    {
        await Task.Run(() => _context.ClickElement(AutomationId));
    }
}
```

### 4.3 Playwright Control Implementation
```csharp
namespace Brinell.Html.Playwright.Controls;

public class ButtonControl : HtmlControlBase<PlaywrightTestContext>
{
    public ButtonControl(PlaywrightTestContext context, IPageObject parent, string automationId)
        : base(context, parent, automationId)
    {
    }

    public override async Task<bool> IsVisibleAsync()
    {
        var element = await _context.Driver.FindElementAsync(AutomationId);
        return element != null && await element.IsVisibleAsync();
    }

    public override async Task ClickAsync()
    {
        var element = await _context.Driver.FindElementAsync(AutomationId);
        if (element != null)
        {
            await element.ClickAsync();
        }
    }
}
```

---

## Phase 5: Sample Application and Tests (1 day)

### 5.1 Playwright Test Project
**Brinell.Samples.Blazor.PlaywrightTests/**
```
Brinell.Samples.Blazor.PlaywrightTests/
├── PageObjects/
│   ├── CounterPage.cs
│   ├── LoginPage.cs
│   └── DashboardPage.cs
├── TestBase/
│   └── BlazorPlaywrightTestBase.cs
├── Tests/
│   ├── CounterTests.cs
│   ├── LoginTests.cs
│   └── NavigationTests.cs
├── xunit.runner.json
└── Brinell.Samples.Blazor.PlaywrightTests.csproj
```

### 5.2 Test Base for Blazor
```csharp
public abstract class BlazorPlaywrightTestBase : PlaywrightUITestBase
{
    protected override string BaseUrl =>
        Environment.GetEnvironmentVariable("BLAZOR_APP_URL") ?? "http://localhost:5180";

    protected override bool Headless =>
        Environment.GetEnvironmentVariable("HEADLESS")?.ToLowerInvariant() == "true";

    protected BlazorPlaywrightTestBase(ITestOutputHelper output)
        : base(output.WriteLine)
    {
    }

    protected async Task NavigateToPageAsync(string relativePath)
    {
        await Context!.NavigateToAsync($"{BaseUrl}{relativePath}");
        await WaitForBlazorReadyAsync();
    }

    protected async Task WaitForBlazorReadyAsync()
    {
        // Playwright's auto-wait handles most of this
        await Context!.Driver.WaitForLoadStateAsync();
    }
}
```

### 5.3 Equivalent Test
```csharp
[Collection("PlaywrightTests")]
public class CounterTests : BlazorPlaywrightTestBase
{
    public CounterTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Counter_ClickIncrement_IncreasesCount()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");

        var counterPage = new CounterPage(Context!);
        await counterPage.WaitForDisplayedAsync();

        // Act
        await counterPage.ClickIncrementAsync();

        // Assert - Playwright auto-waits!
        var count = await counterPage.GetCurrentCountAsync();
        count.Should().Be(1);
    }
}
```

---

## Phase 6: Documentation and Instructions (0.5 days)

### 6.1 Update README.md
- Add Playwright installation instructions
- Document driver selection
- Compare Selenium vs Playwright features

### 6.2 Create Playwright Instructions
**uitests-playwright.instructions.md**
- Playwright-specific patterns
- Async/await usage
- Tracing and debugging
- Network mocking examples

### 6.3 Update Package Documentation
- Brinell.Html.Playwright NuGet description
- API reference for Playwright classes

---

## Phase 7: NuGet Package Updates (0.5 days)

### 7.1 Add Package to Directory.Packages.props
```xml
<!-- Playwright for HTML/Web (alternative to Selenium) -->
<PackageVersion Include="Microsoft.Playwright" Version="1.49.0" />
```

### 7.2 Create Package Projects
Update solution to include:
- Brinell.Html.Core (if using Option B)
- Brinell.Html.Playwright

### 7.3 Pack and Test
```powershell
dotnet pack src/Brinell.Html.Playwright/ -c Release
```

---

## File Structure Summary

```
Brinell/
├── src/
│   ├── Brinell.Core/              # Existing
│   ├── Brinell.Html/              # Selenium (existing, refactored)
│   │   ├── Infrastructure/
│   │   │   ├── SeleniumDriverAdapter.cs
│   │   │   ├── SeleniumElementAdapter.cs
│   │   │   └── SeleniumTestContext.cs
│   │   ├── Controls/
│   │   └── Testing/
│   │       └── HtmlUITestBase.cs
│   ├── Brinell.Html.Playwright/   # NEW
│   │   ├── Infrastructure/
│   │   │   ├── PlaywrightDriverAdapter.cs
│   │   │   ├── PlaywrightElementAdapter.cs
│   │   │   └── PlaywrightTestContext.cs
│   │   ├── Controls/
│   │   └── Testing/
│   │       └── PlaywrightUITestBase.cs
│   ├── Brinell.Wpf/               # Existing
│   └── Brinell.Maui/              # Existing
├── samples/
│   ├── Brinell.Samples.Blazor.App/
│   ├── Brinell.Samples.Blazor.UITests/        # Selenium
│   └── Brinell.Samples.Blazor.PlaywrightTests/ # NEW - Playwright
└── Directory.Packages.props
```

---

## Estimated Timeline

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Architecture Design | 1 day | None |
| Phase 2: Refactor Brinell.Html | 2 days | Phase 1 |
| Phase 3: Implement Playwright | 3 days | Phase 2 |
| Phase 4: Unified Controls | 1 day | Phase 3 |
| Phase 5: Sample and Tests | 1 day | Phase 4 |
| Phase 6: Documentation | 0.5 days | Phase 5 |
| Phase 7: Packaging | 0.5 days | Phase 6 |
| **Total** | **~9 days** | |

---

## Success Criteria

- [ ] Playwright package builds and integrates with Brinell.Core
- [ ] Existing Selenium tests continue to work unchanged
- [ ] Playwright tests run against Blazor sample app
- [ ] All 23 Blazor tests can be ported to Playwright equivalents
- [ ] Tracing feature works for debugging
- [ ] Documentation complete for both drivers
- [ ] NuGet packages generated successfully

---

## Future Enhancements

1. **Unified Test Base** - Single test base that can switch drivers via configuration
2. **Shared Page Objects** - Generic page objects that work with both drivers
3. **Parallel Execution** - Leverage Playwright's browser context isolation
4. **Visual Comparison** - Built-in screenshot comparison
5. **API Testing** - Combine with Playwright's request context for API tests
