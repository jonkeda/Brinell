using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Blazor.Controls;

public class CheckBoxControl<TScope> : Html.Controls.Toggle.CheckBoxControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public CheckBoxControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public CheckBoxControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
