using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// HTML text input control wrapper for Playwright.
/// Works with &lt;input type="text"&gt;, &lt;input type="email"&gt;, &lt;input type="password"&gt;, etc.
/// </summary>
public class TextInputControl : TextControlBase
{
    public TextInputControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TextInputControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the input type attribute (text, email, password, etc.).
    /// </summary>
    public string GetInputType()
    {
        return GetAttribute("type") ?? "text";
    }

    /// <summary>
    /// Get the input type attribute asynchronously.
    /// </summary>
    public async Task<string> GetInputTypeAsync()
    {
        return await GetAttributeAsync("type") ?? "text";
    }

    /// <summary>
    /// Get the maxlength attribute if set.
    /// </summary>
    public int? GetMaxLength()
    {
        var maxLengthAttr = GetAttribute("maxlength");
        if (int.TryParse(maxLengthAttr, out var result))
            return result;
        return null;
    }

    /// <summary>
    /// Assert input type equals expected value.
    /// </summary>
    public void AssertInputType(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetInputType();
        if (actual != expected)
        {
            ThrowAssertionFailed("InputType", actual, expected,
                message ?? $"Expected input type '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("InputType", actual, expected);
    }

    /// <summary>
    /// Assert placeholder text equals expected value.
    /// </summary>
    public void AssertPlaceholder(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetPlaceholder() ?? "(null)";
        if (actual != expected)
        {
            ThrowAssertionFailed("Placeholder", actual, expected,
                message ?? $"Expected placeholder '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("Placeholder", actual, expected);
    }

    /// <summary>
    /// Assert input is read-only.
    /// </summary>
    public void AssertIsReadOnly(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsReadOnly())
        {
            ThrowAssertionFailed("IsReadOnly", "false", "true",
                message ?? $"Expected input '{AutomationId}' to be read-only but it is not.");
        }
        LogAssertPass("IsReadOnly", "true", "true");
    }

    /// <summary>
    /// Assert input is not read-only.
    /// </summary>
    public void AssertIsNotReadOnly(string? message = null)
    {
        CheckVisible(expected: true);
        if (IsReadOnly())
        {
            ThrowAssertionFailed("IsNotReadOnly", "true", "false",
                message ?? $"Expected input '{AutomationId}' to not be read-only but it is.");
        }
        LogAssertPass("IsNotReadOnly", "false", "false");
    }
}
