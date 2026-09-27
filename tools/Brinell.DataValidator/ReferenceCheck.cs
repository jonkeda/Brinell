using System.Text.RegularExpressions;
using Brinell.TestData;
using Brinell.Uat;

namespace Brinell.DataValidator;

internal static class ReferenceCheck
{
    public static IReadOnlyList<TestDataReference> Find(string configPath)
    {
        List<TestDataReference> references = [];
        var config = UatConfigParser.Parse(File.ReadAllText(configPath));
        if (config.Runtime.TryGetValue(UatConfigFields.DefaultTestData, out var fallback) && fallback.Length > 0)
        {
            references.Add(new TestDataReference(configPath, LineOf(configPath, UatConfigFields.DefaultTestData), fallback));
        }

        var scenarios = Path.Combine(Path.GetDirectoryName(configPath)!, "Scenarios");
        if (!Directory.Exists(scenarios))
        {
            return references;
        }

        foreach (var file in Directory.EnumerateFiles(scenarios, "*.uat.md", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var document = UatMarkdownParser.ParseFile(file).Document;
            if (document?.Metadata.TryGetValue(UatMetadataFields.TestData, out var name) == true && name.Length > 0)
            {
                references.Add(new TestDataReference(file, LineOf(file, UatMetadataFields.TestData), name));
            }
        }

        return references;
    }

    public static IEnumerable<TestDataDiagnostic> Check(IEnumerable<TestDataReference> references, IReadOnlyCollection<string> known)
        => references
            .Where(reference => !known.Contains(reference.Name))
            .Select(reference => new TestDataDiagnostic(
                reference.File,
                reference.Line,
                TestDataCodes.UnknownTestSet,
                null,
                $"TestData '{reference.Name}' does not exist.{Hint(reference.Name, known)}"));

    private static string Hint(string name, IReadOnlyCollection<string> known)
        => TestDataTypes.Suggest(name, known) is { } suggestion
            ? $" Did you mean '{suggestion}'?"
            : $" There are: {string.Join(", ", known)}.";

    private static int? LineOf(string file, string field)
    {
        var lines = File.ReadAllLines(file);
        var index = Array.FindIndex(lines, line => FieldRow(field).IsMatch(line));
        return index >= 0 ? index + 1 : null;
    }

    private static Regex FieldRow(string field) => new($"^\\s*\\|\\s*{Regex.Escape(field)}\\s*\\|");
}

internal sealed record TestDataReference(string File, int? Line, string Name);
