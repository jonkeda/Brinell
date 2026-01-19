namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI CollectionView control for displaying scrollable collections with various layouts.
/// Combines list functionality with scroll capabilities.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public class MauiCollectionViewControl<TScope, TItem> : MauiListControl<TScope, TItem>
    where TScope : IMauiScope<TScope>
    where TItem : class
{
    /// <summary>
    /// Creates a CollectionView control.
    /// </summary>
    /// <param name="scope">The containing scope.</param>
    /// <param name="listLocator">Locator for the CollectionView container.</param>
    /// <param name="itemAutomationIdPrefix">Prefix for item AutomationIds.</param>
    /// <param name="itemFactory">Factory to create item containers.</param>
    public MauiCollectionViewControl(
        IMauiScope<TScope> scope,
        Locator listLocator,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, listLocator, itemAutomationIdPrefix, itemFactory)
    {
    }

    /// <summary>
    /// Creates a CollectionView control using automation ID.
    /// </summary>
    public MauiCollectionViewControl(
        IMauiScope<TScope> scope,
        string automationId,
        string itemAutomationIdPrefix,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, automationId, itemAutomationIdPrefix, itemFactory)
    {
    }

    #region CollectionView-Specific Methods

    /// <summary>
    /// Gets the current selection mode of the CollectionView.
    /// </summary>
    /// <returns>The selection mode string, or null if not available.</returns>
    public string? GetSelectionMode()
    {
        var element = TryFindElement();
        if (element == null) return null;

        return element.GetAttribute("SelectionMode");
    }

    /// <summary>
    /// Checks if multiple selection is enabled.
    /// </summary>
    /// <returns>True if multiple selection, false if single/none, null if unknown.</returns>
    public bool? IsMultiSelectEnabled()
    {
        var mode = GetSelectionMode();
        return mode?.Equals("Multiple", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
