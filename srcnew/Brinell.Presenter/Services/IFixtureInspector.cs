namespace Brinell.Presenter.Services;

/// <summary>Lists the fixtures a Pages assembly offers, without loading it.</summary>
public interface IFixtureInspector
{
    /// <summary>Reads the fixtures out of a built assembly.</summary>
    /// <param name="assemblyPath">The Pages assembly.</param>
    /// <returns>The candidates, ranked, or why none could be read.</returns>
    FixtureInspection Inspect(string assemblyPath);
}

/// <summary>What one assembly offers as fixtures.</summary>
/// <param name="Succeeded">Whether the assembly could be read at all.</param>
/// <param name="Candidates">Every plausible fixture, usable ones first.</param>
/// <param name="Error">Why it could not be read, when it could not.</param>
public sealed record FixtureInspection(
    bool Succeeded,
    IReadOnlyList<FixtureCandidate> Candidates,
    string Error)
{
    /// <summary>The fixtures Presenter could actually construct.</summary>
    public IReadOnlyList<FixtureCandidate> Usable => [.. Candidates.Where(candidate => candidate.IsUsable)];

    /// <summary>The one obvious answer, when there is exactly one.</summary>
    public FixtureCandidate? Obvious =>
        Candidates.Count(candidate => candidate.IsUsable && candidate.ScansPages) == 1
            ? Candidates.First(candidate => candidate.IsUsable && candidate.ScansPages)
            : null;

    /// <summary>Finds a candidate the way Presenter resolves a configured fixture name.</summary>
    /// <param name="fixtureName">The name from the config.</param>
    /// <returns>The candidate, or null when the name matches nothing.</returns>
    public FixtureCandidate? Find(string fixtureName)
    {
        if (string.IsNullOrWhiteSpace(fixtureName))
        {
            return null;
        }

        var name = fixtureName.Trim();
        return Candidates.FirstOrDefault(candidate =>
            candidate.FullName.Equals(name, StringComparison.Ordinal) ||
            candidate.Name.Equals(name, StringComparison.Ordinal) ||
            candidate.Name.Equals(name + "Fixture", StringComparison.Ordinal));
    }
}

/// <summary>One type that might be the fixture.</summary>
/// <param name="Name">The type name.</param>
/// <param name="FullName">The namespace-qualified name.</param>
/// <param name="IsConcrete">Whether it can be instantiated at all.</param>
/// <param name="HasParameterlessConstructor">Whether Presenter's own construction rule can reach it.</param>
/// <param name="ScansPages">Whether it carries <c>[TestModuleScan]</c>, which page discovery needs.</param>
/// <param name="FixtureBase">The Brinell fixture base it derives from, when it does.</param>
/// <param name="Target">The target that base implies.</param>
/// <param name="Platforms">The platforms it declares through <c>[UatPlatforms]</c>, when it declares any.</param>
/// <param name="BaseChain">Its base types, nearest first, for showing why it was ranked as it was.</param>
public sealed record FixtureCandidate(
    string Name,
    string FullName,
    bool IsConcrete,
    bool HasParameterlessConstructor,
    bool ScansPages,
    string? FixtureBase,
    string? Target,
    IReadOnlyList<string> Platforms,
    IReadOnlyList<string> BaseChain)
{
    /// <summary>Whether Presenter can construct this fixture.</summary>
    public bool IsUsable => IsConcrete && HasParameterlessConstructor;

    /// <summary>Why this candidate cannot be used, for the picker to show beside it.</summary>
    public string UnusableReason => IsConcrete
        ? HasParameterlessConstructor ? string.Empty : "no parameterless constructor"
        : "abstract";

    /// <summary>A short description of what this candidate is, for the picker.</summary>
    public string Description
    {
        get
        {
            List<string> parts = [];
            if (FixtureBase is not null)
            {
                parts.Add($"derives {FixtureBase}");
            }

            if (ScansPages)
            {
                parts.Add("scans pages");
            }

            if (UnusableReason.Length > 0)
            {
                parts.Add(UnusableReason);
            }

            return string.Join(", ", parts);
        }
    }
}
