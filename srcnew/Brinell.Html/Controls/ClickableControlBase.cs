using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls;

public abstract class ClickableControlBase<TScope> : Control<TScope>, IHtmlAsyncClickable<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected ClickableControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected ClickableControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    // A disabled control is not acted on; the call asks again within its budget (R0).
    protected override void EnsureReadyForActionCore(IHtmlElement element) => EnsureEnabledCore(element);

    public new TScope Click(int? timeoutMs = null)
    {
        return RunWithElement(element => element.Click(), timeoutMs);
    }

    public TScope DoubleClick(int? timeoutMs = null)
    {
        return RunDoWithElement(element => element.DoubleClick(), timeoutMs);
    }

    public TScope RightClick(int? timeoutMs = null)
    {
        return RunWithElement(element => element.RightClick(), timeoutMs);
    }

    public TScope Hover(int? timeoutMs = null)
    {
        return RunWithElement(element => element.Hover(), timeoutMs);
    }

    #region IHtmlAsyncClickable<TScope> explicit implementation

    Task<TScope> IHtmlAsyncClickable<TScope>.Click()
        => Task.FromResult(Click());

    Task<TScope> IHtmlAsyncClickable<TScope>.SendKeys(string text)
        => Task.FromResult(SendKeys(text));

    Task<TScope> IHtmlAsyncClickable<TScope>.Clear()
        => Task.FromResult(Clear());

    Task<TScope> IHtmlAsyncClickable<TScope>.ScrollIntoView(int timeoutMs)
        => Task.FromResult(ScrollIntoView(timeoutMs));

    Task<TScope> IHtmlAsyncClickable<TScope>.DoubleClick()
        => Task.FromResult(DoubleClick());

    Task<TScope> IHtmlAsyncClickable<TScope>.RightClick()
        => Task.FromResult(RightClick());

    Task<TScope> IHtmlAsyncClickable<TScope>.Hover()
        => Task.FromResult(Hover());

    #endregion
}
