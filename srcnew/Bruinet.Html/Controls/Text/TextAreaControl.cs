using Bruinet.Core.Locators;
using Bruinet.Html.Interfaces;

namespace Bruinet.Html.Controls.Text;

/// <summary>
/// HTML text area control. Wraps &lt;textarea&gt;; its members come from
/// <see cref="TextInputControl{TScope}"/>.
/// </summary>
public class TextAreaControl<TScope> : TextInputControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TextAreaControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public TextAreaControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }
}
