namespace Brinell.Maui.Controls.Selection;

/// <summary>
/// MAUI Picker control for dropdown selection.
/// Inherits SelectByText, SelectByIndex, GetSelectedText from SelectorControlBase.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Picker<TScope> : SelectorControlBase<TScope>
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

    #region Picker-Specific Methods

    /// <summary>
    /// Gets the title/header text of the picker.
    /// </summary>
    /// <returns>The title text, or null if not found.</returns>
    public string? GetTitle()
    {
        return GetTitleCore(TryFindElement());
    }

    /// <summary>
    /// Asserts the picker title matches the expected value.
    /// </summary>
    /// <param name="expected">Expected title text. Null skips the assertion.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
            return ContainingScope;

        return RunAssert(nameof(AssertTitle), expected, () =>
        {
            if (timeoutMs.HasValue)
            {
                RunWait(() => GetTitle() == expected, timeoutMs);
            }
            return GetTitle();
        }, message ?? $"Expected picker title to be '{expected}'. Locator: {Locator}");
    }

    #endregion

    #region Core Method Overrides

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
