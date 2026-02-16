using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls;

public abstract class ClickableControlBase<TScope> : Control<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected ClickableControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected ClickableControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public TScope DoubleClick()
    {
        return RunWithElement(element => element.DoubleClick());
    }

    public TScope RightClick()
    {
        return RunWithElement(element => element.RightClick());
    }

    public TScope Hover()
    {
        return RunWithElement(element => element.Hover());
    }
}