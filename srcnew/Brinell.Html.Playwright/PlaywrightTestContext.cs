using Brinell.Core.Configuration;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Core.Logging;
using Brinell.Html.Context;
using Brinell.Html.Interfaces;
using Microsoft.Playwright;

namespace Brinell.Html.Playwright;

public sealed class PlaywrightTestContext : IHtmlTestContext, IAsyncDisposable
{
    private readonly IPage _page;
    private readonly IFrame? _frame;
    private readonly PlaywrightHtmlDriver _driver;
    private readonly bool _ownsLifecycle;
    private readonly TimeoutSettings _timeouts;
    private readonly ITestLogger _logger;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private bool _disposed;

    private PlaywrightTestContext(
        IPlaywright playwright,
        IBrowser browser,
        IBrowserContext browserContext,
        IPage page,
        TimeoutSettings timeouts,
        ITestLogger logger)
    {
        _playwright = playwright;
        _browser = browser;
        _browserContext = browserContext;
        _page = page;
        _driver = new PlaywrightHtmlDriver(page);
        _timeouts = timeouts;
        _logger = logger;
        _ownsLifecycle = true;
    }

    private PlaywrightTestContext(
        IPage page,
        IFrame? frame,
        TimeoutSettings timeouts,
        ITestLogger logger,
        bool ownsLifecycle)
    {
        _page = page;
        _frame = frame;
        _driver = new PlaywrightHtmlDriver(page, frame);
        _timeouts = timeouts;
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

        var browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = options.Headless
        }).ConfigureAwait(false);

        var browserContext = await browser.NewContextAsync().ConfigureAwait(false);
        var page = await browserContext.NewPageAsync().ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            await page.GotoAsync(options.BaseUrl).ConfigureAwait(false);
        }

        return new PlaywrightTestContext(
            playwright,
            browser,
            browserContext,
            page,
            options.Timeouts,
            options.Logger ?? NullTestLogger.Instance);
    }

    public static PlaywrightTestContext ForPage(IPage page, HtmlTestContextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        options ??= new HtmlTestContextOptions();

        return new PlaywrightTestContext(
            page,
            frame: null,
            options.Timeouts,
            options.Logger ?? NullTestLogger.Instance,
            ownsLifecycle: false);
    }

    public PlaywrightTestContext ForFrame(string urlPattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(urlPattern);

        var frame = _page.Frames.FirstOrDefault(f =>
            f.Url.Contains(urlPattern, StringComparison.OrdinalIgnoreCase));

        if (frame == null)
        {
            var frameUrls = string.Join(", ", _page.Frames.Select(f => f.Url));
            throw new InvalidOperationException(
                $"Frame matching URL pattern '{urlPattern}' not found. Available frames: {frameUrls}");
        }

        return new PlaywrightTestContext(
            _page,
            frame,
            _timeouts,
            _logger,
            ownsLifecycle: false);
    }

    public IHtmlTestContext Context => this;

    public IHtmlDriver Driver => _driver;

    public TimeoutSettings Timeouts => _timeouts;

    public ITestLogger Logger => _logger;

    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.Css;

    public IHtmlPage? Page => null;

    public string CurrentUrl => _driver.CurrentUrl;

    public string PageTitle => _driver.PageTitle;

    public IPage InternalPage => _page;

    public IFrame? InternalFrame => _frame;

    public bool IsReady(int? timeoutMs = null) => !_page.IsClosed;

    public bool WaitReady(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _timeouts.PageLoad;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (IsReady())
            {
                return true;
            }

            Thread.Sleep(100);
        }

        return IsReady();
    }

    public IHtmlElement? TryFindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return _driver.FindElements(locator).FirstOrDefault();
    }

    public IHtmlElement FindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

        var timeout = _timeouts.ElementFind;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var element = TryFindElement(locator);
            if (element != null)
            {
                return element;
            }

            Thread.Sleep(100);
        }

        throw new ElementNotFoundException(locator, timeout);
    }

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

    public Task<bool> IsIdleAsync() => Task.FromResult(_driver.IsIdle());

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
