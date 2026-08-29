using Brinell.Maui.Configuration;

namespace Brinell.Maui.Controls.Internal;

internal static class ElementClicker
{
    public static bool TryClick(IMauiElement? element)
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

            element!.Click();
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
            if (TryClick(row))
            {
                return true;
            }
        }

        return TryClick(element);
    }
}
