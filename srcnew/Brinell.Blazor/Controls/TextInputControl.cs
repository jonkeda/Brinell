using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class TextInputControl<TScope> : Html.Controls.Text.TextInputControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TextInputControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TextInputControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
