namespace Brinell.Core.Artifacts;

public sealed class TestArtifactManifest
{
    public string RunId { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<TestArtifactSuite> Suites { get; set; } = [];
}

public sealed class TestArtifactSuite
{
    public string Name { get; set; } = string.Empty;

    public List<TestArtifactRecord> Artifacts { get; set; } = [];
}

public sealed class TestArtifactRecord
{
    public string Kind { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Dictionary<string, string?> Metadata { get; set; } = [];
}
