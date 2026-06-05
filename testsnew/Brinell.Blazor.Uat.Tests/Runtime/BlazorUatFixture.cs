namespace Brinell.Blazor.Uat.Tests.Runtime;

[TestModuleScan(typeof(CounterPage), NamespacePrefix = "Brinell.Blazor.UITests.PageObjects")]
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

        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IHtmlTestContext>(_context));
    }

    public TestComposition Composition { get; }

    public void NavigateToCounter()
    {
        NavigateTo("/counter");
    }

    public void NavigateToLogin()
    {
        NavigateTo("/login");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _context.DisposeAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _host.Dispose();
        }
    }

    private void NavigateTo(string path)
    {
        _context.NavigateTo($"{_host.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}");
    }

    private static bool ParseBool(string? value, bool defaultValue)
        => bool.TryParse(value, out var parsed) ? parsed : defaultValue;
}
