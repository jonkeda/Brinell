namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI ListView control for displaying scrollable lists of items.
/// Wraps MauiListControl pattern with ListView-specific semantics.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public class MauiListViewControl<TScope, TItem> : MauiListControl<TScope, TItem>
    where TScope : IMauiScope<TScope>
    where TItem : class
{
    /// <summary>
    /// Creates a ListView control.
    /// </summary>
    /// <param name="scope">The containing scope.</param>
    /// <param name="listLocator">Locator for the ListView container.</param>
    /// <param name="itemAutomationIdPrefix">Prefix for item AutomationIds (e.g., "ListItem_").</param>
    /// <param name="itemFactory">Factory to create item containers.</param>
    public MauiListViewControl(
        IMauiScope<TScope> scope,
        Locator listLocator,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, listLocator, itemAutomationIdPrefix, itemFactory)
    {
    }

    /// <summary>
    /// Creates a ListView control using automation ID.
    /// </summary>
    public MauiListViewControl(
        IMauiScope<TScope> scope,
        string automationId,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, automationId, itemAutomationIdPrefix, itemFactory)
    {
    }

    #region ListView-Specific Methods

    /// <summary>
    /// Checks if the ListView has a pull-to-refresh capability enabled.
    /// </summary>
    /// <returns>True if refreshable, null if element not found.</returns>
    public bool? IsPullToRefreshEnabled()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var attr = element.GetAttribute("IsPullToRefreshEnabled");
        if (!string.IsNullOrEmpty(attr))
        {
            return attr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    #endregion
}
