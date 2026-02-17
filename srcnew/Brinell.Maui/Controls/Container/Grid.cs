namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI Grid control with container scoping for child element access.
/// Grid is the most common layout container in MAUI, so it supports
/// both existence/visibility checks and scoped child element searches.
/// </summary>
/// <typeparam name="TParent">The parent scope type for fluent chaining.</typeparam>
/// <typeparam name="TSelf">The grid type itself (self-referencing for fluent returns).</typeparam>
public class Grid<TParent, TSelf> : ContainerBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : Grid<TParent, TSelf>
{
    /// <summary>
    /// Creates a new grid control within the specified scope.
    /// </summary>
    public Grid(IMauiScope<TParent> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new grid control within the specified scope using a string locator value.
    /// </summary>
    public Grid(IMauiScope<TParent> scope, string locatorValue)
        : base(scope, new Locator(scope.DefaultLocatorStrategy, locatorValue))
    {
    }
}

/// <summary>
/// MAUI Grid control wrapper for simple existence/visibility checks.
/// Use Grid&lt;TParent, TSelf&gt; when you need container scoping for child elements.
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
