namespace Brinell.Maui.Scopes;

/// <summary>
/// An explicit escape to the driver root for application or platform chrome that does
/// not belong to an element-rooted page or container.
/// </summary>
public sealed class DriverRootScope<TScope>(IMauiScope<TScope> owner) : IMauiScope<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly IMauiScope<TScope> _owner = owner ?? throw new ArgumentNullException(nameof(owner));

    public TScope Self => _owner.Self;
    public LocatorStrategy DefaultLocatorStrategy => Context.DefaultLocatorStrategy;
    public IPageObject? Page => _owner.Page;
    public IMauiTestContext Context => _owner.Context;
    public bool IsReady(int? timeoutMs = null) => true;
    public bool WaitReady(int? timeoutMs = null) => true;
    public IMauiElement? TryFindElement(Locator locator) => Context.TryFindElement(locator);
    public IMauiElement? TryFindElementAfterScroll(Locator locator) => Context.TryFindElementAfterScroll(locator);
    public IMauiElement FindElement(Locator locator) => Context.FindElement(locator);
    public IReadOnlyList<IMauiElement> FindElements(Locator locator) => Context.FindElements(locator);
}