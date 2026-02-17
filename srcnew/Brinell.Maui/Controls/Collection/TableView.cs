namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI TableView control for displaying grouped settings and form-style content.
/// TableView uses sections (TableSection) with cells (TextCell, SwitchCell, EntryCell, etc.).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class TableView<TScope> : ControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a TableView control using an explicit locator.
    /// </summary>
    public TableView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a TableView control using the scope default locator strategy.
    /// </summary>
    public TableView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region TableView-Specific Methods

    /// <summary>
    /// Gets the intent of the TableView (Data, Form, Settings, Menu).
    /// </summary>
    /// <returns>The intent string, or null if element not found.</returns>
    public string? GetIntent()
    {
        var element = TryFindElement();
        if (element == null) return null;

        return element.GetAttribute("Intent");
    }

    /// <summary>
    /// Checks if the TableView has a specific intent.
    /// </summary>
    /// <param name="intent">The intent to check (e.g., "Settings", "Form").</param>
    /// <returns>True if intent matches, null if element not found.</returns>
    public bool? HasIntent(string intent)
    {
        var actual = GetIntent();
        if (actual == null) return null;

        return actual.Equals(intent, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
