using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI base class for toggle/boolean controls (CheckBox, Switch).
/// </summary>
public abstract class ToggleControlBase : ControlBase, IToggleControl
{
    protected ToggleControlBase(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ToggleControlBase(AppiumTestContext context, IPageObject? page, AppiumElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ToggleControlBase(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the control is checked/on.
    /// </summary>
    public abstract bool IsChecked();

    /// <summary>
    /// Toggle the control state.
    /// </summary>
    public virtual void Toggle()
    {
        LogAction("Toggle");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for toggle.");
        element.Click();
    }

    /// <summary>
    /// Set the control to checked/on.
    /// </summary>
    public virtual void Check()
    {
        if (!IsChecked())
        {
            Toggle();
        }
        LogAction("Check", success: IsChecked());
    }

    /// <summary>
    /// Set the control to unchecked/off.
    /// </summary>
    public virtual void Uncheck()
    {
        if (IsChecked())
        {
            Toggle();
        }
        LogAction("Uncheck", success: !IsChecked());
    }

    /// <summary>
    /// Set checked state to specific value.
    /// </summary>
    public virtual void SetChecked(bool value)
    {
        if (value)
            Check();
        else
            Uncheck();
    }

    /// <summary>
    /// Wait for checked state.
    /// </summary>
    public virtual bool WaitChecked(bool expected = true, int? timeoutMs = null)
    {
        Log($"WaitChecked(expected={expected})");
        return _context.WaitFor(() => IsChecked() == expected, timeoutMs,
            expected ? "element checked" : "element unchecked");
    }

    /// <summary>
    /// Assert control is checked.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertChecked(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsChecked())
        {
            ThrowAssertionFailed("Checked", "false", "true",
                message ?? $"Expected element '{AutomationId}' to be checked but it is not.");
        }
        LogAssertPass("Checked", "true", "true");
    }

    /// <summary>
    /// Assert control is unchecked.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertUnchecked(string? message = null)
    {
        CheckVisible(expected: true);
        if (IsChecked())
        {
            ThrowAssertionFailed("Unchecked", "true", "false",
                message ?? $"Expected element '{AutomationId}' to be unchecked but it is checked.");
        }
        LogAssertPass("Unchecked", "false", "false");
    }
}
