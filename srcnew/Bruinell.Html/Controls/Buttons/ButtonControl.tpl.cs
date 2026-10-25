using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Html.Controls.Buttons;

/// <summary>
/// HTML button control. Wraps &lt;button&gt;, &lt;input type="button"&gt;, &lt;input type="submit"&gt;.
/// </summary>
public partial class ButtonControl<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ButtonControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public ButtonControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    /// <summary>Submits the containing form.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SubmitCore(IHtmlElement element, int? timeoutMs = null)
    {
        element.Submit();
    }
}
