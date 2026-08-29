namespace Brinell.Maui.Controls.Internal;

/// <summary>
/// Element-level expand/collapse primitives for disclosure controls.
/// </summary>
/// <remarks>
/// Expanding is a capability, not a hierarchy level: a control may need it alongside
/// clicking or scrolling, and C# allows only one base class. Keeping the mechanics here as
/// statics over <see cref="IMauiElement"/> lets a control implement
/// <c>IExpandableControlObject</c> and delegate, the same way
/// <c>Containers/ScrollHelper</c> serves scrolling.
/// </remarks>
internal static class ExpandHelper
{
    /// <summary>Expands the element. No-op when it is already expanded.</summary>
    public static void Expand(IMauiElement element)
    {
        if (IsExpanded(element) != true)
        {
            Toggle(element);
        }
    }

    /// <summary>Collapses the element. No-op when it is already collapsed.</summary>
    public static void Collapse(IMauiElement element)
    {
        if (IsExpanded(element) == true)
        {
            Toggle(element);
        }
    }

    /// <summary>
    /// Toggles the expanded state by clicking the element.
    /// </summary>
    /// <remarks>
    /// Clicking is the only universally available route: MAUI's Expander does not expose
    /// the UIA ExpandCollapse pattern's Invoke on every platform.
    /// </remarks>
    public static void Toggle(IMauiElement element) => element.Click();

    /// <summary>
    /// Reads an element's expanded state.
    /// </summary>
    /// <returns>
    /// True or false from whichever attribute the platform supplies; null only when the
    /// element is null. An element reporting no such attribute is treated as collapsed.
    /// </returns>
    public static bool? IsExpanded(IMauiElement? element)
    {
        if (element == null) return null;

        // Windows exposes the UIA ExpandCollapse pattern, which reports a state name or
        // its numeric equivalent.
        var expandState = element.GetAttribute("ExpandCollapse.ExpandCollapseState");
        if (!string.IsNullOrEmpty(expandState))
        {
            return expandState.Equals("Expanded", StringComparison.OrdinalIgnoreCase)
                || expandState.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        // Other platforms surface the bound property directly, or an accessibility value.
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
}
