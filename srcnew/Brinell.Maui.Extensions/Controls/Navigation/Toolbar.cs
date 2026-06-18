namespace Brinell.Maui.Extensions.Controls.Navigation;

/// <summary>
/// MAUI Toolbar control for app navigation bars and toolbars.
/// Provides access to toolbar items and navigation actions.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Toolbar<TScope> : ControlBase<TScope>
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

    #region Toolbar Methods

    /// <summary>
    /// Clicks a toolbar item by finding it within the toolbar and clicking.
    /// The item is searched within the toolbar's own element, not the page root.
    /// </summary>
    /// <param name="itemLocator">The locator for the toolbar item.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope ClickToolbarItem(Locator itemLocator, int? timeoutMs = null)
    {
        return RunDoWithElement( toolbarElement =>
        {
            var toolbarItem = toolbarElement.FindElement(itemLocator, timeoutMs ?? DefaultTimeoutMs);
            toolbarItem.Click();
        }, timeoutMs);
    }

    /// <summary>
    /// Gets the title text displayed in the toolbar.
    /// </summary>
    /// <returns>The title text, or null if not available.</returns>
    public string? GetTitle()
    {
        var element = TryFindElement();
        if (element == null) return null;

        // Try common attributes for toolbar title
        var title = element.GetAttribute("Title");
        if (!string.IsNullOrEmpty(title)) return title;

        title = element.GetAttribute("text");
        if (!string.IsNullOrEmpty(title)) return title;

        return element.Text;
    }

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
