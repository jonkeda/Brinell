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
    private global::Bunit.TestDoubles.BunitAuthorizationContext _authorization = null!;
    private IServiceScope? _fallbackScope;

    // Who is signed in, as a browser's cookie keeps it across page loads; null is signed out.
    private (string User, string[] Roles, System.Security.Claims.Claim[] Claims)? _signedIn;

    // The values users entered, by element, with the value attribute they were entered over (BunitHtmlElement.InputValue).
    private readonly Dictionary<string, (string? Attribute, string Value)> _entered = new(StringComparer.Ordinal);
    private IRenderedComponent<IComponent> _root = null!;

    public BunitTestContext(BunitTestContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _logger = options.Logger ?? (ITestLogger?)OpenCallLog() ?? NullTestLogger.Instance;
        _driver = new BunitHtmlDriver(this);
        _signedIn = options.User is { } user ? (user, [.. options.Roles], []) : null;
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

    // Opening an address is a page load, as Playwright's GoTo is: a fresh root and scope, as a new circuit has, still signed in.
    // A link inside the page stays in its circuit (Follow).
    public void NavigateTo(string destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        _back.Add(CurrentUrl);
        _forward.Clear();
        Restart();
        Go(destination);
    }

    /// <summary>A link followed inside the page: the router's navigation in the same circuit, as Blazor's script does it.</summary>
    internal void Follow(string href)
    {
        _back.Add(CurrentUrl);
        _forward.Clear();
        Go(href);
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

    /// <summary>
    /// Signs <paramref name="user"/> in, as a suite's sign-in step does on this head (an app's sign-in page is not a
    /// component): the rendered page sees the new authentication state at once.
    /// </summary>
    /// <param name="user">The user's name, which <c>AuthorizeView</c> shows.</param>
    /// <param name="roles">The user's roles.</param>
    /// <param name="claims">Further claims the app reads, such as the user's id.</param>
    public void SignIn(string user, IEnumerable<string>? roles = null, IEnumerable<System.Security.Claims.Claim>? claims = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(user);
        string[] granted = [.. roles ?? []];
        System.Security.Claims.Claim[] extra = [.. claims ?? []];
        _signedIn = (user, granted, extra);
        _root.InvokeAsync(() =>
        {
            _authorization.SetAuthorized(user);
            _authorization.SetRoles(granted);
            _authorization.SetClaims(extra);
            Authentication.TriggerAuthenticationStateChanged(user, granted, extra, "bUnit");
        }).GetAwaiter().GetResult();
    }

    /// <summary>Signs the user out; the rendered page sees it at once.</summary>
    public void SignOut()
    {
        _signedIn = null;
        _root.InvokeAsync(() =>
        {
            _authorization.SetNotAuthorized();
            Authentication.TriggerUnauthenticationStateChanged();
        }).GetAwaiter().GetResult();
    }

    // bUnit's own provider: setting the authorization context alone does not tell a rendered page.
    private global::Bunit.TestDoubles.BunitAuthenticationStateProvider Authentication
        => (global::Bunit.TestDoubles.BunitAuthenticationStateProvider)_bunit.Services.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>();

    // As clearing a browser's cookies and storage: signed out, a fresh root.
    public void ResetAppState()
    {
        _back.Clear();
        _forward.Clear();
        _signedIn = null;
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

    public void Dispose()
    {
        _bunit.Dispose();
        _fallbackScope?.Dispose();
        _logger.Flush();
        _logger.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _bunit.DisposeAsync().ConfigureAwait(false);
        _fallbackScope?.Dispose();
        _logger.Flush();
        _logger.Dispose();
    }

    // The call log as the browser head writes it, its file named for this head: calls-bunit-….csv.
    private static CsvTestLogger? OpenCallLog()
    {
        var folder = Environment.GetEnvironmentVariable("BRINELL_CALL_LOG");
        return string.IsNullOrWhiteSpace(folder)
            ? null
            : new CsvTestLogger(Path.Combine(folder,
                $"calls-bunit-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..8]}.csv"));
    }

    private void Start()
    {
        _bunit = new BunitContext();
        _bunit.JSInterop.Mode = _options.JsInterop;
        _bunit.JSInterop.SetupVoid(BusyScript, _ => true).SetVoidResult();
        _options.ConfigureJs?.Invoke(_bunit.JSInterop);
        _options.ConfigureServices?.Invoke(_bunit.Services);
        if (_options.FallbackServices is { } fallback)
        {
            // A scope per render of the root, as each circuit has its own: no scoped state outlives a reset.
            _fallbackScope = fallback.GetService<IServiceScopeFactory>()?.CreateScope();
            _bunit.Services.AddFallbackServiceProvider(_fallbackScope?.ServiceProvider ?? fallback);
        }

        _authorization = _bunit.AddAuthorization();
        if (_signedIn is { } signedIn)
        {
            _authorization.SetAuthorized(signedIn.User);
            _authorization.SetRoles(signedIn.Roles);
            _authorization.SetClaims(signedIn.Claims);
        }

        // As a Blazor Server circuit renders, so a component asking RendererInfo gets the answer it has in the app; after the
        // services, since it makes the renderer.
        _bunit.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
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
        _fallbackScope?.Dispose();
        _entered.Clear();
        Start();
    }

    /// <summary>
    /// Keeps what the user entered into the element at <paramref name="key"/> while its value attribute stays
    /// <paramref name="attribute"/>, until the next action; <paramref name="value"/> null forgets it (the render wrote the value).
    /// An element is known by its place, which cannot tell a field rendered anew from the old one, so the entry lasts only as
    /// long as reading back what was just entered needs.
    /// </summary>
    internal void Enter(string key, string? attribute, string? value)
    {
        if (value is null)
        {
            _entered.Remove(key);
        }
        else
        {
            _entered[key] = (attribute, value);
        }
    }

    /// <summary>Forgets every entry: the next action may render the fields anew.</summary>
    internal void ForgetEntries() => _entered.Clear();

    /// <summary>What the user entered at <paramref name="key"/>, while no render has changed its value attribute since; else null.</summary>
    internal string? EnteredValue(string key, string? attribute)
    {
        if (!_entered.TryGetValue(key, out var entered))
        {
            return null;
        }

        if (string.Equals(entered.Attribute, attribute, StringComparison.Ordinal))
        {
            return entered.Value;
        }

        _entered.Remove(key);
        return null;
    }

    private void Go(string url)
    {
        _entered.Clear();
        _root.InvokeAsync(() => Navigation.NavigateTo(url)).GetAwaiter().GetResult();
    }

    private static string Pop(List<string> stack)
    {
        var url = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return url;
    }
}
