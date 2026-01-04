using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Control object for MAUI Switch elements.
/// </summary>
public class SwitchControl : ToggleControlBase
{
    /// <summary>
    /// Creates a new SwitchControl.
    /// </summary>
    public SwitchControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new SwitchControl using AutomationId.
    /// </summary>
    public SwitchControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public override bool IsChecked()
    {
        var element = FindElement();
        if (element is null) return false;

        // Switch uses IsToggled or IsOn attribute
        var isOn = element.GetAttribute("IsOn");
        if (isOn is not null)
            return isOn.Equals("True", StringComparison.OrdinalIgnoreCase);

        var isToggled = element.GetAttribute("IsToggled");
        if (isToggled is not null)
            return isToggled.Equals("True", StringComparison.OrdinalIgnoreCase);

        // Fallback to Toggle.ToggleState
        var toggleState = element.GetAttribute("Toggle.ToggleState");
        return toggleState == "1" || toggleState?.Equals("On", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Gets whether the switch is on. Alias for IsChecked.
    /// </summary>
    public bool IsOn() => IsChecked();

    /// <summary>
    /// Turns the switch on.
    /// </summary>
    public void TurnOn(int? timeoutMs = null)
    {
        Log("TurnOn()");
        Check(timeoutMs);
    }

    /// <summary>
    /// Turns the switch off.
    /// </summary>
    public void TurnOff(int? timeoutMs = null)
    {
        Log("TurnOff()");
        Uncheck(timeoutMs);
    }
}
