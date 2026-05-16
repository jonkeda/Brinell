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

    Task<TScope> IHtmlAsyncFocusable<TScope>.Focus()
        => Task.FromResult(Focus());

    Task<TScope> IHtmlAsyncFocusable<TScope>.Blur()
        => Task.FromResult(Blur());

    Task<bool> IHtmlAsyncFocusable<TScope>.HasFocus()
        => Task.FromResult(HasFocus());

    #endregion
}