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

    public new TScope Click()
    {
        return RunWithElement(element =>
        {
            EnsureEnabledCore(element);
            element.Click();
        });
    }

    public TScope DoubleClick()
    {
        return RunDoWithElement(element => element.DoubleClick());
    }

    public TScope RightClick()
    {
        return RunWithElement(element => element.RightClick());
    }

    public TScope Hover()
    {
        return RunWithElement(element => element.Hover());
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
