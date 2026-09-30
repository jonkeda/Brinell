namespace Brinell.Core.Scraping;

/// <summary>A page or window captured at one moment: its nodes in document order.</summary>
public sealed record ScrapeSnapshot
{
    /// <summary>The platform that captured it: <c>html</c>, and later <c>maui</c>, <c>wpf</c>.</summary>
    public required string Platform { get; init; }

    /// <summary>Where it was: the page's address, or the window's identity.</summary>
    public required string Location { get; init; }

    /// <summary>The page's or window's title.</summary>
    public string Title { get; init; } = "";

    /// <summary>When it was captured.</summary>
    public DateTimeOffset CapturedAt { get; init; }

    /// <summary>The nodes, in document order; node <c>n</c> is at index <c>n - 1</c>, and the first is the root.</summary>
    public required IReadOnlyList<ScrapeNode> Nodes { get; init; }

    /// <summary>The node with this number.</summary>
    public ScrapeNode this[int number] => Nodes[number - 1];

    /// <summary>A node's children, in document order.</summary>
    public IEnumerable<ScrapeNode> ChildrenOf(int number) => Nodes.Where(n => n.Parent == number);
}
