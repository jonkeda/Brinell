using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Blazor.Controls;

public class ProgressControl<TScope> : Html.Controls.Display.ProgressControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ProgressControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ProgressControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
