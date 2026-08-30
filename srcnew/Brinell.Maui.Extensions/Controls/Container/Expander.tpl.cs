using Brinell.Core.Interfaces;

namespace Brinell.Maui.Extensions.Controls.Container;

/// <summary>
/// MAUI Expander control: a disclosure container that shows or hides its content.
/// </summary>
/// <remarks>
/// Derives from <c>Base.ClickableControlBase</c> because an expander is fundamentally
/// clickable — clicking the header is how it toggles. Expanding is declared as a capability
/// (<see cref="IExpandableControlObject{TScope}"/>) rather than inherited, since C# allows
/// one base class and expanding composes with clicking rather than replacing it.
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Expander<TScope> : Brinell.Maui.Controls.Base.ClickableControlBase<TScope>,
    IExpandableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new expander control within the specified scope.
    /// </summary>
    public Expander(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new expander control using the scope's default locator strategy.
    /// </summary>
    public Expander(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>Expands the control. No-op when it is already expanded.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ExpandCore(IMauiElement element, int? timeoutMs = null)
    {
        if (IsExpandedCore(element) != true)
        {
            ToggleExpandedCore(element, timeoutMs);
        }
    }

    /// <summary>Collapses the control. No-op when it is already collapsed.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void CollapseCore(IMauiElement element, int? timeoutMs = null)
    {
        if (IsExpandedCore(element) == true)
        {
            ToggleExpandedCore(element, timeoutMs);
        }
    }

    /// <summary>
    /// Toggles the expanded state by clicking the control.
    /// </summary>
    /// <remarks>
    /// Clicking is the only universally available route: MAUI's Expander does not expose the
    /// UIA ExpandCollapse pattern's Invoke on every platform. Routed through
    /// <c>ClickCore</c> rather than <c>element.Click()</c> so the inherited activation ladder
    /// applies — an expander whose header is a separate child overrides that one method and
    /// expand, collapse and toggle all follow.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ToggleExpandedCore(IMauiElement element, int? timeoutMs = null)
        => ClickCore(element, timeoutMs);

    /// <summary>
    /// Reads the expanded state from the pre-found element.
    /// </summary>
    /// <remarks>
    /// Each platform reports the state differently, so all three spellings are tried: Windows
    /// exposes the UIA ExpandCollapse pattern's state, other platforms surface the bound
    /// property or an accessibility value. A control reporting none of them is treated as
    /// collapsed rather than unknown — an expander that has never been opened is closed.
    /// </remarks>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True when expanded, false when collapsed, null only when the element is null.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsExpandedCore(IMauiElement? element)
    {
        if (element == null) return null;

        var expandState = element.GetAttribute("ExpandCollapse.ExpandCollapseState");
        if (!string.IsNullOrEmpty(expandState))
        {
            return expandState.Equals("Expanded", StringComparison.OrdinalIgnoreCase)
                || expandState.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        foreach (var name in new[] { "IsExpanded", "aria-expanded" })
        {
            var value = element.GetAttribute(name);
            if (!string.IsNullOrEmpty(value))
            {
                return value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Asserts the expander is expanded.
    /// </summary>
    /// <remarks>
    /// A message-only overload of the generated <c>AssertExpanded(bool?, string?, int?)</c>;
    /// the generator emits one member per Core method and cannot know this shorthand is
    /// wanted.
    /// </remarks>
    public TScope AssertExpanded(string? message, int? timeoutMs = null)
        => AssertExpanded(true, message, timeoutMs);

    #endregion
}
