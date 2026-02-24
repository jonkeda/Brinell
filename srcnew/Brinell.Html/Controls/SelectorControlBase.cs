using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls;

public abstract class SelectorControlBase<TScope> : FocusableControlBase<TScope>, IHtmlAsyncSelector<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected SelectorControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected SelectorControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public abstract TScope SelectByValue(string value);

    public abstract TScope SelectByText(string text);

    public abstract string? GetSelectedValue();

    #region IHtmlAsyncSelector<TScope> explicit implementation

    async Task<TScope> IHtmlAsyncSelector<TScope>.SelectByValue(string value)
        => await RunWithElementAsync(async e =>
            await e.SelectOption(value).ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncSelector<TScope>.SelectByText(string text)
        => await RunWithElementAsync(async e =>
            await e.SelectOptionByLabel(text).ConfigureAwait(false)).ConfigureAwait(false);

    async Task<string?> IHtmlAsyncSelector<TScope>.GetSelectedValue()
        => await RunWithElementAsync<string?>(async e =>
            await e.Evaluate<string>("e => e.value").ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncSelector<TScope>.SelectMultiple(params string[] values)
        => await RunWithElementAsync(async e =>
            await e.SelectOption(values).ConfigureAwait(false)).ConfigureAwait(false);

    #endregion
}
