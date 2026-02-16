using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.DateTime;

public class DateInputControl<TScope> : RangeControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public DateInputControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public DateInputControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public TScope SetDate(DateOnly date)
    {
        return SetValue(date.ToString("yyyy-MM-dd"));
    }

    public DateOnly? GetDate()
    {
        var value = GetValue();
        return DateOnly.TryParse(value, out var date) ? date : null;
    }
}
