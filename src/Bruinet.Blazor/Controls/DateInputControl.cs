using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Blazor.Controls;

public class DateInputControl<TScope> : Html.Controls.DateTime.DateInputControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public DateInputControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public DateInputControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
