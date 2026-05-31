namespace Brinell.Blazor.Uat.Tests.Runtime;

public sealed class BlazorUatFixture : IDisposable
{
    private readonly BlazorSampleHost _host;
    private readonly BlazorTestContext _context;
    private bool _disposed;

    public BlazorUatFixture()
    {
        _host = BlazorSampleHost.Start();
        try
        {
            _context = BlazorTestContext.CreateAsync(new HtmlTestContextOptions
            {
                BaseUrl = _host.BaseUrl,
                Headless = ParseBool(Environment.GetEnvironmentVariable("HEADLESS"), true),
                BrowserType = Environment.GetEnvironmentVariable("BROWSER_TYPE") ?? "chromium"
            }).GetAwaiter().GetResult();
        }
        catch
        {
            _host.Dispose();
            throw;
        }

        CounterPage = new CounterPage(_context);
        LoginPage = new LoginPage(_context);
    }

    public CounterPage CounterPage { get; }

    public LoginPage LoginPage { get; }

    public void NavigateToCounter()
    {
        NavigateTo("/counter");
        CounterPage.CountDisplay.AssertVisible(true);
    }

    public void NavigateToLogin()
    {
        NavigateTo("/login");
        LoginPage.EmailInput.AssertVisible(true);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _context.DisposeAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }

    private void NavigateTo(string path)
    {
        _context.NavigateTo($"{_host.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}");
    }

    private static bool ParseBool(string? value, bool defaultValue)
        => bool.TryParse(value, out var parsed) ? parsed : defaultValue;
}
