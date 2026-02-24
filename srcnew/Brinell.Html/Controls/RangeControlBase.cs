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

    async Task<string?> IHtmlAsyncRange<TScope>.GetMin()
        => await RunWithElementAsync<string?>(async e =>
            await e.GetAttribute("min").ConfigureAwait(false)).ConfigureAwait(false);

    async Task<string?> IHtmlAsyncRange<TScope>.GetMax()
        => await RunWithElementAsync<string?>(async e =>
            await e.GetAttribute("max").ConfigureAwait(false)).ConfigureAwait(false);

    async Task<string?> IHtmlAsyncRange<TScope>.GetStep()
        => await RunWithElementAsync<string?>(async e =>
            await e.GetAttribute("step").ConfigureAwait(false)).ConfigureAwait(false);

    async Task<string> IHtmlAsyncRange<TScope>.GetValue()
        => await RunWithElementAsync<string>(async e =>
            await e.GetInputValue().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncRange<TScope>.SetValue(string value)
        => await RunWithElementAsync(async e =>
            await e.Fill(value).ConfigureAwait(false)).ConfigureAwait(false);

    #endregion
}
