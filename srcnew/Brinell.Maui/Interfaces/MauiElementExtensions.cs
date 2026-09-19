namespace Brinell.Maui.Interfaces;

/// <summary>
/// Lookup, geometry and visibility helpers over <see cref="IMauiElement"/> and
/// <see cref="IMauiElementScope"/>.
/// </summary>
/// <remarks>
/// MAUI's own copies of Brinell.Core's <c>ElementGeometryExtensions</c> and
/// <c>ElementScopeExtensions</c>, which are typed on Core interfaces MAUI no longer implements.
/// They move back into Core with the rest of the MAUI shape (<c>.my/stale-readiness/design.md</c>,
/// section 4.5).
/// </remarks>
public static class MauiElementExtensions
{
    /// <summary>
    /// Finds the first descendant matching <paramref name="locator"/>, in one attempt, or throws.
    /// </summary>
    /// <exception cref="ElementNotFoundException">No descendant matches now.</exception>
    public static IMauiElement FindElement(this IMauiElement element, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(element);

        return element.TryFindElement(locator) ?? throw new ElementNotFoundException(locator);
    }

    /// <summary>
    /// Whether the element is shown with a non-empty size.
    /// </summary>
    /// <remarks>
    /// Distinguishes the shown copy of an element from a copy the platform keeps off-screen, such
    /// as a page Shell has navigated away from.
    /// </remarks>
    public static bool HasUsableBounds(this IMauiElement? element)
    {
        try
        {
            return element?.Visible == true
                && element.Rect is { Width: > 0, Height: > 0 };
        }
        catch (StaleElementException)
        {
            // A removed element is not usable. Any other error is not an answer, and propagates.
            return false;
        }
    }

    /// <summary>Whether the centre of <paramref name="child"/> lies inside <paramref name="parent"/>.</summary>
    public static bool ContainsCenter(this IMauiElement parent, IMauiElement child)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);

        return parent.Rect.Contains(ElementGeometryExtensions.CenterOf(child.Rect));
    }

    /// <summary>The element's area in square pixels.</summary>
    public static long Area(this IMauiElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        var rect = element.Rect;
        return (long)rect.Width * rect.Height;
    }

    /// <summary>Whether the element is of the given control type.</summary>
    public static bool IsControlType(this IMauiElement element, string controlType)
    {
        ArgumentNullException.ThrowIfNull(element);

        try
        {
            return string.Equals(element.TagName, controlType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.GetAttribute("controltype"), controlType, StringComparison.OrdinalIgnoreCase);
        }
        catch (StaleElementException)
        {
            // A removed element is not of any type. Any other error propagates.
            return false;
        }
    }

    /// <summary>The first element with usable bounds, or null.</summary>
    public static IMauiElement? FirstVisible(this IEnumerable<IMauiElement>? elements)
        => elements?.FirstOrDefault(e => e.HasUsableBounds());

    /// <summary>The elements in the scope matching <paramref name="locator"/> that have usable bounds.</summary>
    public static IEnumerable<IMauiElement> FindVisibleElements(this IMauiElementScope scope, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return (scope.FindElements(locator) ?? [])
            .Where(element => element.HasUsableBounds());
    }

    /// <summary>The first visible element in the scope with the given automation id.</summary>
    public static IMauiElement? FindVisibleByAutomationId(this IMauiElementScope scope, string automationId)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return scope.FindVisibleElements(Locator.ByAutomationId(automationId)).FirstOrDefault();
    }

    /// <summary>The first visible element in the scope with the given name.</summary>
    public static IMauiElement? FindVisibleByName(this IMauiElementScope scope, string name)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return scope.FindVisibleElements(Locator.ByName(name)).FirstOrDefault();
    }
}
