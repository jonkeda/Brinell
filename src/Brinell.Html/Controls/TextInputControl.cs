using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML text input control wrapper.
/// Works with &lt;input type="text"&gt;, &lt;input type="email"&gt;, &lt;input type="password"&gt;, etc.
/// </summary>
public class TextInputControl : TextControlBase
{
    public TextInputControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TextInputControl(SeleniumTestContext context, string automationId)
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
        AssertAttribute("type", expected, message);
    }
    
    /// <summary>
    /// Assert placeholder text equals expected value.
    /// </summary>
    public void AssertPlaceholder(string expected, string? message = null)
    {
        AssertAttribute("placeholder", expected, message);
    }
    
    /// <summary>
    /// Assert input is read-only.
    /// </summary>
    public void AssertIsReadOnly(string? message = null)
    {
        CheckVisible(expected: true);
        var readOnly = GetAttribute("readonly");
        if (readOnly == null && GetAttribute("readOnly") == null)
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
        var readOnly = GetAttribute("readonly") ?? GetAttribute("readOnly");
        if (readOnly != null)
        {
            ThrowAssertionFailed("IsNotReadOnly", "true", "false",
                message ?? $"Expected input '{AutomationId}' to not be read-only but it is.");
        }
        LogAssertPass("IsNotReadOnly", "false", "false");
    }
}
