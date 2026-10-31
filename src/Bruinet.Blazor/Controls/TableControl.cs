using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Blazor.Controls;

public class TableControl<TScope> : Html.Controls.Collection.TableControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TableControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TableControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
