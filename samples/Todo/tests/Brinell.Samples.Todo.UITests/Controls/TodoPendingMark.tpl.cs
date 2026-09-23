namespace Brinell.Samples.Todo.UITests.Controls;

/// <summary>
/// A list row's "not synced yet" marker (●).
/// </summary>
/// <remarks>
/// The app hides the marker once the row is synced, and a hidden MAUI view leaves the Windows
/// automation tree altogether (probed 2026-09-18: <c>TodoRow_Pending</c> is present with the name
/// "Not synced yet" before Sync and absent after). So absence is the answer "synced":
/// <c>IsShown</c> is false for a missing marker, where plain <c>IsVisible</c> would read null.
/// </remarks>
/// <typeparam name="TScope">The containing scope type, normally the row.</typeparam>
public partial class TodoPendingMark<TScope> : Label<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>Creates the marker within the specified scope.</summary>
    /// <param name="scope">The row.</param>
    /// <param name="locator">The locator for the marker.</param>
    public TodoPendingMark(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>Creates the marker within the specified scope.</summary>
    /// <param name="scope">The row.</param>
    /// <param name="locatorValue">The marker's AutomationId.</param>
    public TodoPendingMark(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>Reads whether the marker is shown, which is whether the row has unsynced changes.</summary>
    /// <param name="element">The pre-found marker, or null when there is none.</param>
    /// <returns>True when shown; false when hidden or absent.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsShownCore(IMauiElement? element) => element?.Visible == true;

    #endregion
}
