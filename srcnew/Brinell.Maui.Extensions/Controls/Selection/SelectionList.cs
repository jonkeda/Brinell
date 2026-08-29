using Brinell.Maui.Configuration;

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
            return ActivateRowCore(item);
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
            return ActivateRowCore(item);
        });
    }

    /// <summary>
    /// Activates a row, given an element found inside it.
    /// </summary>
    /// <remarks>
    /// The element matched by id or name is usually a label inside the row, and on Windows
    /// selection responds to the containing <c>ListItem</c>, not to that label. The containing
    /// row is tried first, then the element itself.
    /// <para>
    /// Overridable so a list whose rows activate differently changes this one method.
    /// </para>
    /// </remarks>
    /// <param name="item">The element found for the row. May be null when the wait timed out.</param>
    /// <returns>True when the row was activated.</returns>
    protected virtual bool ActivateRowCore(IMauiElement? item)
    {
        if (!item.HasUsableBounds())
        {
            return false;
        }

        var center = ElementGeometryExtensions.CenterOf(item!.Rect);

        var containingRows = MauiScope.FindVisibleElements(Locator.ByControlType("ListItem"))
            .Where(row => row.Rect.Contains(center))
            .OrderBy(row => row.Area());

        foreach (var row in containingRows)
        {
            if (TryActivate(row))
            {
                return true;
            }
        }

        return TryActivate(item);
    }

    /// <summary>
    /// Activates one candidate row element, reporting failure rather than throwing.
    /// </summary>
    /// <remarks>
    /// Candidates are tried in turn, so a failure means "not this row" and the caller moves on.
    /// A pointer-policy violation still surfaces: that is configuration, not a wrong candidate.
    /// </remarks>
    private static bool TryActivate(IMauiElement element)
    {
        try
        {
            if (element is ISelectionItemPatternElement { SupportsSelectionItemPattern: true } selectionItem
                && selectionItem.SelectItemPattern())
            {
                return true;
            }

            if (element is IInvokePatternElement { SupportsInvokePattern: true } invoke
                && invoke.InvokePattern())
            {
                return true;
            }

            element.Click();
            return true;
        }
        catch (WindowsInteractionPolicyException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for a visible element with the given automation id.
    /// </summary>
    private IMauiElement? WaitForAutomationId(string automationId, int? timeoutMs)
    {
        IMauiElement? result = null;
        RunWait(() => (result = MauiScope.FindVisibleByAutomationId(automationId)) != null, timeoutMs);
        return result;
    }

    /// <summary>
    /// Waits for a visible element with the given name.
    /// </summary>
    private IMauiElement? WaitForName(string name, int? timeoutMs)
    {
        IMauiElement? result = null;
        RunWait(() => (result = MauiScope.FindVisibleByName(name)) != null, timeoutMs);
        return result;
    }
}
