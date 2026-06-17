using Brinell.Core.Interfaces;

namespace Brinell.Maui.Extensions.Controls.Navigation;

/// <summary>
/// MAUI ShellContent control for Shell navigation items.
/// Represents individual navigation items in a Shell (typically TabBar items).
/// Supports navigation by clicking and checking selection state.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class ShellContent<TScope> : ClickableControlBase<TScope>, ITabControlObject<TScope>
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

    /// <summary>
    /// Navigates to this ShellContent by clicking it. MAUI Shell handles the navigation.
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope ClickAndNavigate()
    {
        base.Click();
        return ContainingScope;
    }

    #region ITabControlObject - Selection State

    /// <summary>
    /// Checks if this ShellContent is currently selected/active.
    /// </summary>
    /// <returns>True if selected, false if not, null if element not found.</returns>
    public bool? IsSelected()
    {
        return IsSelectedCore(TryFindElement());
    }

    /// <summary>
    /// Checks if ShellContent is selected using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if selected, false if not, null if element not found.</returns>
    protected bool? IsSelectedCore(IMauiElement? element)
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

    /// <summary>
    /// Waits for this ShellContent to be selected or unselected.
    /// </summary>
    /// <param name="expected">Expected selected state. Null skips the check.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if condition met within timeout, false if timeout reached.</returns>
    public bool WaitSelected(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null)
            return expected.Value == false;

        return WaitSelectedCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <summary>
    /// Polls selected state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected selected state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitSelectedCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(element, e => IsSelectedCore(e) == expected, timeoutMs);
    }

    /// <summary>
    /// Asserts this ShellContent is selected or unselected.
    /// </summary>
    /// <param name="expected">Expected selected state. Null skips the check.</param>
    /// <param name="message">Optional custom assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertSelected(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(nameof(AssertSelected), expected, () =>
        {
            WaitSelected(expected, timeoutMs);
            return IsSelected();
        }, message ?? $"Expected ShellContent '{_title}' {(expected.Value ? "to be selected" : "not to be selected")}.");
    }

    #endregion

    #region Navigation Helpers

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
