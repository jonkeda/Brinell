namespace Brinell.Uat;

/// <summary>A scenario file with the callable phrases it can see, ready to bind.</summary>
/// <param name="Document">The parsed file; null when it has no <c># Feature:</c> heading.</param>
/// <param name="Callables">
/// The file's own callables and those of every phrase file under its scenario root. The binder
/// binds a step only to those the file sees (<see cref="UatScenarioFiles.PhraseFilesFor"/>), and
/// names where the others are.
/// </param>
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
/// the <c>*.steps.md</c> files of its folder and every folder above it, up to the scenario root.
/// </summary>
public static class UatScenarioFiles
{
    /// <summary>The name ending of a phrase file.</summary>
    public const string PhraseFilePattern = UatFileFormat.StepFilePattern;

    private const string ScenarioFolder = UatFileFormat.FeatureFolder;

    private static readonly Dictionary<string, (DateTime Written, UatPhrasesParseResult Parse)> PhraseFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Parses the file and the phrase files it can see.</summary>
    /// <param name="filePath">A <c>.feature.md</c> file.</param>
    public static UatLoadedScenarioFile Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var parse = UatMarkdownParser.ParseFile(filePath);
        var seen = PhraseFilesFor(filePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var phraseFiles = AllPhraseFiles(filePath).Select(path => (Seen: seen.Contains(path), Parse: ParsePhraseFile(path))).ToList();

        // The file's own callables are seen by the file alone, and their bodies bind in its folder,
        // with its usings.
        var fullPath = Path.GetFullPath(filePath);
        var here = UatNamespaces.Of(fullPath);
        var usings = UatNamespaces.ParseList(parse.Document?.Metadata.GetValueOrDefault(UatMetadataFields.Using));
        List<UatCallable> callables = [.. (parse.Document?.Callables ?? []).Select(callable => callable with { Namespace = here, LocalTo = fullPath, Usings = usings })];
        callables.AddRange(phraseFiles.SelectMany(file => file.Parse.Document?.Callables ?? []));

        // Only a phrase file the scenario sees can break it; another folder's is that folder's problem.
        return new UatLoadedScenarioFile(
            parse.Document,
            new UatCallableSet(callables),
            [.. parse.Diagnostics, .. phraseFiles.Where(file => file.Seen).SelectMany(file => file.Parse.Diagnostics)]);
    }

    /// <summary>
    /// The scenario root of a file: the nearest folder named <c>Features</c> that holds it, the
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

    /// <summary>
    /// The phrase files a scenario file sees: the <c>*.steps.md</c> in its scenario root and in
    /// each folder down to its own, root first. A sibling folder's are not seen (D6).
    /// </summary>
    public static IReadOnlyList<string> PhraseFilesFor(string filePath)
    {
        var root = ScenarioRoot(filePath);
        List<string> folders = [];
        for (var folder = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(filePath))!); folder is not null; folder = folder.Parent)
        {
            folders.Insert(0, folder.FullName);
            if (string.Equals(folder.FullName, root, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return folders
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, PhraseFilePattern, SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    // Every phrase file under the scenario root, so a step that calls another folder's callable
    // is told where it is rather than that it does not exist.
    private static IReadOnlyList<string> AllPhraseFiles(string filePath)
    {
        var root = ScenarioRoot(filePath);
        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, PhraseFilePattern, SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase).ToArray()
            : [];
    }

    /// <summary>A phrase file, parsed, its callables in its folder's namespace; read again only when it changed.</summary>
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
        if (parse.Document is { } document)
        {
            var folder = UatNamespaces.Of(fullPath);
            parse = parse with { Document = document with { Callables = [.. document.Callables.Select(callable => callable with { Namespace = folder })] } };
        }

        lock (PhraseFiles)
        {
            PhraseFiles[fullPath] = (written, parse);
        }

        return parse;
    }
}
