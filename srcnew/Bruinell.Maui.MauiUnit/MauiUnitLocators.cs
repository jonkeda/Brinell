using Bruinell.Core.Locators;
using Microsoft.Maui.Controls;
using MauiUnit.Inspection;

namespace Bruinell.Maui.MauiUnit;

/// <summary>Bruinell's locators on MauiUnit's tree: which nodes under a root a locator names.</summary>
internal static class MauiUnitLocators
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
            LocatorStrategy.Name => node => MauiUnitElement.NameOf(node.Element) == value,
            LocatorStrategy.Text or LocatorStrategy.LinkText => node => node.Text == value,
            LocatorStrategy.PartialLinkText => node => node.Text?.Contains(value, StringComparison.Ordinal) == true,
            LocatorStrategy.ControlType when ControlTypes.TryGetValue(value, out var types)
                => node => types.Any(type => type.IsInstanceOfType(node.Element)),
            LocatorStrategy.ClassName or LocatorStrategy.TagName or LocatorStrategy.ControlType
                => node => IsOfType(node.Element, value),
            _ => throw new DeviceOnlyException($"Locator {locator.Strategy}:{value}"),
        };
    }

    // The control-type words of the platform trees (UI Automation's, Appium's), as the MAUI types a
    // device draws them for: a Label is text there, an Entry an edit, a row a list item.
    private static readonly Dictionary<string, Type[]> ControlTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text"] = [typeof(Label), typeof(Span)],
        ["button"] = [typeof(Microsoft.Maui.Controls.Button), typeof(ImageButton)],
        ["image"] = [typeof(Image), typeof(ImageButton)],
        ["edit"] = [typeof(Entry), typeof(Editor), typeof(SearchBar)],
        ["entry"] = [typeof(Entry), typeof(Editor), typeof(SearchBar)],
        ["checkbox"] = [typeof(CheckBox)],
        ["radiobutton"] = [typeof(RadioButton)],
        ["combobox"] = [typeof(Picker)],
        ["slider"] = [typeof(Slider)],
        ["progressbar"] = [typeof(ProgressBar)],
        ["tabitem"] = [typeof(ShellSection)],
    };

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
