using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Blazor.Controls;

public class RangeInputControl<TScope> : Html.Controls.Range.RangeInputControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public RangeInputControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public RangeInputControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
