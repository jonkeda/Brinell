using Bruinell.Core.Locators;
using Bruinell.NativeAndroid.Interfaces;

namespace Bruinell.NativeAndroid.Controls;

/// <summary>
/// Base class for native Android views that scroll their own content (a scroll view, a nested
/// scroll view): Scroll, ScrollToStart, ScrollToEnd. Named and element-relative; no coordinates.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class ScrollableControlBase<TScope> : ViewBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected ScrollableControlBase(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected ScrollableControlBase(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }

    #region Core Methods

    /// <summary>Scrolls the content once, the finger moving in <paramref name="direction"/>.</summary>
    /// <param name="element">The pre-found view.</param>
    /// <param name="direction">The way the finger moves: <c>Up</c> shows what is below.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ScrollCore(INativeAndroidElement element, GestureDirection direction, int? timeoutMs = null)
    {
        element.Scroll(direction);
    }

    /// <summary>Scrolls to the top, until the content does not move, within the call's budget.</summary>
    /// <param name="element">The pre-found view.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ScrollToStartCore(INativeAndroidElement element, int? timeoutMs = null)
    {
        ScrollToEdge(element, GestureDirection.Down);
    }

    /// <summary>Scrolls to the bottom, until the content does not move, within the call's budget.</summary>
    /// <param name="element">The pre-found view.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ScrollToEndCore(INativeAndroidElement element, int? timeoutMs = null)
    {
        ScrollToEdge(element, GestureDirection.Up);
    }

    // Each scroll is a gesture that moved the content; it stops at the edge or when the call's
    // budget is spent, never by a pause.
    private void ScrollToEdge(INativeAndroidElement element, GestureDirection direction)
    {
        var deadline = Environment.TickCount64 + CallRemainingMs;
        while (element.Scroll(direction, 1.0) && Environment.TickCount64 < deadline)
        {
        }
    }

    #endregion
}
