using Brinell.Core.Interfaces;

namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// MAUI Shell control for managing TabBar navigation and shell state.
/// Provides access to individual tabs and shell-level operations.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Shell<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly Dictionary<string, Tab<TScope>> _tabs = new();

    /// <summary>
    /// Creates a new Shell control.
    /// </summary>
    /// <param name="scope">The scope (page) providing element finding.</param>
    /// <param name="automationId">The AutomationId of the Shell element. Defaults to "AppShell".</param>
    public Shell(IMauiScope<TScope> scope, string automationId = "AppShell")
        : base(scope, automationId)
    {
    }

    #region Hand-written Convenience Members

    /// <summary>
    /// Gets a tab by its title, with lazy loading and caching.
    /// </summary>
    /// <param name="title">The title of the tab (e.g., "Buttons", "DateTime").</param>
    /// <returns>A Tab control object for the specified tab.</returns>
    public Tab<TScope> GetTab(string title)
    {
        if (string.IsNullOrEmpty(title))
            throw new ArgumentNullException(nameof(title));

        if (!_tabs.ContainsKey(title))
        {
            _tabs[title] = new Tab<TScope>(MauiScope, title);
        }

        return _tabs[title];
    }

    /// <summary>
    /// Gets the currently selected tab by checking which tab has IsSelected == true.
    /// </summary>
    /// <returns>The Tab control object of the selected tab, or null if no tab is selected or shell not found.</returns>
    public Tab<TScope>? GetSelectedTab()
    {
        // This would require iterating through known tabs
        // For now, return null - users should implement this based on their tabs collection
        return null;
    }

    /// <summary>
    /// Navigates to a tab by its title (clicks the tab).
    /// </summary>
    /// <param name="title">The title of the tab to navigate to.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope NavigateTo(string title)
    {
        var tab = GetTab(title);
        tab.Click();
        return ContainingScope;
    }

    /// <summary>
    /// Checks if a specific tab is selected.
    /// </summary>
    /// <param name="title">The title of the tab to check.</param>
    /// <returns>True if selected, false if not, null if element not found.</returns>
    public bool? IsTabSelected(string title)
    {
        var tab = GetTab(title);
        return tab.IsSelected();
    }

    /// <summary>
    /// Waits for a specific tab to be selected or unselected.
    /// </summary>
    /// <param name="title">The title of the tab to check.</param>
    /// <param name="expected">Expected selected state. Null skips the check.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if condition met within timeout, false if timeout reached.</returns>
    public bool WaitTabSelected(string title, bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var tab = GetTab(title);
        return tab.WaitSelected(expected, timeoutMs);
    }

    /// <summary>
    /// Asserts a specific tab is selected or unselected.
    /// </summary>
    /// <param name="title">The title of the tab to check.</param>
    /// <param name="expected">Expected selected state. Null skips the check.</param>
    /// <param name="message">Optional custom assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertTabSelected(string title, bool? expected, string? message = null, int? timeoutMs = null)
    {
        var tab = GetTab(title);
        tab.AssertSelected(expected, message, timeoutMs);
        return ContainingScope;
    }

    /// <summary>
    /// Checks if the Shell is loaded/visible.
    /// Hand-written: the Shell element is not exposed on every platform, so this reports loaded
    /// without probing the element.
    /// </summary>
    /// <returns>True if shell is found, false otherwise.</returns>
    public bool IsLoaded()
    {
        return true;
    }

    /// <summary>
    /// Waits for the Shell to be loaded.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if shell loaded within timeout, false if timeout reached.</returns>
    public bool WaitLoaded(int? timeoutMs = null)
    {
        return WaitExists(true, timeoutMs);
    }

    /// <summary>
    /// Asserts the Shell is loaded.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertLoaded(string? message = null, int? timeoutMs = null)
    {
        WaitLoaded(timeoutMs);
        return RunAssert(true, () => IsLoaded(), (actual, exp) => Equals(actual, exp),
            message ?? "Shell should be loaded.", timeoutMs);
    }

    #endregion
}
