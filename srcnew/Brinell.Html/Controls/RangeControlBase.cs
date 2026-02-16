using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls;

public abstract class RangeControlBase<TScope> : FocusableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected RangeControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected RangeControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public string? GetMin()
    {
        return GetAttribute("min");
    }

    public string? GetMax()
    {
        return GetAttribute("max");
    }

    public string? GetStep()
    {
        return GetAttribute("step");
    }

    public string GetValue()
    {
        return RunWithElement(element => element.InputValue);
    }

    public TScope SetValue(string value)
    {
        return RunWithElement(element => element.Fill(value));
    }
}
