using Brinell.Maui.Configuration;

namespace Brinell.Maui.Extensions.Controls.Selection;

/// <summary>
/// Shared GenericBrowser selector used by generated picker/drawer flows.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class GenericBrowser<TScope> : Brinell.Maui.Controls.Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    private const string BrowserAutomationId = "GenericBrowser";
    private const string ItemPrefix = "GenericBrowserItem";
    private const string ItemButtonPrefix = "GenericBrowserItemButton";
    private const string DrawerNativeCloseAutomationId = "DrawerView_Cancel_NativeButton";
    private const string DrawerCloseAutomationId = "DrawerView_Cancel";
    private const string FlyoutNativeCloseAutomationId = "FlyoutContainer_Cancel_NativeButton";
    private const string FlyoutCloseAutomationId = "FlyoutContainer_Cancel";

    /// <summary>
    /// Creates a GenericBrowser control in the specified scope.
    /// </summary>
    public GenericBrowser(IMauiScope<TScope> scope)
        : base(scope, Locator.ByAutomationId(BrowserAutomationId))
    {
    }

    /// <summary>
    /// Selects an item by identifier, throwing when it cannot be selected.
    /// </summary>
    /// <param name="element">The browser element, resolved and ready.</param>
    /// <param name="identifier">The item identifier.</param>
    /// <param name="visibleText">Optional visible text to fall back to.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SelectItemCore(
        IMauiElement element, string identifier, string? visibleText = null, int? timeoutMs = null)
    {
        if (!TrySelectItem(identifier, visibleText, timeoutMs))
        {
            throw new ElementNotFoundException(
                $"Could not select GenericBrowser item '{identifier}'{(visibleText == null ? string.Empty : $" / '{visibleText}'")}.");
        }
    }

    /// <summary>
    /// Attempts to select an item by identifier, optionally falling back to visible text.
    /// </summary>
    public bool TrySelectItem(string identifier, string? visibleText = null, int? timeoutMs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return Run(nameof(TrySelectItem), identifier, () =>
        {
            var automationId = BuildItemAutomationId(identifier);
            var invokeAutomationId = BuildItemButtonAutomationId(identifier);
            var invokeButton = WaitForAutomationId(invokeAutomationId, timeoutMs);
            if (TryActivateElementAndWait(invokeButton, automationId, timeoutMs))
            {
                return true;
            }

            var item = WaitForAutomationId(automationId, timeoutMs);
            if (TryActivateAndWait(item, automationId, timeoutMs))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(visibleText))
            {
                var label = WaitForNameInOpenBrowser(visibleText, timeoutMs);
                if (TryActivateAndWait(label, automationId, timeoutMs))
                {
                    return true;
                }
            }

            return false;
        });
    }

    /// <summary>
    /// Toggles an item in a multiple-selection GenericBrowser without waiting for the drawer to
    /// close, throwing when it cannot be toggled.
    /// </summary>
    /// <param name="element">The browser element, resolved and ready.</param>
    /// <param name="identifier">The item identifier.</param>
    /// <param name="visibleText">Optional visible text to fall back to.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ToggleItemCore(
        IMauiElement element, string identifier, string? visibleText = null, int? timeoutMs = null)
    {
        if (!TryToggleItem(identifier, visibleText, timeoutMs))
        {
            throw new ElementNotFoundException(
                $"Could not toggle GenericBrowser item '{identifier}'{(visibleText == null ? string.Empty : $" / '{visibleText}'")}.");
        }
    }

    /// <summary>
    /// Attempts to toggle an item in a multiple-selection GenericBrowser.
    /// </summary>
    public bool TryToggleItem(string identifier, string? visibleText = null, int? timeoutMs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return Run(nameof(TryToggleItem), identifier, () =>
        {
            var invokeButton = WaitForAutomationId(BuildItemButtonAutomationId(identifier), timeoutMs);
            if (invokeButton != null && TryActivate(invokeButton))
            {
                return true;
            }

            var item = WaitForAutomationId(BuildItemAutomationId(identifier), timeoutMs);
            if (ActivateRowCore(item))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(visibleText))
            {
                var label = WaitForNameInOpenBrowser(visibleText, timeoutMs);
                return ActivateRowCore(label);
            }

            return false;
        });
    }

    /// <summary>
    /// Closes the GenericBrowser drawer/flyout, throwing when it cannot be closed.
    /// </summary>
    /// <param name="element">The browser element, resolved and ready.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void CloseCore(IMauiElement element, int? timeoutMs = null)
    {
        if (!TryClose(timeoutMs))
        {
            throw new ElementNotFoundException("Could not close GenericBrowser.");
        }
    }

    /// <summary>
    /// Attempts to close the GenericBrowser drawer/flyout and waits until it is dismissed.
    /// </summary>
    public bool TryClose(int? timeoutMs = null)
    {
        return Run(nameof(TryClose), (string?)null, () =>
        {
            var closeButton = WaitForAnyAutomationId(timeoutMs,
                DrawerNativeCloseAutomationId,
                DrawerCloseAutomationId,
                FlyoutNativeCloseAutomationId,
                FlyoutCloseAutomationId);

            return closeButton != null
                && TryActivate(closeButton)
                && WaitForCloseSurfaceToDismiss(timeoutMs);
        });
    }

    private bool TryActivateAndWait(IMauiElement? element, string itemAutomationId, int? timeoutMs)
    {
        return ActivateRowCore(element)
            && WaitForItemToClose(itemAutomationId, timeoutMs);
    }

    private bool TryActivateElementAndWait(IMauiElement? element, string itemAutomationId, int? timeoutMs)
    {
        return element != null
            && TryActivate(element)
            && WaitForItemToClose(itemAutomationId, timeoutMs);
    }

    private IMauiElement? WaitForAutomationId(string automationId, int? timeoutMs)
    {
        IMauiElement? result = null;
        RunWait(() => (result = MauiScope.FindVisibleByAutomationId(automationId)) != null, timeoutMs);
        return result;
    }

    private IMauiElement? WaitForAnyAutomationId(int? timeoutMs, params string[] automationIds)
    {
        IMauiElement? result = null;
        RunWait(
            () =>
            {
                foreach (var automationId in automationIds)
                {
                    result = MauiScope.FindVisibleByAutomationId(automationId);
                    if (result != null)
                    {
                        return true;
                    }
                }

                return false;
            },
            timeoutMs);
        return result;
    }

    private IMauiElement? WaitForNameInOpenBrowser(string name, int? timeoutMs)
    {
        IMauiElement? result = null;
        RunWait(() => (result = FindVisibleByNameInOpenBrowser(name)) != null, timeoutMs);
        return result;
    }

    private IMauiElement? FindVisibleByNameInOpenBrowser(string name)
    {
        var browserRoots = MauiScope
            .FindVisibleElements(Locator.ByAutomationId(BrowserAutomationId))
            .ToList();
        if (browserRoots.Count == 0)
        {
            return null;
        }

        foreach (var browserRoot in browserRoots)
        {
            var directChild = browserRoot.FindElements(Locator.ByName(name)).FirstVisible();
            if (directChild != null)
            {
                return directChild;
            }
        }

        return MauiScope.FindVisibleElements(Locator.ByName(name))
            .FirstOrDefault(candidate => browserRoots.Any(root => root.ContainsCenter(candidate)));
    }

    private bool WaitForItemToClose(string automationId, int? timeoutMs)
    {
        return RunWait(
            () => !MauiScope.FindVisibleElements(Locator.ByAutomationId(automationId)).Any(),
            timeoutMs ?? 3_000);
    }

    private bool WaitForCloseSurfaceToDismiss(int? timeoutMs)
    {
        return RunWait(
            () => !HasVisibleElement(BrowserAutomationId)
                  && !HasVisibleElement(DrawerCloseAutomationId)
                  && !HasVisibleElement(FlyoutCloseAutomationId),
            timeoutMs ?? 3_000);
    }

    private bool HasVisibleElement(string automationId)
        => MauiScope.FindVisibleElements(Locator.ByAutomationId(automationId)).Any();

    private static string BuildItemAutomationId(string identifier)
        => BuildAutomationId(ItemPrefix, identifier);

    private static string BuildItemButtonAutomationId(string identifier)
        => BuildAutomationId(ItemButtonPrefix, identifier);

    private static string BuildAutomationId(string prefix, string identifier)
    {
        var safeIdentifier = new string(identifier
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray())
            .Trim('_');

        return string.IsNullOrWhiteSpace(safeIdentifier)
            ? prefix
            : $"{prefix}_{safeIdentifier}";
    }

    /// <summary>
    /// Activates a row of the browser list, given an element found inside it.
    /// </summary>
    /// <remarks>
    /// The element matched by id or name is usually a label inside the row; on Windows the
    /// containing <c>ListItem</c> is what responds to selection. The row is tried first, then
    /// the element itself. Overridable so a browser whose rows differ changes this one method.
    /// </remarks>
    [SkipGeneration("An internal step of the select and toggle methods, not an operation a caller performs.")]
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
    /// Activates one candidate element, reporting failure rather than throwing.
    /// </summary>
    /// <remarks>
    /// Every caller here is walking a list of possible surfaces - item button, row, label,
    /// close button - so a failure means "not this one" and the caller falls back.
    /// A pointer-policy violation still surfaces: that is configuration, not a wrong candidate.
    /// </remarks>
    private static bool TryActivate(IMauiElement element)
    {
        if (!element.HasUsableBounds())
        {
            return false;
        }

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

            if (element is ILegacyIAccessiblePatternElement { SupportsLegacyIAccessiblePattern: true } legacy
                && legacy.DoDefaultActionPattern())
            {
                return true;
            }

            element.Click();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
