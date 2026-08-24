namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// MAUI Menu/FlyoutMenu control for navigation menus.
/// Provides methods for menu item access and interaction.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Menu<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a Menu control with locator.
    /// </summary>
    public Menu(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a Menu control with automation ID.
    /// </summary>
    public Menu(IMauiScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Opens the menu by clicking the pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void OpenCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }

    /// <summary>
    /// Clicks a menu item found within the menu element, not the page root.
    /// </summary>
    /// <param name="element">The pre-found menu element.</param>
    /// <param name="itemLocator">The locator for the menu item.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ClickMenuItemCore(IMauiElement element, Locator itemLocator, int? timeoutMs = null)
    {
        var menuItem = element.FindElement(itemLocator, timeoutMs ?? DefaultTimeoutMs);
        menuItem.Click();
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Checks if the menu is currently open/visible.
    /// Hand-written: an IsOpenCore would collide with OpenCore on the generated member name.
    /// </summary>
    /// <returns>True if open, false if closed, null if element not found.</returns>
    public bool? IsOpen()
    {
        return IsVisible();
    }

    #endregion
}
