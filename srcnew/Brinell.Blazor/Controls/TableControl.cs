using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class TableControl<TScope> : Html.Controls.Collection.TableControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TableControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TableControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
