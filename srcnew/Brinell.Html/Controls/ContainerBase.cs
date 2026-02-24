using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls;

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

    public IPageObject? Page => _parentScope.Page;

    public bool IsReady(int? timeoutMs = null)
    {
        if (!_parentScope.IsReady(timeoutMs))
        {
            return false;
        }

        return _parentScope.TryFindElement(_locator) != null;
    }

    public bool WaitReady(int? timeoutMs = null)
    {
        return Poll(() => IsReady(), timeoutMs ?? DefaultTimeoutMs);
    }

    public IHtmlElement ContainerRoot => _parentScope.FindElement(_locator);

    public IHtmlElement? TryFindElement(Locator locator)
    {
        var root = _parentScope.TryFindElement(_locator);
        if (root == null)
        {
            return null;
        }

        return root.TryFindElement(locator, out var child) ? child : null;
    }

    public IHtmlElement FindElement(Locator locator)
    {
        return ContainerRoot.FindElement(locator);
    }

    public IReadOnlyList<IHtmlElement> FindElements(Locator locator)
    {
        return ContainerRoot.FindElements(locator);
    }

    public async Task<bool> WaitReadyAsync(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return await PollAsync(async () => IsReady(), timeout).ConfigureAwait(false);
    }
}
