using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Blazor.Controls;

public class DateInputControl<TScope> : Html.Controls.DateTime.DateInputControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public DateInputControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public DateInputControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
