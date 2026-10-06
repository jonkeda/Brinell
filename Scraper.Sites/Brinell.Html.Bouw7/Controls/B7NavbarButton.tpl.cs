// <scraped control="b7-navbar-button||hydrated.interactive.right-side|#shadow" from="2026-10-02-recording/008-department.nodes.jsonl#74" />
using Brinell.Core.Locators;
using Brinell.Html.Controls;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Bouw7.Controls;

public partial class B7NavbarButton<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    private static readonly Locator TextPart = Locator.ByCss("div");

    public B7NavbarButton(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public B7NavbarButton(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    protected override string? GetTextCore(IHtmlElement element)
    {
        return element.TryFindElement(TextPart)?.Text?.Trim() ?? element.Text?.Trim();
    }
}
