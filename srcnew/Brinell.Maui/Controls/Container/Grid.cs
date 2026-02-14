namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI Grid control wrapper for layout containers.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Grid<TScope> : ControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new grid control within the specified scope.
    /// </summary>
    public Grid(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new grid control within the specified scope using a string locator value.
    /// </summary>
    public Grid(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
}
