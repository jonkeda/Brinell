namespace Bruinell.Html.UITests.Uat;

using Bruinell.Html.UITests.Uat.Pages;

using Bruinell.Core.Configuration;
using Bruinell.Html.Testing;
using Bruinell.Html.UITests.TestBase;

[TestModuleScan(typeof(CounterPage), NamespacePrefix = "Bruinell.Html.UITests.Uat.Pages")]
public sealed class HtmlUatFixture : IDisposable
{
    private readonly WebAppHost? _host;
    private readonly IHtmlTestContext _context;
    private readonly string _baseUrl;
    private bool _disposed;

    // The browser head starts the sample and a browser; the bUnit head renders its Routes (WebHeads).
    public HtmlUatFixture()
    {
        if (BlazorSampleBunit.IsHead)
        {
            _context = BlazorSampleBunit.Create();
            _baseUrl = BlazorSampleBunit.BaseUrl;
            Composition = TestComposition.ForFixture(this, services =>
                services.AddSingleton(_context));
            return;
        }

        var config = BruinellHtmlConfiguration.Load();
        _host = WebAppHost.Start(new WebAppHostOptions
        {
            AppPath = config?.Html?.AppPath ?? WebAppHost.FindBuiltApp("samples/Bruinell.Samples.Blazor.App"),
            Name = "Blazor sample",
        });
        _baseUrl = _host.BaseUrl;
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
            services.AddSingleton(_context));
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
        try
        {
            (_context as IAsyncDisposable)?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        finally
        {
            _host?.Dispose();
        }
    }

    private void NavigateTo(string path)
    {
        _context.NavigateTo($"{_baseUrl}/{path.TrimStart('/')}");
    }
}
