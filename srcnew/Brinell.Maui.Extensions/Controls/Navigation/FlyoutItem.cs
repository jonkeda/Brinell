namespace Brinell.Maui.Extensions.Controls.Navigation;

/// <summary>
/// MAUI Shell FlyoutItem control for navigation.
/// Uses XPath with @Name (Title) instead of AccessibilityId because
/// FlyoutItem's AutomationId doesn't propagate to the Windows UI Automation tree.
/// Inherits from ClickableControlBase for click functionality.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class FlyoutItem<TScope> : ClickableControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly string _title;

    /// <summary>
    /// Creates a new flyout item control using the item's Title for location.
    /// </summary>
    /// <param name="scope">The scope (page) providing element finding.</param>
    /// <param name="title">The Title of the FlyoutItem (becomes @Name in UI tree).</param>
    public FlyoutItem(IMauiScope<TScope> scope, string title)
        : base(scope, new Locator(LocatorStrategy.XPath, $"//*[@Name='{title}']"))
    {
        _title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <summary>
    /// Gets the title of this flyout item.
    /// </summary>
    public string Title => _title;
}
