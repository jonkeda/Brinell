namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI TableView control for displaying grouped settings and form-style content.
/// TableView uses sections (TableSection) with cells (TextCell, SwitchCell, EntryCell, etc.).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class TableView<TScope> : Base.ViewBase<TScope>
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

    // No Intent: it is a MAUI bindable property that no platform publishes to automation, so
    // the member answered null for every app. A table's intent is not observable from outside.
}
