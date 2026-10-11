using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Microsoft.Playwright;
using static Brinell.Html.Playwright.PlaywrightErrors;

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
    public string PageTitle => Live(() => _page.TitleAsync().GetAwaiter().GetResult());

    /// <inheritdoc />
    public bool IsIdle()
    {
        try
        {
            Live(() => _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 10 }).GetAwaiter().GetResult());
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IHtmlElement> FindElements(Locator locator) => Live<IReadOnlyList<IHtmlElement>>(() =>
    {
        var playwrightLocator = LocatorExtensions.ToPlaywrightLocator(_page, _frame, locator);
        var count = playwrightLocator.CountAsync().GetAwaiter().GetResult();
        var elements = new List<IHtmlElement>(count);
        for (var i = 0; i < count; i++)
        {
            elements.Add(new PlaywrightHtmlElement(playwrightLocator.Nth(i)));
        }

        return elements;
    });

    /// <inheritdoc />
    public void GoTo(string url) => Live(() => _page.GotoAsync(url).GetAwaiter().GetResult());

    /// <inheritdoc />
    public void GoBack() => Live(() => _page.GoBackAsync().GetAwaiter().GetResult());

    /// <inheritdoc />
    public void GoForward() => Live(() => _page.GoForwardAsync().GetAwaiter().GetResult());

    /// <inheritdoc />
    public void Reload() => Live(() => _page.ReloadAsync().GetAwaiter().GetResult());

    /// <inheritdoc />
    public byte[] TakeScreenshot() => Live(() => _page.ScreenshotAsync().GetAwaiter().GetResult());

    /// <inheritdoc />
    /// <remarks>The whole page, not only the viewport, so a saved screen shows everything on it.</remarks>
    public void SaveScreenshot(string path)
        => Live(() => _page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true }).GetAwaiter().GetResult());

    /// <inheritdoc />
    public string GetPageSource()
        => Live(() => (_frame != null ? _frame.ContentAsync() : _page.ContentAsync()).GetAwaiter().GetResult());

    /// <inheritdoc />
    public string GetAutomationTree()
        => Live(() => (_frame != null ? _frame.Locator("body") : _page.Locator("body")).AriaSnapshotAsync().GetAwaiter().GetResult());

    /// <summary>Nothing to release: the context owns the page.</summary>
    public void Dispose()
    {
    }
}
