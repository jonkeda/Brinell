using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for toggle controls (CheckBox, Switch, RadioButton).
/// Provides checked state operations.
/// </summary>
public abstract class ToggleControlBase : ClickableControlBase, IToggleControlObject
{
    /// <summary>
    /// Creates a new toggle control.
    /// </summary>
    protected ToggleControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new toggle control using AutomationId.
    /// </summary>
    protected ToggleControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    #region Checked State

    /// <inheritdoc />
    public virtual bool IsChecked()
    {
        var element = FindElement();
        if (element is null) return false;

        // Try common toggle attributes
        var toggled = element.GetAttribute("Toggle.ToggleState");
        if (toggled is not null)
            return toggled == "1" || toggled.Equals("On", StringComparison.OrdinalIgnoreCase);

        var isChecked = element.GetAttribute("IsChecked");
        if (isChecked is not null)
            return isChecked.Equals("True", StringComparison.OrdinalIgnoreCase);

        var isOn = element.GetAttribute("IsOn");
        if (isOn is not null)
            return isOn.Equals("True", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    /// <inheritdoc />
    public virtual bool WaitChecked(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(IsChecked, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public virtual void CheckChecked(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (!WaitChecked(expected, timeoutMs))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Toggle is {(expected.Value ? "not checked" : "still checked")}",
                Locator.Value,
                timeout,
                "CheckChecked",
                $"Checked={IsChecked()}");
        }
    }

    /// <inheritdoc />
    public virtual void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        CheckChecked(expected, timeoutMs);

        var actual = IsChecked();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected toggle to be {(expected.Value ? "checked" : "unchecked")}, but was {(actual ? "checked" : "unchecked")}",
                Locator.Value,
                "AssertChecked");
        }
    }

    #endregion

    #region Toggle Actions

    /// <inheritdoc />
    public virtual void Toggle(int? timeoutMs = null)
    {
        Log("Toggle()");
        PerformToggle(timeoutMs);
    }

    /// <summary>
    /// Performs the toggle action. Override for control-specific behavior.
    /// </summary>
    protected virtual void PerformToggle(int? timeoutMs = null)
    {
        Click(timeoutMs);
    }

    /// <inheritdoc />
    public virtual void Check(int? timeoutMs = null)
    {
        Log("Check()");
        if (!IsChecked())
        {
            PerformToggle(timeoutMs);
            WaitChecked(true, timeoutMs);
        }
    }

    /// <inheritdoc />
    public virtual void Uncheck(int? timeoutMs = null)
    {
        Log("Uncheck()");
        if (IsChecked())
        {
            PerformToggle(timeoutMs);
            WaitChecked(false, timeoutMs);
        }
    }

    /// <inheritdoc />
    public virtual void SetChecked(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;

        Log($"SetChecked({expected})");
        if (expected.Value)
            Check(timeoutMs);
        else
            Uncheck(timeoutMs);
    }

    #endregion
}
