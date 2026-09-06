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
/// Position lives here rather than on <see cref="CollectionObjectBase{TParent, TSelf, TItem}"/>:
/// only a carousel has a "current" item, so putting it on the shared base would give every
/// collection a member most cannot honour.
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
    /// Gets the carousel's current zero-based position.
    /// </summary>
    /// <remarks>
    /// A carousel that reports no Position attribute is at zero, not unknown: the control
    /// exists and is showing its first card.
    /// </remarks>
    /// <param name="element">The carousel's own element (may be null).</param>
    /// <returns>The position, or null when the carousel cannot be resolved.</returns>
    [AbsenceTolerant]
    protected virtual int? GetPositionCore(IMauiElement? element)
    {
        if (element == null) return null;

        var attribute = element.GetAttribute("Position");
        return !string.IsNullOrEmpty(attribute) && int.TryParse(attribute, out var position)
            ? position
            : 0;
    }

    /// <summary>
    /// Whether the carousel loops back to the start.
    /// </summary>
    /// <param name="element">The carousel's own element (may be null).</param>
    /// <returns>True when looping is enabled; null when the carousel cannot be resolved.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsLoopEnabledCore(IMauiElement? element)
    {
        if (element == null) return null;

        var attribute = element.GetAttribute("Loop");
        return !string.IsNullOrEmpty(attribute)
            && attribute.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Swipes to the next card.
    /// </summary>
    /// <remarks>
    /// Pointer input, and therefore policy-gated on Windows. The swipe is a no-op rather
    /// than a failure where pointer input is forbidden.
    /// </remarks>
    /// <param name="element">The carousel's own element.</param>
    protected virtual void SwipeNextCore(IMauiElement element)
        => element.TrySwipeLeft();

    /// <summary>
    /// Swipes to the previous card.
    /// </summary>
    /// <param name="element">The carousel's own element.</param>
    protected virtual void SwipePreviousCore(IMauiElement element)
        => element.TrySwipeRight();

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Gets the card at the carousel's current position.
    /// </summary>
    /// <remarks>
    /// Hand-written: it returns a scoped item object rather than a value read from an
    /// element, so it has no Core form.
    /// </remarks>
    /// <returns>The current card, or null when the position cannot be determined.</returns>
    public TItem? GetCurrentItem()
    {
        var position = GetPosition();
        return position == null ? null : TryItem(position.Value);
    }

    #endregion
}
