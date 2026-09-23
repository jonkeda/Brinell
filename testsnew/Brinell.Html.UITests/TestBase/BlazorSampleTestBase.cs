namespace Brinell.Html.UITests.TestBase;

public abstract class BlazorSampleTestBase : IAsyncLifetime
{
    private PlaywrightTestContext? _context;

    protected IHtmlTestContext Context => _context
        ?? throw new InvalidOperationException("Context has not been initialized.");

    protected string BaseUrl => Environment.GetEnvironmentVariable("BLAZOR_APP_URL")
        ?? "http://localhost:5180";

    public virtual async Task InitializeAsync()
    {
        _context = await PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions
        {
            BaseUrl = BaseUrl,
            Headless = ParseBool(Environment.GetEnvironmentVariable("HEADLESS"), true),
            BrowserType = Environment.GetEnvironmentVariable("BROWSER_TYPE") ?? "chromium"
        });
    }

    public virtual async Task DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }
    }

    protected void NavigateToPage(string path)
    {
        var destination = $"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        Context.NavigateTo(destination);
        WaitForBlazorCircuit();
    }

    protected async Task NavigateToPageAsync(string path)
    {
        var destination = $"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        if (_context is PlaywrightTestContext pwContext)
        {
            await pwContext.NavigateToAsync(destination).ConfigureAwait(false);
        }
        else
        {
            Context.NavigateTo(destination);
        }
        await WaitForBlazorCircuitAsync().ConfigureAwait(false);
    }

    private void WaitForBlazorCircuit()
    {
        var body = Context.FindElement(new Locator(LocatorStrategy.Css, "body"));
        var deadline = DateTime.UtcNow.AddMilliseconds(10_000);
        while (DateTime.UtcNow < deadline)
        {
            var ready = body.Evaluate<bool>("() => typeof window.Blazor !== 'undefined' || typeof window._blazor !== 'undefined'");
            if (ready) return;
            Thread.Yield();
        }
        throw new TimeoutException("Blazor circuit was not ready within 10 s.");
    }

    private async Task WaitForBlazorCircuitAsync()
    {
        var body = Context.FindElement(new Locator(LocatorStrategy.Css, "body"));
        var deadline = DateTime.UtcNow.AddMilliseconds(10_000);
        while (DateTime.UtcNow < deadline)
        {
            var ready = body.Evaluate<bool>("() => typeof window.Blazor !== 'undefined' || typeof window._blazor !== 'undefined'");
            if (ready) return;
            await Task.Delay(50).ConfigureAwait(false);
        }
        throw new TimeoutException("Blazor circuit was not ready within 10 s.");
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;
    }
}