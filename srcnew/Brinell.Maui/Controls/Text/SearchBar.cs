namespace Brinell.Maui.Controls.Text;

/// <summary>
/// MAUI SearchBar control for search text input with search action support.
/// Inherits all text manipulation from Entry, adds search-specific methods.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class SearchBar<TScope> : Entry<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new search bar control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the search bar element.</param>
    public SearchBar(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new search bar control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public SearchBar(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Search-Specific Methods

    /// <summary>
    /// Enters text and submits the search.
    /// </summary>
    /// <param name="searchText">The text to search for. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Search(string? searchText, int? timeoutMs = null)
    {
        return RunSetWithElement( searchText, element =>
        {
            SetTextCore(element, searchText!, timeoutMs);
            // Submit search
            SubmitSearchCore(element);
        }, timeoutMs);
    }

    /// <summary>
    /// Submits the current search query.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SubmitSearch(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            SubmitSearchCore(element);
        }, timeoutMs);
    }

    /// <summary>
    /// Core implementation for submitting search.
    /// Platform-specific: sends Enter key to trigger search action.
    /// </summary>
    /// <param name="element">The search bar element.</param>
    protected virtual void SubmitSearchCore(IMauiElement element)
    {
        // Submit the search by pressing Enter key
        element.Submit();
    }
    
    /// <summary>
    /// Clears the element with fallback for complex controls.
    /// </summary>
    /// <param name="element">The element to clear.</param>
    protected virtual void ClearElementCore(IMauiElement element)
    {
        // For Windows/FlaUI, use ClearWithFallback for robust clearing
        if (element is Interfaces.INestedTextElement textElement)
        {
            textElement.ClearWithFallback();
            return;
        }
        
        element.Clear();
    }

    #endregion
    
    #region Text Override for Nested TextBox
    
    /// <summary>
    /// Gets the text using nested TextBox discovery for Windows SearchBar.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The text content, or null if not found.</returns>
    protected override string? GetTextCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        // For Windows/FlaUI, use GetNestedText which handles AutoSuggestBox structure
        if (element is Interfaces.INestedTextElement textElement)
        {
            var text = textElement.GetNestedText();
            if (text != null)
                return text;
        }
        
        // Fall back to base implementation
        return base.GetTextCore(element);
    }
    
    #endregion
}
