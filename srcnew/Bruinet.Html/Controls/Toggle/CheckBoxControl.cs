using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Html.Controls.Toggle;

/// <summary>
/// HTML check box control. Wraps &lt;input type="checkbox"&gt;; its members come from
/// <see cref="ToggleControlBase{TScope}"/>.
/// </summary>
public class CheckBoxControl<TScope> : ToggleControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public CheckBoxControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public CheckBoxControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }
}
