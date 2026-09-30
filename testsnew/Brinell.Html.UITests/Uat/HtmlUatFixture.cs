namespace Brinell.Html.UITests.Uat;

using Brinell.Html.UITests.Uat.Pages;

using Brinell.Core.Configuration;
using Brinell.Html.Testing;

[TestModuleScan(typeof(CounterPage), NamespacePrefix = "Brinell.Html.UITests.Uat.Pages")]
public sealed class HtmlUatFixture : IDisposable
{
    private readonly WebAppHost _host;
    private readonly PlaywrightTestContext _context;
    private bool _disposed;

    public HtmlUatFixture()
    {
        var config = BrinellHtmlConfiguration.Load();
        _host = WebAppHost.Start(new WebAppHostOptions
        {
            AppPath = config?.Html?.AppPath ?? WebAppHost.FindBuiltApp("samples/Brinell.Samples.Blazor.App"),
            Name = "Blazor sample",
        });
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
        _context.NavigateTo($"{_host.BaseUrl}/{path.TrimStart('/')}");
    }
}
