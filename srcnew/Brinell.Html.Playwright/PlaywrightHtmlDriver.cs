using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Microsoft.Playwright;

namespace Brinell.Html.Playwright;

/// <summary>
/// <see cref="IHtmlDriver"/> over one Playwright page, or one frame in it.
/// </summary>
/// <remarks>
/// The browser, its context and the page belong to <see cref="PlaywrightTestContext"/>, which
/// creates and closes them; this driver only talks to the page.
/// </remarks>
public sealed class PlaywrightHtmlDriver : IHtmlDriver
{
    private readonly IPage _page;
    private readonly IFrame? _frame;

    /// <summary>Drives <paramref name="page"/>, or <paramref name="frame"/> in it when one is given.</summary>
    public PlaywrightHtmlDriver(IPage page, IFrame? frame = null)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _frame = frame;
    }

    /// <inheritdoc />
    public IHtmlElement PageRoot
        => new PlaywrightHtmlElement(_frame != null ? _frame.Locator("body") : _page.Locator("body"));

    /// <inheritdoc />
    public string CurrentUrl => _page.Url;

    /// <inheritdoc />
    public string PageTitle => _page.TitleAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public bool IsIdle()
    {
        try
        {
            _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 10 }).GetAwaiter().GetResult();
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IHtmlElement> FindElements(Locator locator)
    {
        var playwrightLocator = LocatorExtensions.ToPlaywrightLocator(_page, _frame, locator);
        var count = playwrightLocator.CountAsync().GetAwaiter().GetResult();
        var elements = new List<IHtmlElement>(count);
        for (var i = 0; i < count; i++)
        {
            elements.Add(new PlaywrightHtmlElement(playwrightLocator.Nth(i)));
        }

        return elements;
    }

    /// <inheritdoc />
    public void GoTo(string url) => _page.GotoAsync(url).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void GoBack() => _page.GoBackAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public void GoForward() => _page.GoForwardAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public void Reload() => _page.ReloadAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public byte[] TakeScreenshot() => _page.ScreenshotAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public void SaveScreenshot(string path)
        => _page.ScreenshotAsync(new PageScreenshotOptions { Path = path }).GetAwaiter().GetResult();

    /// <inheritdoc />
    public string GetPageSource()
        => (_frame != null ? _frame.ContentAsync() : _page.ContentAsync()).GetAwaiter().GetResult();

    /// <inheritdoc />
    public string GetAutomationTree()
        => (_frame != null ? _frame.Locator("body") : _page.Locator("body")).AriaSnapshotAsync().GetAwaiter().GetResult();

    /// <summary>Nothing to release: the context owns the page.</summary>
    public void Dispose()
    {
    }
}
