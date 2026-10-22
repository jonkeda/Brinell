using Brinell.Core.Calls;
using Brinell.Core.Configuration;
using Brinell.Core.Diagnostics;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Core.Logging;
using Brinell.Core.Scopes;
using Brinell.Html.Context;
using Brinell.Html.Interfaces;
using Microsoft.Playwright;

namespace Brinell.Html.Playwright;

public sealed class PlaywrightTestContext : IHtmlTestContext, IHtmlPageProbe, Brinell.Core.Interfaces.IScreenshotSource, IAsyncDisposable, Brinell.Core.Recording.IRecordable
{
    // A new page and driver when ResetAppState opens a new browser context (one per scenario).
    private IPage _page;
    private readonly IFrame? _frame;
    private PlaywrightHtmlDriver _driver;
    private readonly bool _ownsLifecycle;
    private readonly TimeoutSettings _timeouts;
    private readonly ITestLogger _logger;
    private readonly HtmlTestContextOptions _options;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private BrowserNewContextOptions? _contextOptions;
    private bool _disposed;

    private PlaywrightTestContext(
        IPlaywright playwright,
        IBrowser browser,
        IBrowserContext browserContext,
        IPage page,
        HtmlTestContextOptions options)
    {
        _options = options;
        _playwright = playwright;
        _browser = browser;
        _browserContext = browserContext;
        _page = page;
        _driver = new PlaywrightHtmlDriver(page);
        _timeouts = options.Timeouts;
        _logger = options.Logger ?? (ITestLogger?)OpenCallLog() ?? NullTestLogger.Instance;
        _ownsLifecycle = true;
    }

    private PlaywrightTestContext(
        IPage page,
        IFrame? frame,
        HtmlTestContextOptions options,
        ITestLogger logger,
        bool ownsLifecycle)
    {
        _options = options;
        _page = page;
        _frame = frame;
        _driver = new PlaywrightHtmlDriver(page, frame);
        _timeouts = options.Timeouts;
        _logger = logger;
        _ownsLifecycle = ownsLifecycle;
    }

    public static async Task<PlaywrightTestContext> CreateAsync(HtmlTestContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);

        var browserType = options.BrowserType.ToLowerInvariant() switch
        {
            "firefox" => playwright.Firefox,
            "webkit" => playwright.Webkit,
            _ => playwright.Chromium
        };

        // Someone is watching (BRINELL_AUT_WINDOW=shown, the Presenter): a window to see, opened
        // where it will be placed, and a page that fills it.
        var shown = AutWindow.IsShown();
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = options.Headless && !shown
        };
        if (shown && AutWindow.Bounds() is { } bounds && ReferenceEquals(browserType, playwright.Chromium))
        {
            launchOptions.Args = [$"--window-position={bounds.X},{bounds.Y}", $"--window-size={bounds.Width},{bounds.Height}"];
        }

        var browser = await browserType.LaunchAsync(launchOptions).ConfigureAwait(false);

        var contextOptions = shown ? new BrowserNewContextOptions { ViewportSize = ViewportSize.NoViewport } : null;
        var browserContext = await browser.NewContextAsync(contextOptions).ConfigureAwait(false);
        var page = await browserContext.NewPageAsync().ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            await page.GotoAsync(options.BaseUrl).ConfigureAwait(false);
        }

        return new PlaywrightTestContext(playwright, browser, browserContext, page, options) { _contextOptions = contextOptions };
    }

    public static PlaywrightTestContext ForPage(IPage page, HtmlTestContextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        options ??= new HtmlTestContextOptions();

        return new PlaywrightTestContext(
            page,
            frame: null,
            options,
            options.Logger ?? (ITestLogger?)OpenCallLog() ?? NullTestLogger.Instance,
            ownsLifecycle: false);
    }

    public IHtmlTestContext Context => this;

    public IHtmlDriver Driver => _driver;

    public string? Name => _options.Name;

    public NearMissSettings NearMiss => _options.NearMiss;

    public TimeoutSettings Timeouts => _timeouts;

    public ITestLogger Logger => _logger;

    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.Css;

    public IHtmlPage? Page => null;

    public string CurrentUrl => _driver.CurrentUrl;

    public string PageTitle => _driver.PageTitle;

    public IPage InternalPage => _page;

    /// <summary>The browser page; null once it is closed.</summary>
    public Brinell.Core.Recording.RecordingTarget? RecordingTarget
        => _page.IsClosed ? null : new Brinell.Core.Recording.RecordingTarget.BrowserPage(_page);

    public IFrame? InternalFrame => _frame;

    /// <summary>Ready while the page is open; a closed page is gone, not "not ready yet" (R0).</summary>
    public ScopeReadiness ProbeReadiness()
        => _page.IsClosed
            ? throw new AppUnavailableException("the browser page has closed.")
            : ScopeReadiness.Ready(nameof(PlaywrightTestContext));

    /// <summary>The page is either open or gone; there is nothing to wait for.</summary>
    public bool WaitReady(int? timeoutMs = null) => ProbeReadiness().IsReady;

    public IHtmlElement? TryFindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return _driver.FindElements(locator).FirstOrDefault();
    }

    // One attempt (R2): the call around it is what waits.
    public IHtmlElement FindElement(Locator locator)
        => TryFindElement(locator) ?? throw new ElementNotFoundException(locator);

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return _driver.FindElements(locator);
    }

    public void NavigateTo(string destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        _driver.GoTo(destination);
    }

    public async Task NavigateToAsync(string destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        await _page.GotoAsync(destination).ConfigureAwait(false);
    }

    public void NavigateBack() => _driver.GoBack();

    public void GoForward() => _driver.GoForward();

    public bool IsIdle() => _driver.IsIdle();

    public PageProbe ProbePage(string busyAttribute) => _driver.ProbePage(busyAttribute);

    public void Refresh() => _driver.Reload();

    public byte[] TakeScreenshot() => _driver.TakeScreenshot();

    public void SaveScreenshot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _driver.SaveScreenshot(path);
    }

    private Brinell.Core.Services.ScreenshotService? _screenshots;

    /// <summary>
    /// This page's screenshots, PNGs in the run's screenshots folder; what a failing UAT scenario
    /// leaves as its evidence when its fixture holds this context.
    /// </summary>
    public Brinell.Core.Interfaces.IScreenshotService Screenshots => _screenshots ??= new(
        SaveScreenshot,
        _logger,
        new ScreenshotSettings
        {
            OutputDirectory = Brinell.Core.Artifacts.DefaultTestArtifactPathProvider.Create().ScreenshotsDirectory,
            CaptureOnFailure = true,
            IncludeTimestamp = true,
            Format = ScreenshotFormat.Png,
        });

    /// <summary>
    /// The app as a new visitor sees it. A context that launched its browser closes its browser
    /// context and opens a new one with a blank page: cookies, storage and the server's circuit go
    /// with it, at the cost of the page load the next step makes anyway. A context over a page it
    /// was handed (<see cref="ForPage"/>) can only clear that page's cookies.
    /// </summary>
    public void ResetAppState()
    {
        if (!_ownsLifecycle || _browser is null || _frame is not null)
        {
            (_browserContext ?? _page.Context).ClearCookiesAsync().GetAwaiter().GetResult();
            return;
        }

        // The screen's size is the environment a test set, not app state: it carries over.
        var viewport = _page.ViewportSize;
        _browserContext?.CloseAsync().GetAwaiter().GetResult();
        _browserContext = _browser.NewContextAsync(_contextOptions).GetAwaiter().GetResult();
        _page = _browserContext.NewPageAsync().GetAwaiter().GetResult();
        if (viewport is not null && _contextOptions?.ViewportSize != ViewportSize.NoViewport)
        {
            _page.SetViewportSizeAsync(viewport.Width, viewport.Height).GetAwaiter().GetResult();
        }

        _driver = new PlaywrightHtmlDriver(_page);
    }

    /// <summary>
    /// A call log in the folder <c>BRINELL_CALL_LOG</c> names, when it is set: one CSV file per
    /// context, with every call's entry and exit, near-miss warnings included. Only when the
    /// options give no logger of their own. The same switch as MAUI's.
    /// </summary>
    private static CsvTestLogger? OpenCallLog()
    {
        var folder = Environment.GetEnvironmentVariable("BRINELL_CALL_LOG");
        return string.IsNullOrWhiteSpace(folder)
            ? null
            : new CsvTestLogger(Path.Combine(folder,
                $"calls-html-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..8]}.csv"));
    }

    public void Dispose()
    {
        DisposeAsyncCore(isAsync: false).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore(isAsync: true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private async Task DisposeAsyncCore(bool isAsync)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!_ownsLifecycle)
        {
            return;
        }

        if (_browserContext != null)
        {
            if (isAsync)
            {
                await _browserContext.CloseAsync().ConfigureAwait(false);
            }
            else
            {
                _browserContext.CloseAsync().GetAwaiter().GetResult();
            }

            _browserContext = null;
        }

        if (_browser != null)
        {
            if (isAsync)
            {
                await _browser.CloseAsync().ConfigureAwait(false);
            }
            else
            {
                _browser.CloseAsync().GetAwaiter().GetResult();
            }

            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;

        _logger.Flush();
        _logger.Dispose();
    }
}
