namespace Brinell.Uat;

public abstract class UatSpecFormatTestBase
{
    protected virtual IReadOnlyList<string> RequiredMetadata { get; } =
    [
        UatMetadataFields.App,
        UatMetadataFields.Area,
        UatMetadataFields.Target,
        UatMetadataFields.Tags,
        UatMetadataFields.Mode,
        UatMetadataFields.Requires,
        UatMetadataFields.Priority,
        UatMetadataFields.Evidence
    ];

    protected virtual string? ExpectedApp => null;

    protected virtual string? ExpectedTarget => null;

    protected virtual string ConfigFilePath => UatScenarioSource.GetConfigFilePath();

    protected virtual Type? RuntimeRootType => null;

    protected static IEnumerable<object[]> GetScenarioFiles(
        string folderName = "Scenarios",
        string? filterEnvironmentVariable = null)
    {
        return UatScenarioSource.GetScenarioFileTheoryData(
            folderName,
            filterEnvironmentVariable: filterEnvironmentVariable);
    }

    protected void AssertUatFileParsesAndContainsRequiredMetadata(string filePath)
    {
        var parse = UatMarkdownParser.ParseFile(filePath);

        if (!parse.Success || parse.Document is null)
        {
            throw new InvalidOperationException(
                UatDiagnosticsFormatter.FormatDiagnostics(parse.Diagnostics));
        }

        foreach (var field in RequiredMetadata)
        {
            if (!parse.Document.Metadata.TryGetValue(field, out var value) ||
                string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{Path.GetFileName(filePath)} must set metadata field '{field}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(ExpectedApp))
        {
            AssertMetadataEquals(parse.Document, filePath, UatMetadataFields.App, ExpectedApp);
        }

        if (!string.IsNullOrWhiteSpace(ExpectedTarget))
        {
            AssertMetadataEquals(parse.Document, filePath, UatMetadataFields.Target, ExpectedTarget);
        }

        if (parse.Document.Scenarios.Count == 0)
        {
            throw new InvalidOperationException($"{Path.GetFileName(filePath)} must contain at least one scenario.");
        }

        var untagged = parse.Document.Scenarios.FirstOrDefault(scenario => scenario.Tags.Count == 0);
        if (untagged is not null)
        {
            throw new InvalidOperationException(
                $"{Path.GetFileName(filePath)} scenario '{untagged.Name}' must set at least one tag.");
        }
    }

    protected void AssertUatFileBindsThroughCatalog(string filePath)
    {
        var parse = UatMarkdownParser.ParseFile(filePath);
        if (!parse.Success || parse.Document is null)
        {
            throw new InvalidOperationException(
                UatDiagnosticsFormatter.FormatDiagnostics(parse.Diagnostics));
        }

        var bind = UatBinder.Bind(parse.Document, CreateCatalog());
        if (!bind.Success || bind.Document is null)
        {
            throw new InvalidOperationException(
                UatDiagnosticsFormatter.FormatDiagnostics(bind.Diagnostics));
        }
    }

    protected void AssertUatConfigParses()
    {
        AssertConfig(UatConfigParser.ParseFile(ConfigFilePath, GetReportingSuiteName()));
    }

    protected virtual UatCommandCatalog CreateCatalog()
    {
        var catalog = UatSpecCommandCatalog.CreateDefault();
        if (RuntimeRootType is { } runtimeRootType)
        {
            UatReflectionRuntime.RegisterCompositionPhrases(catalog, runtimeRootType);
            UatReflectionRuntime.RegisterRootPhrases(catalog, runtimeRootType);
        }

        return catalog;
    }

    protected virtual void AssertConfig(UatConfig config)
    {
    }

    protected virtual string? GetReportingSuiteName()
    {
        return RuntimeRootType?.Assembly.GetName().Name ?? GetType().Assembly.GetName().Name;
    }

    private static void AssertMetadataEquals(
        UatDocument document,
        string filePath,
        string field,
        string expected)
    {
        if (!document.Metadata.TryGetValue(field, out var actual) ||
            !actual.Equals(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{Path.GetFileName(filePath)} must set metadata field '{field}' to '{expected}'. Actual value: '{actual ?? "(missing)"}'.");
        }
    }
}
