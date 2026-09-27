namespace Brinell.Uat.Preview;

/// <summary>
/// The gate's check of one feature or step file, as its text is now: the parse, and with the suite's
/// catalogs the bind. The preview draws it, and the language server reports and navigates it, so
/// both say what the gate would say.
/// </summary>
/// <param name="FilePath">The file checked.</param>
/// <param name="IsSteps">Whether it is a step file (<c>*.steps.md</c>).</param>
/// <param name="Diagnostics">The parse (and, when bound, bind) diagnostics; some may be in another file.</param>
/// <param name="Scenarios">Each scenario (an outline's each row) as the binder bound it; empty when not bound.</param>
/// <param name="Callables">A step file's callable steps; empty for a feature file.</param>
public sealed record UatFileAnalysis(
    string FilePath,
    bool IsSteps,
    IReadOnlyList<UatDiagnostic> Diagnostics,
    IReadOnlyList<UatBoundScenario> Scenarios,
    IReadOnlyList<UatCallable> Callables)
{
    /// <summary>Checks <paramref name="text"/>, the content of <paramref name="filePath"/>.</summary>
    /// <param name="text">The file's text, which may be unsaved.</param>
    /// <param name="filePath">The file's path, which says whether it is a feature or a step file.</param>
    /// <param name="catalogs">The suite's catalogs; null to parse only.</param>
    public static UatFileAnalysis Analyze(string text, string filePath, UatCatalogSet? catalogs)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var isSteps = IsStepFile(filePath);
        List<UatDiagnostic> diagnostics = [];
        if (UatFileFormat.NewFileName(filePath) is { } renamed)
        {
            diagnostics.Add(new UatDiagnostic(
                UatDiagnosticSeverity.Error,
                UatFileFormat.OldFormCode,
                $"'{Path.GetFileName(filePath)}' is the old name of '{Path.GetFileName(renamed)}'. Run 'brinell-data validate --fix' to rename it.",
                new UatSourceLocation(filePath, 1)));
        }

        if (isSteps)
        {
            var parse = UatScenarioFiles.ParsePhrases(filePath, text);
            diagnostics.AddRange(catalogs is null ? parse.Diagnostics : UatScenarioFiles.CheckPhrases(parse, catalogs));
            return new UatFileAnalysis(filePath, true, diagnostics, [], parse.Document?.Callables ?? []);
        }

        if (catalogs is null)
        {
            diagnostics.AddRange(UatMarkdownParser.Parse(text, filePath).Diagnostics);
            return new UatFileAnalysis(filePath, false, diagnostics, [], []);
        }

        // The gate's own check, so a step shows the words the gate would fail with.
        var check = UatScenarioFiles.Load(filePath, text).Check(catalogs);
        diagnostics.AddRange(check.Diagnostics);
        return new UatFileAnalysis(filePath, false, diagnostics, check.Bind?.Scenarios ?? [], []);
    }

    /// <summary>Whether a path is a step file (or one in the old form).</summary>
    /// <param name="filePath">A feature or step file.</param>
    public static bool IsStepFile(string filePath)
        => filePath.EndsWith(UatFileFormat.StepsExtension, StringComparison.OrdinalIgnoreCase) ||
           filePath.EndsWith(".phrases.md", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a diagnostic is in this file, rather than in a step file it sees.</summary>
    /// <param name="diagnostic">One of <see cref="Diagnostics"/>.</param>
    public bool IsHere(UatDiagnostic diagnostic)
        => diagnostic.Location.FilePath is not { } file || string.Equals(Path.GetFullPath(file), Path.GetFullPath(FilePath), StringComparison.OrdinalIgnoreCase);
}
