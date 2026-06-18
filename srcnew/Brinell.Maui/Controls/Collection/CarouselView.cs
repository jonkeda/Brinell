namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI CarouselView control for displaying swipeable item carousels.
/// Combines list functionality with swipe-based navigation between items.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public class CarouselView<TScope, TItem> : List<TScope, TItem>
    where TScope : IMauiScope<TScope>
    where TItem : class
{
    /// <summary>
    /// Creates a CarouselView control.
    /// </summary>
    /// <param name="scope">The containing scope.</param>
    /// <param name="listLocator">Locator for the CarouselView container.</param>
    /// <param name="itemAutomationIdPrefix">Prefix for item AutomationIds.</param>
    /// <param name="itemFactory">Factory to create item containers.</param>
    public CarouselView(
        IMauiScope<TScope> scope,
        Locator listLocator,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, listLocator, itemAutomationIdPrefix, itemFactory)
    {
    }

    /// <summary>
    /// Creates a CarouselView control using automation ID.
    /// </summary>
    public CarouselView(
        IMauiScope<TScope> scope,
        string automationId,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, automationId, itemAutomationIdPrefix, itemFactory)
    {
    }

    #region CarouselView-Specific Methods

    /// <summary>
    /// Gets the current position (0-based index) of the carousel.
    /// </summary>
    /// <returns>The current position, or null if element not found.</returns>
    public int? GetPosition()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var attr = element.GetAttribute("Position");
        if (!string.IsNullOrEmpty(attr) && int.TryParse(attr, out var position))
        {
            return position;
        }

        return 0;
    }

    /// <summary>
    /// Swipes to the next item in the carousel.
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SwipeNext(int? timeoutMs = null)
    {
        return RunDoWithElement(  element =>
        {
            var rect = element.Rect;
            var centerY = rect.Y + rect.Height / 2;
            var startX = rect.X + rect.Width - 20;
            var endX = rect.X + 20;

            element.Swipe(startX, centerY, endX, centerY);
        }, timeoutMs);
    }

    /// <summary>
    /// Swipes to the previous item in the carousel.
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SwipePrevious(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            var rect = element.Rect;
            var centerY = rect.Y + rect.Height / 2;
            var startX = rect.X + 20;
            var endX = rect.X + rect.Width - 20;

            element.Swipe(startX, centerY, endX, centerY);
        }, timeoutMs);
    }

    /// <summary>
    /// Waits for the carousel to reach the expected position.
    /// </summary>
    /// <param name="expectedPosition">The expected 0-based position.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if position was reached, false if timeout.</returns>
    public bool WaitPosition(int expectedPosition, int? timeoutMs = null)
    {
        return RunWait(() => GetPosition() == expectedPosition, timeoutMs);
    }

    /// <summary>
    /// Asserts the carousel is at the expected position.
    /// </summary>
    /// <param name="expectedPosition">The expected 0-based position.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertPosition(int expectedPosition, string? message = null, int? timeoutMs = null)
    {
        if (!WaitPosition(expectedPosition, timeoutMs))
        {
            var actual = GetPosition();
            throw new AssertionException(
                message ?? $"Expected carousel position {expectedPosition} but got {actual}. Locator: {Locator}");
        }
        return ContainingScope;
    }

    /// <summary>
    /// Gets whether the carousel loops back to the beginning.
    /// </summary>
    /// <returns>True if looping is enabled, null if element not found.</returns>
    public bool? IsLoopEnabled()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var attr = element.GetAttribute("Loop");
        if (!string.IsNullOrEmpty(attr))
        {
            return attr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Gets the current item at the carousel's position.
    /// </summary>
    /// <returns>The current item, or null if position cannot be determined.</returns>
    public TItem? GetCurrentItem()
    {
        var position = GetPosition();
        if (position == null) return null;

        return Item(position.Value);
    }

    #endregion
}
