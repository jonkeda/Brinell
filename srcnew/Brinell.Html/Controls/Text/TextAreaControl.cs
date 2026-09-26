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

    public TScope AppendText(string text, int? timeoutMs = null)
    {
        return RunWithElement(element => ActAndConfirm(
            "AppendText",
            () =>
            {
                element.Focus();
                element.SendKeys(text);
            },
            () => element.InputValue, value => value?.EndsWith(text, StringComparison.Ordinal) == true,
            $"a value ending in '{text}'", timeoutMs), timeoutMs);
    }
}
