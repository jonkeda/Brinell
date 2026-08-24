using Brinell.Core.Interfaces;

namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// MAUI ShellContent control for Shell navigation items.
/// Represents individual navigation items in a Shell (typically TabBar items).
/// Supports navigation by clicking and checking selection state.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class ShellContent<TScope> : Base.ClickableControlBase<TScope>, ITabControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly string _route;
    private readonly string _title;

    /// <summary>
    /// Creates a new ShellContent control by AutomationId.
    /// Uses AutomationId locator (preferred for Windows reliability).
    /// </summary>
    /// <param name="scope">The scope (page) providing element finding.</param>
    /// <param name="automationId">The AutomationId of the ShellContent element.</param>
    /// <param name="title">The Title attribute for display/assertion.</param>
    /// <remarks>This is the primary constructor recommended for most use cases.</remarks>
    public ShellContent(IMauiScope<TScope> scope, string automationId, string title)
        : base(scope, new Locator("TabItem", title))
    {
        _route = automationId ?? throw new ArgumentNullException(nameof(automationId));
        _title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <summary>
    /// Creates a new ShellContent control by route.
    /// Uses XPath to locate: //ShellContent[@Route='...']
    /// </summary>
    /// <param name="scope">The scope (page) providing element finding.</param>
    /// <param name="route">The Route attribute of the ShellContent element (e.g., "ButtonsPage").</param>
    public ShellContent(IMauiScope<TScope> scope, string route)
        : base(scope, Locator.ByXPath($"//ShellContent[@Route='{route}']"))
    {
        _route = route ?? throw new ArgumentNullException(nameof(route));
        _title = route; // Default to route
    }

    /// <summary>
    /// Gets the route of this ShellContent item.
    /// </summary>
    public string Route => _route;

    /// <summary>
    /// Gets the title of this ShellContent item (ITabControlObject implementation).
    /// </summary>
    public string Title => _title;

    #region Selection State - Core Methods

    /// <summary>
    /// Checks if ShellContent is selected using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if selected, false if not, null if element not found.</returns>
    protected virtual bool? IsSelectedCore(IMauiElement? element)
    {
        if (element == null) return null;

        // For MAUI ShellContent, check the Selected property or aria-selected attribute
        var selected = element.GetAttribute("Selected")
                    ?? element.GetAttribute("IsSelected")
                    ?? element.GetAttribute("aria-selected")
                    ?? element.GetAttribute("selected");

        if (selected != null)
            return selected.Equals("true", StringComparison.OrdinalIgnoreCase);

        // Fallback: check if element has "selected" in class/state
        var className = element.GetAttribute("class") ?? "";
        return className.Contains("selected", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Navigates to this ShellContent by clicking it. MAUI Shell handles the navigation.
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope ClickAndNavigate()
    {
        return Click();
    }

    /// <summary>
    /// Navigates to this ShellContent and waits for it to be selected.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for selection wait.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope NavigateTo(int? timeoutMs = null)
    {
        ClickAndNavigate();
        WaitSelected(true, timeoutMs);
        return ContainingScope;
    }

    /// <summary>
    /// Asserts this ShellContent is selected, with automatic wait.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertIsSelected(int? timeoutMs = null)
    {
        return AssertSelected(true, timeoutMs: timeoutMs);
    }

    /// <summary>
    /// Asserts this ShellContent is not selected, with automatic wait.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertIsNotSelected(int? timeoutMs = null)
    {
        return AssertSelected(false, timeoutMs: timeoutMs);
    }

    #endregion
}
