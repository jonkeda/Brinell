using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

public sealed class DomDiffService
{
    public DomDiffResult Compare(DomSnapshot before, DomSnapshot after)
    {
        var beforeElements = FlattenElements(before.RootElement);
        var afterElements = FlattenElements(after.RootElement);

        var added = new List<DomElement>();
        var removed = new List<DomElement>();
        var changed = new List<DomElementChange>();
        var unchangedCount = 0;

        var matchedAfter = new HashSet<int>();

        // Try to match each "before" element to an "after" element
        foreach (var be in beforeElements)
        {
            var matchIndex = FindMatch(be, afterElements, matchedAfter);
            if (matchIndex < 0)
            {
                removed.Add(be.Element);
            }
            else
            {
                matchedAfter.Add(matchIndex);
                var ae = afterElements[matchIndex];
                var changes = CompareAttributes(be.Element, ae.Element);
                if (changes.Count > 0)
                {
                    changed.Add(new DomElementChange
                    {
                        Before = be.Element,
                        After = ae.Element,
                        ChangedAttributes = changes
                    });
                }
                else
                {
                    unchangedCount++;
                }
            }
        }

        // Any unmatched "after" elements are added
        for (var i = 0; i < afterElements.Count; i++)
        {
            if (!matchedAfter.Contains(i))
                added.Add(afterElements[i].Element);
        }

        return new DomDiffResult
        {
            Added = added,
            Removed = removed,
            Changed = changed,
            UnchangedCount = unchangedCount
        };
    }

    private static int FindMatch(FlatElement target, List<FlatElement> candidates, HashSet<int> alreadyMatched)
    {
        // Priority 1: Match by id
        if (target.Element.Id is not null)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!alreadyMatched.Contains(i) && candidates[i].Element.Id == target.Element.Id)
                    return i;
            }
        }

        // Priority 2: Match by data-testid
        if (target.Element.DataTestId is not null)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!alreadyMatched.Contains(i) && candidates[i].Element.DataTestId == target.Element.DataTestId)
                    return i;
            }
        }

        // Priority 3: Match by name
        if (target.Element.Name is not null)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!alreadyMatched.Contains(i) && candidates[i].Element.Name == target.Element.Name
                    && candidates[i].Element.Tag == target.Element.Tag)
                    return i;
            }
        }

        // Priority 4: Match by structural path (tag + parent path)
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!alreadyMatched.Contains(i) && candidates[i].Path == target.Path)
                return i;
        }

        return -1;
    }

    private static List<string> CompareAttributes(DomElement before, DomElement after)
    {
        var changes = new List<string>();

        if (before.Tag != after.Tag) changes.Add(nameof(DomElement.Tag));
        if (before.Id != after.Id) changes.Add(nameof(DomElement.Id));
        if (before.ClassName != after.ClassName) changes.Add(nameof(DomElement.ClassName));
        if (before.Name != after.Name) changes.Add(nameof(DomElement.Name));
        if (before.Type != after.Type) changes.Add(nameof(DomElement.Type));
        if (before.DataTestId != after.DataTestId) changes.Add(nameof(DomElement.DataTestId));
        if (before.Role != after.Role) changes.Add(nameof(DomElement.Role));
        if (before.AriaLabel != after.AriaLabel) changes.Add(nameof(DomElement.AriaLabel));
        if (before.Placeholder != after.Placeholder) changes.Add(nameof(DomElement.Placeholder));
        if (before.TextContent != after.TextContent) changes.Add(nameof(DomElement.TextContent));

        return changes;
    }

    private static List<FlatElement> FlattenElements(DomElement root)
    {
        var result = new List<FlatElement>();
        FlattenRecursive(root, "", result);
        return result;
    }

    private static void FlattenRecursive(DomElement element, string parentPath, List<FlatElement> result)
    {
        var path = string.IsNullOrEmpty(parentPath) ? element.Tag : $"{parentPath}/{element.Tag}";

        // Disambiguate siblings with same tag
        var sameSiblingCount = result.Count(r => r.Path == path);
        var fullPath = sameSiblingCount > 0 ? $"{path}[{sameSiblingCount}]" : path;

        result.Add(new FlatElement(element, fullPath));

        foreach (var child in element.Children)
            FlattenRecursive(child, fullPath, result);
    }

    private sealed record FlatElement(DomElement Element, string Path);
}
