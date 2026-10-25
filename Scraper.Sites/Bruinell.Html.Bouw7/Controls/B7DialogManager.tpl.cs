// <scraped control="b7-dialog-manager||hydrated|#shadow" from="2026-10-02-recording/008-department.nodes.jsonl#502" />
using Bruinell.Core.Locators;
using Bruinell.Html.Controls;
using Bruinell.Html.Interfaces;

namespace Bruinell.Html.Bouw7.Controls;

public partial class B7DialogManager<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public B7DialogManager(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public B7DialogManager(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    protected override string? GetTextCore(IHtmlElement element)
    {
        return element.Text?.Trim();
    }
}
