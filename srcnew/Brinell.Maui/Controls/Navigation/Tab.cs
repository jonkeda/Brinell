using Brinell.Core.Interfaces;

namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// MAUI Tab control for Shell TabBar navigation.
/// Tabs are rendered as ListItem elements with @Name attribute.
/// Inherits from ClickableControlBase for click functionality.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Tab<TScope> : ClickableControlBase<TScope>, ITabControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly string _title;

    /// <summary>
    /// Creates a new tab control.
    /// </summary>
    /// <param name="scope">The scope (page) providing element finding.</param>
    /// <param name="title">The Title of the Tab (used in XPath: //ListItem[@Title='...']).</param>
    public Tab(IMauiScope<TScope> scope, string title)
        : base(scope, Locator.ByXPath($"//ListItem[@Title='{title}']"))
    {
        _title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <inheritdoc />
    public string Title => _title;

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

        // For MAUI TabBar, check the Selected property or aria-selected attribute
        var selected = element.GetAttribute("Selected") 
                    ?? element.GetAttribute("IsSelected")
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
        if (expected == null)
            return true;

        return RunWaitWithElement(
            element => IsSelectedCore(element) == expected, timeoutMs);
    }

    /// <inheritdoc />
    public TScope AssertSelected(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssertWithElement(expected, IsSelectedCore,
        (actual, exp) => Equals(actual, exp),
        message ?? $"Expected tab '{_title}' {(expected.Value ? "to be selected" : "not to be selected")}.", timeoutMs);
    }

    #endregion
}
