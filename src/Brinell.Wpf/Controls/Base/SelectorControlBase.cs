using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls.Base;

/// <summary>
/// WPF base class for controls that select from a list of items.
/// </summary>
public abstract class SelectorControlBase : ControlBase, ISelectorControl
{
    protected SelectorControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// </summary>
    protected SelectorControlBase(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected SelectorControlBase(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the ComboBox pattern from the element.
    /// </summary>
    protected ComboBox? GetComboBox()
    {
        var element = FindElement();
        return element?.AsComboBox();
    }

    /// <summary>
    /// Get the ListBox pattern from the element.
    /// </summary>
    protected ListBox? GetListBox()
    {
        var element = FindElement();
        return element?.AsListBox();
    }

    /// <summary>
    /// Get the selected item text.
    /// Handles both bound items and static ComboBoxItems.
    /// For ComboBoxes with SelectedItem binding to enum/object, we need to get the displayed value.
    /// </summary>
    public virtual string? GetSelectedText()
    {
        var comboBox = GetComboBox();
        if (comboBox != null)
        {
            // First try to get the value from the ComboBox itself (displayed selection text)
            // This works better when SelectedItem is bound to an enum/object
            try
            {
                var value = comboBox.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch (FlaUI.Core.Exceptions.PatternNotSupportedException)
            {
                // Value pattern not supported, fall through to other methods
            }
            
            // For WPF ComboBox with static ComboBoxItems, the displayed text is in a 
            // ContentPresenter/TextBlock within the ToggleButton of the closed ComboBox.
            // Look in the ComboBox's visual tree for the displayed selection text.
            var displayedText = GetDisplayedTextFromComboBox(comboBox);
            if (!string.IsNullOrEmpty(displayedText))
            {
                return displayedText;
            }
            
            // Fall back to SelectedItem properties
            var selected = comboBox.SelectedItem;
            if (selected != null)
            {
                // Try Text property first (works for bound items)
                if (!string.IsNullOrEmpty(selected.Text))
                {
                    return selected.Text;
                }
                // Fall back to Name property (works for static ComboBoxItem with Content)
                if (!string.IsNullOrEmpty(selected.Name))
                {
                    return selected.Name;
                }
                
                // For WPF static ComboBoxItems, the content may be in a child TextBlock
                // Try to find a child element with text
                var contentText = GetContentFromChildren(selected);
                if (!string.IsNullOrEmpty(contentText))
                {
                    return contentText;
                }
            }
            
            // Last resort: expand the combobox and search for selected item
            try
            {
                var expandCollapsePattern = comboBox.Patterns.ExpandCollapse.PatternOrDefault;
                var wasExpanded = expandCollapsePattern?.ExpandCollapseState.Value == 
                    FlaUI.Core.Definitions.ExpandCollapseState.Expanded;
                
                if (!wasExpanded)
                {
                    comboBox.Expand();
                    Thread.Sleep(100);
                }
                
                foreach (var item in comboBox.Items)
                {
                    // Check if this item is selected using SelectionItemPattern
                    try
                    {
                        var selectionPattern = item.Patterns.SelectionItem.PatternOrDefault;
                        if (selectionPattern?.IsSelected.Value == true)
                        {
                            // Item is selected - try to get its displayed text
                            var itemText = item.Name;
                            if (string.IsNullOrEmpty(itemText))
                            {
                                itemText = GetDisplayedTextFromItem(item);
                            }
                            if (!wasExpanded)
                            {
                                comboBox.Collapse();
                            }
                            return itemText;
                        }
                    }
                    catch { }
                }
                
                if (!wasExpanded)
                {
                    comboBox.Collapse();
                }
            }
            catch { }
            
            return null;
        }
        
        var listBox = GetListBox();
        if (listBox != null)
        {
            var selected = listBox.SelectedItem;
            if (selected != null)
            {
                if (!string.IsNullOrEmpty(selected.Text))
                {
                    return selected.Text;
                }
                if (!string.IsNullOrEmpty(selected.Name))
                {
                    return selected.Name;
                }
            }
            return null;
        }
        
        return null;
    }

    /// <summary>
    /// Try to find the displayed selection text from the ComboBox's visual tree.
    /// When a WPF ComboBox is closed, the selection is displayed in a ContentPresenter
    /// or TextBlock inside the toggle button, not in the SelectedItem.
    /// </summary>
    private static string? GetDisplayedTextFromComboBox(ComboBox comboBox)
    {
        // The WPF ComboBox template typically has structure:
        // ComboBox → ToggleButton → Grid → ContentPresenter → TextBlock (with Text)
        // We need to search recursively for a TextBlock with non-empty Name
        return SearchForTextInVisualTree(comboBox, maxDepth: 5);
    }
    
    /// <summary>
    /// Recursively search for text content in the visual tree.
    /// </summary>
    private static string? SearchForTextInVisualTree(FlaUI.Core.AutomationElements.AutomationElement element, int maxDepth)
    {
        if (maxDepth <= 0) return null;
        
        var children = element.FindAllChildren();
        foreach (var child in children)
        {
            // Skip popup/list items - we want the displayed selection, not the dropdown items
            var controlType = child.ControlType;
            if (controlType == FlaUI.Core.Definitions.ControlType.List ||
                controlType == FlaUI.Core.Definitions.ControlType.ListItem ||
                controlType == FlaUI.Core.Definitions.ControlType.Window)
            {
                continue;
            }
            
            // For TextBlock-like elements, check the Name property (holds displayed text)
            if (controlType == FlaUI.Core.Definitions.ControlType.Text)
            {
                if (!string.IsNullOrEmpty(child.Name))
                {
                    return child.Name;
                }
            }
            
            // Recurse into children
            var found = SearchForTextInVisualTree(child, maxDepth - 1);
            if (!string.IsNullOrEmpty(found))
            {
                return found;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Try to extract content text from child elements of a ComboBoxItem.
    /// WPF static ComboBoxItems with Content="Text" render the text in a child TextBlock.
    /// </summary>
    private static string? GetContentFromChildren(FlaUI.Core.AutomationElements.AutomationElement element)
    {
        // Try finding child elements with text content
        var children = element.FindAllChildren();
        foreach (var child in children)
        {
            // Check if the child has Name (often holds the displayed text)
            if (!string.IsNullOrEmpty(child.Name))
            {
                return child.Name;
            }
            
            // Recurse into nested ContentPresenter
            var nestedText = GetContentFromChildren(child);
            if (!string.IsNullOrEmpty(nestedText))
            {
                return nestedText;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Get the selected item index.
    /// </summary>
    public virtual int GetSelectedIndex()
    {
        var comboBox = GetComboBox();
        if (comboBox != null)
        {
            var items = comboBox.Items;
            var selected = comboBox.SelectedItem;
            if (selected != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].Equals(selected))
                        return i;
                }
            }
        }
        
        var listBox = GetListBox();
        if (listBox != null)
        {
            var items = listBox.Items;
            var selected = listBox.SelectedItem;
            if (selected != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].Equals(selected))
                        return i;
                }
            }
        }
        
        return -1;
    }

    /// <summary>
    /// Select an item by index.
    /// </summary>
    public virtual void SelectByIndex(int index)
    {
        LogAction("SelectByIndex", index.ToString());
        
        var comboBox = GetComboBox();
        if (comboBox != null)
        {
            SelectComboBoxItemByIndex(comboBox, index);
            return;
        }
        
        var listBox = GetListBox();
        if (listBox != null)
        {
            var items = listBox.Items;
            if (index >= 0 && index < items.Length)
            {
                items[index].Click();
            }
        }
    }
    
    /// <summary>
    /// Select a ComboBox item using multiple fallback approaches.
    /// </summary>
    private void SelectComboBoxItemByIndex(ComboBox comboBox, int targetIndex)
    {
        // Expand to get items
        comboBox.Expand();
        Thread.Sleep(200);
        
        var items = comboBox.Items;
        if (targetIndex < 0 || targetIndex >= items.Length)
        {
            comboBox.Collapse();
            return;
        }
        
        var targetItem = items[targetIndex];
        
        // Log item info for debugging
        var bounds = targetItem.BoundingRectangle;
        System.Diagnostics.Debug.WriteLine($"[ComboBox Selection] Item {targetIndex}: Name='{targetItem.Name}', Bounds={bounds}");
        
        // Try invoking the item directly
        try
        {
            var invokePattern = targetItem.Patterns.Invoke.PatternOrDefault;
            if (invokePattern != null)
            {
                invokePattern.Invoke();
                Thread.Sleep(200);
                return;
            }
        }
        catch { }
        
        // Get the clickable point at center of item and click it
        try
        {
            var centerX = bounds.Left + bounds.Width / 2;
            var centerY = bounds.Top + bounds.Height / 2;
            
            // Use Mouse click directly at the center point
            FlaUI.Core.Input.Mouse.Click(new System.Drawing.Point((int)centerX, (int)centerY));
            Thread.Sleep(200);
            return;
        }
        catch { }
        
        // Fallback: Try SelectionItemPattern
        try
        {
            var selectionPattern = targetItem.Patterns.SelectionItem.PatternOrDefault;
            if (selectionPattern != null)
            {
                selectionPattern.Select();
                Thread.Sleep(100);
                comboBox.Collapse();
                Thread.Sleep(100);
                return;
            }
        }
        catch { }
        
        // Fallback: regular Click
        try
        {
            targetItem.Click();
            Thread.Sleep(100);
        }
        catch { }
        
        // Collapse as last resort
        try { comboBox.Collapse(); } catch { }
    }

    /// <summary>
    /// Select an item by text.
    /// For WPF ComboBox with static ComboBoxItems, the text may be in the Name property.
    /// </summary>
    public virtual void SelectByText(string text)
    {
        LogAction("SelectByText", text);
        
        var comboBox = GetComboBox();
        if (comboBox != null)
        {
            // Find the index of the item with matching text
            comboBox.Expand();
            Thread.Sleep(100);
            
            int targetIndex = -1;
            var items = comboBox.Items;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                // Check Text, Name, and child text content
                if (item.Text == text || 
                    item.Name == text || 
                    GetDisplayedTextFromItem(item) == text)
                {
                    targetIndex = i;
                    break;
                }
            }
            
            comboBox.Collapse();
            Thread.Sleep(50);
            
            if (targetIndex >= 0)
            {
                SelectComboBoxItemByIndex(comboBox, targetIndex);
            }
            return;
        }
        
        var listBox = GetListBox();
        if (listBox != null)
        {
            var item = listBox.Items.FirstOrDefault(i => i.Text == text || i.Name == text);
            if (item != null)
            {
                item?.Click();
                Thread.Sleep(100);
            }
        }
    }
    
    /// <summary>
    /// Get the displayed text from a ComboBoxItem by searching its children.
    /// </summary>
    private static string? GetDisplayedTextFromItem(FlaUI.Core.AutomationElements.AutomationElement item)
    {
        // Look in the item's children for text content
        var children = item.FindAllChildren();
        foreach (var child in children)
        {
            if (child.ControlType == FlaUI.Core.Definitions.ControlType.Text)
            {
                if (!string.IsNullOrEmpty(child.Name))
                    return child.Name;
            }
        }
        return null;
    }

    /// <summary>
    /// Get all items.
    /// For WPF static ComboBoxItems, the displayed text may be in Name instead of Text.
    /// </summary>
    public virtual IReadOnlyList<string> GetItems()
    {
        var comboBox = GetComboBox();
        if (comboBox != null)
        {
            // Expand to get items (WPF may not populate items collection until expanded)
            var expandPattern = comboBox.Patterns.ExpandCollapse.PatternOrDefault;
            var wasExpanded = expandPattern?.ExpandCollapseState.Value == 
                FlaUI.Core.Definitions.ExpandCollapseState.Expanded;
            
            if (!wasExpanded)
            {
                comboBox.Expand();
                Thread.Sleep(150);
            }
            
            var items = comboBox.Items.Select(i => 
                !string.IsNullOrEmpty(i.Text) ? i.Text : 
                !string.IsNullOrEmpty(i.Name) ? i.Name : 
                GetDisplayedTextFromItem(i) ?? string.Empty).ToList();
            
            if (!wasExpanded)
            {
                comboBox.Collapse();
                Thread.Sleep(100);
            }
            return items;
        }
        
        var listBox = GetListBox();
        if (listBox != null)
        {
            return listBox.Items.Select(i => 
                !string.IsNullOrEmpty(i.Text) ? i.Text : 
                !string.IsNullOrEmpty(i.Name) ? i.Name : string.Empty).ToList();
        }
        
        return Array.Empty<string>();
    }

    /// <summary>
    /// Get the count of items.
    /// </summary>
    public virtual int GetItemCount()
    {
        var comboBox = GetComboBox();
        if (comboBox != null)
        {
            return comboBox.Items.Length;
        }
        
        var listBox = GetListBox();
        if (listBox != null)
        {
            return listBox.Items.Length;
        }
        
        return 0;
    }

    /// <summary>
    /// Assert selected text equals expected.
    /// </summary>
    public virtual void AssertSelectedText(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedText();
        if (actual != expected)
        {
            ThrowAssertionFailed("SelectedText", actual, expected,
                message ?? $"Expected selected text '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("SelectedText", actual, expected);
    }
}
