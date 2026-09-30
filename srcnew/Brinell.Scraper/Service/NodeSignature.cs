using Brinell.Core.Scraping;

namespace Brinell.Scraper.Service;

/// <summary>
/// What makes two nodes "the same kind of thing" across pages: the type, the role, the class names that
/// are not generated ones, and the types of the direct children. Text and ids are left out.
/// </summary>
public static class NodeSignature
{
    /// <summary>The signature of a node in its snapshot.</summary>
    public static string Of(ScrapeSnapshot snapshot, ScrapeNode node)
    {
        var classes = node.Attribute("class") is { } c
            ? string.Join('.', c.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(IsStable).Order(StringComparer.Ordinal))
            : "";
        var children = string.Join(',', snapshot.ChildrenOf(node.Number).Select(n => n.Kind == ScrapeNodeKind.ShadowRoot ? "#shadow" : n.Type));
        return $"{node.Type}|{node.Role}|{classes}|{children}";
    }

    // Generated class names (Emotion's css-1x2y3z, JSS's jss123, styled-components' sc-bdVaJa, CSS
    // modules' _abc123) differ per build, so they cannot say two nodes are alike; Angular's ng-* are state.
    private static bool IsStable(string name) =>
        !(name.StartsWith("css-", StringComparison.Ordinal)
          || name.StartsWith("jss", StringComparison.Ordinal) && name.Skip(3).All(char.IsAsciiDigit)
          || name.StartsWith("sc-", StringComparison.Ordinal)
          || name.StartsWith('_') && name.Any(char.IsAsciiDigit)
          || name.StartsWith("ng-", StringComparison.Ordinal));
}
