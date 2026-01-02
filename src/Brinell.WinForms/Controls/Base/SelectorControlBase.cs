using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls.Base;

/// <summary>
/// Abstract base class for selector controls (ComboBox, ListBox, TreeView, TabControl).
/// Provides item selection, enumeration, and related operations.
/// </summary>
public abstract class SelectorControlBase : ControlBase
{
    /// <summary>
    /// Create a selector control with page context and AutomationId.
    /// </summary>
    protected SelectorControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a selector control that searches within a container element.
    /// </summary>
    protected SelectorControlBase(FlaUITestContext context, IPageObject? page, AutomationElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    /// <summary>
    /// Create a selector control without page context (for global controls).
    /// </summary>
    protected SelectorControlBase(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the currently selected item text.
    /// Override in derived classes for specific control types.
    /// </summary>
    public virtual string GetSelectedItem()
    {
        var element = FindElement();
        if (element == null) return string.Empty;

        var comboBox = element.AsComboBox();
        if (comboBox != null)
        {
            var selectedItem = comboBox.SelectedItem;
            return selectedItem?.ToString() ?? string.Empty;
        }

        var listBox = element.AsListBox();
        if (listBox != null)
        {
            var selectedItem = listBox.SelectedItem;
            return selectedItem?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Get the index of the currently selected item.
    /// </summary>
    public virtual int GetSelectedIndex()
    {
        var element = FindElement();
        if (element == null) return -1;

        var selectedItem = GetSelectedItem();
        if (string.IsNullOrEmpty(selectedItem)) return -1;

        var allItems = GetItems();
        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] == selectedItem)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Get all available items in the control.
    /// </summary>
    public virtual IReadOnlyList<string> GetItems()
    {
        var element = FindElement();
        if (element == null) return new List<string>();

        var comboBox = element.AsComboBox();
        if (comboBox != null)
        {
            var items = comboBox.Items.Select(item => item?.ToString() ?? string.Empty).ToList();
            return items.AsReadOnly();
        }

        var listBox = element.AsListBox();
        if (listBox != null)
        {
            var items = listBox.Items.Select(item => item?.ToString() ?? string.Empty).ToList();
            return items.AsReadOnly();
        }

        return new List<string>();
    }

    /// <summary>
    /// Get the count of items in the control.
    /// </summary>
    public virtual int GetItemCount()
    {
        return GetItems().Count;
    }

    /// <summary>
    /// Select an item by its text value.
    /// Includes async handling with Application.DoEvents() to ensure state propagation.
    /// </summary>
    public virtual void SelectByText(string text)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SelectByText", $"Element '{AutomationId}' not visible for selection.");
        }

        var comboBox = element!.AsComboBox();
        if (comboBox != null)
        {
            var item = comboBox.Items.FirstOrDefault(i => i?.ToString() == text);
            if (item == null)
            {
                ThrowCheckFailed("SelectByText", 
                    $"Item '{text}' not found in element '{AutomationId}'. Available items: {string.Join(", ", GetItems())}");
            }

            item!.Select();
            System.Windows.Forms.Application.DoEvents(); // Force UI update
            System.Threading.Thread.Sleep(100); // Allow state to propagate
            LogAction("SelectByText", text);
            return;
        }

        var listBox = element.AsListBox();
        if (listBox != null)
        {
            var item = listBox.Items.FirstOrDefault(i => i?.ToString() == text);
            if (item == null)
            {
                ThrowCheckFailed("SelectByText", 
                    $"Item '{text}' not found in element '{AutomationId}'. Available items: {string.Join(", ", GetItems())}");
            }

            item!.Select();
            System.Windows.Forms.Application.DoEvents(); // Force UI update
            System.Threading.Thread.Sleep(100); // Allow state to propagate
            LogAction("SelectByText", text);
            return;
        }

        ThrowCheckFailed("SelectByText", $"Element '{AutomationId}' is not a ComboBox or ListBox.");
    }

    /// <summary>
    /// Select an item by its index.
    /// </summary>
    public virtual void SelectByIndex(int index)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SelectByIndex", $"Element '{AutomationId}' not visible for selection.");
        }

        var comboBox = element!.AsComboBox();
        if (comboBox != null)
        {
            if (index < 0 || index >= comboBox.Items.Length)
            {
                ThrowCheckFailed("SelectByIndex", 
                    $"Index {index} out of range. Element '{AutomationId}' has {comboBox.Items.Length} items.");
            }

            // FlaUI ComboBox items have a Select() method
            var item = comboBox.Items[index];
            item!.Select();
            System.Windows.Forms.Application.DoEvents(); // Force UI update
            System.Threading.Thread.Sleep(100); // Allow state to propagate
            LogAction("SelectByIndex", index.ToString());
            return;
        }

        var listBox = element.AsListBox();
        if (listBox != null)
        {
            if (index < 0 || index >= listBox.Items.Length)
            {
                ThrowCheckFailed("SelectByIndex", 
                    $"Index {index} out of range. Element '{AutomationId}' has {listBox.Items.Length} items.");
            }

            // FlaUI ListBox items have a Select() method
            var item = listBox.Items[index];
            item!.Select();
            System.Windows.Forms.Application.DoEvents(); // Force UI update
            System.Threading.Thread.Sleep(100); // Allow state to propagate
            LogAction("SelectByIndex", index.ToString());
            return;
        }

        ThrowCheckFailed("SelectByIndex", $"Element '{AutomationId}' is not a ComboBox or ListBox.");
    }

    /// <summary>
    /// Wait for an item to be selected.
    /// </summary>
    public virtual bool WaitSelected(string expectedItem, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(() => GetSelectedItem() == expectedItem, timeout,
            $"item '{expectedItem}' selected");
        LogWait($"Selected={expectedItem}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Assert that a specific item is selected.
    /// </summary>
    public virtual void AssertSelectedItem(string expected)
    {
        var actual = GetSelectedItem();
        if (actual != expected)
        {
            ThrowAssertionFailed("SelectedItem", actual, expected,
                $"Expected item '{expected}' to be selected in element '{AutomationId}' but got '{actual}'.");
        }
        LogAssertPass("SelectedItem", actual, expected);
    }

    /// <summary>
    /// Wait and assert that a specific item is selected.
    /// </summary>
    public virtual void AssertSelectedItemWait(string expected, int? timeoutMs = null)
    {
        if (!WaitSelected(expected, timeoutMs))
        {
            var actual = GetSelectedItem();
            ThrowAssertionFailed("SelectedItemWait", actual, expected,
                $"Expected item '{expected}' to be selected in element '{AutomationId}' but got '{actual}'.");
        }
        LogAssertPass("SelectedItemWait", expected, expected);
    }

    /// <summary>
    /// Select multiple items (for ListBox with multi-select).
    /// Override in ListBox-specific control.
    /// </summary>
    public virtual void SelectMultiple(params string[] items)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SelectMultiple", $"Element '{AutomationId}' not visible for selection.");
        }

        var listBox = element!.AsListBox();
        if (listBox == null)
        {
            ThrowCheckFailed("SelectMultiple", $"Element '{AutomationId}' is not a ListBox.");
            return; // Unreachable but makes compiler happy
        }

        foreach (var itemText in items)
        {
            var selectedItem = listBox.Items.FirstOrDefault(i => i?.ToString() == itemText);
            if (selectedItem != null)
            {
                selectedItem.Select();
                System.Windows.Forms.Application.DoEvents();
                System.Threading.Thread.Sleep(50);
            }
            else
            {
                ThrowCheckFailed("SelectMultiple", 
                    $"Item '{itemText}' not found in element '{AutomationId}'.");
            }
        }

        LogAction("SelectMultiple", string.Join(", ", items));
    }

    /// <summary>
    /// Get all selected items (for multi-select ListBox).
    /// </summary>
    public virtual IReadOnlyList<string> GetSelectedItems()
    {
        var element = FindElement();
        if (element == null) return new List<string>();

        var listBox = element.AsListBox();
        if (listBox != null)
        {
            var selectedItems = listBox.SelectedItems.Select(item => item?.ToString() ?? string.Empty).ToList();
            return selectedItems.AsReadOnly();
        }

        // For single-select controls, return current selection as a list
        var selected = GetSelectedItem();
        return string.IsNullOrEmpty(selected) ? new List<string>() : new List<string> { selected };
    }
}
