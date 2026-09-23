using Brinell.Stride.Communication;
using Brinell.Stride.Infrastructure;
using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Combo box (drop-down) control for Stride UI.
/// </summary>
public class ComboBox<TScope> : SelectorControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public ComboBox(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    /// <summary>
    /// Check if dropdown is currently open.
    /// </summary>
    public bool IsOpen() => GetState().IsOpen ?? false;

    /// <summary>
    /// Open the dropdown.
    /// </summary>
    public TScope Open()
    {
        if (!IsOpen())
        {
            Context.ClickElement(AutomationId);
        }
        return ContainingScope;
    }

    /// <summary>
    /// Close the dropdown.
    /// </summary>
    public TScope Close()
    {
        if (IsOpen())
        {
            Context.PressKey(VirtualKey.Escape);
        }
        return ContainingScope;
    }

    public override TScope SelectByIndex(int index)
    {
        Open();
        return base.SelectByIndex(index);
    }

    public override TScope SelectByText(string text)
    {
        Open();
        return base.SelectByText(text);
    }
}
