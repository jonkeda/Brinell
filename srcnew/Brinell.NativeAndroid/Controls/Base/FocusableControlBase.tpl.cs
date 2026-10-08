using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.NativeAndroid.Interfaces;

namespace Brinell.NativeAndroid.Controls;

/// <summary>
/// Base class for native Android controls that take focus: Focus, IsFocused, and Blur as hiding
/// the keyboard.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class FocusableControlBase<TScope> : ClickableControlBase<TScope>, IFocusableControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected FocusableControlBase(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected FocusableControlBase(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }

    #region Core Methods

    /// <summary>Taps the control to focus it, and confirms it has focus.</summary>
    /// <param name="element">The pre-found view.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void FocusCore(INativeAndroidElement element, int? timeoutMs = null)
    {
        if (element.Focused)
        {
            return;
        }

        ActAndConfirm(
            "Focus", element.Click,
            () => IsFocusedCore(element), focused => focused == true,
            "focus", timeoutMs);
    }

    /// <summary>
    /// Hides the soft keyboard. Android keeps focus on the view (there is no route to clear it),
    /// so this confirms the keyboard is gone, not that focus moved.
    /// </summary>
    /// <param name="element">The pre-found view.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void BlurCore(INativeAndroidElement element, int? timeoutMs = null)
    {
        ActAndConfirm(
            "Blur", Context.Driver.HideKeyboard,
            () => (bool?)Context.Driver.IsKeyboardShown, shown => shown == false,
            "the keyboard hidden", timeoutMs);
    }

    /// <summary>Whether the control has focus.</summary>
    /// <param name="element">The pre-found view.</param>
    protected virtual bool? IsFocusedCore(INativeAndroidElement? element)
        => element?.Focused;

    #endregion
}
