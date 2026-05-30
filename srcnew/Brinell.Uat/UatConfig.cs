namespace Brinell.Uat;

public sealed record UatConfig(
    IReadOnlyDictionary<string, string> Runtime,
    IReadOnlyList<UatAssemblyRegistration> Assemblies,
    UatDiscoverySettings Discovery);

public sealed record UatAssemblyRegistration(string Kind, string Assembly);

public sealed record UatDiscoverySettings(
    bool RequireExplicitUatAttributes = false,
    bool AllowNameInference = true);

public static class UatConfigParser
{
    public static UatConfig Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        Dictionary<string, string> runtime = new(StringComparer.OrdinalIgnoreCase);
        List<UatAssemblyRegistration> assemblies = [];
        Dictionary<string, string> discovery = new(StringComparer.OrdinalIgnoreCase);

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

        return new UatConfig(
            runtime,
            assemblies,
            new UatDiscoverySettings(
                ReadBool(discovery, "RequireExplicitUatAttributes", defaultValue: false),
                ReadBool(discovery, "AllowNameInference", defaultValue: true)));
    }

    public static UatConfig ParseFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Parse(File.ReadAllText(filePath));
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
