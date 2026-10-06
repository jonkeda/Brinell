// <scraped control="div||v-input__append-inner|div" from="2026-10-02-recording/008-department.nodes.jsonl#147" />
using Brinell.Core.Locators;
using Brinell.Html.Controls;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Bouw7.Controls;

public partial class VInputAppendInner<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public VInputAppendInner(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public VInputAppendInner(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    protected override string? GetTextCore(IHtmlElement element)
    {
        return element.Text?.Trim();
    }
}
