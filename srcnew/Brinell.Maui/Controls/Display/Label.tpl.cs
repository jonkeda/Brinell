namespace Brinell.Maui.Controls.Display;

using Brinell.Maui.Interfaces;

/// <summary>
/// MAUI Label control for read-only text display.
/// Uses generated GetText(), AssertText(), and AssertTextContains() members.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Label<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new label control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the label element.</param>
    public Label(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new label control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Label(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Text - Core Methods

    /// <summary>
    /// Gets text from the label element, with support for nested text structures.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The text content, or null if not found.</returns>
    [GenerateComparisons(Comparison.Equals | Comparison.Contains | Comparison.StartsWith
        | Comparison.EndsWith | Comparison.Empty)]
    protected virtual string? GetTextCore(IMauiElement? element)
    {
        if (element == null) return null;

        // For Windows/FlaUI, use GetNestedText which handles complex label structures
        if (element is INestedTextElement<IMauiElement> textElement)
        {
            var text = textElement.GetNestedText();
            if (text != null)
                return text;
        }

        // Fall back to the raw element text
        return element.Text;
    }

    #endregion
}
