using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML text input control wrapper.
/// Works with &lt;input type="text"&gt;, &lt;input type="email"&gt;, &lt;input type="password"&gt;, etc.
/// </summary>
public class TextInputControl : TextControlBase
{
    public TextInputControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TextInputControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the input type attribute (text, email, password, etc.).
    /// </summary>
    public string GetInputType()
    {
        return GetAttribute("type") ?? "text";
    }

    /// <summary>
    /// Get the maxlength attribute if set.
    /// </summary>
    public int? GetMaxLength()
    {
        var maxLengthAttr = GetAttribute("maxlength");
        if (int.TryParse(maxLengthAttr, out var result))
            return result;
        return null;
    }
}
