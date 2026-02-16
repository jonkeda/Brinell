using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Toggle;

public class CheckBoxControl<TScope> : ToggleControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public CheckBoxControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public CheckBoxControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public TScope Check()
    {
        return SetChecked(true);
    }

    public TScope Uncheck()
    {
        return SetChecked(false);
    }

    public TScope Toggle()
    {
        return SetChecked(!IsChecked());
    }
}
