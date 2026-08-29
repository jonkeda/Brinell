namespace Brinell.Maui.Extensions.Controls.Selection;

/// <summary>
/// Shared GenericBrowser selector used by generated picker/drawer flows.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class GenericBrowser<TScope> : Brinell.Maui.Controls.Base.ViewBase<TScope>
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
    /// Selects an item by identifier, optionally falling back to visible text.
    /// </summary>
    public TScope SelectItem(string identifier, string? visibleText = null, int? timeoutMs = null)
    {
        if (!TrySelectItem(identifier, visibleText, timeoutMs))
        {
            throw new ElementNotFoundException(
                $"Could not select GenericBrowser item '{identifier}'{(visibleText == null ? string.Empty : $" / '{visibleText}'")}.");
        }

        return ContainingScope;
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
    /// Toggles an item in a multiple-selection GenericBrowser without waiting
    /// for the drawer to close.
    /// </summary>
    public TScope ToggleItem(string identifier, string? visibleText = null, int? timeoutMs = null)
    {
        if (!TryToggleItem(identifier, visibleText, timeoutMs))
        {
            throw new ElementNotFoundException(
                $"Could not toggle GenericBrowser item '{identifier}'{(visibleText == null ? string.Empty : $" / '{visibleText}'")}.");
        }

        return ContainingScope;
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
            if (ElementClicker.TryClick(invokeButton))
            {
                return true;
            }

            var item = WaitForAutomationId(BuildItemAutomationId(identifier), timeoutMs);
            if (ElementClicker.TryActivateContainingListItemOrElement(MauiScope, item))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(visibleText))
            {
                var label = WaitForNameInOpenBrowser(visibleText, timeoutMs);
                return ElementClicker.TryActivateContainingListItemOrElement(MauiScope, label);
            }

            return false;
        });
    }

    /// <summary>
    /// Closes the GenericBrowser drawer/flyout.
    /// </summary>
    public TScope Close(int? timeoutMs = null)
    {
        if (!TryClose(timeoutMs))
        {
            throw new ElementNotFoundException("Could not close GenericBrowser.");
        }

        return ContainingScope;
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

            return ElementClicker.TryClick(closeButton)
                && WaitForCloseSurfaceToDismiss(timeoutMs);
        });
    }

    private bool TryActivateAndWait(IMauiElement? element, string itemAutomationId, int? timeoutMs)
    {
        return ElementClicker.TryActivateContainingListItemOrElement(MauiScope, element)
            && WaitForItemToClose(itemAutomationId, timeoutMs);
    }

    private bool TryActivateElementAndWait(IMauiElement? element, string itemAutomationId, int? timeoutMs)
    {
        return ElementClicker.TryClick(element)
            && WaitForItemToClose(itemAutomationId, timeoutMs);
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

    private IMauiElement? WaitForAnyAutomationId(int? timeoutMs, params string[] automationIds)
    {
        IMauiElement? result = null;
        ElementSearch.WaitUntil(
            () =>
            {
                foreach (var automationId in automationIds)
                {
                    result = ElementSearch.FindVisibleByAutomationId(MauiScope, automationId);
                    if (result != null)
                    {
                        return true;
                    }
                }

                return false;
            },
            TimeSpan.FromMilliseconds(timeoutMs ?? DefaultTimeoutMs));
        return result;
    }

    private IMauiElement? WaitForNameInOpenBrowser(string name, int? timeoutMs)
    {
        IMauiElement? result = null;
        ElementSearch.WaitUntil(
            () =>
            {
                result = FindVisibleByNameInOpenBrowser(name);
                return result != null;
            },
            TimeSpan.FromMilliseconds(timeoutMs ?? DefaultTimeoutMs));
        return result;
    }

    private IMauiElement? FindVisibleByNameInOpenBrowser(string name)
    {
        var browserRoots = (MauiScope.FindElements(Locator.ByAutomationId(BrowserAutomationId)) ?? Array.Empty<IMauiElement>())
            .Where(ElementSearch.HasUsableBounds)
            .ToList();
        if (browserRoots.Count == 0)
        {
            return null;
        }

        foreach (var browserRoot in browserRoots)
        {
            var directChild = ElementSearch.FirstVisible(browserRoot.FindElements(Locator.ByName(name)));
            if (directChild != null)
            {
                return directChild;
            }
        }

        return (MauiScope.FindElements(Locator.ByName(name)) ?? Array.Empty<IMauiElement>())
            .Where(ElementSearch.HasUsableBounds)
            .FirstOrDefault(candidate => browserRoots.Any(root => ElementSearch.ContainsCenter(root, candidate)));
    }

    private bool WaitForItemToClose(string automationId, int? timeoutMs)
    {
        return ElementSearch.WaitUntil(
            () => !MauiScope.FindElements(Locator.ByAutomationId(automationId)).Any(ElementSearch.HasUsableBounds),
            TimeSpan.FromMilliseconds(timeoutMs ?? 3_000));
    }

    private bool WaitForCloseSurfaceToDismiss(int? timeoutMs)
    {
        return ElementSearch.WaitUntil(
            () => !HasVisibleElement(BrowserAutomationId)
                  && !HasVisibleElement(DrawerCloseAutomationId)
                  && !HasVisibleElement(FlyoutCloseAutomationId),
            TimeSpan.FromMilliseconds(timeoutMs ?? 3_000));
    }

    private bool HasVisibleElement(string automationId)
        => (MauiScope.FindElements(Locator.ByAutomationId(automationId)) ?? Array.Empty<IMauiElement>())
            .Any(ElementSearch.HasUsableBounds);

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
}
