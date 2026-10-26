using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Blazor.Controls;

public class TextAreaControl<TScope> : Html.Controls.Text.TextAreaControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TextAreaControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TextAreaControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
