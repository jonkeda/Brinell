using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Controls;
using Brinell.Html.Interfaces;

namespace Brinell.Blazor.Controls;

public class IFrameControl<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public IFrameControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public IFrameControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    public string? GetSource() => RunWithElement(e => e.GetDomAttribute("src"));
    public string? GetTitle() => RunWithElement(e => e.GetDomAttribute("title"));
    public string? GetName() => RunWithElement(e => e.GetDomAttribute("name"));

    public TScope ClickInside(string selector) => RunWithElement(e =>
        e.Evaluate($"(iframe) => iframe.contentDocument.querySelector('{EscapeSelector(selector)}').click()"));

    public TScope FillInside(string selector, string? text) => RunWithElement(e =>
        e.Evaluate($"(iframe) => {{ const el = iframe.contentDocument.querySelector('{EscapeSelector(selector)}'); el.value = '{EscapeJsString(text)}'; el.dispatchEvent(new Event('input', {{ bubbles: true }})); }}"));

    public string? GetTextInside(string selector) => RunWithElement(e =>
        e.Evaluate<string?>($"(iframe) => {{ const el = iframe.contentDocument.querySelector('{EscapeSelector(selector)}'); return el ? el.textContent : null; }}"));

    public bool ElementExistsInside(string selector) => RunWithElement(e =>
        e.Evaluate<bool>($"(iframe) => iframe.contentDocument.querySelector('{EscapeSelector(selector)}') !== null"));

    public bool WaitForElementInside(string selector, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return Poll(() => ElementExistsInside(selector), timeout);
    }

    // Assertions
    public TScope AssertSource(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("src");
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected iframe src '{expected}' but was '{actual}'");
    });

    public TScope AssertSourceContains(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("src");
        if (expected != null && (actual == null || !actual.Contains(expected, StringComparison.Ordinal)))
            throw new AssertionException(message ?? $"Expected iframe src to contain '{expected}' but was '{actual}'");
    });

    public TScope AssertElementExistsInside(string selector, string? message = null) => RunAssert(e =>
    {
        var exists = e.Evaluate<bool>($"(iframe) => iframe.contentDocument.querySelector('{EscapeSelector(selector)}') !== null");
        if (!exists)
            throw new AssertionException(message ?? $"Expected element '{selector}' to exist inside iframe");
    });

    private static string EscapeSelector(string selector) => selector.Replace("'", "\\'");
    private static string EscapeJsString(string? value) => value?.Replace("\\", "\\\\").Replace("'", "\\'") ?? "";
}
