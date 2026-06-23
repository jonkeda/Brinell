namespace Brinell.Blazor.Uat.Tests.Runtime;

using Brinell.Core.Configuration;

[TestModuleScan(typeof(CounterPage), NamespacePrefix = "Brinell.Blazor.UITests.PageObjects")]
public sealed class BlazorUatFixture : IDisposable
{
    private readonly BlazorSampleHost _host;
    private readonly BlazorTestContext _context;
    private bool _disposed;

    public BlazorUatFixture()
    {
        var config = BrinellBlazorConfiguration.Load();
        _host = BlazorSampleHost.Start(config?.Blazor);
        try
        {
            _context = BlazorTestContext.CreateAsync(new HtmlTestContextOptions
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
}
