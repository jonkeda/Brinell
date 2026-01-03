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
    /// Handles different attribute names for Windows/Android/iOS platforms.
    /// </summary>
    public override bool IsChecked()
    {
        var element = FindElement();
        if (element == null) return false;

        // Windows UIA uses Toggle.ToggleState ("1" = on, "0" = off)
        var toggleState = element.GetAttribute("Toggle.ToggleState");
        if (toggleState != null)
        {
            return toggleState == "1" || toggleState.Equals("On", StringComparison.OrdinalIgnoreCase);
        }

        // Try standard checked attribute (Android/iOS)
        var checkedAttr = element.GetAttribute("checked");
        if (checkedAttr != null)
        {
            return checkedAttr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // Try IsToggled for MAUI Switch
        var isToggled = element.GetAttribute("IsToggled");
        if (isToggled != null)
        {
            return isToggled.Equals("true", StringComparison.OrdinalIgnoreCase) || isToggled == "1";
        }

        return false;
    }

    /// <summary>
    /// Toggle the switch state.
    /// On Windows, uses TapAtCoordinates for more reliable toggling.
    /// </summary>
    public override void Toggle()
    {
        LogAction("Toggle");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Switch '{AutomationId}' not visible for toggle.");
        
        // On Windows, clicking on the switch element may not toggle it.
        // Use TapAtCoordinates on the element center for more reliable interaction.
        var location = element.Location;
        var size = element.Size;
        var centerX = location.X + (size.Width / 2);
        var centerY = location.Y + (size.Height / 2);
        
        _context.Driver.TapAtCoordinates(centerX, centerY);
        
        // Small delay for UI to update
        Thread.Sleep(100);
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
