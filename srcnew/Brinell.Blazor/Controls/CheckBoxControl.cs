using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class CheckBoxControl<TScope> : Html.Controls.Toggle.CheckBoxControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public CheckBoxControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public CheckBoxControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
