using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI SearchBar control wrapper.
/// Provides search input functionality.
/// </summary>
public class SearchBarControl : TextControlBase
{
    public SearchBarControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public SearchBarControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Enter search text and submit the search.
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    public void Search(string searchText)
    {
        LogAction("Search", searchText);
        SetText(searchText);
        Submit();
    }

    /// <summary>
    /// Submit the current search query.
    /// </summary>
    public void Submit()
    {
        LogAction("Submit");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"SearchBar '{AutomationId}' not visible for submit.");
        
        // Send Enter key to submit
        element.SendKeys("\n");
        _context.HideKeyboard();
    }

    /// <summary>
    /// Get the current search text.
    /// </summary>
    public string GetSearchText() => GetText();

    /// <summary>
    /// Clear the search and optionally dismiss keyboard.
    /// </summary>
    public void ClearSearch()
    {
        LogAction("ClearSearch");
        Clear();
        _context.HideKeyboard();
    }
}
