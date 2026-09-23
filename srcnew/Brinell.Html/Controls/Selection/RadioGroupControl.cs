using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Selection;

public class RadioGroupControl<TScope> : SelectorControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public RadioGroupControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public RadioGroupControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public override TScope SelectByValue(string value)
    {
        var root = FindElement();
        var radio = root.FindElement(Locator.ByCss($"input[type='radio'][value='{EscapeCssValue(value)}']"));
        radio.Click();
        return ContainingScope;
    }

    public override TScope SelectByText(string text)
    {
        var root = FindElement();
        var labels = root.FindElements(Locator.ByCss("label"));

        foreach (var label in labels)
        {
            if (!string.Equals(label.Text?.Trim(), text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (label.TryFindElement(Locator.ByCss("input[type='radio']"), out var nestedRadio))
            {
                nestedRadio!.Click();
                return ContainingScope;
            }

            var forId = label.GetAttribute("for");
            if (!string.IsNullOrWhiteSpace(forId))
            {
                var byId = root.FindElement(Locator.ByCss($"input[type='radio']#{EscapeCssId(forId)}"));
                byId.Click();
                return ContainingScope;
            }
        }

        throw new InvalidOperationException($"Radio option with label '{text}' was not found.");
    }

    public override string? GetSelectedValue()
    {
        var root = FindElement();
        var found = root.TryFindElement(Locator.ByCss("input[type='radio']:checked"), out var selected);
        return found ? selected?.GetAttribute("value") : null;
    }

    private static string EscapeCssValue(string value)
    {
        return value.Replace("\\", "\\\\").Replace("'", "\\'");
    }

    private static string EscapeCssId(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace(".", "\\.")
            .Replace("#", "\\#")
            .Replace(":", "\\:");
    }
}
