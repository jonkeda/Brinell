namespace Brinell.Samples.Todo.UITests.Controls;

/// <summary>
/// A label the app hides when it has nothing to say - a row's due date - for which "not shown" is
/// an answer rather than a failure.
/// </summary>
/// <remarks>
/// A hidden MAUI view leaves the automation tree on both platforms, so <c>IsShown</c> is false for a
/// missing label. Plain <c>IsVisible</c> reads null for a missing element instead of false.
/// </remarks>
/// <typeparam name="TScope">The containing scope type, normally a row.</typeparam>
public partial class ShownLabel<TScope> : Label<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>Creates the label within the specified scope.</summary>
    /// <param name="scope">The row or container holding it.</param>
    /// <param name="locator">The locator for the label.</param>
    public ShownLabel(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>Creates the label within the specified scope.</summary>
    /// <param name="scope">The row or container holding it.</param>
    /// <param name="locatorValue">The label's AutomationId.</param>
    public ShownLabel(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>Reads whether the label is shown.</summary>
    /// <param name="element">The pre-found label, or null when there is none.</param>
    /// <returns>True when shown; false when hidden or absent.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsShownCore(IMauiElement? element) => element?.Visible == true;

    #endregion
}
