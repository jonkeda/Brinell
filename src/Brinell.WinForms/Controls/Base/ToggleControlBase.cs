using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls.Base;

/// <summary>
/// Abstract base class for toggle controls (CheckBox, RadioButton, ToggleSwitch).
/// Extends InputControlBase with boolean state-specific operations.
/// </summary>
public abstract class ToggleControlBase : InputControlBase
{
    /// <summary>
    /// Create a toggle control with page context and AutomationId.
    /// </summary>
    protected ToggleControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a toggle control that searches within a container element.
    /// </summary>
    protected ToggleControlBase(FlaUITestContext context, IPageObject? page, FlaUI.Core.AutomationElements.AutomationElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    /// <summary>
    /// Create a toggle control without page context (for global controls).
    /// </summary>
    protected ToggleControlBase(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the control is currently checked/selected.
    /// </summary>
    public virtual bool IsChecked()
    {
        var element = FindElement();
        if (element == null) return false;

        var checkBox = element.AsCheckBox();
        if (checkBox != null)
        {
            return checkBox.IsChecked ?? false;
        }

        return false;
    }

    /// <summary>
    /// Set the checked state to the specified value.
    /// </summary>
    public virtual void SetChecked(bool value)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SetChecked", $"Element '{AutomationId}' not visible for toggling.");
        }

        var checkBox = element!.AsCheckBox();
        if (checkBox != null)
        {
            var currentState = checkBox.IsChecked ?? false;
            
            // Only click if state needs to change
            if (currentState != value)
            {
                element!.Click();
                System.Threading.Thread.Sleep(100); // Allow UI to process state change
            }
            
            LogAction("SetChecked", value.ToString());
        }
        else
        {
            ThrowCheckFailed("SetChecked", $"Element '{AutomationId}' is not a CheckBox/RadioButton.");
        }
    }

    /// <summary>
    /// Check the control (set to true).
    /// </summary>
    public virtual void Check()
    {
        SetChecked(true);
    }

    /// <summary>
    /// Uncheck the control (set to false).
    /// </summary>
    public virtual void Uncheck()
    {
        SetChecked(false);
    }

    /// <summary>
    /// Wait for the control to reach the expected checked state.
    /// </summary>
    public virtual bool WaitChecked(bool expected, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(() => IsChecked() == expected, timeout,
            expected ? "element checked" : "element unchecked");
        LogWait($"Checked={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Assert that the control is checked.
    /// </summary>
    public virtual void AssertChecked(string? message = null)
    {
        if (!IsChecked())
        {
            ThrowAssertionFailed("Checked", "false", "true",
                message ?? $"Expected element '{AutomationId}' to be checked but it is not.");
        }
        LogAssertPass("Checked", "true", "true");
    }

    /// <summary>
    /// Assert that the control is unchecked.
    /// </summary>
    public virtual void AssertUnchecked(string? message = null)
    {
        if (IsChecked())
        {
            ThrowAssertionFailed("Unchecked", "true", "false",
                message ?? $"Expected element '{AutomationId}' to be unchecked but it is.");
        }
        LogAssertPass("Unchecked", "false", "false");
    }

    /// <summary>
    /// Wait and assert that the control reaches the expected checked state.
    /// </summary>
    public virtual void AssertCheckedWait(bool expected, int? timeoutMs = null)
    {
        if (!WaitChecked(expected, timeoutMs))
        {
            var actual = IsChecked();
            ThrowAssertionFailed($"Checked{(expected ? "" : "Wait")}", actual.ToString(), expected.ToString(),
                $"Expected element '{AutomationId}' to be {(expected ? "checked" : "unchecked")} but got {(actual ? "checked" : "unchecked")}.");
        }
        LogAssertPass($"AssertCheckedWait={expected}", expected.ToString(), expected.ToString());
    }
}
