using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class DateInputControl<TScope> : Html.Controls.DateTime.DateInputControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public DateInputControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public DateInputControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
