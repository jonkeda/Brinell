using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Text;

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

    public TScope AppendText(string text)
    {
        return RunWithElement(element =>
        {
            element.Focus();
            element.SendKeys(text);
        });
    }
}
