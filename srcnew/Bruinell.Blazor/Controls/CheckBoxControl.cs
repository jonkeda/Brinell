using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Blazor.Controls;

public class CheckBoxControl<TScope> : Html.Controls.Toggle.CheckBoxControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public CheckBoxControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public CheckBoxControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
