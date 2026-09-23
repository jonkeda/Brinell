using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Selection;

public class SelectControl<TScope> : SelectorControlBase<TScope>
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

    public override TScope SelectByValue(string value)
    {
        return RunWithElement(element => element.SelectOption(value));
    }

    public override TScope SelectByText(string text)
    {
        var root = FindElement();
        var options = root.FindElements(Locator.ByCss("option"));

        foreach (var option in options)
        {
            if (!string.Equals(option.Text?.Trim(), text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var optionValue = option.GetAttribute("value");
            root.SelectOption(string.IsNullOrEmpty(optionValue) ? text : optionValue);
            return ContainingScope;
        }

        throw new InvalidOperationException($"Select option with text '{text}' was not found.");
    }

    public override string? GetSelectedValue()
    {
        return RunWithElement(element => element.InputValue);
    }

    public TScope SelectMultiple(params string[] values)
    {
        return RunWithElement(element => element.SelectOption(values));
    }
}
