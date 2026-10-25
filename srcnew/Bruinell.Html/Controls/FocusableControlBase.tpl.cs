using Bruinell.Core.Interfaces;
using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Html.Controls;

/// <summary>
/// Base class for Html controls that take focus: Focus, Blur, IsFocused.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class FocusableControlBase<TScope> : ClickableControlBase<TScope>, IFocusableControlObject<TScope>
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

    #region Core Methods

    /// <summary>Focuses the control, and confirms it has focus.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void FocusCore(IHtmlElement element, int? timeoutMs = null)
    {
        ActAndConfirm(
            "Focus", element.Focus,
            () => IsFocusedCore(element), focused => focused == true,
            "focus", timeoutMs);
    }

    /// <summary>Removes focus from the control, and confirms it lost it.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void BlurCore(IHtmlElement element, int? timeoutMs = null)
    {
        ActAndConfirm(
            "Blur", element.Blur,
            () => IsFocusedCore(element), focused => focused == false,
            "no focus", timeoutMs);
    }

    /// <summary>Whether the control has focus.</summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual bool? IsFocusedCore(IHtmlElement? element)
        => element?.Evaluate<bool>(HasFocusScript);

    #endregion
}
