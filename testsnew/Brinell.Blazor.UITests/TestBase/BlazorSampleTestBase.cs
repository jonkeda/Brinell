using Brinell.Blazor.UITests.PageObjects;

namespace Brinell.Blazor.UITests.TestBase;

/// <summary>
/// One browser per test, on the shared Blazor sample (<see cref="BlazorSample"/>).
/// </summary>
public abstract class BlazorSampleTestBase : IAsyncLifetime
{
    private BlazorTestContext? _context;

    protected IHtmlTestContext Context => _context
        ?? throw new InvalidOperationException("Context has not been initialized.");

    protected string BaseUrl => BlazorSample.BaseUrl;

    public virtual async Task InitializeAsync()
    {
        _context = await BlazorTestContext.CreateAsync(new HtmlTestContextOptions
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
