using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls;

public abstract class Control<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected Control(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    protected Control(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public TScope Click(int? timeoutMs = null)
    {
        return RunWithElement(element => element.Click(), timeoutMs);
    }

    public TScope SendKeys(string text)
    {
        return SendKeys(text, null);
    }

    public TScope Clear(int? timeoutMs = null)
    {
        return RunWithElement(element => element.Clear(), timeoutMs);
    }
}