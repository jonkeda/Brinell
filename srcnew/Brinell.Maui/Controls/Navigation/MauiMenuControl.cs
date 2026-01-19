namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// MAUI Menu/FlyoutMenu control for navigation menus.
/// Provides methods for menu item access and interaction.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiMenuControl<TScope> : MauiControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a Menu control with locator.
    /// </summary>
    public MauiMenuControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a Menu control with automation ID.
    /// </summary>
    public MauiMenuControl(IMauiScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    #region Menu Methods

    /// <summary>
    /// Checks if the menu is currently open/visible.
    /// </summary>
    /// <returns>True if open, false if closed, null if element not found.</returns>
    public bool? IsOpen()
    {
        return IsVisible();
    }

    /// <summary>
    /// Opens the menu by clicking on it.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Open(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Open), timeoutMs, element =>
        {
            element.Click();
        });
    }

    /// <summary>
    /// Clicks a menu item by finding it and clicking.
    /// </summary>
    /// <param name="itemLocator">The locator for the menu item.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope ClickMenuItem(Locator itemLocator, int? timeoutMs = null)
    {
        var menuItem = new MauiButtonControl<TScope>(MauiScope, itemLocator);
        return menuItem.Click(timeoutMs);
    }

    #endregion
}
