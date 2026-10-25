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
using Microsoft.AspNetCore.Components.Web;
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

    // PageTitle renders into the head, which App.razor holds and the root does not: the last PageTitle
    // the page rendered, as a browser shows the last one, its content rendered on its own.
    public string PageTitle
    {
        get
        {
            var title = _root.FindComponents<PageTitle>().LastOrDefault()?.Instance.ChildContent;
            return title is null ? string.Empty : BunitLocators.Normalize(_bunit.Render(title).Nodes.Select(node => node.TextContent).Aggregate(string.Empty, string.Concat));
        }
    }

    /// <summary>The root's markup as it is now.</summary>
    public string Markup => _root.Markup;

    /// <summary>The services the app was rendered with, for a test that arranges through them.</summary>
    public IServiceProvider Services => _bunit.Services;

    private NavigationManager Navigation => _bunit.Services.GetRequiredService<NavigationManager>();

    public ScopeReadiness ProbeReadiness()
    {
        ThrowIfAppFailed();
        return ScopeReadiness.Ready(nameof(BunitTestContext));
    }

    public bool WaitReady(int? timeoutMs = null) => ProbeReadiness().IsReady;

    public bool IsIdle() => true;

    // The page's own busy signal: the last brinellBusy.set call its layout made, which bUnit's JS interop
    // records. A root without that script is interactive from its first render, since nothing prerenders.
    public PageProbe ProbePage(string busyAttribute)
    {
        ThrowIfAppFailed();
        var busy = _bunit.JSInterop.Invocations
            .Where(call => call.Identifier == BusyScript)
            .Select(call => call.Arguments.FirstOrDefault())
            .LastOrDefault() is true;
        return new PageProbe("complete", true, busy ? "true" : "false");
    }

    /// <summary>The script a Blazor app's layout calls to say whether it is busy (<c>brinell-busy.js</c>).</summary>
    public const string BusyScript = "brinellBusy.set";

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

    internal IReadOnlyList<IElement> Select(Locator locator)
    {
        ThrowIfAppFailed();
        return BunitLocators.FindAll(css => _root.Nodes.QuerySelectorAll(css), locator);
    }

    // A handler that threw after its first await reaches bUnit's renderer, not the event that raised it:
    // the next look at the page reports it.
    internal void ThrowIfAppFailed()
    {
        if (_bunit.Renderer.UnhandledException is { IsCompleted: true } failed)
        {
            throw HandlerFailed("a handler", failed.Result);
        }
    }

    internal static AppUnavailableException HandlerFailed(string handler, Exception error)
        => new($"the page's {handler} threw {error.GetType().Name}: {error.Message} (in a browser the circuit ends here).", error);

    public void Dispose() => _bunit.Dispose();

    public ValueTask DisposeAsync() => _bunit.DisposeAsync();

    private void Start()
    {
        _bunit = new BunitContext();
        _bunit.JSInterop.Mode = _options.JsInterop;
        _bunit.JSInterop.SetupVoid(BusyScript, _ => true).SetVoidResult();
        _options.ConfigureJs?.Invoke(_bunit.JSInterop);
        _options.ConfigureServices?.Invoke(_bunit.Services);
        var authorization = _bunit.AddAuthorization();
        if (_options.User is { } user)
        {
            authorization.SetAuthorized(user);
            authorization.SetRoles([.. _options.Roles]);
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
