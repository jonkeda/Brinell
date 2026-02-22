using Brinell.Stride.Communication;
using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// List box control for Stride UI.
/// </summary>
public class ListBox<TScope> : SelectorControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public ListBox(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    /// <summary>
    /// Double-click on an item by index.
    /// </summary>
    public TScope DoubleClickItem(int index)
    {
        var items = GetItems();
        if (index < 0 || index >= items.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is outside range [0, {items.Count - 1}]");

        // Server-side: select the item then click twice
        SelectByIndex(index);
        var cmd = AutomationCommand.Action("Click", AutomationId);
        Context.SendCommand(cmd);
        Context.SendCommand(cmd);

        return ContainingScope;
    }

    /// <summary>
    /// Scroll to an item by index.
    /// </summary>
    public TScope ScrollToItem(int index)
    {
        Context.SendCommand(AutomationCommand.Action("ScrollToIndex", AutomationId, index));
        return ContainingScope;
    }
}
