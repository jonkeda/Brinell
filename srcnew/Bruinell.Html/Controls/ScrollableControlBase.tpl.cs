using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Html.Controls;

/// <summary>
/// Base class for Html controls that scroll their own content: ScrollTo, ScrollToTop.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class ScrollableControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected ScrollableControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected ScrollableControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    #region Core Methods

    /// <summary>
    /// Scrolls the control into view, then swipes its content in the direction of
    /// <paramref name="x"/> and <paramref name="y"/>.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="x">The horizontal direction: negative, zero or positive.</param>
    /// <param name="y">The vertical direction: negative, zero or positive.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ScrollToCore(IHtmlElement element, int x, int y, int? timeoutMs = null)
    {
        element.ScrollIntoView(CallRemainingMs);

        if (x == 0 && y == 0)
        {
            return;
        }

        var rect = element.Rect;
        var centerX = rect.Left + (rect.Width / 2);
        var centerY = rect.Top + (rect.Height / 2);

        var deltaX = Math.Sign(x) * Math.Max(20, rect.Width / 4);
        var deltaY = Math.Sign(y) * Math.Max(20, rect.Height / 4);

        element.Swipe(centerX, centerY, centerX - deltaX, centerY - deltaY);
    }

    /// <summary>Scrolls the control into view.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ScrollToTopCore(IHtmlElement element, int? timeoutMs = null)
    {
        element.ScrollIntoView(CallRemainingMs);
    }

    #endregion
}
