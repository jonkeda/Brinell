namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI IsoPaneView (TwoPaneView) control for dual-pane layouts on foldable devices.
/// IsoPaneView is used to display content across multiple panes on foldable screens.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class IsoPaneView<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new IsoPaneView control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the IsoPaneView element.</param>
    public IsoPaneView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new IsoPaneView control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public IsoPaneView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
}
