using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Toggle;

public class RadioButtonControl<TScope> : ToggleControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public RadioButtonControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public RadioButtonControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public TScope Select()
    {
        return SetChecked(true);
    }
}
