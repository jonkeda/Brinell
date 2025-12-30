using Microsoft.Playwright;
using Brinell.Core.Abstractions;

namespace Brinell.Html.Playwright.Infrastructure;

/// <summary>
/// Playwright driver adapter for web UI automation.
/// Wraps Playwright's IPage to provide a consistent API.
/// </summary>
public class PlaywrightDriverAdapter : IDriverAdapter
{
    private readonly IPage _page;
    private readonly IBrowser _browser;
    private readonly IBrowserContext _browserContext;
    private readonly string _automationIdAttribute;
    private bool _disposed;

    /// <summary>
    /// The underlying Playwright page.
    /// </summary>
    public IPage Page => _page;

    /// <summary>
    /// The underlying Playwright browser.
    /// </summary>
    public IBrowser Browser => _browser;

    /// <summary>
    /// The underlying Playwright browser context.
    /// </summary>
    public IBrowserContext BrowserContext => _browserContext;

    /// <summary>
    /// Create driver adapter with an existing Playwright page, browser, and context.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    /// <param name="browser">The Playwright browser instance.</param>
    /// <param name="browserContext">The Playwright browser context.</param>
    /// <param name="automationIdAttribute">The HTML attribute used for automation IDs (default: data-automation-id).</param>
    public PlaywrightDriverAdapter(
        IPage page,
        IBrowser browser,
        IBrowserContext browserContext,
        string automationIdAttribute = "data-automation-id")
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        _browserContext = browserContext ?? throw new ArgumentNullException(nameof(browserContext));
        _automationIdAttribute = automationIdAttribute;
    }

    /// <summary>
    /// Build a CSS selector for the given automation ID.
    /// Supports CSS selectors (starting with #, ., or [) directly.
    /// </summary>
    private string BuildSelector(string automationId)
    {
        // If it's already a CSS selector, use it directly
        if (automationId.StartsWith('#') || automationId.StartsWith('.') || automationId.StartsWith('['))
        {
            return automationId;
        }

        // Otherwise, search by automation ID attribute or id
        return $"[{_automationIdAttribute}='{automationId}'], [id='{automationId}']";
    }

    /// <summary>
    /// Find element by AutomationId (data-automation-id attribute by default).
    /// Supports CSS selectors if the automationId starts with '#', '.', or '['.
    /// This is a synchronous wrapper - prefer async methods in Playwright.
    /// </summary>
    public IElementAdapter? FindElement(string automationId)
    {
        var selector = BuildSelector(automationId);
        var locator = _page.Locator(selector).First;

        // Check if element exists using sync wrapper
        var count = locator.CountAsync().GetAwaiter().GetResult();
        if (count > 0)
        {
            return new PlaywrightElementAdapter(locator, automationId);
        }

        return null;
    }

    /// <summary>
    /// Find element by AutomationId asynchronously (preferred).
    /// </summary>
    public async Task<IElementAdapter?> FindElementAsync(string automationId)
    {
        var selector = BuildSelector(automationId);
        var locator = _page.Locator(selector).First;

        var count = await locator.CountAsync();
        if (count > 0)
        {
            return new PlaywrightElementAdapter(locator, automationId);
        }

        return null;
    }

    /// <summary>
    /// Find element by AutomationId and return the raw ILocator directly.
    /// </summary>
    public ILocator? FindLocator(string automationId)
    {
        var selector = BuildSelector(automationId);
        var locator = _page.Locator(selector).First;

        var count = locator.CountAsync().GetAwaiter().GetResult();
        return count > 0 ? locator : null;
    }

    /// <summary>
    /// Find element by XPath.
    /// </summary>
    public IElementAdapter? FindElementByXPath(string xpath)
    {
        var locator = _page.Locator($"xpath={xpath}").First;
        var count = locator.CountAsync().GetAwaiter().GetResult();

        return count > 0 ? new PlaywrightElementAdapter(locator, xpath) : null;
    }

    /// <summary>
    /// Find element by CSS selector.
    /// </summary>
    public IElementAdapter? FindElementByCss(string cssSelector)
    {
        var locator = _page.Locator(cssSelector).First;
        var count = locator.CountAsync().GetAwaiter().GetResult();

        return count > 0 ? new PlaywrightElementAdapter(locator, cssSelector) : null;
    }

    /// <summary>
    /// Find all elements matching AutomationId.
    /// </summary>
    public IReadOnlyCollection<IElementAdapter> FindElements(string automationId)
    {
        var selector = BuildSelector(automationId);
        var locator = _page.Locator(selector);
        var count = locator.CountAsync().GetAwaiter().GetResult();

        var elements = new List<IElementAdapter>();
        for (int i = 0; i < count; i++)
        {
            elements.Add(new PlaywrightElementAdapter(locator.Nth(i), automationId));
        }

        return elements;
    }

    /// <summary>
    /// Click an element.
    /// </summary>
    public void Click(IElementAdapter element)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            playwrightElement.Locator.ClickAsync().GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Click an element asynchronously.
    /// </summary>
    public async Task ClickAsync(IElementAdapter element)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            await playwrightElement.Locator.ClickAsync();
        }
    }

    /// <summary>
    /// Send keys/text to an element (replaces existing text).
    /// </summary>
    public void SendKeys(IElementAdapter element, string text)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            // Playwright's FillAsync replaces existing text
            playwrightElement.Locator.FillAsync(text).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Type text into an element (character by character, appends).
    /// </summary>
    public async Task TypeAsync(IElementAdapter element, string text)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            await playwrightElement.Locator.PressSequentiallyAsync(text);
        }
    }

    /// <summary>
    /// Fill text into an element (replaces existing text).
    /// </summary>
    public async Task FillAsync(IElementAdapter element, string text)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            await playwrightElement.Locator.FillAsync(text);
        }
    }

    /// <summary>
    /// Clear an element's text.
    /// </summary>
    public void Clear(IElementAdapter element)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            playwrightElement.Locator.ClearAsync().GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Get element's text content.
    /// </summary>
    public string? GetText(IElementAdapter element)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            var locator = playwrightElement.Locator;

            // Get tag name to handle inputs differently
            var tagName = locator.EvaluateAsync<string>("el => el.tagName.toLowerCase()")
                .GetAwaiter().GetResult();

            if (tagName == "input" || tagName == "textarea")
            {
                return locator.InputValueAsync().GetAwaiter().GetResult();
            }

            if (tagName == "select")
            {
                // For select, get selected option text
                return locator.Locator("option:checked").TextContentAsync()
                    .GetAwaiter().GetResult() ?? string.Empty;
            }

            return locator.TextContentAsync().GetAwaiter().GetResult() ?? string.Empty;
        }

        return null;
    }

    /// <summary>
    /// Get an attribute value from an element.
    /// </summary>
    public string? GetAttribute(IElementAdapter element, string name)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            return playwrightElement.Locator.GetAttributeAsync(name)
                .GetAwaiter().GetResult();
        }

        return null;
    }

    /// <summary>
    /// Check if element is displayed (visible).
    /// </summary>
    public bool IsDisplayed(IElementAdapter element)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            try
            {
                return playwrightElement.Locator.IsVisibleAsync()
                    .GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if element is enabled.
    /// </summary>
    public bool IsEnabled(IElementAdapter element)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            try
            {
                return playwrightElement.Locator.IsEnabledAsync()
                    .GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if element is checked (for checkboxes/radio buttons).
    /// </summary>
    public bool IsChecked(IElementAdapter element)
    {
        if (element is PlaywrightElementAdapter playwrightElement)
        {
            try
            {
                return playwrightElement.Locator.IsCheckedAsync()
                    .GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Navigate to a URL.
    /// </summary>
    public void NavigateTo(string url)
    {
        _page.GotoAsync(url).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate to a URL asynchronously.
    /// </summary>
    public async Task NavigateToAsync(string url)
    {
        await _page.GotoAsync(url);
    }

    /// <summary>
    /// Get current URL.
    /// </summary>
    public string GetCurrentUrl()
    {
        return _page.Url;
    }

    /// <summary>
    /// Get page title.
    /// </summary>
    public string GetTitle()
    {
        return _page.TitleAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Refresh the current page.
    /// </summary>
    public void Refresh()
    {
        _page.ReloadAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate back.
    /// </summary>
    public void Back()
    {
        _page.GoBackAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigate forward.
    /// </summary>
    public void Forward()
    {
        _page.GoForwardAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Execute JavaScript.
    /// </summary>
    public object? ExecuteScript(string script, params object[] args)
    {
        return _page.EvaluateAsync<object?>(script, args).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Execute JavaScript asynchronously.
    /// </summary>
    public async Task<T?> ExecuteScriptAsync<T>(string script, params object[] args)
    {
        return await _page.EvaluateAsync<T?>(script, args);
    }

    /// <summary>
    /// Take a screenshot.
    /// </summary>
    public byte[]? TakeScreenshot()
    {
        return _page.ScreenshotAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Take a screenshot asynchronously.
    /// </summary>
    public async Task<byte[]> TakeScreenshotAsync()
    {
        return await _page.ScreenshotAsync();
    }

    /// <summary>
    /// Take a full-page screenshot.
    /// </summary>
    public async Task<byte[]> TakeFullPageScreenshotAsync()
    {
        return await _page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
    }

    /// <summary>
    /// Wait for selector to be visible.
    /// </summary>
    public async Task WaitForSelectorAsync(string selector, int timeoutMs = 30000)
    {
        await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
        {
            Timeout = timeoutMs,
            State = WaitForSelectorState.Visible
        });
    }

    /// <summary>
    /// Wait for page to reach load state.
    /// </summary>
    public async Task WaitForLoadStateAsync(LoadState? state = null)
    {
        await _page.WaitForLoadStateAsync(state ?? LoadState.Load);
    }

    /// <summary>
    /// Wait for navigation to complete.
    /// </summary>
    public async Task WaitForNavigationAsync(int timeoutMs = 30000)
    {
        await _page.WaitForURLAsync(_page.Url, new PageWaitForURLOptions { Timeout = timeoutMs });
    }

    /// <summary>
    /// Find element within a container element by AutomationId.
    /// </summary>
    public ILocator? FindLocatorInContainer(ILocator container, string automationId)
    {
        var selector = BuildSelector(automationId);
        var locator = container.Locator(selector).First;
        var count = locator.CountAsync().GetAwaiter().GetResult();

        return count > 0 ? locator : null;
    }

    /// <summary>
    /// Find all elements within a container element by AutomationId.
    /// </summary>
    public ILocator FindLocatorsInContainer(ILocator container, string automationId)
    {
        var selector = BuildSelector(automationId);
        return container.Locator(selector);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _browserContext?.CloseAsync().GetAwaiter().GetResult();
            _browser?.CloseAsync().GetAwaiter().GetResult();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
