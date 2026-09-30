using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

public sealed class ControlGroupDetector
{
    public List<ControlGroupSuggestion> Detect(DomElement root)
    {
        var suggestions = new List<ControlGroupSuggestion>();
        ScanElement(root, suggestions);
        return suggestions;
    }

    private void ScanElement(DomElement element, List<ControlGroupSuggestion> suggestions)
    {
        // Check current element against detection rules
        switch (element.Tag.ToLowerInvariant())
        {
            case "form":
                suggestions.Add(new ControlGroupSuggestion
                {
                    ContainerType = "FormContainer",
                    DisplayName = element.Id is not null ? $"Form: {element.Id}" : "Form",
                    Element = element,
                    ChildElements = CollectFormChildren(element)
                });
                break;

            case "table":
                if (HasChild(element, "thead") && HasChild(element, "tbody"))
                {
                    suggestions.Add(new ControlGroupSuggestion
                    {
                        ContainerType = "TableContainer",
                        DisplayName = element.Id is not null ? $"Table: {element.Id}" : "Table",
                        Element = element
                    });
                }
                break;

            case "ul":
            case "ol":
                var listItems = element.Children
                    .Where(c => c.Tag.Equals("li", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (listItems.Count >= 2)
                {
                    suggestions.Add(new ControlGroupSuggestion
                    {
                        ContainerType = "ListContainer",
                        DisplayName = element.Id is not null ? $"List: {element.Id}" : $"List ({listItems.Count} items)",
                        Element = element,
                        ChildElements = listItems
                    });
                }
                break;

            case "nav":
                suggestions.Add(new ControlGroupSuggestion
                {
                    ContainerType = "NavigationContainer",
                    DisplayName = element.AriaLabel ?? "Navigation",
                    Element = element
                });
                break;

            case "fieldset":
                var legend = element.Children
                    .FirstOrDefault(c => c.Tag.Equals("legend", StringComparison.OrdinalIgnoreCase));
                if (legend is not null)
                {
                    suggestions.Add(new ControlGroupSuggestion
                    {
                        ContainerType = "FieldsetContainer",
                        DisplayName = legend.TextContent ?? "Fieldset",
                        Element = element,
                        ChildElements = CollectFormChildren(element)
                    });
                }
                break;

            case "div":
                var role = element.Role?.ToLowerInvariant();
                if (role is "dialog" or "form" or "tablist")
                {
                    suggestions.Add(new ControlGroupSuggestion
                    {
                        ContainerType = "RoleContainer",
                        DisplayName = $"{role}: {element.AriaLabel ?? element.Id ?? "unnamed"}",
                        Element = element
                    });
                }
                break;
        }

        // Recurse into children
        foreach (var child in element.Children)
            ScanElement(child, suggestions);
    }

    private static bool HasChild(DomElement element, string tag)
    {
        return element.Children.Any(c => c.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
    }

    private static List<DomElement> CollectFormChildren(DomElement parent)
    {
        var results = new List<DomElement>();
        CollectFormChildrenRecursive(parent, results);
        return results;
    }

    private static void CollectFormChildrenRecursive(DomElement element, List<DomElement> results)
    {
        foreach (var child in element.Children)
        {
            if (child.Tag is "input" or "select" or "textarea" or "button")
                results.Add(child);
            CollectFormChildrenRecursive(child, results);
        }
    }
}
