using System.Text;
using AngleSharp.Dom;
using Brinell.Core.Calls;
using Brinell.Core.Configuration;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Core.Logging;
using Brinell.Core.Scopes;
using Brinell.Html.Interfaces;
using ElementNotFoundException = Brinell.Core.Exceptions.ElementNotFoundException;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Html.Bunit;

/// <summary>
/// The bUnit head (.my/bunit/plan.md): the suite's root component rendered by bUnit, with the app's
/// services, behind the same <see cref="IHtmlTestContext"/> the browser head has, so page objects and
/// controls run unchanged. Navigation goes through bUnit's navigation manager; the root's router renders
/// the page.
/// </summary>
public sealed class BunitTestContext : IHtmlTestContext, IHtmlPageProbe, IAsyncDisposable
{
    private readonly BunitTestContextOptions _options;
    private readonly ITestLogger _logger;
    private readonly BunitHtmlDriver _driver;
    private readonly List<string> _back = [];
    private readonly List<string> _forward = [];
    private BunitContext _bunit = null!;
    private IRenderedComponent<IComponent> _root = null!;

    public BunitTestContext(BunitTestContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _logger = options.Logger ?? NullTestLogger.Instance;
        _driver = new BunitHtmlDriver(this);
        Start();
    }

    public IHtmlTestContext Context => this;

    public IHtmlDriver Driver => _driver;

    public string? Name => _options.Name;

    public NearMissSettings NearMiss => _options.NearMiss;

    public TimeoutSettings Timeouts => _options.Timeouts;

    public ITestLogger Logger => _logger;

    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.Css;

    public IHtmlPage? Page => null;

    public string CurrentUrl => Navigation.Uri;

    // PageTitle renders into the head, which App.razor holds and the root does not.
    public string PageTitle => string.Empty;

    /// <summary>The root's markup as it is now.</summary>
    public string Markup => _root.Markup;

    /// <summary>The services the app was rendered with, for a test that arranges through them.</summary>
    public IServiceProvider Services => _bunit.Services;

    private NavigationManager Navigation => _bunit.Services.GetRequiredService<NavigationManager>();

    public ScopeReadiness ProbeReadiness() => ScopeReadiness.Ready(nameof(BunitTestContext));

    public bool WaitReady(int? timeoutMs = null) => ProbeReadiness().IsReady;

    public bool IsIdle() => true;

    // As brinell-busy.js says once the circuit is up: rendered by bUnit, the page is interactive at once.
    public PageProbe ProbePage(string busyAttribute) => new("complete", true, "false");

    public IHtmlElement? TryFindElement(Locator locator) => _driver.FindElements(locator).FirstOrDefault();

    public IHtmlElement FindElement(Locator locator) => TryFindElement(locator) ?? throw new ElementNotFoundException(locator);

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator) => _driver.FindElements(locator);

    public void NavigateTo(string destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        _back.Add(CurrentUrl);
        _forward.Clear();
        Go(destination);
    }

    public void NavigateBack()
    {
        if (_back.Count == 0)
        {
            return;
        }

        _forward.Add(CurrentUrl);
        Go(Pop(_back));
    }

    public void GoForward()
    {
        if (_forward.Count == 0)
        {
            return;
        }

        _back.Add(CurrentUrl);
        Go(Pop(_forward));
    }

    public void Refresh()
    {
        var url = CurrentUrl;
        Restart();
        Go(url);
    }

    // No pixels on this head: the markup stands in, so a failure still leaves what the page showed.
    public byte[] TakeScreenshot() => Encoding.UTF8.GetBytes(Markup);

    public void SaveScreenshot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllBytes(Path.ChangeExtension(path, ".html"), TakeScreenshot());
    }

    public void ResetAppState()
    {
        _back.Clear();
        _forward.Clear();
        Restart();
    }

    internal IEnumerable<IElement> Nodes() => _root.Nodes.OfType<IElement>();

    internal IReadOnlyList<IElement> Select(Locator locator) => BunitLocators.FindAll(css => _root.Nodes.QuerySelectorAll(css), locator);

    public void Dispose() => _bunit.Dispose();

    public ValueTask DisposeAsync() => _bunit.DisposeAsync();

    private void Start()
    {
        _bunit = new BunitContext();
        _bunit.JSInterop.Mode = JSRuntimeMode.Loose;
        _options.ConfigureServices?.Invoke(_bunit.Services);
        if (_options.User is { } user)
        {
            _bunit.AddAuthorization().SetAuthorized(user);
        }

        var root = _options.RootComponent;
        _root = _bunit.Render(builder =>
        {
            builder.OpenComponent(0, root);
            builder.CloseComponent();
        });
        if (_options.BaseUrl is { Length: > 0 } baseUrl)
        {
            Go(baseUrl);
        }
    }

    private void Restart()
    {
        _bunit.Dispose();
        Start();
    }

    private void Go(string url) => _root.InvokeAsync(() => Navigation.NavigateTo(url)).GetAwaiter().GetResult();

    private static string Pop(List<string> stack)
    {
        var url = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return url;
    }
}
