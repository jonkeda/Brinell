namespace Brinell.Maui.Controls.Selection;

/// <summary>
/// MAUI Picker control for dropdown selection.
/// Inherits SelectByText, SelectByIndex, GetSelectedText from SelectorControlBase.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Picker<TScope> : Base.SelectorControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new picker control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the picker element.</param>
    public Picker(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new picker control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Picker(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Title - Core Methods

    /// <summary>
    /// Gets the picker title from the element.
    /// </summary>
    /// <param name="element">The picker element (may be null).</param>
    /// <returns>The title text.</returns>
    protected virtual string? GetTitleCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try Title attribute first
        var title = element.GetAttribute("Title");
        if (!string.IsNullOrEmpty(title))
            return title;

        // Try Name attribute
        var name = element.GetAttribute("Name");
        if (!string.IsNullOrEmpty(name))
            return name;

        return null;
    }

    #endregion
}
