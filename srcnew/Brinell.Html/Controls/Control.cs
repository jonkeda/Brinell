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

    public TScope Click()
    {
        return RunWithElement(element => element.Click());
    }

    public TScope SendKeys(string text)
    {
        return RunWithElement(element => element.SendKeys(text));
    }

    public TScope Clear()
    {
        return RunWithElement(element => element.Clear());
    }

    public TScope ScrollIntoView(int timeoutMs = 5000)
    {
        return RunWithElement(element => element.ScrollIntoView(timeoutMs));
    }
}