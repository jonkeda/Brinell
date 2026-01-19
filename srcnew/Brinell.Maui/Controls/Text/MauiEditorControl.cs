namespace Brinell.Maui.Controls.Text;

/// <summary>
/// MAUI Editor control for multi-line text input.
/// Inherits all text manipulation from MauiEntryControl.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiEditorControl<TScope> : MauiEntryControl<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new editor control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the editor element.</param>
    public MauiEditorControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new editor control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiEditorControl(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
}
