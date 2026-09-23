using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class RangeInputControl<TScope> : Html.Controls.Range.RangeInputControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public RangeInputControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public RangeInputControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
