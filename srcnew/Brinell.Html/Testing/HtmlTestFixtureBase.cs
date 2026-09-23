using Brinell.Core.Configuration;
using Brinell.Core.Artifacts;
using Brinell.Html.Context;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Testing;

/// <summary>
/// Base fixture for HTML UI tests.
/// Configuration is loaded from brinell.html.config.json
/// </summary>
public abstract class HtmlTestFixtureBase
{
    private IHtmlTestContext? _context;

    /// <summary>
    /// Current HTML configuration loaded from brinell.html.config.json
    /// </summary>
    protected BrinellHtmlConfiguration Configuration { get; } = BrinellHtmlConfiguration.Load();

    protected IHtmlTestContext Context => _context
        ?? throw new InvalidOperationException("Context not initialized. Ensure InitializeAsync has completed.");

    /// <summary>
    /// Allows per-test configuration overrides.
    /// </summary>
    protected void SetupWith(Action<BrinellHtmlConfiguration> configureAction)
    {
        ArgumentNullException.ThrowIfNull(configureAction);
        configureAction(Configuration);
    }

    protected virtual HtmlTestContextOptions CreateOptions() => new()
    {
        BaseUrl = Configuration?.Html?.AppUrl,
        Headless = Configuration?.Browser?.Headless ?? true,
        BrowserType = Configuration?.Browser?.BrowserType ?? "chromium"
    };

    protected abstract Task<IHtmlTestContext> CreateContextAsync(HtmlTestContextOptions options);

    public virtual async Task InitializeAsync()
    {
        var options = CreateOptions();
        _context = await CreateContextAsync(options).ConfigureAwait(false);
    }

    public virtual async Task DisposeAsync()
    {
        if (_context is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _context?.Dispose();
        }

        _context = null;
    }

    protected void NavigateTo(string path)
    {
        var options = CreateOptions();
        var baseUrl = options.BaseUrl?.TrimEnd('/') ?? string.Empty;
        Context.NavigateTo($"{baseUrl}{path}");
    }

    protected virtual ITestArtifactPathProvider GetArtifactPathProvider()
    {
        return DefaultTestArtifactPathProvider.Create(Configuration.Artifacts, GetType().Assembly.GetName().Name);
    }
}