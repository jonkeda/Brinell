using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Controls;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class ImageControl<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ImageControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ImageControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    public string? GetSource() => RunWithElement(e => e.GetDomAttribute("src"));
    public string? GetAltText() => RunWithElement(e => e.GetDomAttribute("alt"));

    public bool IsLoaded() => RunWithElement(e =>
        e.Evaluate<bool>("img => img.complete && img.naturalWidth > 0"));

    public bool WaitLoaded(bool expected = true, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return Poll(() => IsLoaded() == expected, timeout);
    }

    public int GetNaturalWidth() => RunWithElement(e =>
        e.Evaluate<int>("img => img.naturalWidth"));
    public int GetNaturalHeight() => RunWithElement(e =>
        e.Evaluate<int>("img => img.naturalHeight"));

    // Assertions
    public TScope AssertSource(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("src");
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected src '{expected}' but was '{actual}'");
    });
    public TScope AssertSourceContains(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("src");
        if (expected != null && (actual == null || !actual.Contains(expected, StringComparison.Ordinal)))
            throw new AssertionException(message ?? $"Expected src to contain '{expected}' but was '{actual}'");
    });
    public TScope AssertAltText(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("alt");
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected alt '{expected}' but was '{actual}'");
    });
}
