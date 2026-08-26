using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI ScrollView container. Scopes searches to the view's own subtree and adds
/// scrolling.
/// </summary>
/// <remarks>
/// <para>
/// A scrolling container needs both scroll behaviour and container scoping, and C# gives
/// one base class. Scoping wins the base slot — it is the reason to model a ScrollView as
/// a container at all — and the scroll mechanics are delegated to
/// <see cref="ScrollHelper"/>, which <see cref="CollectionObjectBase{TParent, TSelf, TItem}"/>
/// also uses.
/// </para>
/// <para>
/// Scrolling is UI Automation first, falling back to a pointer swipe only where pointer
/// input is permitted. The methods here report progress rather than throwing when
/// scrolling is not possible, because "cannot scroll further" is an ordinary outcome.
/// </para>
/// <para>
/// Unlike the other layout containers, <c>ScrollView</c> is addressable on Windows
/// without an automation handler.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The view type itself (self-referencing for fluent returns).</typeparam>
public class ScrollView<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
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

    /// <summary>
    /// Scrolls one viewport toward the end of the content.
    /// </summary>
    /// <returns>The container, for chaining.</returns>
    public TSelf ScrollForward()
    {
        ScrollHelper.TrySwipeForward(TryGetContainerRoot());
        return Self;
    }

    /// <summary>
    /// Scrolls one viewport back toward the start of the content.
    /// </summary>
    /// <returns>The container, for chaining.</returns>
    public TSelf ScrollBack()
    {
        ScrollHelper.TrySwipeBack(TryGetContainerRoot());
        return Self;
    }

    /// <summary>
    /// Brings a descendant into view, scrolling if necessary.
    /// </summary>
    /// <param name="locator">Locator for the descendant, resolved within this container.</param>
    /// <returns>The container, for chaining.</returns>
    /// <remarks>
    /// Asks the element to scroll itself into view via the platform's scroll-item
    /// pattern. Silently does nothing when the element is not present — use
    /// <see cref="ContainerObjectBase{TParent, TSelf}.FindElement"/> first if absence
    /// should be an error.
    /// </remarks>
    public TSelf ScrollTo(Locator locator)
    {
        ScrollHelper.TryScrollIntoView(TryFindElement(locator));
        return Self;
    }

    /// <summary>
    /// Brings a descendant into view by automation id.
    /// </summary>
    /// <returns>The container, for chaining.</returns>
    public TSelf ScrollTo(string automationId)
        => ScrollTo(Locator.ByAutomationId(automationId));
}

/// <summary>
/// A <see cref="ScrollView{TParent, TSelf}"/> for use where no view-specific subclass is
/// needed.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed class ScrollView<TParent> : ScrollView<TParent, ScrollView<TParent>>
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
