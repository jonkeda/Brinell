namespace Brinell.Blazor.UITests.TestBase;

public abstract class BlazorSampleTestBase : IAsyncLifetime
{
    private BlazorTestContext? _context;

    protected IHtmlTestContext Context => _context
        ?? throw new InvalidOperationException("Context has not been initialized.");

    protected string BaseUrl => Environment.GetEnvironmentVariable("BLAZOR_APP_URL")
        ?? "http://localhost:5180";

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

    protected void NavigateToPage(string path)
    {
        var destination = $"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        Context.NavigateTo(destination);
        _context!.WaitForBlazorReady();
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;
    }
}
