namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that select from a list of items.
/// </summary>
public interface ISelectorControl : IControlObject
{
    /// <summary>
    /// Get the selected item text.
    /// </summary>
    string? GetSelectedText();
    
    /// <summary>
    /// Get the selected item index.
    /// </summary>
    int GetSelectedIndex();
    
    /// <summary>
    /// Select an item by index.
    /// </summary>
    void SelectByIndex(int index);
    
    /// <summary>
    /// Select an item by text.
    /// </summary>
    void SelectByText(string text);
    
    /// <summary>
    /// Get all items.
    /// </summary>
    IReadOnlyList<string> GetItems();
    
    /// <summary>
    /// Get the count of items.
    /// </summary>
    int GetItemCount();
    
    /// <summary>
    /// Assert selected text equals expected.
    /// </summary>
    void AssertSelectedText(string expected, string? message = null);
}
