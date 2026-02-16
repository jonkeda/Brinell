using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.DateTime;

public class TimeInputControl<TScope> : RangeControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TimeInputControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public TimeInputControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public TScope SetTime(TimeOnly time)
    {
        return SetValue(time.ToString("HH:mm"));
    }

    public TimeOnly? GetTime()
    {
        var value = GetValue();
        return TimeOnly.TryParse(value, out var time) ? time : null;
    }
}
