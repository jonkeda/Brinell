namespace Brinell.Html.Uat.Tests.Runtime;

using Brinell.Core.Configuration;

[TestModuleScan(typeof(CounterPage), NamespacePrefix = "Brinell.Html.Uat.Tests.Pages")]
public sealed class HtmlUatFixture : IDisposable
{
    private readonly HtmlSampleHost _host;
    private readonly PlaywrightTestContext _context;
    private bool _disposed;

    public HtmlUatFixture()
    {
        var config = BrinellHtmlConfiguration.Load();
        _host = HtmlSampleHost.Start(config?.Html);
        try
        {
            _context = PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions
            {
                BaseUrl = _host.BaseUrl,
                Headless = config?.Browser?.Headless ?? true,
                BrowserType = config?.Browser?.BrowserType ?? "chromium"
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
}
