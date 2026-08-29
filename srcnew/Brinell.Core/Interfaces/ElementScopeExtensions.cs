using Brinell.Core.Locators;

namespace Brinell.Core.Interfaces;

/// <summary>
/// Visible-first element search over a scope.
/// </summary>
/// <remarks>
/// These replace the search half of the former <c>ElementSearch</c> static helper in
/// <c>Brinell.Maui</c>. Searching a scope is part of the scope contract, not knowledge about
/// any particular control, so it belongs here rather than in a control base class.
/// <para>
/// Finding an element by structural position — the inner button of a compound control, the row
/// containing a cell — is <em>not</em> here. That is control knowledge, and lives in the control
/// object that owns the view (see <c>ViewBase.FindChildCore</c>).
/// </para>
/// </remarks>
public static class ElementScopeExtensions
{
    /// <summary>
    /// The first element with usable bounds, or null when none qualifies.
    /// </summary>
    /// <remarks>
    /// A locator often matches several elements when only one is on screen — an off-screen
    /// template row, or a control duplicated across tabs. Taking the first <em>visible</em>
    /// match rather than the first match is what makes those cases behave.
    /// </remarks>
    public static TElement? FirstVisible<TElement>(this IEnumerable<TElement>? elements)
        where TElement : class, IElement<TElement>
        => elements?.FirstOrDefault(e => e.HasUsableBounds());

    /// <summary>
    /// The elements matching the locator that have usable bounds, never null.
    /// </summary>
    /// <remarks>
    /// Drivers are inconsistent about whether "nothing matched" is an empty list or null, so
    /// this normalizes both to an empty sequence. Callers composing LINQ over a search should
    /// prefer this over <see cref="IElementScope{TElement}.FindElements"/> for that reason.
    /// </remarks>
    public static IEnumerable<TElement> FindVisibleElements<TElement>(
        this IElementScope<TElement> scope,
        Locator locator)
        where TElement : class, IElement<TElement>
    {
        ArgumentNullException.ThrowIfNull(scope);

        return (scope.FindElements(locator) ?? [])
            .Where(element => element.HasUsableBounds());
    }

    /// <summary>
    /// Finds the first visible element in the scope with the given automation id.
    /// </summary>
    public static TElement? FindVisibleByAutomationId<TElement>(
        this IElementScope<TElement> scope,
        string automationId)
        where TElement : class, IElement<TElement>
    {
        ArgumentNullException.ThrowIfNull(scope);

        return scope.FindVisibleElements(Locator.ByAutomationId(automationId)).FirstOrDefault();
    }

    /// <summary>
    /// Finds the first visible element in the scope with the given name.
    /// </summary>
    public static TElement? FindVisibleByName<TElement>(
        this IElementScope<TElement> scope,
        string name)
        where TElement : class, IElement<TElement>
    {
        ArgumentNullException.ThrowIfNull(scope);

        return scope.FindVisibleElements(Locator.ByName(name)).FirstOrDefault();
    }

    /// <summary>
    /// Whether the element reports the given control type, by tag name or attribute.
    /// </summary>
    /// <remarks>
    /// Drivers disagree about where control type lives: FlaUI surfaces it as the tag name,
    /// Appium as a <c>controltype</c> attribute. Both are checked so callers need not care.
    /// </remarks>
    public static bool IsControlType<TSelf>(this IElement<TSelf> element, string controlType)
        where TSelf : IElement<TSelf>
    {
        ArgumentNullException.ThrowIfNull(element);

        try
        {
            return string.Equals(element.TagName, controlType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.GetAttribute("controltype"), controlType, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Stale element mid-poll: report "not this type" rather than failing the caller.
            return false;
        }
    }
}
