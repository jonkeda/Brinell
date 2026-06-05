using Brinell.Core.Artifacts;

namespace Brinell.Uat;

public sealed record UatConfig(
    IReadOnlyDictionary<string, string> Runtime,
    IReadOnlyList<UatAssemblyRegistration> Assemblies,
    UatDiscoverySettings Discovery,
    UatReportingSettings Reporting,
    IReadOnlyList<UatSkipRule> SkipRules)
{
    public UatConfig(
        IReadOnlyDictionary<string, string> Runtime,
        IReadOnlyList<UatAssemblyRegistration> Assemblies,
        UatDiscoverySettings Discovery)
        : this(Runtime, Assemblies, Discovery, UatReportingSettings.CreateDefault(), [])
    {
    }

    public UatSkipDecision EvaluateSkip(
        IEnumerable<string> tags,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        ArgumentNullException.ThrowIfNull(tags);

        if (SkipRules.Count == 0)
        {
            return UatSkipDecision.Run;
        }

        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;
        HashSet<string> normalizedTags = new(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            var normalized = UatTagConventions.NormalizeTag(tag);
            if (normalized.Length > 0)
            {
                normalizedTags.Add(normalized);
            }
        }

        foreach (var rule in SkipRules)
        {
            if (!normalizedTags.Contains(rule.Tag))
            {
                continue;
            }

            var value = getEnvironmentVariable(rule.EnvironmentVariable);
            if (!UatConfigParser.IsEnabled(value))
            {
                return new UatSkipDecision(
                    ShouldSkip: true,
                    Reason: $"Scenario tag '@{rule.Tag}' requires environment variable '{rule.EnvironmentVariable}' to be enabled.",
                    Rule: rule);
            }
        }

        return UatSkipDecision.Run;
    }
}

public sealed record UatAssemblyRegistration(string Kind, string Assembly);

public sealed record UatDiscoverySettings(
    bool RequireExplicitUatAttributes = false,
    bool AllowNameInference = true);

public sealed record UatReportingSettings(
    string OutputDirectory,
    bool ScreenshotOnFailure = false,
    bool IncludeRuntimeTrace = false)
{
    public static UatReportingSettings CreateDefault(string? suiteName = null)
    {
        return new UatReportingSettings(
            DefaultTestArtifactPathProvider.Create(suiteName).UatDirectory);
    }
}

public sealed record UatSkipRule
{
    public UatSkipRule(string tag, string environmentVariable)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(environmentVariable);

        Tag = UatTagConventions.NormalizeTag(tag);
        EnvironmentVariable = environmentVariable.Trim();
    }

    public string Tag { get; init; }

    public string EnvironmentVariable { get; init; }
}

public sealed record UatSkipDecision(bool ShouldSkip, string? Reason, UatSkipRule? Rule)
{
    public static UatSkipDecision Run { get; } = new(false, null, null);
}

public static class UatMetadataFields
{
    public const string App = "App";
    public const string Area = "Area";
    public const string Target = "Target";
    public const string Tags = "Tags";
    public const string Mode = "Mode";
    public const string Requires = "Requires";
    public const string Owner = "Owner";
    public const string Priority = "Priority";
    public const string Evidence = "Evidence";
}

public static class UatTagConventions
{
    public const string Smoke = "smoke";
    public const string Regression = "regression";
    public const string Automated = "automated";
    public const string SemiAutomated = "semi-automated";
    public const string Manual = "manual";
    public const string Hardware = "hardware";
    public const string LiveApi = "live-api";
    public const string Maui = "maui";
    public const string Windows = "windows";
    public const string Android = "android";
    public const string Ios = "ios";
    public const string Deterministic = "deterministic";
    public const string OpenAiLive = "openai-live";

    public static string NormalizeTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return tag.Trim().TrimStart('@').Trim();
    }
}

public static class UatConfigParser
{
    public static UatConfig Parse(string markdown, string? suiteName = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        Dictionary<string, string> runtime = new(StringComparer.OrdinalIgnoreCase);
        List<UatAssemblyRegistration> assemblies = [];
        Dictionary<string, string> discovery = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> reporting = new(StringComparer.OrdinalIgnoreCase);
        List<UatSkipRule> skipRules = [];

        var sections = ParseSections(markdown);
        if (sections.TryGetValue("Runtime", out var runtimeTable))
        {
            runtime = ToFieldValueDictionary(runtimeTable);
        }

        if (sections.TryGetValue("Assemblies", out var assembliesTable))
        {
            foreach (var row in assembliesTable.Rows)
            {
                if (row.Cells.TryGetValue("Kind", out var kind) &&
                    row.Cells.TryGetValue("Assembly", out var assembly) &&
                    kind.Length > 0 &&
                    assembly.Length > 0)
                {
                    assemblies.Add(new UatAssemblyRegistration(kind, assembly));
                }
            }
        }

        if (sections.TryGetValue("Discovery", out var discoveryTable))
        {
            discovery = ToFieldValueDictionary(discoveryTable);
        }

        if (sections.TryGetValue("Reporting", out var reportingTable))
        {
            reporting = ToFieldValueDictionary(reportingTable);
        }

        if (sections.TryGetValue("Skip Rules", out var skipRulesTable))
        {
            foreach (var row in skipRulesTable.Rows)
            {
                if (row.Cells.TryGetValue("Tag", out var tag) &&
                    row.Cells.TryGetValue("EnvironmentVariable", out var environmentVariable) &&
                    tag.Length > 0 &&
                    environmentVariable.Length > 0)
                {
                    skipRules.Add(new UatSkipRule(tag, environmentVariable));
                }
            }
        }

        return new UatConfig(
            runtime,
            assemblies,
            new UatDiscoverySettings(
                ReadBool(discovery, "RequireExplicitUatAttributes", defaultValue: false),
                ReadBool(discovery, "AllowNameInference", defaultValue: true)),
            new UatReportingSettings(
                ResolveReportingOutputDirectory(
                    ReadString(reporting, "OutputDirectory", string.Empty),
                    suiteName),
                ReadBool(reporting, "ScreenshotOnFailure", defaultValue: false),
                ReadBool(reporting, "IncludeRuntimeTrace", defaultValue: false)),
            skipRules);
    }

    public static UatConfig ParseFile(string filePath, string? suiteName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Parse(File.ReadAllText(filePath), suiteName);
    }

    private static Dictionary<string, UatTable> ParseSections(string markdown)
    {
        var lines = markdown
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        Dictionary<string, UatTable> sections = new(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                currentSection = trimmed[3..].Trim();
                continue;
            }

            if (currentSection is null ||
                !IsTableRow(trimmed) ||
                i + 1 >= lines.Length ||
                !IsSeparatorRow(lines[i + 1].Trim()))
            {
                continue;
            }

            var columns = SplitTableRow(trimmed);
            i += 2;
            List<UatTableRow> rows = [];
            while (i < lines.Length && IsTableRow(lines[i].Trim()))
            {
                var cells = SplitTableRow(lines[i].Trim());
                if (cells.Count == columns.Count)
                {
                    Dictionary<string, string> row = new(StringComparer.Ordinal);
                    for (var column = 0; column < columns.Count; column++)
                    {
                        row[columns[column]] = cells[column];
                    }

                    rows.Add(new UatTableRow(row, new UatSourceLocation(null, i + 1)));
                }

                i++;
            }

            sections[currentSection] = new UatTable(columns, rows, new UatSourceLocation(null, i + 1));
            i--;
        }

        return sections;
    }

    private static Dictionary<string, string> ToFieldValueDictionary(UatTable table)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (var row in table.Rows)
        {
            if (row.Cells.TryGetValue("Field", out var field) &&
                row.Cells.TryGetValue("Value", out var value) &&
                field.Length > 0)
            {
                values[field] = value;
            }
        }

        return values;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> values, string key, bool defaultValue)
    {
        return values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static string ReadString(IReadOnlyDictionary<string, string> values, string key, string defaultValue)
    {
        return values.TryGetValue(key, out var value) && value.Length > 0
            ? value
            : defaultValue;
    }

    private static string ResolveReportingOutputDirectory(string configured, string? suiteName)
    {
        var provider = DefaultTestArtifactPathProvider.Create(suiteName);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return provider.UatDirectory;
        }

        var expanded = configured
            .Replace("$(BrinellTestResults)", provider.SuiteDirectory, StringComparison.OrdinalIgnoreCase)
            .Replace("${BrinellTestResults}", provider.SuiteDirectory, StringComparison.OrdinalIgnoreCase);

        return Path.GetFullPath(
            Path.IsPathRooted(expanded)
                ? expanded
                : Path.Combine(provider.SuiteDirectory, expanded));
    }

    public static bool IsEnabled(string? value)
    {
        return value is not null &&
            (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("on", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTableRow(string line)
    {
        return line.StartsWith('|') && line.EndsWith('|') && line.Count(x => x == '|') >= 2;
    }

    private static bool IsSeparatorRow(string line)
    {
        return IsTableRow(line) && SplitTableRow(line).All(cell =>
        {
            var trimmed = cell.Replace(" ", string.Empty, StringComparison.Ordinal);
            return trimmed.Length >= 3 && trimmed.All(x => x is '-' or ':');
        });
    }

    private static IReadOnlyList<string> SplitTableRow(string line)
    {
        return line.Trim()[1..^1].Split('|').Select(x => x.Trim()).ToArray();
    }
}
