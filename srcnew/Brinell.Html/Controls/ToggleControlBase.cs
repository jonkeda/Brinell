using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls;

public abstract class ToggleControlBase<TScope> : ClickableControlBase<TScope>, IHtmlAsyncToggle<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected ToggleControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected ToggleControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public bool IsChecked()
    {
        return RunWithElement(element => element.IsChecked);
    }

    public TScope SetChecked(bool value)
    {
        return RunWithElement(element =>
        {
            if (value)
            {
                element.Check();
                return;
            }

            element.Uncheck();
        });
    }

    public bool WaitChecked(bool expected, int? timeoutMs = null)
    {
        return Poll(() => IsChecked() == expected, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertChecked(bool expected)
    {
        if (!WaitChecked(expected))
        {
            var actual = IsChecked();
            throw new AssertionException(
                $"Checked state mismatch. Expected: {expected}, Actual: {actual}");
        }

        return ContainingScope;
    }

    #region IHtmlAsyncToggle<TScope> explicit implementation

    async Task<bool> IHtmlAsyncToggle<TScope>.IsChecked()
        => await RunWithElementAsync<bool>(async e =>
            await e.GetIsChecked().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncToggle<TScope>.SetChecked(bool value)
    {
        if (value)
            return await RunWithElementAsync(async e => await e.Check().ConfigureAwait(false)).ConfigureAwait(false);
        else
            return await RunWithElementAsync(async e => await e.Uncheck().ConfigureAwait(false)).ConfigureAwait(false);
    }

    async Task<bool> IHtmlAsyncToggle<TScope>.WaitChecked(bool expected, int? timeoutMs)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return await PollAsync(async () =>
        {
            var element = TryFindAsyncElement();
            if (element == null) return false;
            return await element.GetIsChecked().ConfigureAwait(false) == expected;
        }, timeout).ConfigureAwait(false);
    }

    async Task<TScope> IHtmlAsyncToggle<TScope>.AssertChecked(bool expected)
    {
        var self = (IHtmlAsyncToggle<TScope>)this;
        if (!await self.WaitChecked(expected).ConfigureAwait(false))
        {
            var actual = await self.IsChecked().ConfigureAwait(false);
            throw new AssertionException(
                $"Checked state mismatch. Expected: {expected}, Actual: {actual}");
        }
        return ContainingScope;
    }

    async Task<TScope> IHtmlAsyncToggle<TScope>.Check()
        => await RunWithElementAsync(async e => await e.Check().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncToggle<TScope>.Uncheck()
        => await RunWithElementAsync(async e => await e.Uncheck().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncToggle<TScope>.Toggle()
    {
        var self = (IHtmlAsyncToggle<TScope>)this;
        var current = await self.IsChecked().ConfigureAwait(false);
        return await self.SetChecked(!current).ConfigureAwait(false);
    }

    #endregion
}