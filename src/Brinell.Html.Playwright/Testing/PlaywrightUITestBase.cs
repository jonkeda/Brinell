using Microsoft.Playwright;
using Brinell.Core.Logging;
using Brinell.Core.Testing;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Testing;

/// <summary>
/// Supported browser types for Playwright UI testing.
/// </summary>
public enum PlaywrightBrowserType
{
    /// <summary>Chromium-based browser (Chrome, Edge).</summary>
    Chromium,

    /// <summary>Mozilla Firefox browser.</summary>
    Firefox,

    /// <summary>Apple WebKit browser (Safari).</summary>
    WebKit
}

/// <summary>
/// Base class for HTML/web UI tests using Playwright.
/// </summary>
public abstract class PlaywrightUITestBase : UITestBase<PlaywrightTestContext>
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private IPage? _page;
    private PlaywrightDriverAdapter? _driver;

    /// <summary>
    /// The Playwright driver adapter.
    /// </summary>
    protected PlaywrightDriverAdapter? Driver => _driver;

    /// <summary>
    /// The underlying Playwright page.
    /// </summary>
    protected IPage? Page => _page;

    /// <summary>
    /// The underlying Playwright browser.
    /// </summary>
    protected IBrowser? Browser => _browser;

    /// <summary>
    /// The underlying Playwright browser context.
    /// </summary>
    protected IBrowserContext? BrowserContext => _browserContext;

    /// <summary>
    /// The browser type to use. Override to change browser. Default is Chromium.
    /// </summary>
    protected virtual PlaywrightBrowserType BrowserType => PlaywrightBrowserType.Chromium;

    /// <summary>
    /// Whether to run browser in headless mode. Override to change. Default is true.
    /// </summary>
    protected virtual bool Headless => true;

    /// <summary>
    /// Slow motion delay in milliseconds. Override to enable. Default is 0 (disabled).
    /// </summary>
    protected virtual int SlowMo => 0;

    /// <summary>
    /// Default viewport width. Override to change. Default is 1920.
    /// </summary>
    protected virtual int ViewportWidth => 1920;

    /// <summary>
    /// Default viewport height. Override to change. Default is 1080.
    /// </summary>
    protected virtual int ViewportHeight => 1080;

    /// <summary>
    /// The base URL for the web application. Override this in your test class.
    /// </summary>
    protected abstract string BaseUrl { get; }

    /// <summary>
    /// Create a new Playwright UI test base with optional output writer.
    /// </summary>
    protected PlaywrightUITestBase(Action<string>? outputWriter = null)
        : base(outputWriter)
    {
    }

    /// <summary>
    /// Launch browser and navigate to the base URL.
    /// </summary>
    protected async Task LaunchBrowserAsync()
    {
        await LaunchBrowserAsync(BaseUrl);
    }

    /// <summary>
    /// Launch browser and navigate to a specific URL.
    /// </summary>
    protected async Task LaunchBrowserAsync(string url)
    {
        Log($"Launching {BrowserType} browser...");

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        var browserType = BrowserType switch
        {
            PlaywrightBrowserType.Chromium => _playwright.Chromium,
            PlaywrightBrowserType.Firefox => _playwright.Firefox,
            PlaywrightBrowserType.WebKit => _playwright.Webkit,
            _ => _playwright.Chromium
        };

        _browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            SlowMo = SlowMo
        });

        _browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = ViewportWidth,
                Height = ViewportHeight
            }
        });

        _page = await _browserContext.NewPageAsync();

        _driver = new PlaywrightDriverAdapter(_page, _browser, _browserContext);

        var logger = CsvTestLogger.CreateDefault(TestName);
        InitializeContext(new PlaywrightTestContext(_driver, Log), logger);

        Log($"Navigating to: {url}");
        await _driver.NavigateToAsync(url);
    }

    /// <summary>
    /// Launch browser synchronously (convenience wrapper).
    /// </summary>
    protected void LaunchBrowser()
    {
        LaunchBrowserAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Launch browser and navigate to a specific URL synchronously (convenience wrapper).
    /// </summary>
    protected void LaunchBrowser(string url)
    {
        LaunchBrowserAsync(url).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate to a URL relative to the base URL.
    /// </summary>
    protected async Task NavigateToAsync(string relativePath)
    {
        var url = CombineUrl(BaseUrl, relativePath);
        Log($"Navigating to: {url}");
        await _driver!.NavigateToAsync(url);
    }

    /// <summary>
    /// Navigate to a URL relative to the base URL (sync wrapper).
    /// </summary>
    protected void NavigateTo(string relativePath)
    {
        NavigateToAsync(relativePath).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate to an absolute URL.
    /// </summary>
    protected async Task NavigateToAbsoluteAsync(string absoluteUrl)
    {
        Log($"Navigating to: {absoluteUrl}");
        await _driver!.NavigateToAsync(absoluteUrl);
    }

    /// <summary>
    /// Navigate to an absolute URL (sync wrapper).
    /// </summary>
    protected void NavigateToAbsolute(string absoluteUrl)
    {
        NavigateToAbsoluteAsync(absoluteUrl).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Refresh the current page.
    /// </summary>
    protected async Task RefreshPageAsync()
    {
        Log("Refreshing page");
        await _page!.ReloadAsync();
    }

    /// <summary>
    /// Refresh the current page (sync wrapper).
    /// </summary>
    protected void RefreshPage()
    {
        RefreshPageAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate back.
    /// </summary>
    protected async Task NavigateBackAsync()
    {
        Log("Navigating back");
        await _page!.GoBackAsync();
    }

    /// <summary>
    /// Navigate back (sync wrapper).
    /// </summary>
    protected void NavigateBack()
    {
        NavigateBackAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate forward.
    /// </summary>
    protected async Task NavigateForwardAsync()
    {
        Log("Navigating forward");
        await _page!.GoForwardAsync();
    }

    /// <summary>
    /// Navigate forward (sync wrapper).
    /// </summary>
    protected void NavigateForward()
    {
        NavigateForwardAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get current page URL.
    /// </summary>
    protected string GetCurrentUrl()
    {
        return _page?.Url ?? string.Empty;
    }

    /// <summary>
    /// Get current page title.
    /// </summary>
    protected async Task<string> GetPageTitleAsync()
    {
        return await _page!.TitleAsync();
    }

    /// <summary>
    /// Get current page title (sync wrapper).
    /// </summary>
    protected string GetPageTitle()
    {
        return GetPageTitleAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Assert current URL equals expected value.
    /// </summary>
    protected void AssertUrl(string expected, string? message = null)
    {
        var actual = GetCurrentUrl();
        if (actual != expected)
        {
            throw new Brinell.Core.Logging.AssertionException(
                message ?? $"Expected URL '{expected}' but got '{actual}'.");
        }
    }

    /// <summary>
    /// Assert current URL contains expected substring.
    /// </summary>
    protected void AssertUrlContains(string expected, string? message = null)
    {
        var actual = GetCurrentUrl();
        if (!actual.Contains(expected))
        {
            throw new Brinell.Core.Logging.AssertionException(
                message ?? $"Expected URL to contain '{expected}' but got '{actual}'.");
        }
    }

    /// <summary>
    /// Assert page title equals expected value.
    /// </summary>
    protected async Task AssertTitleAsync(string expected, string? message = null)
    {
        var actual = await GetPageTitleAsync();
        if (actual != expected)
        {
            throw new Brinell.Core.Logging.AssertionException(
                message ?? $"Expected title '{expected}' but got '{actual}'.");
        }
    }

    /// <summary>
    /// Assert page title contains expected substring.
    /// </summary>
    protected async Task AssertTitleContainsAsync(string expected, string? message = null)
    {
        var actual = await GetPageTitleAsync();
        if (!actual.Contains(expected))
        {
            throw new Brinell.Core.Logging.AssertionException(
                message ?? $"Expected title to contain '{expected}' but got '{actual}'.");
        }
    }

    /// <summary>
    /// Execute JavaScript on the page.
    /// </summary>
    protected async Task<T?> ExecuteScriptAsync<T>(string script, params object[] args)
    {
        return await _page!.EvaluateAsync<T?>(script, args);
    }

    /// <summary>
    /// Execute JavaScript on the page.
    /// </summary>
    protected object? ExecuteScript(string script, params object[] args)
    {
        return _driver?.ExecuteScript(script, args);
    }

    /// <summary>
    /// Wait for page to reach load state.
    /// </summary>
    protected async Task WaitForLoadStateAsync(LoadState? state = null)
    {
        await _page!.WaitForLoadStateAsync(state ?? LoadState.Load);
    }

    /// <summary>
    /// Wait for a selector to be visible.
    /// </summary>
    protected async Task WaitForSelectorAsync(string selector, int? timeoutMs = null)
    {
        await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
        {
            Timeout = timeoutMs ?? 30000,
            State = WaitForSelectorState.Visible
        });
    }

    /// <summary>
    /// Start tracing for debugging.
    /// </summary>
    protected async Task StartTracingAsync(string? name = null)
    {
        await _browserContext!.Tracing.StartAsync(new TracingStartOptions
        {
            Name = name ?? TestName,
            Screenshots = true,
            Snapshots = true
        });
    }

    /// <summary>
    /// Stop tracing and save to file.
    /// </summary>
    protected async Task<string> StopTracingAsync(string? path = null)
    {
        var tracePath = path ?? Path.Combine(Path.GetTempPath(), "OraveyUITests", $"{TestName}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(tracePath)!);

        await _browserContext!.Tracing.StopAsync(new TracingStopOptions
        {
            Path = tracePath
        });

        Log($"Trace saved: {tracePath}");
        return tracePath;
    }

    /// <summary>
    /// Close the browser.
    /// </summary>
    protected async Task CloseBrowserAsync()
    {
        Log("Closing browser");

        if (_browserContext != null)
        {
            await _browserContext.CloseAsync();
            _browserContext = null;
        }

        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
        _page = null;
        _driver = null;
    }

    /// <summary>
    /// Close the browser (sync wrapper).
    /// </summary>
    protected void CloseBrowser()
    {
        CloseBrowserAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Combine base URL with relative path.
    /// </summary>
    private static string CombineUrl(string baseUrl, string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return baseUrl;

        baseUrl = baseUrl.TrimEnd('/');
        relativePath = relativePath.TrimStart('/');
        return $"{baseUrl}/{relativePath}";
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                CloseBrowser();
            }
            catch (Exception ex)
            {
                Log($"Error closing browser: {ex.Message}");
            }
        }

        base.Dispose(disposing);
    }
}
