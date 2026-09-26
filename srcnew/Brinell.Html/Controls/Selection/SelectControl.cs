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
        return RunWithElement(element => Select(element, value, null));
    }

    /// <summary>Selects the option whose text is <paramref name="text"/>, as one call.</summary>
    public override TScope SelectByText(string text)
    {
        return RunWithElement(element =>
        {
            var option = element.FindElements(Locator.ByCss("option"))
                .FirstOrDefault(candidate => string.Equals(candidate.Text?.Trim(), text.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Select option with text '{text}' was not found.");

            var optionValue = option.GetAttribute("value");
            Select(element, string.IsNullOrEmpty(optionValue) ? text : optionValue, null);
        });
    }

    private void Select(IHtmlElement element, string value, int? timeoutMs)
        => ActAndConfirm(
            "SelectOption", () => element.SelectOption(value),
            () => element.InputValue, selected => selected == value,
            $"'{value}' selected", timeoutMs);

    public override string? GetSelectedValue()
    {
        return RunWithElement(element => element.InputValue);
    }

    public TScope SelectMultiple(params string[] values)
    {
        return RunWithElement(element => element.SelectOption(values));
    }
}
