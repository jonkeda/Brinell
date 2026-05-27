namespace Brinell.Maui.Controls;

internal static class ElementActivator
{
    public static bool TryActivate(IMauiElement? element)
    {
        if (!ElementSearch.HasUsableBounds(element))
        {
            return false;
        }

        try
        {
            if (element is ISelectionItemPatternElement selectionItem
                && selectionItem.SupportsSelectionItemPattern
                && selectionItem.SelectItemPattern())
            {
                return true;
            }

            if (element is IInvokePatternElement invoke
                && invoke.SupportsInvokePattern
                && invoke.InvokePattern())
            {
                return true;
            }

            if (element is ILegacyIAccessiblePatternElement legacy
                && legacy.SupportsLegacyIAccessiblePattern
                && legacy.DoDefaultActionPattern())
            {
                return true;
            }

            try
            {
                element!.Click();
                return true;
            }
            catch (InvalidOperationException ex) when (IsPointerDisabledFailure(ex))
            {
                return TryKeyboardActivate(element!, Keys.Space)
                       || TryKeyboardActivate(element!, Keys.Enter);
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool TryKeyboardActivate(IMauiElement element, string key)
    {
        try
        {
            element.SendKeys(key);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPointerDisabledFailure(InvalidOperationException exception)
        => exception.Message.Contains("Pointer gestures are disabled", StringComparison.Ordinal);

    public static bool TryActivateContainingListItemOrElement(
        IMauiElementScope scope,
        IMauiElement? element)
    {
        if (!ElementSearch.HasUsableBounds(element))
        {
            return false;
        }

        foreach (var row in ElementSearch.FindContainingListItems(scope, element!))
        {
            if (TryActivate(row))
            {
                return true;
            }
        }

        return TryActivate(element);
    }
}
