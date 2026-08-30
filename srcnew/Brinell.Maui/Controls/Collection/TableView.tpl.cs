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

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Gets the intent of the TableView (Data, Form, Settings, Menu).
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The intent string, or null if the element is not found.</returns>
    [AbsenceTolerant]
    protected virtual string? GetIntentCore(IMauiElement? element)
        => element?.GetAttribute("Intent");

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Checks if the TableView has a specific intent.
    /// </summary>
    /// <remarks>
    /// Hand-written because the comparison is case-insensitive; the generated
    /// <c>AssertIntent</c> compares with <c>==</c>. Kept as a question rather than an
    /// assertion so a caller can branch on it.
    /// </remarks>
    /// <param name="intent">The intent to check (e.g., "Settings", "Form").</param>
    /// <returns>True if intent matches, null if the element is not found.</returns>
    public bool? HasIntent(string intent)
    {
        var actual = GetIntent();
        if (actual == null) return null;

        return actual.Equals(intent, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
