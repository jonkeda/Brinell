namespace Brinell.Html.Uat.Tests.Runtime;

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

        CounterPage = new CounterPage(_context);
        FormControlsPage = new FormControlsPage(_context);
    }

    public CounterPage CounterPage { get; }

    public FormControlsPage FormControlsPage { get; }

    public void NavigateToCounter()
    {
        NavigateTo("/counter");
        CounterPage.CountDisplay.AssertVisible(true);
    }

    public void NavigateToFormControls()
    {
        NavigateTo("/form-controls");
        FormControlsPage.TermsCheckBox.AssertVisible(true);
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
