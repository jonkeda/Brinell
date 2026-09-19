using System.Globalization;
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
/// <b>Windows (probed 2026-09-18, MAUI 10).</b> The carousel is a UI Automation <c>List</c>
/// whose cards are <c>ListItem</c>s with <c>PositionInSet</c>; the current card is the only one
/// not off screen. The list publishes a Scroll pattern, but a scroll through it shows the next
/// card without MAUI's <c>Position</c> following, so it is not a swipe. The app declares
/// <c>uia:GestureAutomation.Verbs="SwipeLeft,SwipeRight,GetState"</c> on the carousel: the
/// swipes set <c>Position</c> (refused at either end when <c>Loop</c> is off), and
/// <c>GetState</c> answers <c>Position</c>.
/// </para>
/// <para>
/// <b>A looping carousel cannot be searched on Windows.</b> With <c>Loop</c> on (MAUI's
/// default) the list publishes its cards thousands of times over, and any descendant search
/// that walks through it - even one for a control after it - does not finish. Set
/// <c>Loop="False"</c> on a carousel a test looks inside or past.
/// </para>
/// <para>
/// <b>Android.</b> The swipes are touch; no state read answers <c>Position</c>, so it is null
/// and the swipes cannot confirm that the carousel moved.
/// </para>
/// <para>
/// There is no <c>Loop</c> member: neither platform publishes it to automation.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The carousel type itself (self-referencing).</typeparam>
/// <typeparam name="TItem">The card type.</typeparam>
public abstract partial class CarouselView<TParent, TSelf, TItem>
    : CollectionObjectBase<TParent, TSelf, TItem>
    where TParent : IMauiScope<TParent>
    where TSelf : CarouselView<TParent, TSelf, TItem>
    where TItem : class, IMauiItemObject<TSelf, TItem>
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
    /// Gets the index of the current card, counted from zero.
    /// </summary>
    /// <remarks>
    /// The app's own <c>Position</c>, through the bridge's <c>GetState</c>: the card on screen
    /// and the IndicatorView's selected dot both follow it. Null where the app does not declare
    /// <c>GetState</c> on the carousel, and on Android.
    /// </remarks>
    /// <param name="element">The carousel's own element.</param>
    /// <returns>The position, or null where the platform does not publish it.</returns>
    protected virtual int? GetPositionCore(IMauiElement element)
    {
        if (element.ReadState("Position") is not { } reported)
        {
            return null;
        }

        return int.TryParse(reported, NumberStyles.Integer, CultureInfo.InvariantCulture, out var position)
            ? position
            : throw new BrinellException(
                $"The carousel answered Position with '{reported}', which is not a number. Locator: {Locator}");
    }

    /// <summary>
    /// Swipes to the next card, and waits for the carousel to arrive there.
    /// </summary>
    /// <remarks>
    /// A gesture, not coordinates: the bridge's swipe verb on Windows, a real swipe on a touch
    /// platform. Throws where the platform has no route, naming the declaration to add, and on
    /// Windows when the carousel refuses (the last card, without <c>Loop</c>). Where the
    /// position is not published (Android) the move cannot be confirmed and is not waited for.
    /// </remarks>
    /// <param name="element">The carousel's own element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds for the move to show.</param>
    protected virtual void SwipeNextCore(IMauiElement element, int? timeoutMs = null)
        => SwipeAndConfirm(element, MauiGesture.SwipeLeft, "next", timeoutMs);

    /// <summary>
    /// Swipes to the previous card, and waits for the carousel to arrive there.
    /// </summary>
    /// <remarks>
    /// The mirror of <c>SwipeNext</c>; on Windows the first card, without <c>Loop</c>, refuses.
    /// </remarks>
    /// <param name="element">The carousel's own element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds for the move to show.</param>
    protected virtual void SwipePreviousCore(IMauiElement element, int? timeoutMs = null)
        => SwipeAndConfirm(element, MauiGesture.SwipeRight, "previous", timeoutMs);

    #endregion

    #region Helpers

    /// <summary>Swipes once and waits for the position to change, where it is published.</summary>
    private void SwipeAndConfirm(IMauiElement element, MauiGesture gesture, string direction, int? timeoutMs)
    {
        var before = GetPositionCore(element);

        element.PerformGesture(gesture);

        if (before is null)
        {
            return;
        }

        if (!Until(() => GetPositionCore(element), actual => actual != before, timeoutMs, out var lastError))
        {
            throw new TimeoutException(
                $"Carousel '{Locator}' was swiped to the {direction} card and stayed at position {before}.",
                lastError);
        }
    }

    #endregion
}
