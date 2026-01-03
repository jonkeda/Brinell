using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms TrackBar control wrapper.
/// Uses shared RangeControlBase for FlaUI integration.
/// Equivalent to WPF Slider control.
/// </summary>
public class TrackBarControl : RangeControlBase, IRangeControl
{
    public TrackBarControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TrackBarControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }
}
