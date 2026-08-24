namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// MAUI Toolbar control for app navigation bars and toolbars.
/// Provides access to toolbar items and navigation actions.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Toolbar<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a Toolbar control with locator.
    /// </summary>
    public Toolbar(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a Toolbar control with automation ID.
    /// </summary>
    public Toolbar(IMauiScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Clicks a toolbar item found within the toolbar element, not the page root.
    /// </summary>
    /// <param name="element">The pre-found toolbar element.</param>
    /// <param name="itemLocator">The locator for the toolbar item.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ClickToolbarItemCore(IMauiElement element, Locator itemLocator, int? timeoutMs = null)
    {
        var toolbarItem = element.FindElement(itemLocator, timeoutMs ?? DefaultTimeoutMs);
        toolbarItem.Click();
    }

    /// <summary>
    /// Gets the title text displayed in the toolbar.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The title text, or null if not available.</returns>
    protected virtual string? GetTitleCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try common attributes for toolbar title
        var title = element.GetAttribute("Title");
        if (!string.IsNullOrEmpty(title)) return title;

        title = element.GetAttribute("text");
        if (!string.IsNullOrEmpty(title)) return title;

        return element.Text;
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Clicks the back navigation button (if present).
    /// </summary>
    /// <param name="backButtonLocator">The locator for the back button.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope GoBack(Locator backButtonLocator, int? timeoutMs = null)
    {
        return ClickToolbarItem(backButtonLocator, timeoutMs);
    }

    #endregion
}
