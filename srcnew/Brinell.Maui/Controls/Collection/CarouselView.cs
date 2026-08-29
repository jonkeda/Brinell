using Brinell.Maui.Containers;
using Brinell.Maui.Controls.Internal;

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
public abstract class CarouselView<TParent, TSelf, TItem>
    : CollectionObjectBase<TParent, TSelf, TItem>
    where TParent : IMauiScope<TParent>
    where TSelf : CarouselView<TParent, TSelf, TItem>
    where TItem : ItemContainerBase<TSelf, TItem>
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

    #region Position

    /// <summary>
    /// Gets the carousel's current zero-based position.
    /// </summary>
    /// <returns>The position, or null when the carousel cannot be resolved.</returns>
    public int? GetPosition()
    {
        var root = TryGetContainerRoot();
        if (root == null) return null;

        var attribute = root.GetAttribute("Position");
        return !string.IsNullOrEmpty(attribute) && int.TryParse(attribute, out var position)
            ? position
            : 0;
    }

    /// <summary>
    /// Whether the carousel loops back to the start.
    /// </summary>
    /// <returns>True when looping is enabled; null when the carousel cannot be resolved.</returns>
    public bool? IsLoopEnabled()
    {
        var root = TryGetContainerRoot();
        if (root == null) return null;

        var attribute = root.GetAttribute("Loop");
        return !string.IsNullOrEmpty(attribute)
            && attribute.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the card at the carousel's current position.
    /// </summary>
    /// <returns>The current card, or null when the position cannot be determined.</returns>
    public TItem? GetCurrentItem()
    {
        var position = GetPosition();
        return position == null ? null : TryItem(position.Value);
    }

    /// <summary>
    /// Swipes to the next card.
    /// </summary>
    /// <returns>The carousel, for chaining.</returns>
    /// <remarks>
    /// Pointer input, and therefore policy-gated on Windows. The swipe is a no-op rather
    /// than a failure where pointer input is forbidden.
    /// </remarks>
    public TSelf SwipeNext()
    {
        GestureHelper.TrySwipeLeft(TryGetContainerRoot());
        return Self;
    }

    /// <summary>
    /// Swipes to the previous card.
    /// </summary>
    /// <returns>The carousel, for chaining.</returns>
    public TSelf SwipePrevious()
    {
        GestureHelper.TrySwipeRight(TryGetContainerRoot());
        return Self;
    }

    /// <summary>
    /// Waits until the carousel reaches a position.
    /// </summary>
    public bool WaitPosition(int expectedPosition, int? timeoutMs = null)
        => RunWait(() => GetPosition() == expectedPosition, timeoutMs);

    /// <summary>
    /// Asserts the carousel's position, returning the carousel for chaining.
    /// </summary>
    public TSelf AssertPosition(int expectedPosition, string? message = null, int? timeoutMs = null)
    {
        if (!WaitPosition(expectedPosition, timeoutMs))
        {
            throw new AssertionException(
                message ?? $"Expected carousel position {expectedPosition} but it was {GetPosition()}. Locator: {Locator}");
        }

        return Self;
    }

    #endregion
}
