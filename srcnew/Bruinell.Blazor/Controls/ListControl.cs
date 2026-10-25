using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Blazor.Controls;

public class ListControl<TScope> : Html.Controls.Collection.ListControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ListControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ListControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
