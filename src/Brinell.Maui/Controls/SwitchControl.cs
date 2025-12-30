using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Switch control wrapper.
/// Inherits from ToggleControlBase for standard toggle behavior.
/// Provides IsOn/TurnOn/TurnOff aliases for switch-specific terminology.
/// </summary>
public class SwitchControl : ToggleControlBase
{
    public SwitchControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public SwitchControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the switch is on/checked (immediate, no wait).
    /// </summary>
    public override bool IsChecked()
    {
        var element = FindElement();
        if (element != null)
        {
            var checkedAttr = element.GetAttribute("checked");
            return checkedAttr?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        }
        return false;
    }

    // ===== Switch-specific aliases =====

    /// <summary>
    /// Check if the switch is on (alias for IsChecked).
    /// </summary>
    public bool IsOn() => IsChecked();

    /// <summary>
    /// Turn the switch on (alias for Check).
    /// </summary>
    public void TurnOn() => Check();

    /// <summary>
    /// Turn the switch off (alias for Uncheck).
    /// </summary>
    public void TurnOff() => Uncheck();

    /// <summary>
    /// Wait for switch to have specific state (alias for WaitChecked).
    /// </summary>
    public bool WaitForState(bool expectedOn, int? timeoutMs = null) => WaitChecked(expectedOn, timeoutMs);

    // ===== Switch-specific Assert aliases =====

    /// <summary>
    /// Assert switch is on (alias for AssertChecked).
    /// </summary>
    public void AssertIsOn(string? message = null) => AssertChecked(message ?? $"Expected switch '{AutomationId}' to be on.");

    /// <summary>
    /// Assert switch is off (alias for AssertUnchecked).
    /// </summary>
    public void AssertIsOff(string? message = null) => AssertUnchecked(message ?? $"Expected switch '{AutomationId}' to be off.");
}
