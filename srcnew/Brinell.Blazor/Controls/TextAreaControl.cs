using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class TextAreaControl<TScope> : Html.Controls.Text.TextAreaControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TextAreaControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TextAreaControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
