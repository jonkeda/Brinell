using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Html.Controls.Toggle;

/// <summary>
/// HTML radio button control. Wraps one &lt;input type="radio"&gt;; for a group read as one
/// selector, use <c>RadioGroupControl</c>.
/// </summary>
public class RadioButtonControl<TScope> : ToggleControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public RadioButtonControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public RadioButtonControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    /// <summary>Selects the radio button. An alias for <c>Check</c>.</summary>
    public TScope Select(int? timeoutMs = null) => Check(timeoutMs);
}
