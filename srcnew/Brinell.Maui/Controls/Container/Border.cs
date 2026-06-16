namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI Border control for visual containers with border styling.
/// Border is used to wrap content with optional border, shadow, and background styling.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Border<TScope> : ControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new border control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the border element.</param>
    public Border(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new border control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Border(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
}
