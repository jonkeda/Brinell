using Bruinell.Core.Interfaces;
using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Html.Controls.Buttons;

/// <summary>
/// HTML anchor/link control. Wraps &lt;a&gt; elements.
/// </summary>
public partial class LinkControl<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public LinkControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public LinkControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    /// <summary>The link's <c>href</c>.</summary>
    /// <param name="element">The pre-found element.</param>
    [GenerateComparisons(Comparison.Equals | Comparison.Contains)]
    protected virtual string? GetHrefCore(IHtmlElement element)
    {
        return element.GetAttribute("href");
    }
}
