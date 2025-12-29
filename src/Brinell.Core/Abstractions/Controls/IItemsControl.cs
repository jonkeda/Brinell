namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that contain collections of items.
/// </summary>
public interface IItemsControl : IControlObject
{
    /// <summary>
    /// Get the count of items.
    /// </summary>
    int GetItemCount();
    
    /// <summary>
    /// Get item text at index.
    /// </summary>
    string GetItemText(int index);
    
    /// <summary>
    /// Click an item by index.
    /// </summary>
    void ClickItem(int index);
    
    /// <summary>
    /// Click an item by text.
    /// </summary>
    void ClickItem(string text);
    
    /// <summary>
    /// Check if an item exists.
    /// </summary>
    bool HasItem(string text);
}
