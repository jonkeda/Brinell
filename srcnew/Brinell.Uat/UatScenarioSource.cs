namespace Brinell.Uat;

public static class UatScenarioSource
{
    /// <summary>The suite's config: <paramref name="configFileName"/>, or else <c>uat.config.md</c> or <c>bdd.config.md</c>.</summary>
    public static string GetConfigFilePath(
        string? baseDirectory = null,
        string? configFileName = null)
    {
        var directory = baseDirectory ?? AppContext.BaseDirectory;
        return configFileName is null ? UatFileFormat.FindConfig(directory) : Path.Combine(directory, configFileName);
    }

    /// <summary>
    /// Enumerates scenario files, optionally filtered by configuration.
    /// </summary>
    public static IEnumerable<string> EnumerateScenarioFiles(
        string folderName = UatFileFormat.FeatureFolder,
        string? baseDirectory = null,
        string? configurationFilter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);

        var scenarioDirectory = Path.Combine(baseDirectory ?? AppContext.BaseDirectory, folderName);
        IEnumerable<string> files = Directory.Exists(scenarioDirectory)
            ? Directory
                .EnumerateFiles(scenarioDirectory, UatFileFormat.FeatureFilePattern, SearchOption.AllDirectories)
                .Order(StringComparer.OrdinalIgnoreCase)
            : Array.Empty<string>();

        return ApplyFilter(files, configurationFilter);
    }

    /// <summary>
    /// Gets scenario files as theory data, optionally filtered by configuration.
    /// </summary>
    public static IEnumerable<object[]> GetScenarioFileTheoryData(
        string folderName = UatFileFormat.FeatureFolder,
        string? baseDirectory = null,
        string? configurationFilter = null)
    {
        foreach (var filePath in EnumerateScenarioFiles(folderName, baseDirectory, configurationFilter))
        {
            yield return [filePath];
        }
    }

    /// <summary>
    /// Gets one theory case per scenario - <c>[filePath, scenarioName]</c> - so each scenario (and
    /// each outline row) passes or fails on its own.
    /// </summary>
    /// <remarks>
    /// Cases come grouped by the files' <c>TestData</c> metadata, then by file and position, which
    /// is <see cref="GetOrderKey"/>. A runner that keeps that order (an xUnit test-case orderer)
    /// switches test data once per test set. A file that does not parse still yields one case,
    /// with an empty scenario name, so its diagnostics are reported instead of the file vanishing.
    /// A suite whose head is opt-in (<see cref="UatTagConventions.OptInHeads"/>) lists only the
    /// scenarios tagged for it, read from the config next to the scenarios; on any other head a
    /// scenario for another head still shows, as skipped.
    /// </remarks>
    public static IEnumerable<object[]> GetScenarioTheoryData(
        string folderName = UatFileFormat.FeatureFolder,
        string? baseDirectory = null,
        string? configurationFilter = null)
    {
        var configPath = GetConfigFilePath(baseDirectory);
        var config = File.Exists(configPath) ? UatConfigParser.ParseFile(configPath) : null;
        var optIn = config?.SuiteHead is { } head && UatTagConventions.OptInHeads.Contains(head, StringComparer.OrdinalIgnoreCase);

        return EnumerateScenarios(folderName, baseDirectory, configurationFilter)
            .Where(scenario => !optIn || scenario.ScenarioName.Length == 0 || config!.RunsOnHead(scenario.Tags))
            .Select(scenario => new object[] { scenario.FilePath, scenario.ScenarioName });
    }

    /// <summary>Every scenario in the scenario files, in <see cref="GetOrderKey"/> order.</summary>
    public static IReadOnlyList<UatScenarioReference> EnumerateScenarios(
        string folderName = UatFileFormat.FeatureFolder,
        string? baseDirectory = null,
        string? configurationFilter = null)
    {
        return EnumerateScenarioFiles(folderName, baseDirectory, configurationFilter)
            .SelectMany(ScenariosOf)
            .OrderBy(scenario => scenario.OrderKey, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// The position of a scenario in a run: its file's test set, then the file, then its place in
    /// the file.
    /// </summary>
    public static string GetOrderKey(string filePath, string scenarioName)
    {
        return ScenariosOf(filePath).FirstOrDefault(scenario => scenario.ScenarioName == scenarioName)?.OrderKey
            ?? new UatScenarioReference(filePath, scenarioName, null, int.MaxValue).OrderKey;
    }

    private static IEnumerable<UatScenarioReference> ScenariosOf(string filePath)
    {
        var document = UatMarkdownParser.ParseFile(filePath).Document;
        if (document is null || document.Scenarios.Count == 0)
        {
            return [new UatScenarioReference(filePath, string.Empty, null, 0)];
        }

        var testData = document.Metadata.TryGetValue(UatMetadataFields.TestData, out var name) && name.Length > 0 ? name : null;
        return document.Scenarios.Select((scenario, index) => new UatScenarioReference(filePath, scenario.Name, testData, index) { Tags = scenario.Tags });
    }

    public static IEnumerable<string> ApplyFilter(IEnumerable<string> filePaths, string? filter)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        if (string.IsNullOrWhiteSpace(filter))
            return filePaths;

        var terms = filter.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return terms.Length == 0
            ? filePaths
            : filePaths.Where(path => terms.Any(term =>
                path.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(path).Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}

/// <summary>One scenario of a scenario file.</summary>
/// <param name="FilePath">The file.</param>
/// <param name="ScenarioName">The scenario's name; outline rows carry their <c>[n]</c> suffix.</param>
/// <param name="TestData">The file's <c>TestData</c> metadata, or <c>null</c>.</param>
/// <param name="Index">The scenario's position in the file.</param>
public sealed record UatScenarioReference(string FilePath, string ScenarioName, string? TestData, int Index)
{
    /// <summary>The scenario's own tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Sorts by test set, then file, then position.</summary>
    public string OrderKey => $"{TestData}\u0001{FilePath}\u0001{Index:D6}";
}
