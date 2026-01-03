using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF Slider control wrapper.
/// Uses WPF-specific RangeControlBase for FlaUI integration.
/// </summary>
public class SliderControl : RangeControlBase, IRangeControl
{
    public SliderControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public SliderControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }
}
