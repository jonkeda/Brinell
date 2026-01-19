using Brinell.Core;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Controls;
using Brinell.Maui.Interfaces;

namespace Brinell.Maui.CommunityToolkit.Controls;

/// <summary>
/// MAUI Tab control for CommunityToolkit TabView navigation.
/// <para>
/// Uses AutomationId for reliable, fast element location (unlike Shell TabBar which requires XPath).
/// Implements element-passing optimization pattern from SPEC-015b.
/// </para>
/// <example>
/// XAML:
/// <code>
/// &lt;toolkit:TabView AutomationId="MainTabView"&gt;
///     &lt;toolkit:TabViewItem AutomationId="ContainersTab" Header="Containers"&gt;
///         &lt;ContentView ...&gt;
///     &lt;/toolkit:TabViewItem&gt;
/// &lt;/toolkit:TabView&gt;
/// </code>
/// 
/// Test Code:
/// <code>
/// var containersTab = new TabViewControl&lt;MainWindowPage&gt;(page, "ContainersTab");
/// containersTab.Click();
/// </code>
/// </example>
/// </summary>
/// <typeparam name="TScope">The scope type (typically a page object)</typeparam>
public class TabViewControl<TScope> : MauiClickableControlBase<TScope>, ITabControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly string _automationId;

    /// <summary>
    /// Creates a new TabView control.
    /// </summary>
    /// <param name="scope">The scope (page) providing element finding.</param>
    /// <param name="automationId">The AutomationId of the TabViewItem element.</param>
    /// <exception cref="ArgumentNullException">If automationId is null.</exception>
    public TabViewControl(IMauiScope<TScope> scope, string automationId)
        : base(scope, Locator.ByAutomationId(automationId))
    {
        _automationId = automationId ?? throw new ArgumentNullException(nameof(automationId));
    }

    /// <inheritdoc />
    public string Title => _automationId;

    #region ITabControlObject - Selection State

    /// <inheritdoc />
    public bool? IsSelected()
    {
        return IsSelectedCore(TryFindElement());
    }

    /// <summary>
    /// Checks if tab is selected using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if selected, false if not, null if element not found.</returns>
    protected bool? IsSelectedCore(IMauiElement? element)
    {
        if (element == null) return null;

        // For CommunityToolkit TabView, check the IsSelected property
        var selected = element.GetAttribute("IsSelected") 
                    ?? element.GetAttribute("Selected")
                    ?? element.GetAttribute("aria-selected");

        if (selected != null)
            return selected.Equals("true", StringComparison.OrdinalIgnoreCase);

        // Fallback: check if element has "selected" in class/state
        var className = element.GetAttribute("class") ?? "";
        return className.Contains("selected", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public TScope AssertSelected(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(nameof(AssertSelected), expected, () =>
        {
            WaitSelected(expected, timeoutMs);
            return IsSelected();
        }, message ?? $"Expected tab '{_automationId}' {(expected.Value ? "to be selected" : "not to be selected")}.");
    }

    #endregion
}
