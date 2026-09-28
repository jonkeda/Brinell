namespace Brinell.Uat;

public abstract class UatSpecFormatTestBase
{
    protected virtual IReadOnlyList<string> RequiredMetadata { get; } =
    [
        BddMetadataFields.App,
        BddMetadataFields.Area,
        BddMetadataFields.Target,
        BddMetadataFields.Tags,
        BddMetadataFields.Mode,
        BddMetadataFields.Requires,
        BddMetadataFields.Priority,
        BddMetadataFields.Evidence
    ];

    protected virtual string? ExpectedApp => null;

    protected virtual string? ExpectedTarget => null;

    protected virtual string ConfigFilePath => BddScenarioSource.GetConfigFilePath();

    protected virtual Type? RuntimeRootType => null;

    protected static IEnumerable<object[]> GetScenarioFiles(
        string folderName = BddFileFormat.FeatureFolder,
        string? configurationFilter = null)
    {
        return BddScenarioSource.GetScenarioFileTheoryData(
            folderName,
            configurationFilter: configurationFilter);
    }

    protected void AssertUatFileParsesAndContainsRequiredMetadata(string filePath)
    {
        var parse = BddMarkdownParser.ParseFile(filePath);

        if (!parse.Success || parse.Document is null)
        {
            throw new InvalidOperationException(
                BddDiagnosticsFormatter.FormatDiagnostics(parse.Diagnostics));
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
            AssertMetadataEquals(parse.Document, filePath, BddMetadataFields.App, ExpectedApp);
        }

        if (!string.IsNullOrWhiteSpace(ExpectedTarget))
        {
            AssertMetadataEquals(parse.Document, filePath, BddMetadataFields.Target, ExpectedTarget);
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
        var file = BddScenarioFiles.Load(filePath);
        if (!file.Success)
        {
            throw new InvalidOperationException(
                BddDiagnosticsFormatter.FormatDiagnostics(file.Diagnostics));
        }

        var bind = file.Bind(new BddCatalogSet(CreateCatalog()));
        if (!bind.Success || bind.Document is null)
        {
            throw new InvalidOperationException(
                BddDiagnosticsFormatter.FormatDiagnostics(bind.Diagnostics));
        }
    }

    protected void AssertUatConfigParses()
    {
        AssertConfig(BddConfigParser.ParseFile(ConfigFilePath, GetReportingSuiteName()));
    }

    protected virtual BddCommandCatalog CreateCatalog()
    {
        var catalog = UatSpecCommandCatalog.CreateDefault();
        if (RuntimeRootType is { } runtimeRootType)
        {
            UatReflectionRuntime.RegisterCompositionPhrases(catalog, runtimeRootType);
            UatReflectionRuntime.RegisterRootPhrases(catalog, runtimeRootType);
            catalog.IsGeneric = false;
        }

        return catalog;
    }

    protected virtual void AssertConfig(BddConfig config)
    {
    }

    protected virtual string? GetReportingSuiteName()
    {
        return RuntimeRootType?.Assembly.GetName().Name ?? GetType().Assembly.GetName().Name;
    }

    private static void AssertMetadataEquals(
        BddDocument document,
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
