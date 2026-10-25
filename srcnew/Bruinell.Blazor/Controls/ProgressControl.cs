using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Blazor.Controls;

public class ProgressControl<TScope> : Html.Controls.Display.ProgressControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ProgressControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ProgressControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
