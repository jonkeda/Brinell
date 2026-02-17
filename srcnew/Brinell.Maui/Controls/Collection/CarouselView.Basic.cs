namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI CarouselView control wrapper for root-level carousel interactions.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class CarouselView<TScope> : ControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a CarouselView control using an explicit locator.
    /// </summary>
    public CarouselView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a CarouselView control using the scope default locator strategy.
    /// </summary>
    public CarouselView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
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

    #endregion
}
