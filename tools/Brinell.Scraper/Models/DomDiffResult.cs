using Brinell.Scraper.Models;

namespace Brinell.Scraper.Models;

public sealed class DomDiffResult
{
    public List<DomElement> Added { get; init; } = [];
    public List<DomElement> Removed { get; init; } = [];
    public List<DomElementChange> Changed { get; init; } = [];
    public int UnchangedCount { get; init; }
}

public sealed class DomElementChange
{
    public required DomElement Before { get; init; }
    public required DomElement After { get; init; }
    public List<string> ChangedAttributes { get; init; } = [];
}
