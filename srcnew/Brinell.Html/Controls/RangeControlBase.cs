using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls;

public abstract class RangeControlBase<TScope> : FocusableControlBase<TScope>, IHtmlAsyncRange<TScope>
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

    #region IHtmlAsyncRange<TScope> explicit implementation

    Task<string?> IHtmlAsyncRange<TScope>.GetMin()
        => Task.FromResult(GetMin());

    Task<string?> IHtmlAsyncRange<TScope>.GetMax()
        => Task.FromResult(GetMax());

    Task<string?> IHtmlAsyncRange<TScope>.GetStep()
        => Task.FromResult(GetStep());

    Task<string> IHtmlAsyncRange<TScope>.GetValue()
        => Task.FromResult(GetValue());

    Task<TScope> IHtmlAsyncRange<TScope>.SetValue(string value)
        => Task.FromResult(SetValue(value));

    #endregion
}
