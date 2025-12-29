using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls.Base;

/// <summary>
/// HTML/Selenium base class for toggle controls (checkbox, radio).
/// </summary>
public abstract class ToggleControlBase : ControlBase, IToggleControl
{
    protected ToggleControlBase(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ToggleControlBase(SeleniumTestContext context, IPageObject? page, IWebElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ToggleControlBase(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the control is checked.
    /// </summary>
    public virtual bool IsChecked()
    {
        var element = FindElement();
        return element?.Selected ?? false;
    }

    /// <summary>
    /// Toggle the checked state.
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
    /// Check (set to true) the control.
    /// </summary>
    public virtual void Check()
    {
        LogAction("Check");
        if (!IsChecked())
        {
            Toggle();
        }
    }

    /// <summary>
    /// Uncheck (set to false) the control.
    /// </summary>
    public virtual void Uncheck()
    {
        LogAction("Uncheck");
        if (IsChecked())
        {
            Toggle();
        }
    }

    /// <summary>
    /// Set checked state.
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
            $"element '{AutomationId}' checked = {expected}");
    }

    /// <summary>
    /// Assert checked state.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertChecked(string? message = null)
    {
        CheckVisible(expected: true);
        var actual = IsChecked();
        if (!actual)
        {
            ThrowAssertionFailed("Checked", "false", "true",
                message ?? $"Expected element '{AutomationId}' to be checked but it was not.");
        }
        LogAssertPass("Checked", "true", "true");
    }
    
    /// <summary>
    /// Assert unchecked state.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertUnchecked(string? message = null)
    {
        CheckVisible(expected: true);
        var actual = IsChecked();
        if (actual)
        {
            ThrowAssertionFailed("Unchecked", "true", "false",
                message ?? $"Expected element '{AutomationId}' to be unchecked but it was checked.");
        }
        LogAssertPass("Unchecked", "false", "false");
    }
}
