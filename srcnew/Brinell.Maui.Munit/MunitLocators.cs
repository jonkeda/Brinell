using Brinell.Core.Locators;
using Microsoft.Maui.Controls;
using MUnit.Inspection;

namespace Brinell.Maui.Munit;

/// <summary>Brinell's locators on MUnit's tree: which nodes under a root a locator names.</summary>
internal static class MunitLocators
{
    /// <summary>The nodes under <paramref name="root"/> (itself excluded) that <paramref name="locator"/> names, in reading order.</summary>
    public static IEnumerable<VisualNode> Match(VisualNode root, Locator locator)
    {
        IEnumerable<VisualNode> scopes = [root];
        if (locator.Parent is { } parent)
            scopes = Match(root, parent);

        if (locator is { Strategy: LocatorStrategy.XPath, Value: "./*" or "*" })
            return scopes.SelectMany(scope => scope.Children);

        var predicate = Predicate(locator);
        return scopes.SelectMany(scope => scope.DescendantsAndSelf().Skip(1)).Where(predicate).Distinct();
    }

    private static Func<VisualNode, bool> Predicate(Locator locator)
    {
        var value = locator.Value;
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId or LocatorStrategy.Id or LocatorStrategy.AccessibilityId
                or LocatorStrategy.DataAutomationId or LocatorStrategy.DataTestId
                => node => node.AutomationId == value,
            LocatorStrategy.Name => node => MunitMauiElement.NameOf(node.Element) == value,
            LocatorStrategy.Text or LocatorStrategy.LinkText => node => node.Text == value,
            LocatorStrategy.PartialLinkText => node => node.Text?.Contains(value, StringComparison.Ordinal) == true,
            LocatorStrategy.ClassName or LocatorStrategy.TagName or LocatorStrategy.ControlType
                => node => IsOfType(node.Element, value),
            _ => throw new DeviceOnlyException($"Locator {locator.Strategy}:{value}"),
        };
    }

    // "Button" names Button and every type derived from it, as a platform's class name names the control.
    private static bool IsOfType(Element element, string typeName)
    {
        for (var type = element.GetType(); type is not null && type != typeof(object); type = type.BaseType)
        {
            if (type.Name == typeName || type.FullName == typeName)
                return true;
        }

        return false;
    }
}
