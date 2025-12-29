using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.Wpf.Controls.Base;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF ListBox/ListView control wrapper.
/// Uses WPF-specific ItemsControlBase for FlaUI integration.
/// </summary>
public class ListBoxControl : ItemsControlBase, IItemsControl
{
    public ListBoxControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ListBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get item elements from the ListBox.
    /// </summary>
    protected override AutomationElement[] GetItemElements()
    {
        var element = FindElement();
        if (element != null)
        {
            var listBox = element.AsListBox();
            return listBox?.Items.Cast<AutomationElement>().ToArray() ?? Array.Empty<AutomationElement>();
        }
        return Array.Empty<AutomationElement>();
    }

    /// <summary>
    /// Get selected item text (immediate, no wait).
    /// </summary>
    public string? GetSelectedText()
    {
        var element = FindElement();
        if (element != null)
        {
            var listBox = element.AsListBox();
            return listBox?.SelectedItem?.Text;
        }
        return null;
    }

    /// <summary>
    /// Get selected item index (immediate, no wait). Returns -1 if none selected.
    /// </summary>
    public int GetSelectedIndex()
    {
        var element = FindElement();
        if (element != null)
        {
            var listBox = element.AsListBox();
            if (listBox?.SelectedItem != null)
            {
                for (int i = 0; i < listBox.Items.Length; i++)
                {
                    if (listBox.Items[i].Text == listBox.SelectedItem.Text)
                        return i;
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Select item by index.
    /// </summary>
    public void SelectByIndex(int index)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var listBox = element.AsListBox();
            if (listBox != null && index < listBox.Items.Length)
            {
                listBox.Items[index].Select();
            }
        }
        LogAction("SelectByIndex", index.ToString());
    }

    /// <summary>
    /// Select item by text.
    /// </summary>
    public void SelectByText(string text)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var listBox = element.AsListBox();
            var item = listBox?.Items.FirstOrDefault(i => i.Text == text);
            item?.Select();
        }
        LogAction("SelectByText", text);
    }

    /// <summary>
    /// Get all items as IReadOnlyList.
    /// </summary>
    public IReadOnlyList<string> GetItems()
    {
        return GetItemsArray();
    }

    /// <summary>
    /// Get all items as string array.
    /// </summary>
    public string[] GetItemsArray()
    {
        var element = FindElement();
        if (element != null)
        {
            var listBox = element.AsListBox();
            if (listBox != null)
            {
                return listBox.Items.Select(i => i.Text ?? "").ToArray();
            }
        }
        return [];
    }

    /// <summary>
    /// Get selected item text (from base class).
    /// </summary>
    public override string GetText()
    {
        return GetSelectedText() ?? string.Empty;
    }

    /// <summary>
    /// Select item by index (alias for SelectByIndex).
    /// </summary>
    public void SelectIndex(int index) => SelectByIndex(index);
    
    /// <summary>
    /// Select item by index (alias for SelectByIndex).
    /// </summary>
    public void SelectItemByIndex(int index) => SelectByIndex(index);

    /// <summary>
    /// Wait for item count.
    /// </summary>
    public bool WaitForItemCount(int expectedCount, int? timeoutMs = null)
    {
        return WaitItemCount(expectedCount, timeoutMs);
    }
}
