using Brinell.Core.Testing;

namespace Brinell.Uat.Workspace;

/// <summary>A suite a tool cannot load, with the reason in words a developer can act on.</summary>
/// <param name="message">What is wrong, and what to do.</param>
public sealed class UatWorkspaceException(string message) : Exception(message);

/// <summary>
/// One suite: a project with a <c>uat.config.md</c>, or a <c>bdd.config.md</c> for a
/// <c>service</c> suite. It reads the config, lists the suite's feature and step files, and builds
/// its catalogs from a shadow copy of the build output, so a build never meets a locked file (AD-018).
/// </summary>
public sealed class UatSuite : IDisposable
{
    private const string PagesKind = "Pages";

    // A service suite has no pages; its fixture and steps are in a Steps assembly.
    private const string StepsKind = "Steps";

    private readonly object _gate = new();
    private UatSuiteLoad? _load;

    private UatSuite(string configPath)
    {
        ConfigPath = Path.GetFullPath(configPath);
        ProjectDirectory = Path.GetDirectoryName(ConfigPath)!;
    }

    /// <summary>The config's full path.</summary>
    public string ConfigPath { get; }

    /// <summary>The folder of the config: the suite's project.</summary>
    public string ProjectDirectory { get; }

    /// <summary>The project folder's name, which is the suite's name in pickers and messages.</summary>
    public string Name => Path.GetFileName(ProjectDirectory);

    /// <summary>The config's <c>Head</c>, or null.</summary>
    public string? Head => ReadConfig().SuiteHead;

    // The loaded assemblies' context, for a test that Reload releases it.
    internal WeakReference? LoadContext => _load?.Context;

    /// <summary>Whether the suite's catalogs are loaded now.</summary>
    public bool IsLoaded => _load is not null;

    /// <summary>Opens the suite of a config file. Nothing is loaded until <see cref="Catalogs"/> is asked for.</summary>
    /// <param name="configPath">A <c>uat.config.md</c> or <c>bdd.config.md</c>.</param>
    public static UatSuite Open(string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        return File.Exists(configPath)
            ? new UatSuite(configPath)
            : throw new UatWorkspaceException($"'{configPath}' does not exist.");
    }

    /// <summary>
    /// The config as the suite reads it. An <c>AppsFile</c> the build copies (<c>apps.md</c> from
    /// a shared project) is found in the project's output when it is not beside the config.
    /// </summary>
    public UatConfig ReadConfig()
    {
        var config = UatConfigParser.Parse(File.ReadAllText(ConfigPath));
        if (config.Apps.Count > 0 || !config.Runtime.TryGetValue(UatConfigFields.AppsFile, out var appsFile) || appsFile.Length == 0)
        {
            return config;
        }

        var path = Path.GetFullPath(Path.Combine(ProjectDirectory, appsFile));
        if (!File.Exists(path))
        {
            var bin = Path.Combine(ProjectDirectory, "bin");
            path = (Directory.Exists(bin) ? Directory.EnumerateFiles(bin, Path.GetFileName(appsFile), SearchOption.AllDirectories) : [])
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
                ?? throw new UatWorkspaceException($"{ConfigPath} names the AppsFile '{appsFile}', which is neither beside it nor in its build output. Build the UAT project first.");
        }

        return config with
        {
            Apps = TestAppDefinition.ReadMarkdownFile(path).Select(app => new UatAppConfig(app.Name, app.Values)).ToArray(),
        };
    }

    /// <summary>Whether the config names a fixture and an assembly to find it in, so the suite's steps can be bound.</summary>
    public bool CanBind
    {
        get
        {
            var config = UatConfigParser.Parse(File.ReadAllText(ConfigPath));
            return FixtureName(config) is not null && config.Assemblies.Any(NamesFixtureAssembly);
        }
    }

    /// <summary>The folder of feature and step files beside the config.</summary>
    public string FeatureFolder => Path.Combine(ProjectDirectory, UatFileFormat.FeatureFolder);

    /// <summary>The feature files in the project's own <c>Features</c> folder and its subfolders.</summary>
    public IReadOnlyList<string> FeatureFiles => FilesIn(FeatureFolder, UatFileFormat.FeatureFilePattern);

    /// <summary>The step files: <c>*.steps.md</c> in the project's own <c>Features</c> folder and its subfolders.</summary>
    public IReadOnlyList<string> StepFiles => FilesIn(FeatureFolder, UatFileFormat.StepFilePattern);

    /// <summary>
    /// The feature and step files the project links from another project into its <c>Features</c>
    /// folder (AD-014): the MAUI suite's files, run on the web head.
    /// </summary>
    public IReadOnlyList<string> LinkedFiles => UatLinkedFiles.Of(ProjectDirectory);

    /// <summary>
    /// Whether a file belongs to the suite: it is in the project's folder (outside its build
    /// output), or the project links it.
    /// </summary>
    /// <param name="filePath">A feature or step file.</param>
    public bool Contains(string filePath)
    {
        var path = Path.GetFullPath(filePath);
        if (path.StartsWith(ProjectDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            var relative = Path.GetRelativePath(ProjectDirectory, path);
            return !relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                   !relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        return LinkedFiles.Contains(path, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A warning when the project has step files its csproj does not copy: the gate reads the
    /// build output, so its calls would bind to nothing there while a tool reading the project
    /// folder passes.
    /// </summary>
    public string? CopyWarning()
    {
        if (StepFiles.Count == 0)
        {
            return null;
        }

        var copies = Directory.EnumerateFiles(ProjectDirectory, "*.csproj")
            .Any(csproj => File.ReadAllText(csproj).Contains(UatFileFormat.StepsExtension, StringComparison.OrdinalIgnoreCase));
        return copies
            ? null
            : "The Features folder has *.steps.md files that the project does not copy to its output, so the xUnit gate cannot see them. Add <None Update=\"Features\\**\\*.steps.md\" CopyToOutputDirectory=\"PreserveNewest\" /> to the csproj.";
    }

    /// <summary>
    /// The catalogs the suite's steps bind against, built from its fixture type without starting
    /// it. Loaded once, from a shadow copy of the build output, and kept until <see cref="Reload"/>.
    /// </summary>
    /// <exception cref="UatWorkspaceException">The config names no fixture, or nothing is built yet.</exception>
    public UatCatalogSet Catalogs
    {
        get
        {
            lock (_gate)
            {
                _load ??= Load();
                return _load.Catalogs;
            }
        }
    }

    /// <summary>The catalogs, or null with the reason when the suite cannot be bound yet (no fixture named, not built).</summary>
    /// <param name="reason">Why there are no catalogs.</param>
    public UatCatalogSet? TryGetCatalogs(out string? reason)
    {
        try
        {
            reason = null;
            return Catalogs;
        }
        catch (UatWorkspaceException exception)
        {
            reason = exception.Message;
            return null;
        }
    }

    /// <summary>
    /// Drops the loaded catalogs and unloads their assemblies, for after a build. The next
    /// <see cref="Catalogs"/> loads the new build output.
    /// </summary>
    public void Reload()
    {
        UatSuiteLoad? old;
        lock (_gate)
        {
            old = _load;
            _load = null;
        }

        old?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose() => Reload();

    private UatSuiteLoad Load()
    {
        var config = ReadConfig();
        var fixtureName = FixtureName(config)
            ?? throw new UatWorkspaceException($"{ConfigPath} names no Fixture in its Runtime table, so the scenarios cannot be checked.");

        // The fixture is in the UAT project's own assembly or in one of the Pages or Steps assemblies.
        var candidates = config.Assemblies
            .Where(NamesFixtureAssembly)
            .Select(a => Path.GetFullPath(Path.Combine(ProjectDirectory, a.Assembly)))
            .Concat(OwnAssemblies())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(File.Exists)
            .ToList();
        if (candidates.Count == 0)
        {
            throw new UatWorkspaceException($"No assembly of {ConfigPath} is built yet. Build the UAT project first.");
        }

        var load = new UatSuiteLoad();
        try
        {
            foreach (var path in candidates)
            {
                if (load.TypesOf(path).FirstOrDefault(type => type.Name == fixtureName || type.FullName == fixtureName) is { } fixture)
                {
                    load.Catalogs = UatScenarioCheck.CatalogsFor(fixture, config);
                    return load;
                }
            }
        }
        catch
        {
            load.Dispose();
            throw;
        }

        load.Dispose();
        throw new UatWorkspaceException($"No fixture '{fixtureName}' (the config's Runtime Fixture) in {string.Join(", ", candidates.Select(Path.GetFileName))}.");
    }

    private static string? FixtureName(UatConfig config)
        => config.Runtime.TryGetValue("Fixture", out var name) && name.Trim() is { Length: > 0 } trimmed ? trimmed : null;

    private static bool NamesFixtureAssembly(UatAssemblyRegistration assembly)
        => string.Equals(assembly.Kind, PagesKind, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(assembly.Kind, StepsKind, StringComparison.OrdinalIgnoreCase);

    private IEnumerable<string> OwnAssemblies()
    {
        var bin = Path.Combine(ProjectDirectory, "bin");
        return Directory.Exists(bin)
            ? Directory.EnumerateFiles(bin, Name + ".dll", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc)
            : [];
    }

    private static IReadOnlyList<string> FilesIn(string folder, string pattern)
        => Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList()
            : [];
}
