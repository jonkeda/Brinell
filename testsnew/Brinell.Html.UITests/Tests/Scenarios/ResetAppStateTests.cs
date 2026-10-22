using Microsoft.Playwright;

namespace Brinell.Html.UITests.Tests.Scenarios;

/// <summary>
/// <c>ResetAppState</c> gives the next scenario a new visitor: a new browser context on the same
/// browser, so neither cookies nor <c>localStorage</c> carry over (a tab a page remembered).
/// </summary>
public sealed class ResetAppStateTests : IAsyncLifetime
{
    private const string Origin = "http://reset.brinell.test/";
    private PlaywrightTestContext? _context;

    public async Task InitializeAsync()
        => _context = await PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions { Headless = true });

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
    }

    private async Task<IPage> OpenOriginAsync()
    {
        var page = _context!.InternalPage;
        await page.Context.RouteAsync(Origin + "**", route => route.FulfillAsync(new RouteFulfillOptions
        {
            ContentType = "text/html",
            Body = "<html><body>reset</body></html>",
        }));
        await page.GotoAsync(Origin);
        return page;
    }

    [Fact]
    public async Task ResetAppState_ClearsStorageAndCookies()
    {
        var page = await OpenOriginAsync();
        await page.EvaluateAsync("() => { localStorage.setItem('things.tab', 'Models'); document.cookie = 'kept=1'; }");
        Assert.Equal("Models", await page.EvaluateAsync<string?>("() => localStorage.getItem('things.tab')"));

        _context!.ResetAppState();

        var fresh = await OpenOriginAsync();
        Assert.NotSame(page, fresh);
        Assert.Null(await fresh.EvaluateAsync<string?>("() => localStorage.getItem('things.tab')"));
        Assert.Equal(string.Empty, await fresh.EvaluateAsync<string>("() => document.cookie"));
        Assert.True(page.IsClosed);
    }

    [Fact]
    public async Task ResetAppState_KeepsTheScreenSize()
    {
        await _context!.InternalPage.SetViewportSizeAsync(390, 844);

        _context.ResetAppState();

        Assert.Equal(390, _context.InternalPage.ViewportSize?.Width);
        Assert.Equal(844, _context.InternalPage.ViewportSize?.Height);
    }
}
