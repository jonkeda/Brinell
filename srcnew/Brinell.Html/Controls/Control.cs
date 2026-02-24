using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls;

public abstract class Control<TScope> : ControlBase<TScope>, IHtmlAsyncClickable<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected Control(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected Control(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public TScope Click()
    {
        return RunWithElement(element => element.Click());
    }

    public TScope SendKeys(string text)
    {
        return RunWithElement(element => element.SendKeys(text));
    }

    public TScope Clear()
    {
        return RunWithElement(element => element.Clear());
    }

    public TScope ScrollIntoView(int timeoutMs = 5000)
    {
        return RunWithElement(element => element.ScrollIntoView(timeoutMs));
    }

    #region IHtmlAsyncClickable<TScope> explicit implementation

    async Task<TScope> IHtmlAsyncClickable<TScope>.Click()
        => await RunWithElementAsync(async e => await e.Click().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncClickable<TScope>.SendKeys(string text)
        => await RunWithElementAsync(async e => await e.SendKeys(text).ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncClickable<TScope>.Clear()
        => await RunWithElementAsync(async e => await e.Clear().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncClickable<TScope>.ScrollIntoView(int timeoutMs)
        => await RunWithElementAsync(async e => await e.ScrollIntoView(timeoutMs).ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncClickable<TScope>.DoubleClick()
        => await RunWithElementAsync(async e => await e.DoubleClick().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncClickable<TScope>.RightClick()
        => await RunWithElementAsync(async e => await e.RightClick().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncClickable<TScope>.Hover()
        => await RunWithElementAsync(async e => await e.Hover().ConfigureAwait(false)).ConfigureAwait(false);

    #endregion
}