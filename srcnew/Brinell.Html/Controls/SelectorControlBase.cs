using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls;

public abstract class SelectorControlBase<TScope> : FocusableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected SelectorControlBase(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected SelectorControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public abstract TScope SelectByValue(string value);

    public abstract TScope SelectByText(string text);

    public abstract string? GetSelectedValue();
}
