namespace Brinell.Maui.Controls.Shapes;

/// <summary>
/// MAUI Ellipse shape control for drawing elliptical or circular shapes.
/// Ellipse is used as a visual drawing element, typically not requiring interaction testing.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Ellipse<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new Ellipse control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the Ellipse element.</param>
    public Ellipse(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new Ellipse control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Ellipse(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
}
