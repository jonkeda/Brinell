using Brinell.Core.Abstractions;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI list box controls.
/// </summary>
public class StrideListBoxControl : StrideSelectorControlBase
{
    /// <summary>
    /// Create a new list box control.
    /// </summary>
    public StrideListBoxControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Double-click on an item by index.
    /// </summary>
    public void DoubleClickItem(int index)
    {
        var items = GetItems();
        if (index < 0 || index >= items.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"Index {index} is outside range [0, {items.Count - 1}]");
        }

        // Get item bounds and double-click
        var itemAutomationId = $"{_automationId}_Item_{index}";
        var itemState = Context.GetElementState(itemAutomationId);

        if (itemState.Exists)
        {
            Context.Input.DoubleClick(itemState.Bounds.CenterX, itemState.Bounds.CenterY);
        }
        else
        {
            // Fallback: select then double-click on control center
            SelectByIndex(index);
            var bounds = GetBounds();
            Context.Input.DoubleClick(bounds.CenterX, bounds.CenterY);
        }

        LogAction("DoubleClickItem", index.ToString());
    }

    /// <summary>
    /// Scroll to an item by index.
    /// </summary>
    public void ScrollToItem(int index)
    {
        Context.SendCommand(Communication.AutomationCommand.Action("ScrollToIndex", _automationId, index));
        LogAction("ScrollToItem", index.ToString());
    }
}
