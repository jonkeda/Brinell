using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms Button control wrapper.
/// </summary>
public class ButtonControl : ControlBase, IButton
{
    public ButtonControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a button control that searches within a container element.
    /// </summary>
    public ButtonControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ButtonControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Click the button using the Invoke pattern.
    /// </summary>
    public override void Click()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Click", $"Element '{AutomationId}' not visible for click.");
        }
        
        var button = element!.AsButton();
        button.Invoke();
        LogAction("Click");
    }

    /// <summary>
    /// Get button text/content.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        if (element != null)
        {
            var button = element.AsButton();
            return button?.Name ?? element.Name ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Assert button text equals expected value.
    /// </summary>
    public override void AssertTextEquals(string expected, string? message = null)
    {
        var actual = GetText();
        if (actual != expected)
        {
            ThrowAssertionFailed("TextEquals", actual, expected,
                message ?? $"Button '{AutomationId}' text is '{actual}', expected '{expected}'.");
        }
        LogAssertPass("TextEquals", actual, expected);
    }
}
