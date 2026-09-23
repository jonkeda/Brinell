using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html;

public static class HtmlAsyncExtensions
{
    #region IHtmlAsyncControlObject<TScope>

    public static Task<bool> IsExistsAsync<TScope>(this IHtmlAsyncControlObject<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.IsExists();

    public static Task<bool?> IsVisibleAsync<TScope>(this IHtmlAsyncControlObject<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.IsVisible();

    public static Task<bool?> IsEnabledAsync<TScope>(this IHtmlAsyncControlObject<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.IsEnabled();

    public static Task<bool> WaitExistsAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitExists(expected, timeoutMs);

    public static Task<bool> WaitVisibleAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitVisible(expected, timeoutMs);

    public static Task<bool> WaitEnabledAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitEnabled(expected, timeoutMs);

    public static Task<TScope> AssertExistsAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertExists(expected, message, timeoutMs);

    public static Task<TScope> AssertVisibleAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertVisible(expected, message, timeoutMs);

    public static Task<TScope> AssertEnabledAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertEnabled(expected, message, timeoutMs);

    public static Task<string?> GetTextAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.GetText(timeoutMs);

    public static Task<bool> WaitTextAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        string? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitText(expected, timeoutMs);

    public static Task<TScope> AssertTextAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        string? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertText(expected, message, timeoutMs);

    public static Task<TScope> AssertTextContainsAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        string? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertTextContains(expected, message, timeoutMs);

    public static Task<string?> GetAttributeAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        string name)
        where TScope : IHtmlScope<TScope>
        => control.GetAttribute(name);

    #endregion

    #region IHtmlAsyncClickable<TScope>

    public static Task<TScope> ClickAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Click();

    public static Task<TScope> SendKeysAsync<TScope>(this IHtmlAsyncClickable<TScope> control, string text)
        where TScope : IHtmlScope<TScope>
        => control.SendKeys(text);

    public static Task<TScope> ClearAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Clear();

    public static Task<TScope> ScrollIntoViewAsync<TScope>(this IHtmlAsyncClickable<TScope> control,
        int timeoutMs = 5000)
        where TScope : IHtmlScope<TScope>
        => control.ScrollIntoView(timeoutMs);

    public static Task<TScope> DoubleClickAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.DoubleClick();

    public static Task<TScope> RightClickAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.RightClick();

    public static Task<TScope> HoverAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Hover();

    #endregion

    #region IHtmlAsyncFocusable<TScope>

    public static Task<TScope> FocusAsync<TScope>(this IHtmlAsyncFocusable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Focus();

    public static Task<TScope> BlurAsync<TScope>(this IHtmlAsyncFocusable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Blur();

    public static Task<bool> HasFocusAsync<TScope>(this IHtmlAsyncFocusable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.HasFocus();

    #endregion

    #region IHtmlAsyncToggle<TScope>

    public static Task<bool> IsCheckedAsync<TScope>(this IHtmlAsyncToggle<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.IsChecked();

    public static Task<TScope> SetCheckedAsync<TScope>(this IHtmlAsyncToggle<TScope> control, bool value)
        where TScope : IHtmlScope<TScope>
        => control.SetChecked(value);

    public static Task<bool> WaitCheckedAsync<TScope>(this IHtmlAsyncToggle<TScope> control,
        bool expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitChecked(expected, timeoutMs);

    public static Task<TScope> AssertCheckedAsync<TScope>(this IHtmlAsyncToggle<TScope> control,
        bool expected)
        where TScope : IHtmlScope<TScope>
        => control.AssertChecked(expected);

    public static Task<TScope> CheckAsync<TScope>(this IHtmlAsyncToggle<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Check();

    public static Task<TScope> UncheckAsync<TScope>(this IHtmlAsyncToggle<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Uncheck();

    public static Task<TScope> ToggleAsync<TScope>(this IHtmlAsyncToggle<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Toggle();

    #endregion

    #region IHtmlAsyncEditable<TScope>

    public static Task<TScope> SetTextAsync<TScope>(this IHtmlAsyncEditable<TScope> control, string text)
        where TScope : IHtmlScope<TScope>
        => control.SetText(text);

    public static Task<string?> GetValueAsync<TScope>(this IHtmlAsyncEditable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetValue();

    public static Task<TScope> TypeTextAsync<TScope>(this IHtmlAsyncEditable<TScope> control, string text)
        where TScope : IHtmlScope<TScope>
        => control.TypeText(text);

    public static Task<TScope> AssertValueAsync<TScope>(this IHtmlAsyncEditable<TScope> control,
        string? expected)
        where TScope : IHtmlScope<TScope>
        => control.AssertValue(expected);

    public static Task<TScope> WaitValueAsync<TScope>(this IHtmlAsyncEditable<TScope> control,
        string? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitValue(expected, timeoutMs);

    public static Task<TScope> AppendTextAsync<TScope>(this IHtmlAsyncEditable<TScope> control,
        string text)
        where TScope : IHtmlScope<TScope>
        => control.AppendText(text);

    #endregion

    #region IHtmlAsyncSelector<TScope>

    public static Task<TScope> SelectByValueAsync<TScope>(this IHtmlAsyncSelector<TScope> control,
        string value)
        where TScope : IHtmlScope<TScope>
        => control.SelectByValue(value);

    public static Task<TScope> SelectByTextAsync<TScope>(this IHtmlAsyncSelector<TScope> control,
        string text)
        where TScope : IHtmlScope<TScope>
        => control.SelectByText(text);

    public static Task<string?> GetSelectedValueAsync<TScope>(this IHtmlAsyncSelector<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetSelectedValue();

    public static Task<TScope> SelectMultipleAsync<TScope>(this IHtmlAsyncSelector<TScope> control,
        params string[] values)
        where TScope : IHtmlScope<TScope>
        => control.SelectMultiple(values);

    #endregion

    #region IHtmlAsyncRange<TScope>

    public static Task<string?> GetMinAsync<TScope>(this IHtmlAsyncRange<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetMin();

    public static Task<string?> GetMaxAsync<TScope>(this IHtmlAsyncRange<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetMax();

    public static Task<string?> GetStepAsync<TScope>(this IHtmlAsyncRange<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetStep();

    public static Task<string?> GetRangeValueAsync<TScope>(this IHtmlAsyncRange<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetValue();

    public static Task<TScope> SetRangeValueAsync<TScope>(this IHtmlAsyncRange<TScope> control,
        string value)
        where TScope : IHtmlScope<TScope>
        => control.SetValue(value);

    #endregion

    #region IHtmlAsyncScrollable<TScope>

    public static Task<TScope> ScrollToAsync<TScope>(this IHtmlAsyncScrollable<TScope> control,
        int x, int y)
        where TScope : IHtmlScope<TScope>
        => control.ScrollTo(x, y);

    public static Task<TScope> ScrollToTopAsync<TScope>(this IHtmlAsyncScrollable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.ScrollToTop();

    #endregion
}
