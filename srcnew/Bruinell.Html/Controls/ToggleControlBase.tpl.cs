using Bruinell.Core.Interfaces;
using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Html.Controls;

/// <summary>
/// Base class for Html controls with a checked state: Toggle, Check, Uncheck, SetChecked.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class ToggleControlBase<TScope> : ClickableControlBase<TScope>, IToggleControlObject<TScope>
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

    #region Core Methods

    /// <summary>Whether the control is checked.</summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual bool? IsCheckedCore(IHtmlElement? element) => element?.IsChecked;

    /// <summary>
    /// Sets the checked state, and confirms it took. Does nothing when it is already in that state.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="checked">The state to set; null skips.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SetCheckedCore(IHtmlElement element, bool? @checked, int? timeoutMs = null)
    {
        if (@checked is not { } value || element.IsChecked == value)
        {
            return;
        }

        ActAndConfirm(
            value ? "Check" : "Uncheck",
            () =>
            {
                if (value)
                {
                    element.Check();
                }
                else
                {
                    element.Uncheck();
                }
            },
            () => IsCheckedCore(element), isChecked => isChecked == value,
            value ? "checked" : "unchecked", timeoutMs);
    }

    /// <summary>Flips the checked state, and confirms it changed.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ToggleCore(IHtmlElement element, int? timeoutMs = null)
    {
        SetCheckedCore(element, !element.IsChecked, timeoutMs);
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>Checks the control. An alias for <c>SetChecked(true)</c>.</summary>
    public TScope Check(int? timeoutMs = null) => SetChecked(true, timeoutMs);

    /// <summary>Unchecks the control. An alias for <c>SetChecked(false)</c>.</summary>
    public TScope Uncheck(int? timeoutMs = null) => SetChecked(false, timeoutMs);

    #endregion
}
