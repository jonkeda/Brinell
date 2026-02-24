using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class TabContainerControl<TParent, TScope> : Html.Controls.Container.TabContainerControl<TParent, TScope>
    where TParent : IHtmlScope<TParent>
    where TScope : IHtmlContainer<TParent, TScope>
{
    public TabContainerControl(IHtmlScope<TParent> parentScope, Locator locator, string tabSelector = "[role='tab']")
        : base(parentScope, locator, tabSelector) { }
}
