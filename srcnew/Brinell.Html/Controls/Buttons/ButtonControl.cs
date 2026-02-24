using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls.Buttons;

/// <summary>
/// HTML button control. Wraps &lt;button&gt;, &lt;input type="button"&gt;, &lt;input type="submit"&gt;.
/// </summary>
public class ButtonControl<TScope> : ClickableControlBase<TScope>
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

    /// <summary>
    /// Submit the containing form.
    /// </summary>
    public TScope Submit()
    {
        return RunWithElement(element => element.Submit());
    }

    public async Task<TScope> SubmitAsync()
        => await RunWithElementAsync(async e => await e.Submit().ConfigureAwait(false)).ConfigureAwait(false);
}
