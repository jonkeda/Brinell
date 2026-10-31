using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Blazor.Controls;

public class TextInputControl<TScope> : Html.Controls.Text.TextInputControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TextInputControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TextInputControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
