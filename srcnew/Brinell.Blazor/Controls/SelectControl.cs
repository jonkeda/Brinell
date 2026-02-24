using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class SelectControl<TScope> : Html.Controls.Selection.SelectControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public SelectControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public SelectControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
