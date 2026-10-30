using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Html.Controls.Range;

/// <summary>
/// HTML range and number input control. Wraps &lt;input type="range|number"&gt;; its members come
/// from <see cref="RangeControlBase{TScope}"/>.
/// </summary>
public class RangeInputControl<TScope> : RangeControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public RangeInputControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public RangeInputControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }
}
