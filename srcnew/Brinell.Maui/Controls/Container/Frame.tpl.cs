namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI Frame control for bordered containers with optional shadow styling.
/// Frame wraps content with a border, shadow, and background.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Frame<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new Frame control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the Frame element.</param>
    public Frame(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new Frame control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Frame(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
}
