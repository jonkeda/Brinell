using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls;

public abstract class ScrollableControlBase<TScope> : ClickableControlBase<TScope>
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

    public TScope ScrollTo(int x, int y)
    {
        return RunWithElement(element =>
        {
            element.ScrollIntoView();

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
        });
    }

    public TScope ScrollToTop()
    {
        return RunWithElement(element => element.ScrollIntoView());
    }
}
