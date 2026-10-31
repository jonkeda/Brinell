using Bruinet.Core.Calls;
using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Html.Controls.Container;

public class ScrollContainerControl<TParent, TScope> : ContainerBase<TParent, TScope>
    where TParent : IHtmlScope<TParent>
    where TScope : IHtmlContainer<TParent, TScope>
{
    public ScrollContainerControl(IHtmlScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    public override TScope Self => (TScope)(object)this;

    public TScope ScrollToTop()
    {
        ContainerRoot.ScrollIntoView(CallBudget.RemainingOr(DefaultTimeoutMs));
        return Self;
    }

    public TScope ScrollBy(int deltaX, int deltaY)
    {
        var element = ContainerRoot;
        var rect = element.Rect;
        var centerX = rect.Left + (rect.Width / 2);
        var centerY = rect.Top + (rect.Height / 2);

        element.Swipe(centerX, centerY, centerX - deltaX, centerY - deltaY);

        return Self;
    }
}
