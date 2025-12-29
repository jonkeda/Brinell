using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML textarea control wrapper.
/// Works with &lt;textarea&gt; elements.
/// </summary>
public class TextAreaControl : TextControlBase
{
    public TextAreaControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TextAreaControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the rows attribute if set.
    /// </summary>
    public int? GetRows()
    {
        var rowsAttr = GetAttribute("rows");
        if (int.TryParse(rowsAttr, out var result))
            return result;
        return null;
    }

    /// <summary>
    /// Get the cols attribute if set.
    /// </summary>
    public int? GetCols()
    {
        var colsAttr = GetAttribute("cols");
        if (int.TryParse(colsAttr, out var result))
            return result;
        return null;
    }
}
