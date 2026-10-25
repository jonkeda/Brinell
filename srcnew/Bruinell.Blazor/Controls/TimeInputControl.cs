using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Blazor.Controls;

public class TimeInputControl<TScope> : Html.Controls.DateTime.TimeInputControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TimeInputControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TimeInputControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
