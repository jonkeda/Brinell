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

    private const string HasFocusScript = "el => document.activeElement === el";

    public TScope Focus(int? timeoutMs = null)
    {
        return RunWithElement(element => ActAndConfirm(
            "Focus", element.Focus,
            () => element.Evaluate<bool>(HasFocusScript), focused => focused,
            "focus", timeoutMs), timeoutMs);
    }

    public TScope Blur(int? timeoutMs = null)
    {
        return RunWithElement(element => ActAndConfirm(
            "Blur", element.Blur,
            () => element.Evaluate<bool>(HasFocusScript), focused => !focused,
            "no focus", timeoutMs), timeoutMs);
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