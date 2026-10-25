using System.Reflection;

namespace Bruinell.Bdd.Workspace;

/// <summary>
/// Whether a suite was built against the Bruinell the tool runs: the tool loads the suite's steps
/// with its own Bruinell assemblies, so a suite built against another major or minor version may
/// bind differently from its own test run. A patch version apart is the same.
/// </summary>
public static class BddVersionCheck
{
    /// <summary>The Bruinell assemblies this tool runs, by name.</summary>
    public static IReadOnlyDictionary<string, Version> Own => AppDomain.CurrentDomain.GetAssemblies()
        .Select(assembly => assembly.GetName())
        .Where(IsBruinell)
        .GroupBy(name => name.Name!, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First().Version ?? new Version(0, 0), StringComparer.OrdinalIgnoreCase);

    /// <summary>What differs: one sentence per Bruinell assembly referenced at another major or minor version.</summary>
    /// <param name="referenced">The Bruinell assemblies the suite's build references.</param>
    /// <param name="own">The tool's own; <see cref="Own"/> by default.</param>
    public static IReadOnlyList<string> Mismatches(IEnumerable<AssemblyName> referenced, IReadOnlyDictionary<string, Version>? own = null)
    {
        ArgumentNullException.ThrowIfNull(referenced);
        own ??= Own;
        return referenced
            .Where(IsBruinell)
            .GroupBy(name => name.Name!, StringComparer.OrdinalIgnoreCase)
            .Select(group => (Name: group.Key, Version: group.Max(name => name.Version) ?? new Version(0, 0)))
            .Where(reference => own.TryGetValue(reference.Name, out var mine) && (mine.Major != reference.Version.Major || mine.Minor != reference.Version.Minor))
            .OrderBy(reference => reference.Name, StringComparer.OrdinalIgnoreCase)
            .Select(reference => $"the suite is built against {reference.Name} {reference.Version.ToString(3)}, and the extension runs {own[reference.Name].ToString(3)}")
            .ToList();
    }

    private static bool IsBruinell(AssemblyName name) => name.Name?.StartsWith("Bruinell.", StringComparison.OrdinalIgnoreCase) == true;
}
