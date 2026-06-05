namespace Brinell.Html.Uat.Tests.Runtime;

[TestModuleScan(typeof(CounterPage), NamespacePrefix = "Brinell.Html.Uat.Tests.Pages")]
public sealed class HtmlUatFixture : IDisposable
{
    private readonly HtmlSampleHost _host;
    private readonly PlaywrightTestContext _context;
    private bool _disposed;

    public HtmlUatFixture()
    {
        _host = HtmlSampleHost.Start();
        try
        {
            _context = PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions
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

    public void NavigateToFormControls()
    {
        NavigateTo("/form-controls");
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
