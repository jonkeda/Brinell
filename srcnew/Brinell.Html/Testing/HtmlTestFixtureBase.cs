using Brinell.Html.Context;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Testing;

/// <summary>
/// Base fixture for HTML UI tests.
/// </summary>
public abstract class HtmlTestFixtureBase
{
    private IHtmlTestContext? _context;

    protected IHtmlTestContext Context => _context
        ?? throw new InvalidOperationException("Context not initialized. Ensure InitializeAsync has completed.");

    protected virtual HtmlTestContextOptions CreateOptions() => new();

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
}