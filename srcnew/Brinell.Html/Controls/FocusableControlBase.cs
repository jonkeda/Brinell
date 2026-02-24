using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls;

public abstract class FocusableControlBase<TScope> : ClickableControlBase<TScope>, IHtmlAsyncFocusable<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected FocusableControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected FocusableControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public TScope Focus()
    {
        return RunWithElement(element => element.Focus());
    }

    public TScope Blur()
    {
        return RunWithElement(element => element.Blur());
    }

    public bool HasFocus()
    {
        return RunWithElement(element =>
        {
            var focused = element.GetAttribute("focused") ?? element.GetAttribute("data-focused");
            return string.Equals(focused, "true", StringComparison.OrdinalIgnoreCase);
        });
    }

    #region IHtmlAsyncFocusable<TScope> explicit implementation

    async Task<TScope> IHtmlAsyncFocusable<TScope>.Focus()
        => await RunWithElementAsync(async e => await e.Focus().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncFocusable<TScope>.Blur()
        => await RunWithElementAsync(async e => await e.Blur().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<bool> IHtmlAsyncFocusable<TScope>.HasFocus()
        => await RunWithElementAsync<bool>(async e =>
            await e.Evaluate<bool>("e => document.activeElement === e").ConfigureAwait(false)).ConfigureAwait(false);

    #endregion
}