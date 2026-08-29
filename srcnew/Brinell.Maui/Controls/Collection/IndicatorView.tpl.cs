namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI IndicatorView control for displaying carousel or collection indicators.
/// IndicatorView shows visual indicators (typically dots) representing positions in a collection.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class IndicatorView<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new IndicatorView control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the IndicatorView element.</param>
    public IndicatorView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new IndicatorView control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public IndicatorView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
}
