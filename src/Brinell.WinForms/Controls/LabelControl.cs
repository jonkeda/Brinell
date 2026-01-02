using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms Label control wrapper.
/// </summary>
public class LabelControl : ControlBase, ILabel
{
    public LabelControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public LabelControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public LabelControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get label text.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        if (element != null)
        {
            var label = element.AsLabel();
            return label?.Text ?? element.Name ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Assert label text equals expected value.
    /// </summary>
    public override void AssertTextEquals(string expected, string? message = null)
    {
        var actual = GetText();
        if (actual != expected)
        {
            ThrowAssertionFailed("TextEquals", actual, expected,
                message ?? $"Label '{AutomationId}' text is '{actual}', expected '{expected}'.");
        }
        LogAssertPass("TextEquals", actual, expected);
    }

    /// <summary>
    /// Assert label text contains substring.
    /// </summary>
    public override void AssertTextContains(string substring, string? message = null)
    {
        var actual = GetText();
        if (!actual.Contains(substring))
        {
            ThrowAssertionFailed("TextContains", actual, substring,
                message ?? $"Label '{AutomationId}' text does not contain '{substring}'.");
        }
        LogAssertPass("TextContains", actual, substring);
    }
}
