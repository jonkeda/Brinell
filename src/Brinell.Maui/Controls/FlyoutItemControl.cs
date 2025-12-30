using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI FlyoutItem control wrapper.
/// Represents an item in the Shell flyout menu.
/// </summary>
public class FlyoutItemControl : ContentControlBase
{
    public FlyoutItemControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public FlyoutItemControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if this flyout item is selected.
    /// </summary>
    public bool IsSelected()
    {
        var element = FindElement();
        if (element == null) return false;
        
        var selected = element.GetAttribute("selected");
        return selected?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Get the icon associated with this flyout item.
    /// </summary>
    public string? GetIcon()
    {
        var element = FindElement();
        return element?.GetAttribute("icon") ?? element?.GetAttribute("content-desc");
    }

    #region Assert Methods

    /// <summary>
    /// Assert the flyout item is selected.
    /// </summary>
    public void AssertSelected(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsSelected())
        {
            ThrowAssertionFailed("Selected", "false", "true",
                message ?? $"Expected flyout item '{AutomationId}' to be selected.");
        }
        LogAssertPass("Selected", "true", "true");
    }

    /// <summary>
    /// Assert the flyout item is not selected.
    /// </summary>
    public void AssertNotSelected(string? message = null)
    {
        CheckVisible(expected: true);
        if (IsSelected())
        {
            ThrowAssertionFailed("NotSelected", "true", "false",
                message ?? $"Expected flyout item '{AutomationId}' to not be selected.");
        }
        LogAssertPass("NotSelected", "false", "false");
    }

    #endregion
}
