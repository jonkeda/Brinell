namespace Brinell.Maui.Extensions.Controls.Selection;

/// <summary>
/// List selection helper for MAUI list rows whose command surface may be exposed as
/// a UIA ListItem rather than the text/layout element found by AutomationId.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class SelectionList<TScope> : Brinell.Maui.Controls.Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a selection list in the specified scope.
    /// </summary>
    public SelectionList(IMauiScope<TScope> scope)
        : base(scope, Locator.ByControlType("List"))
    {
    }

    /// <summary>
    /// Selects a row by AutomationId.
    /// </summary>
    public TScope SelectByAutomationId(string automationId, int? timeoutMs = null)
    {
        if (!TrySelectByAutomationId(automationId, timeoutMs))
        {
            throw new ElementNotFoundException($"Could not select list item '{automationId}'.");
        }

        return ContainingScope;
    }

    /// <summary>
    /// Attempts to select a row by AutomationId.
    /// </summary>
    public bool TrySelectByAutomationId(string automationId, int? timeoutMs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);

        return Run(nameof(TrySelectByAutomationId), automationId, () =>
        {
            var item = WaitForAutomationId(automationId, timeoutMs);
            return ElementClicker.TryActivateContainingListItemOrElement(MauiScope, item);
        });
    }

    /// <summary>
    /// Attempts to select a row by visible text.
    /// </summary>
    public bool TrySelectByText(string text, int? timeoutMs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return Run(nameof(TrySelectByText), text, () =>
        {
            var item = WaitForName(text, timeoutMs);
            return ElementClicker.TryActivateContainingListItemOrElement(MauiScope, item);
        });
    }

    private IMauiElement? WaitForAutomationId(string automationId, int? timeoutMs)
    {
        IMauiElement? result = null;
        ElementSearch.WaitUntil(
            () =>
            {
                result = ElementSearch.FindVisibleByAutomationId(MauiScope, automationId);
                return result != null;
            },
            TimeSpan.FromMilliseconds(timeoutMs ?? DefaultTimeoutMs));
        return result;
    }

    private IMauiElement? WaitForName(string name, int? timeoutMs)
    {
        IMauiElement? result = null;
        ElementSearch.WaitUntil(
            () =>
            {
                result = ElementSearch.FindVisibleByName(MauiScope, name);
                return result != null;
            },
            TimeSpan.FromMilliseconds(timeoutMs ?? DefaultTimeoutMs));
        return result;
    }
}
