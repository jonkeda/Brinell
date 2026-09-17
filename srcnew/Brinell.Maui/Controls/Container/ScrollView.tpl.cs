using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI ScrollView container. Scopes searches to the view's own subtree and adds
/// scrolling.
/// </summary>
/// <remarks>
/// Scrolling methods report progress rather than throwing when the content cannot scroll further.
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The view type itself (self-referencing for fluent returns).</typeparam>
public partial class ScrollView<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : ScrollView<TParent, TSelf>
{
    /// <summary>
    /// Creates a ScrollView container within the specified scope.
    /// </summary>
    public ScrollView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a ScrollView container using the scope's default locator strategy.
    /// </summary>
    public ScrollView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }

    /// <inheritdoc />
    /// <remarks>A scroll view is the element that scrolls for everything inside it.</remarks>
    public override IMauiElement? ScrollingRoot => TryGetContainerRoot();

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Scrolls one viewport toward the end of the content.
    /// </summary>
    /// <remarks>At the end of the content this does nothing.</remarks>
    /// <param name="element">The container's own element.</param>
    protected virtual void ScrollForwardCore(IMauiElement element)
        => ScrollHelper.StepForward(element);

    /// <summary>
    /// Scrolls one viewport back toward the start of the content.
    /// </summary>
    /// <param name="element">The container's own element.</param>
    protected virtual void ScrollBackCore(IMauiElement element)
        => ScrollHelper.StepBack(element);

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Brings a descendant into view, scrolling if necessary.
    /// </summary>
    /// <param name="locator">Locator for the descendant, resolved within this container.</param>
    /// <returns>The container, for chaining.</returns>
    /// <remarks>
    /// Does nothing when the element is not present; use
    /// <see cref="ContainerObjectBase{TParent, TSelf}.FindElement"/> first if absence should be an
    /// error.
    /// </remarks>
    public TSelf ScrollTo(Locator locator)
    {
        ScrollHelper.ScrollIntoView(TryFindElement(locator));
        return Self;
    }

    /// <summary>
    /// Brings a descendant into view by automation id.
    /// </summary>
    /// <returns>The container, for chaining.</returns>
    public TSelf ScrollTo(string automationId)
        => ScrollTo(Locator.ByAutomationId(automationId));

    #endregion
}

/// <summary>
/// A <see cref="ScrollView{TParent, TSelf}"/> for use where no view-specific subclass is
/// needed.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed partial class ScrollView<TParent> : ScrollView<TParent, ScrollView<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>
    /// Creates a ScrollView container within the specified scope.
    /// </summary>
    public ScrollView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a ScrollView container using the scope's default locator strategy.
    /// </summary>
    public ScrollView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}
