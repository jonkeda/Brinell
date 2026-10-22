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

public sealed class PlaywrightTestContext : IHtmlTestContext, IHtmlPageProbe, IAsyncDisposable, Brinell.Core.Recording.IRecordable
{
    private readonly IPage _page;
    private readonly IFrame? _frame;
    private readonly PlaywrightHtmlDriver _driver;
    private readonly bool _ownsLifecycle;
    private readonly TimeoutSettings _timeouts;
    private readonly ITestLogger _logger;
    private readonly HtmlTestContextOptions _options;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
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

        var browserContext = await browser.NewContextAsync(shown ? new BrowserNewContextOptions { ViewportSize = ViewportSize.NoViewport } : null).ConfigureAwait(false);
        var page = await browserContext.NewPageAsync().ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            await page.GotoAsync(options.BaseUrl).ConfigureAwait(false);
        }

        return new PlaywrightTestContext(playwright, browser, browserContext, page, options);
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

    public void ResetAppState()
    {
        _browserContext?.ClearCookiesAsync().GetAwaiter().GetResult();
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
