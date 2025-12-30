using Brinell.Core.Abstractions;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI combo box (drop-down) controls.
/// </summary>
public class StrideComboBoxControl : StrideSelectorControlBase
{
    /// <summary>
    /// Create a new combo box control.
    /// </summary>
    public StrideComboBoxControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Check if dropdown is currently open.
    /// </summary>
    public bool IsOpen() => GetState().IsOpen ?? false;

    /// <summary>
    /// Open the dropdown.
    /// </summary>
    public void Open()
    {
        if (!IsOpen())
        {
            var bounds = GetBounds();
            Context.Input.Click(bounds.CenterX, bounds.CenterY);
            LogAction("Open");
        }
    }

    /// <summary>
    /// Close the dropdown.
    /// </summary>
    public void Close()
    {
        if (IsOpen())
        {
            Context.PressKey(VirtualKey.Escape);
            LogAction("Close");
        }
    }

    /// <inheritdoc />
    public new void SelectByIndex(int index)
    {
        Open();
        base.SelectByIndex(index);
    }

    /// <inheritdoc />
    public new void SelectByText(string text)
    {
        Open();
        base.SelectByText(text);
    }
}
