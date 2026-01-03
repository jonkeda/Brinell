using FlaUI.Core.Definitions;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

using FlaUIComboBox = FlaUI.Core.AutomationElements.ComboBox;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms ComboBox control wrapper.
/// Uses shared SelectorControlBase for FlaUI integration with WinForms-specific overrides.
/// </summary>
public class ComboBoxControl : SelectorControlBase, ISelectorControl
{
    public ComboBoxControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ComboBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get selected item text (from base class).
    /// </summary>
    public override string GetText()
    {
        return GetSelectedText() ?? string.Empty;
    }

    /// <summary>
    /// WinForms-specific: Select an item by text.
    /// Uses Click() to open dropdown (Expand pattern doesn't work for WinForms).
    /// </summary>
    public override void SelectByText(string text)
    {
        LogAction("SelectByText", text);
        
        var comboBox = GetComboBox();
        if (comboBox == null) return;
        
        // For WinForms, we must use Click() to open dropdown - Expand() doesn't work
        comboBox.Click();
        
        // Wait for dropdown to be expanded
        _context.WaitFor(() => 
        {
            var state = comboBox.Patterns.ExpandCollapse.PatternOrDefault?.ExpandCollapseState.Value;
            return state == ExpandCollapseState.Expanded;
        }, 2000, "dropdown expanded");
        
        // Now get items (only available when expanded)
        var items = comboBox.Items;
        int targetIndex = -1;
        
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var itemText = item.Name;
            if (string.IsNullOrEmpty(itemText))
            {
                itemText = item.Text;
            }
            
            if (itemText == text)
            {
                targetIndex = i;
                break;
            }
        }
        
        if (targetIndex >= 0 && targetIndex < items.Length)
        {
            // Click the item to select it
            items[targetIndex].Click();
            
            // Wait for dropdown to close
            _context.WaitFor(() => 
            {
                var state = comboBox.Patterns.ExpandCollapse.PatternOrDefault?.ExpandCollapseState.Value;
                return state == ExpandCollapseState.Collapsed;
            }, 2000, "dropdown closed");
            
            // Verify selection
            _context.WaitFor(() => GetSelectedText() == text, 1000, $"selection = '{text}'");
        }
        else
        {
            // Close dropdown if item not found
            comboBox.Collapse();
        }
    }

    /// <summary>
    /// WinForms-specific: Select an item by index.
    /// Uses Click() to open dropdown (Expand pattern doesn't work for WinForms).
    /// </summary>
    public override void SelectByIndex(int index)
    {
        LogAction("SelectByIndex", index.ToString());
        
        var comboBox = GetComboBox();
        if (comboBox == null) return;
        
        // For WinForms, we must use Click() to open dropdown - Expand() doesn't work
        comboBox.Click();
        
        // Wait for dropdown to be expanded
        _context.WaitFor(() => 
        {
            var state = comboBox.Patterns.ExpandCollapse.PatternOrDefault?.ExpandCollapseState.Value;
            return state == ExpandCollapseState.Expanded;
        }, 2000, "dropdown expanded");
        
        // Now get items (only available when expanded)
        var items = comboBox.Items;
        
        if (index >= 0 && index < items.Length)
        {
            var expectedText = items[index].Name ?? items[index].Text;
            
            // Click the item to select it
            items[index].Click();
            
            // Wait for dropdown to close
            _context.WaitFor(() => 
            {
                var state = comboBox.Patterns.ExpandCollapse.PatternOrDefault?.ExpandCollapseState.Value;
                return state == ExpandCollapseState.Collapsed;
            }, 2000, "dropdown closed");
            
            // Verify selection
            _context.WaitFor(() => GetSelectedText() == expectedText, 1000, $"selection = index {index}");
        }
        else
        {
            // Close dropdown if index out of range
            comboBox.Collapse();
        }
    }

    /// <summary>
    /// WinForms-specific: Get selected text.
    /// WinForms ComboBox often has the selection text in the Name property directly.
    /// </summary>
    public override string? GetSelectedText()
    {
        var comboBox = GetComboBox();
        if (comboBox == null) return null;
        
        // For WinForms DropDownList style, the selected item's text is in Value
        try
        {
            var value = comboBox.Value;
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }
        catch { }
        
        // Fall back to SelectedItem
        var selected = comboBox.SelectedItem;
        if (selected != null)
        {
            if (!string.IsNullOrEmpty(selected.Name))
            {
                return selected.Name;
            }
            if (!string.IsNullOrEmpty(selected.Text))
            {
                return selected.Text;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Expand/Open the dropdown.
    /// Uses Click() because Expand() doesn't work for WinForms ComboBox.
    /// </summary>
    public virtual void Open()
    {
        CheckVisible();
        var comboBox = GetComboBox();
        if (comboBox == null) return;
        
        comboBox.Click();
        
        _context.WaitFor(() => 
        {
            var state = comboBox.Patterns.ExpandCollapse.PatternOrDefault?.ExpandCollapseState.Value;
            return state == ExpandCollapseState.Expanded;
        }, 2000, "dropdown expanded");
        
        LogAction("Open");
    }

    /// <summary>
    /// Collapse/Close the dropdown.
    /// </summary>
    public virtual void Close()
    {
        var comboBox = GetComboBox();
        comboBox?.Collapse();
        LogAction("Close");
    }
}
