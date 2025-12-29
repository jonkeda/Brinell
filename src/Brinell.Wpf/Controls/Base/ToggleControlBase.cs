using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls.Base;

/// <summary>
/// WPF base class for toggle/boolean controls (checkbox, radio button, toggle button).
/// </summary>
public abstract class ToggleControlBase : ControlBase, IToggleControl
{
    protected ToggleControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// </summary>
    protected ToggleControlBase(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ToggleControlBase(FlaUITestContext context, string automationId)
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
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Toggle", $"Element '{AutomationId}' not visible for toggle.");
        }
        element!.Click();
        LogAction("Toggle");
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
        LogAction("Check", IsChecked().ToString());
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
        LogAction("Uncheck", (!IsChecked()).ToString());
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
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(() => IsChecked() == expected, timeout,
            expected ? "element checked" : "element unchecked");
        LogWait($"Checked={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Assert control is checked.
    /// </summary>
    public virtual void AssertChecked(string? message = null)
    {
        CheckVisible(expected: true);
        var actual = IsChecked();
        if (!actual)
        {
            ThrowAssertionFailed("Checked", "false", "true",
                message ?? $"Expected element '{AutomationId}' to be checked but it is not.");
        }
        LogAssertPass("Checked", "true", "true");
    }

    /// <summary>
    /// Assert control is unchecked.
    /// </summary>
    public virtual void AssertUnchecked(string? message = null)
    {
        CheckVisible(expected: true);
        var actual = IsChecked();
        if (actual)
        {
            ThrowAssertionFailed("Unchecked", "true", "false",
                message ?? $"Expected element '{AutomationId}' to be unchecked but it is checked.");
        }
        LogAssertPass("Unchecked", "false", "false");
    }
}
