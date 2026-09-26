using Brinell.Core.Calls;
using Brinell.Core.Composition;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Core.Scopes;
using Brinell.Html.Context;
using Brinell.Html.Interfaces;
using Brinell.Html.Scopes;

namespace Brinell.Html.Pages;

// Core's [TestPage], because Html pages no longer implement Core's IPageObject: test composition
// and UAT find them by the attribute (the same step MAUI took, .my/stale-readiness/plan.md 2.1).
[TestPage]
public abstract class HtmlPageObjectBase<TSelf> : ObjectBase, IHtmlPage<TSelf>
    where TSelf : HtmlPageObjectBase<TSelf>
{
    /// <summary>The attribute on <c>&lt;body&gt;</c> that carries the busy signal (design 7.2).</summary>
    public const string BusyAttribute = "data-busy";

    private static readonly Locator Body = Locator.ByCss("body");

    private readonly IHtmlTestContext _context;

    protected HtmlPageObjectBase(IHtmlTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected HtmlPageObjectBase(IHtmlTestContext context, string url)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Url = url;
    }

    public override IHtmlTestContext Context => _context;

    public TSelf Self => (TSelf)this;

    public string? Url { get; }

    public virtual string Name => GetType().Name;

    protected override string ScopeName => Name;

    public LocatorStrategy DefaultLocatorStrategy => _context.DefaultLocatorStrategy;

    public IHtmlPage? Page => this;

    /// <summary>How this page reads <c>data-busy</c>. Html pages: optional; Blazor pages: required.</summary>
    protected virtual BusySignal BusySignal => BusySignal.Optional;

    protected virtual bool CompareUrl()
    {
        if (Url is null)
        {
            return true;
        }

        var uri = new Uri(_context.CurrentUrl);

        return uri.LocalPath == Url;
    }

    /// <summary>Whether the page is loaded now: its URL, and the browser idle. The override point.</summary>
    public virtual bool IsLoaded()
    {
        return CompareUrl()
            && _context.IsIdle();
    }

    /// <summary>
    /// One probe of the page (design 7.2): loaded, then the busy signal. A page has no parent to
    /// ask.
    /// </summary>
    public virtual ScopeReadiness ProbeReadiness()
    {
        if (!IsLoaded())
        {
            return new ScopeReadiness(Name, ScopeReadinessState.NotLoaded);
        }

        if (BusySignal == BusySignal.Ignore)
        {
            return ScopeReadiness.Ready(Name);
        }

        var value = _context.TryFindElement(Body)?.GetAttribute(BusyAttribute);
        return value switch
        {
            null when BusySignal == BusySignal.Required
                => new ScopeReadiness(Name, ScopeReadinessState.NotLoaded, $"no {BusyAttribute} on <body> yet: the app is not interactive"),
            null => ScopeReadiness.Ready(Name),
            _ when string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) => ScopeReadiness.Ready(Name),
            _ when string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                => new ScopeReadiness(Name, ScopeReadinessState.Busy, $"{BusyAttribute}: '{value}'"),
            _ => new ScopeReadiness(Name, ScopeReadinessState.InvalidBusySignal,
                $"Page '{Name}': {BusyAttribute} on <body> must be 'true' or 'false', but is '{value}'."),
        };
    }

    public bool IsReady() => ProbeReadiness().IsReady;

    public bool WaitReady(int? timeoutMs = null)
        => RunProbe(() => ScopeGate.Check(ProbeReadiness()) ?? Observation.Done(), timeoutMs ?? _context.Timeouts.PageLoad);

    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        return RunProbe(
            () => IsLoaded() == expected.Value ? Observation.Done() : Observation.Pending(),
            timeoutMs ?? _context.Timeouts.PageLoad);
    }

    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return;
        }

        RunProbe(
            () => IsLoaded() == expected.Value ? Observation.Done() : Observation.Pending(),
            timeoutMs ?? _context.Timeouts.PageLoad,
            readiness => new ScopeNotReadyException(
                readiness,
                message ?? $"Expected page '{Name}' {(expected.Value ? "to be loaded" : "not to be loaded")} but it is {(expected.Value ? "not" : "still loaded")}."));
    }

    public bool IsBusy()
    {
        var readiness = ProbeReadiness();
        if (readiness.IsConfigurationError)
        {
            throw ScopeGate.Misconfigured(readiness);
        }

        return readiness.IsBusy;
    }

    public bool WaitBusy(bool? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        return RunProbe(() =>
        {
            var readiness = ProbeReadiness();
            if (readiness.IsConfigurationError)
            {
                throw ScopeGate.Misconfigured(readiness);
            }

            return readiness.IsLoaded && readiness.IsBusy == expected.Value
                ? Observation.Done()
                : Observation.ScopeNotReady(readiness);
        }, timeoutMs ?? _context.Timeouts.PageLoad);
    }

    public void AssertIdle(string? message = null, int? timeoutMs = null)
    {
        var budget = timeoutMs ?? _context.Timeouts.PageLoad;
        RunProbe(
            () => ScopeGate.Check(ProbeReadiness()) ?? Observation.Done(),
            budget,
            readiness => new ScopeNotReadyException(
                readiness,
                message ?? $"Page '{Name}' did not become ready within {budget} ms. Last readiness state: {readiness.State}" +
                    (readiness.Detail is null ? "." : $" ({readiness.Detail}).")));
    }

    public virtual string? GetTitle()
    {
        return _context.PageTitle;
    }

    public bool WaitTitle(string? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        return RunProbe(
            () => GetTitle() == expected ? Observation.Done() : Observation.Pending(),
            timeoutMs ?? _context.Timeouts.DefaultWait);
    }

    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return;
        }

        RunProbe(
            () => GetTitle() == expected ? Observation.Done() : Observation.Pending(),
            timeoutMs ?? _context.Timeouts.DefaultWait,
            _ =>
            {
                var actual = GetTitle();
                return new AssertionException(
                    message ?? $"Expected page title '{expected}' but got '{actual ?? "(null)"}'.", expected, actual);
            });
    }

    public void TakeScreenshot(string? filename = null)
    {
        var path = filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        _context.SaveScreenshot(path);
    }

    public IHtmlElement? TryFindElement(Locator locator)
    {
        return _context.TryFindElement(locator);
    }

    public IHtmlElement FindElement(Locator locator)
    {
        return _context.FindElement(locator);
    }

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator)
    {
        return _context.FindElements(locator);
    }

    protected HtmlTestContextOptions DefaultOptions() => new();
}
