using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Calls;
using Brinell.Html.Interfaces;
using Brinell.Html.Scopes;

namespace Brinell.Html.Controls;

/// <summary>
/// A scope inside another scope, found by its own root element. Its readiness asks the parent
/// first, then checks its root, then its own content (design 7.2).
/// </summary>
public abstract class ContainerBase<TParent, TScope> : ObjectBase, IHtmlContainer<TParent, TScope>
    where TParent : IHtmlScope<TParent>
    where TScope : IHtmlContainer<TParent, TScope>
{
    private readonly IHtmlScope<TParent> _parentScope;
    private readonly Locator _locator;

    protected ContainerBase(IHtmlScope<TParent> parentScope, Locator locator)
    {
        _parentScope = parentScope ?? throw new ArgumentNullException(nameof(parentScope));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }

    public override IHtmlTestContext Context => _parentScope.Context;

    public TParent Parent => _parentScope.Self;

    public abstract TScope Self { get; }

    public LocatorStrategy DefaultLocatorStrategy => _parentScope.DefaultLocatorStrategy;

    public IHtmlPage? Page => _parentScope.Page;

    /// <summary>The container's locator in its parent.</summary>
    protected Locator Locator => _locator;

    protected override string ScopeName => $"{GetType().Name}('{_locator}')";

    /// <summary>
    /// Whether the parent's readiness is on this container's path. False for a scope shown over
    /// its parent (a dialog over a busy page), which must be usable while the parent is not.
    /// </summary>
    protected virtual bool AsksParent => true;

    /// <summary>
    /// The container's own content check, once its root is there: a spinner gone, rows loaded.
    /// Ready by default.
    /// </summary>
    protected virtual ScopeReadiness ProbeContentReadiness(IHtmlElement root) => ContentReady();

    /// <summary>A ready answer from this container.</summary>
    protected ScopeReadiness ContentReady() => ScopeReadiness.Ready(ScopeName);

    /// <summary>A "content not ready" answer from this container.</summary>
    protected ScopeReadiness ContentNotReady(string? detail = null)
        => new(ScopeName, ScopeReadinessState.ContentNotReady, detail);

    /// <summary>The root element, found in one attempt, or null. Override to find it another way.</summary>
    protected virtual IHtmlElement? TryFindRootElement() => _parentScope.TryFindElement(_locator);

    public ScopeReadiness ProbeReadiness()
    {
        if (AsksParent && _parentScope.ProbeReadiness() is { IsReady: false } parent)
        {
            return parent;
        }

        return TryFindRootElement() is { } root
            ? ProbeContentReadiness(root)
            : new ScopeReadiness(ScopeName, ScopeReadinessState.MissingRoot);
    }

    public bool IsReady() => ProbeReadiness().IsReady;

    public bool WaitReady(int? timeoutMs = null)
        => RunProbe(() => ScopeGate.Check(ProbeReadiness()) ?? Observation.Done(), timeoutMs ?? DefaultTimeoutMs);

    public IHtmlElement ContainerRoot => TryFindRootElement() ?? throw new ElementNotFoundException(_locator);

    public IHtmlElement? TryFindElement(Locator locator)
    {
        return TryFindRootElement()?.TryFindElement(locator);
    }

    public IHtmlElement FindElement(Locator locator)
    {
        return TryFindElement(locator) ?? throw new ElementNotFoundException(locator);
    }

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator)
    {
        return TryFindRootElement()?.FindElements(locator) ?? [];
    }

    public Task<bool> WaitReadyAsync(int? timeoutMs = null) => Task.FromResult(WaitReady(timeoutMs));
}
