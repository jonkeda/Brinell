using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI CarouselView: a swipeable collection with a notion of current position.
/// </summary>
/// <remarks>
/// <para>
/// Derive from this rather than instantiating it — the base is self-referencing so that
/// every member returns the concrete collection type:
/// </para>
/// <code>
/// public class BannerCarousel : CarouselView&lt;HomePage, BannerCarousel, BannerCard&gt;
/// {
///     public BannerCarousel(IMauiScope&lt;HomePage&gt; scope)
///         : base(scope, "Banners", ItemStrategy.ByAutomationId("BannerCard"),
///                (c, root, i) =&gt; new BannerCard(c, root, i)) { }
/// }
/// </code>
/// <para>
/// There is deliberately no <c>Position</c> or <c>IsLoopEnabled</c> here. Both were read from
/// a MAUI bindable property through <c>GetAttribute</c>, and neither platform publishes those
/// to automation: Windows maps seven attribute names and nothing else, Android raises for an
/// unknown one. The members existed and answered a constant - 0 and false - whatever the app
/// did. A carousel's position needs an automation source before it can be reported.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The carousel type itself (self-referencing).</typeparam>
/// <typeparam name="TItem">The card type.</typeparam>
public abstract partial class CarouselView<TParent, TSelf, TItem>
    : CollectionObjectBase<TParent, TSelf, TItem>
    where TParent : IMauiScope<TParent>
    where TSelf : CarouselView<TParent, TSelf, TItem>
    where TItem : class, IMauiItemContainer<TSelf, TItem>
{
    /// <summary>
    /// Creates a CarouselView bound to an explicit locator.
    /// </summary>
    protected CarouselView(
        IMauiScope<TParent> parentScope,
        Locator locator,
        IItemStrategy itemStrategy,
        Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, locator, itemStrategy, itemFactory)
    {
    }

    /// <summary>
    /// Creates a CarouselView using the scope's default locator strategy.
    /// </summary>
    protected CarouselView(
        IMauiScope<TParent> parentScope,
        string automationId,
        IItemStrategy itemStrategy,
        Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, automationId, itemStrategy, itemFactory)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Swipes to the next card.
    /// </summary>
    /// <remarks>
    /// Pointer input, and therefore policy-gated on Windows. The swipe is a no-op rather
    /// than a failure where pointer input is forbidden.
    /// </remarks>
    /// <param name="element">The carousel's own element.</param>
    protected virtual void SwipeNextCore(IMauiElement element)
        => element.SwipeLeft();

    /// <summary>
    /// Swipes to the previous card.
    /// </summary>
    /// <param name="element">The carousel's own element.</param>
    protected virtual void SwipePreviousCore(IMauiElement element)
        => element.SwipeRight();

    #endregion

}
