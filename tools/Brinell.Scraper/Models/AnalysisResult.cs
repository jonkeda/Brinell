namespace Brinell.Scraper.Models;

public sealed class AnalysisResult
{
    public List<ControlProposal> ProposedControls { get; init; } = [];
    public LocatorReport? LocatorReport { get; init; }
}

public sealed class ControlProposal
{
    public string Name { get; init; } = "";
    public string DomSignature { get; init; } = "";
    public int Frequency { get; init; }
    public int Confidence { get; init; }
    public string ExampleSnippet { get; init; } = "";
    public List<string> SuggestedProperties { get; init; } = [];
    public bool IsApproved { get; set; }
}

public sealed class LocatorReport
{
    public List<string> StableAttributes { get; init; } = [];
    public List<string> UnstableAttributes { get; init; } = [];
    public string Recommendations { get; init; } = "";
}
