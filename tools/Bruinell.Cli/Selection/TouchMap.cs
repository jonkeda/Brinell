using System.Text.Json;
using System.Text.Json.Serialization;
using Bruinell.Bdd;

namespace Bruinell.Cli.Selection;

/// <summary>
/// Which test used which page objects and controls, read from a run's timings (<c>timings-*.csv</c>,
/// <c>BRUINELL_TIMINGS</c>): the map <see cref="Affected"/> follows from a changed page to the tests that reach it
/// (.my/test-selection/next-plan.md, item 3). Kept beside the suite as <see cref="FileName"/>, written after a full run.
/// </summary>
/// <param name="Tests">Every test and scenario the run's rows name, in name order.</param>
internal sealed record TouchMap(IReadOnlyList<TouchedTest> Tests)
{
    /// <summary>The map's file, in the suite's folder.</summary>
    public const string FileName = "touch-map.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// The map of the rows in <paramref name="csvFiles"/>: a scenario's rows by its name, any other test's by its full
    /// name. A scenario's filter is its <c>BddScenario</c> address, found in the suite's feature files.
    /// </summary>
    /// <param name="csvFiles">The timings files of one or more runs of the suite, on any head.</param>
    /// <param name="suiteFolder">The suite's project folder, with its <c>uat.config.md</c> and feature files.</param>
    public static TouchMap Read(IEnumerable<string> csvFiles, string suiteFolder)
    {
        var scenarios = SuiteScenarios.Of(suiteFolder);
        var tests = new Dictionary<string, (string Filter, string? File, SortedSet<string> Pages, SortedSet<string> Controls)>(StringComparer.Ordinal);
        foreach (var csv in csvFiles)
        {
            using var reader = new StreamReader(csv);
            var header = reader.ReadLine()?.Split(';') ?? [];
            int Column(string name) => Array.IndexOf(header, name) is var at and >= 0 ? at : throw new FormatException($"{csv} has no {name} column: not a timings file.");
            var (test, scenario, kind, page, control) = (Column("Test"), Column("Scenario"), Column("Kind"), Column("Page"), Column("Control"));
            while (reader.ReadLine() is { } line)
            {
                var cells = line.Split(';');
                if (cells.Length < header.Length)
                    continue;
                var key = cells[scenario].Length > 0 ? cells[scenario] : cells[test];
                if (key.Length == 0)
                    continue;
                if (!tests.TryGetValue(key, out var touched))
                {
                    var (filter, file) = cells[scenario].Length > 0
                        ? scenarios.AddressOf(cells[scenario]) is { } found ? ($"{BddScenarioAddress.Trait}={found.Address}", found.File) : ($"{BddScenarioAddress.Trait}~{BddScenarioAddress.Slug(cells[scenario])}", null)
                        : ($"FullyQualifiedName={cells[test]}", (string?)null);
                    tests[key] = touched = (filter, file, new SortedSet<string>(StringComparer.Ordinal), new SortedSet<string>(StringComparer.Ordinal));
                }

                if (cells[kind] is "call" or "query")
                {
                    if (cells[page].Length > 0)
                        touched.Pages.Add(cells[page]);
                    if (cells[control].Length > 0)
                        touched.Controls.Add(cells[control]);
                }
            }
        }

        return new TouchMap(tests
            .OrderBy(test => test.Key, StringComparer.Ordinal)
            .Select(test => new TouchedTest(test.Key, test.Value.Filter, test.Value.File, [.. test.Value.Pages], [.. test.Value.Controls]))
            .ToList());
    }

    /// <summary>The timings files a path names: a <c>.csv</c> itself, or every <c>timings-*.csv</c> under a folder.</summary>
    public static IEnumerable<string> CsvFiles(string path)
        => Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "timings-*.csv", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            : File.Exists(path) ? [path] : throw new FileNotFoundException($"No timings at {path}.");

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Options) + Environment.NewLine);

    public static TouchMap Load(string path)
        => File.Exists(path)
            ? JsonSerializer.Deserialize<TouchMap>(File.ReadAllText(path), Options) ?? throw new FormatException($"{path} is not a touch map.")
            : throw new FileNotFoundException($"No touch map at {path}: write one with 'bruinell tests touch-map' from a full run's timings.");
}

/// <summary>One test or scenario, and what it reached.</summary>
/// <param name="Name">The test's full name, or the scenario's name.</param>
/// <param name="Filter">The <c>dotnet test --filter</c> term that runs it alone.</param>
/// <param name="File">A scenario's feature file, relative to the suite; null for a test.</param>
/// <param name="Pages">The page objects (and scopes) its calls went through.</param>
/// <param name="Controls">The controls its calls went to: their locators or names.</param>
internal sealed record TouchedTest(string Name, string Filter, string? File, IReadOnlyList<string> Pages, IReadOnlyList<string> Controls);

/// <summary>A suite's scenarios: each one's file and address, by name and by file.</summary>
internal sealed class SuiteScenarios
{
    private readonly List<(string File, string Name, string Address)> _scenarios;

    private SuiteScenarios(List<(string File, string Name, string Address)> scenarios) => _scenarios = scenarios;

    /// <summary>The scenarios of the feature files beside the suite's config; none without one.</summary>
    public static SuiteScenarios Of(string suiteFolder)
    {
        var config = BddScenarioSource.GetConfigFilePath(suiteFolder);
        if (!File.Exists(config))
            return new SuiteScenarios([]);
        var root = Path.GetDirectoryName(config)!;
        return new SuiteScenarios(BddScenarioSource.EnumerateScenarios(baseDirectory: root)
            .Where(scenario => scenario.ScenarioName.Length > 0)
            .Select(scenario =>
            {
                var file = Path.GetRelativePath(root, scenario.FilePath).Replace('\\', '/');
                return (file, scenario.ScenarioName, BddScenarioAddress.Of(file, scenario.ScenarioName));
            })
            .ToList());
    }

    /// <summary>The scenario's address and file; null when no feature file has it.</summary>
    public (string Address, string File)? AddressOf(string name)
        => _scenarios.FirstOrDefault(scenario => scenario.Name == name) is { Address: not null } found ? (found.Address, found.File) : null;

    /// <summary>Every scenario of one feature file, relative to the suite.</summary>
    public IEnumerable<(string Name, string Address)> InFile(string file)
        => _scenarios.Where(scenario => string.Equals(scenario.File, file, StringComparison.OrdinalIgnoreCase)).Select(scenario => (scenario.Name, scenario.Address));
}
