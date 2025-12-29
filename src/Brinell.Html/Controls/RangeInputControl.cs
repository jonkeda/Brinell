using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML range input (slider) control wrapper.
/// Works with &lt;input type="range"&gt; elements.
/// </summary>
public class RangeInputControl : RangeControlBase
{
    public RangeInputControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public RangeInputControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get current value as string.
    /// </summary>
    public override string GetText()
    {
        return GetValue().ToString();
    }
}
