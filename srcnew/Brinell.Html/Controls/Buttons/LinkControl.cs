using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Buttons;

/// <summary>
/// HTML anchor/link control. Wraps &lt;a&gt; elements.
/// </summary>
public class LinkControl<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public LinkControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public LinkControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    /// <summary>
    /// Get the href attribute value.
    /// </summary>
    public string? Href => GetAttribute("href", null);

    /// <summary>
    /// Assert the href attribute matches expected.
    /// </summary>
    public TScope AssertHref(string? expected)
    {
        var actual = Href;
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                $"Href mismatch. Expected: '{expected}', Actual: '{actual}'");
        }

        return ContainingScope;
    }
}
