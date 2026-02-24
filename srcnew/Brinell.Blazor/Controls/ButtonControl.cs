using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class ButtonControl<TScope> : Html.Controls.Buttons.ButtonControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ButtonControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ButtonControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
