using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Blazor.Controls;

public class TextAreaControl<TScope> : Html.Controls.Text.TextAreaControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TextAreaControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TextAreaControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
