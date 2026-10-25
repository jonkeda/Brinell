using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Blazor.Controls;

public class LinkControl<TScope> : Html.Controls.Buttons.LinkControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public LinkControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public LinkControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
