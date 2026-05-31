using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Core.Logging;
using Brinell.Html.Context;
using Brinell.Html.Interfaces;
using Brinell.Html.Playwright;
using Microsoft.Playwright;

namespace Brinell.Blazor.Context;

public sealed class BlazorTestContext : IHtmlTestContext, IAsyncDisposable
{
    private readonly PlaywrightTestContext _inner;

    private BlazorTestContext(PlaywrightTestContext inner) => _inner = inner;

    public static async Task<BlazorTestContext> CreateAsync(HtmlTestContextOptions options)
        => new(await PlaywrightTestContext.CreateAsync(options));

    public static BlazorTestContext ForPage(IPage page, HtmlTestContextOptions? options = null)
        => new(PlaywrightTestContext.ForPage(page, options));

    // IHtmlTestContext / ITestContext properties
    public IHtmlTestContext Context => this;
    public TimeoutSettings Timeouts => _inner.Timeouts;
    public ITestLogger Logger => _inner.Logger;
    public LocatorStrategy DefaultLocatorStrategy => _inner.DefaultLocatorStrategy;
    public IPageObject? Page => _inner.Page;
    public string CurrentUrl => _inner.CurrentUrl;
    public string PageTitle => _inner.PageTitle;

    // IElementScope<IHtmlElement>
    public bool IsReady(int? timeoutMs = null) => _inner.IsReady(timeoutMs);
    public bool WaitReady(int? timeoutMs = null) => _inner.WaitReady(timeoutMs);
    public IHtmlElement? TryFindElement(Locator locator) => _inner.TryFindElement(locator);
    public IHtmlElement FindElement(Locator locator) => _inner.FindElement(locator);
    public IReadOnlyList<IHtmlElement> FindElements(Locator locator) => _inner.FindElements(locator);

    // ITestContext navigation
    public void NavigateTo(string destination) => _inner.NavigateTo(destination);
    public void NavigateBack() => _inner.NavigateBack();
    public void GoForward() => _inner.GoForward();
    public void Refresh() => _inner.Refresh();

    // ITestContext screenshots
    public byte[] TakeScreenshot() => _inner.TakeScreenshot();
    public void SaveScreenshot(string path) => _inner.SaveScreenshot(path);

    // ITestContext state
    public void ResetAppState() => _inner.ResetAppState();

    // Blazor-specific
    public void WaitForBlazorReady(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Timeouts.PageLoad;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var ready = _inner.InternalPage
                .EvaluateAsync<bool>("() => typeof window.Blazor !== 'undefined' || typeof window._blazor !== 'undefined'")
                .GetAwaiter().GetResult();
            if (ready) return;
            Thread.Sleep(100);
        }

        throw new TimeoutException($"Blazor was not ready within {timeout}ms.");
    }

    // Internal Playwright access
    internal IPage InternalPage => _inner.InternalPage;

    // IDisposable / IAsyncDisposable
    public void Dispose() => _inner.Dispose();

    public ValueTask DisposeAsync() => ((IAsyncDisposable)_inner).DisposeAsync();
}
