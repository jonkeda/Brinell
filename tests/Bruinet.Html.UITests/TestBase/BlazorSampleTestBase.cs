using Bruinet.Html.UITests.PageObjects;

namespace Bruinet.Html.UITests.TestBase;

/// <summary>
/// One browser per test, on the shared Blazor sample (<see cref="BlazorSample"/>); with
/// <c>BRUINET_HEAD=bunit</c>, the sample's <c>Routes</c> rendered by bUnit instead (AD-030).
/// </summary>
public abstract class BlazorSampleTestBase : IAsyncLifetime
{
    private IHtmlTestContext? _context;

    protected IHtmlTestContext Context => _context
        ?? throw new InvalidOperationException("Context has not been initialized.");

    protected string BaseUrl => BlazorSampleBunit.IsHead ? BlazorSampleBunit.BaseUrl : BlazorSample.BaseUrl;

    public virtual async Task InitializeAsync()
    {
        if (BlazorSampleBunit.IsHead)
        {
            _context = BlazorSampleBunit.Create();
            return;
        }

        _context = await PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions
        {
            BaseUrl = BaseUrl,
            Headless = ParseBool(Environment.GetEnvironmentVariable("HEADLESS"), true),
            BrowserType = Environment.GetEnvironmentVariable("BROWSER_TYPE") ?? "chromium"
        });
    }

    public virtual async Task DisposeAsync()
    {
        if (_context is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    /// <summary>Opens <paramref name="path"/> and waits until the app on it is interactive.</summary>
    protected void NavigateToPage(string path)
    {
        Context.NavigateTo($"{BaseUrl}/{path.TrimStart('/')}");
        new SampleShell(Context).AssertIdle();
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;
    }
}
