namespace Brinell.Uat;

/// <summary>A scenario file with the callable phrases it can see, ready to bind.</summary>
/// <param name="Document">The parsed file; null when it has no <c># UAT:</c> heading.</param>
/// <param name="Callables">The file's own callables and the global phrase files'.</param>
/// <param name="Diagnostics">The parse problems of the file and of the phrase files it sees.</param>
public sealed record UatLoadedScenarioFile(
    UatDocument? Document,
    UatCallableSet Callables,
    IReadOnlyList<UatDiagnostic> Diagnostics)
{
    /// <summary>Whether the file and its phrase files parsed without errors.</summary>
    public bool Success => Document is not null && Diagnostics.All(x => x.Severity != UatDiagnosticSeverity.Error);

    /// <summary>Binds the file against <paramref name="catalogs"/>, with its callables.</summary>
    public UatBindResult Bind(UatCatalogSet catalogs)
        => UatBinder.Bind(Document ?? throw new InvalidOperationException("The file did not parse."), catalogs, Callables);
}

/// <summary>
/// The one loader of scenario files, for the xUnit gate, the tool and the Presenter: the file, and
/// the global <c>*.phrases.md</c> files at its scenario root.
/// </summary>
public static class UatScenarioFiles
{
    /// <summary>The name ending of a phrase file.</summary>
    public const string PhraseFilePattern = "*.phrases.md";

    private const string ScenarioFolder = "Scenarios";

    private static readonly Dictionary<string, (DateTime Written, UatPhrasesParseResult Parse)> PhraseFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Parses the file and the phrase files it can see.</summary>
    /// <param name="filePath">A <c>.uat.md</c> file.</param>
    public static UatLoadedScenarioFile Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var parse = UatMarkdownParser.ParseFile(filePath);
        var phraseFiles = PhraseFilesFor(filePath).Select(ParsePhraseFile).ToList();

        List<UatCallable> callables = [.. parse.Document?.Callables ?? []];
        callables.AddRange(phraseFiles.SelectMany(file => file.Document?.Callables ?? []));
        return new UatLoadedScenarioFile(
            parse.Document,
            new UatCallableSet(callables),
            [.. parse.Diagnostics, .. phraseFiles.SelectMany(file => file.Diagnostics)]);
    }

    /// <summary>
    /// The scenario root of a file: the nearest folder named <c>Scenarios</c> that holds it, the
    /// folder itself included; the file's own folder when there is none.
    /// </summary>
    public static string ScenarioRoot(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var folder = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        for (var candidate = new DirectoryInfo(folder); candidate is not null; candidate = candidate.Parent)
        {
            if (candidate.Name.Equals(ScenarioFolder, StringComparison.OrdinalIgnoreCase))
            {
                return candidate.FullName;
            }
        }

        return folder;
    }

    /// <summary>The global phrase files a scenario file sees: the <c>*.phrases.md</c> directly in its scenario root.</summary>
    public static IReadOnlyList<string> PhraseFilesFor(string filePath)
    {
        var root = ScenarioRoot(filePath);
        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, PhraseFilePattern, SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase).ToArray()
            : [];
    }

    /// <summary>A phrase file, parsed; read again only when it changed.</summary>
    public static UatPhrasesParseResult ParsePhraseFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var fullPath = Path.GetFullPath(filePath);
        var written = File.GetLastWriteTimeUtc(fullPath);
        lock (PhraseFiles)
        {
            if (PhraseFiles.TryGetValue(fullPath, out var cached) && cached.Written == written)
            {
                return cached.Parse;
            }
        }

        var parse = UatMarkdownParser.ParsePhrases(File.ReadAllText(fullPath), filePath);
        lock (PhraseFiles)
        {
            PhraseFiles[fullPath] = (written, parse);
        }

        return parse;
    }
}
