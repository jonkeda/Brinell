using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms Button control wrapper.
/// Uses shared ContentControlBase for FlaUI integration.
/// </summary>
public class ButtonControl : ContentControlBase, IButton
{
    public ButtonControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a button control that searches within a container element.
    /// Use this for buttons inside list items or repeated templates.
    /// </summary>
    public ButtonControl(FlaUITestContext context, PageBase? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ButtonControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Click the button using the Invoke pattern.
    /// Waits for the button to be both visible and enabled before clicking.
    /// </summary>
    public override void Click()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Click", $"Element '{AutomationId}' not visible for click.");
        }
        
        // Wait for the button to be enabled before clicking
        if (!WaitEnabled(expected: true))
        {
            ThrowCheckFailed("Click", $"Element '{AutomationId}' is not enabled for click.");
        }
        
        // Re-fetch the element to ensure we have the latest state
        element = FindElement();
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
}
