using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Blazor.Controls;

public class RadioButtonControl<TScope> : Html.Controls.Toggle.RadioButtonControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public RadioButtonControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public RadioButtonControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
