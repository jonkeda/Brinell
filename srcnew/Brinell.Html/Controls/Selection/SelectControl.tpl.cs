using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Selection;

/// <summary>
/// HTML select control. Wraps &lt;select&gt;; its selection members come from
/// <see cref="SelectorControlBase{TScope}"/>.
/// </summary>
public partial class SelectControl<TScope> : SelectorControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public SelectControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public SelectControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    /// <summary>Selects several options by value, in a &lt;select multiple&gt;.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="values">The options' values.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SelectMultipleCore(IHtmlElement element, string[] values, int? timeoutMs = null)
    {
        element.SelectOption(values);
    }
}
