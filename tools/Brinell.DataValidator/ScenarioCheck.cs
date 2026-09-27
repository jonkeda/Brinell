using System.Reflection;
using System.Runtime.Loader;
using Brinell.Uat;

namespace Brinell.DataValidator;

// The scenario side of the tool: the suite's fixture type, loaded next to the tool's own Brinell.Uat,
// and the same checks the xUnit gate runs (AD-016).
internal static class ScenarioCheck
{
    private const string PagesKind = "Pages";

    // A service suite has no pages; its fixture and steps are in a Steps assembly.
    private const string StepsKind = "Steps";

    private static bool NamesFixtureAssembly(UatAssemblyRegistration assembly)
        => string.Equals(assembly.Kind, PagesKind, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(assembly.Kind, StepsKind, StringComparison.OrdinalIgnoreCase);

    // The config as the suite reads it. An AppsFile the build copies (apps.md from a shared project)
    // is found in the project's output when it is not beside the config.
    public static UatConfig ReadConfig(string configPath)
    {
        var config = UatConfigParser.Parse(File.ReadAllText(configPath));
        if (config.Apps.Count > 0 || !config.Runtime.TryGetValue(UatConfigFields.AppsFile, out var appsFile) || appsFile.Length == 0)
        {
            return config;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(configPath))!;
        var path = Path.GetFullPath(Path.Combine(directory, appsFile));
        if (!File.Exists(path))
        {
            var bin = Path.Combine(directory, "bin");
            path = (Directory.Exists(bin) ? Directory.EnumerateFiles(bin, Path.GetFileName(appsFile), SearchOption.AllDirectories) : [])
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
                ?? throw new ToolException($"{configPath} names the AppsFile '{appsFile}', which is neither beside it nor in its build output. Build the UAT project first.");
        }

        return config with
        {
            Apps = Brinell.Core.Testing.TestAppDefinition.ReadMarkdownFile(path).Select(app => new UatAppConfig(app.Name, app.Values)).ToArray(),
        };
    }

    public static bool CanCheck(string configPath)
    {
        var config = UatConfigParser.Parse(File.ReadAllText(configPath));
        return config.Runtime.TryGetValue("Fixture", out var fixture) && fixture.Trim().Length > 0 &&
               config.Assemblies.Any(NamesFixtureAssembly);
    }

    public static UatCatalogSet Catalogs(string configPath)
    {
        var config = ReadConfig(configPath);
        var fixtureName = config.Runtime.TryGetValue("Fixture", out var name) && name.Trim() is { Length: > 0 } trimmed
            ? trimmed
            : throw new ToolException($"{configPath} names no Fixture in its Runtime table, so the scenarios cannot be checked.");

        // The fixture is in the UAT project's own assembly or in one of the Pages or Steps assemblies.
        var directory = Path.GetDirectoryName(Path.GetFullPath(configPath))!;
        var candidates = config.Assemblies
            .Where(NamesFixtureAssembly)
            .Select(a => Path.GetFullPath(Path.Combine(directory, a.Assembly)))
            .Concat(OwnAssemblies(directory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(File.Exists)
            .ToList();
        if (candidates.Count == 0)
        {
            throw new ToolException($"No assembly of {configPath} is built yet. Build the UAT project first.");
        }

        foreach (var path in candidates)
        {
            if (TypesOf(Load(path)).FirstOrDefault(type => type.Name == fixtureName || type.FullName == fixtureName) is { } fixture)
            {
                return UatScenarioCheck.CatalogsFor(fixture, config);
            }
        }

        throw new ToolException($"No fixture '{fixtureName}' (the config's Runtime Fixture) in {string.Join(", ", candidates.Select(Path.GetFileName))}.");
    }

    /// <summary>The folder of feature and step files beside the config.</summary>
    public static string ScenarioFolder(string configPath) => Path.Combine(Path.GetDirectoryName(configPath)!, UatFileFormat.FeatureFolder);

    /// <summary>The step files: <c>*.steps.md</c> in the project's Features folder and its subfolders.</summary>
    public static IReadOnlyList<string> PhraseFiles(string configPath)
    {
        var scenarios = ScenarioFolder(configPath);
        return Directory.Exists(scenarios)
            ? Directory.EnumerateFiles(scenarios, UatScenarioFiles.PhraseFilePattern, SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList()
            : [];
    }

    /// <summary>
    /// A warning when the project has phrase files its csproj does not copy: the gate reads the
    /// build output, so its calls would bind to nothing there while this tool, reading the project
    /// folder, passes.
    /// </summary>
    public static string? CopyWarning(string configPath)
    {
        var project = Path.GetDirectoryName(configPath)!;
        if (PhraseFiles(configPath).Count == 0)
        {
            return null;
        }

        var copies = Directory.EnumerateFiles(project, "*.csproj")
            .Any(csproj => File.ReadAllText(csproj).Contains(".steps.md", StringComparison.OrdinalIgnoreCase));
        return copies
            ? null
            : "The Features folder has *.steps.md files that the project does not copy to its output, so the xUnit gate cannot see them. Add <None Update=\"Features\\**\\*.steps.md\" CopyToOutputDirectory=\"PreserveNewest\" /> to the csproj.";
    }

    public static IReadOnlyList<string> Files(string configPath)
    {
        var scenarios = ScenarioFolder(configPath);
        return Directory.Exists(scenarios)
            ? Directory.EnumerateFiles(scenarios, UatFileFormat.FeatureFilePattern, SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList()
            : [];
    }

    private static IEnumerable<string> OwnAssemblies(string projectDirectory)
    {
        var bin = Path.Combine(projectDirectory, "bin");
        return Directory.Exists(bin)
            ? Directory.EnumerateFiles(bin, Path.GetFileName(projectDirectory) + ".dll", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc)
            : [];
    }

    private static Assembly Load(string path)
    {
        // The tool's own Brinell assemblies may need what only the suite ships (xunit for Brinell.Core).
        var folder = Path.GetDirectoryName(path)!;
        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
            File.Exists(Path.Combine(folder, assemblyName.Name + ".dll")) ? context.LoadFromAssemblyPath(Path.Combine(folder, assemblyName.Name + ".dll")) : null;

        try
        {
            return new SuiteLoadContext(path).LoadFromAssemblyPath(path);
        }
        catch (Exception exception) when (exception is BadImageFormatException or FileLoadException)
        {
            throw new ToolException($"'{path}' cannot be loaded ({exception.Message}).");
        }
    }

    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    // An assembly the tool ships (Brinell.Uat, Brinell.Core, DI) comes from the tool, so the suite's
    // attributes and interfaces are the tool's types; everything else from the suite's own folder.
    private sealed class SuiteLoadContext(string mainAssemblyPath) : AssemblyLoadContext(isCollectible: false)
    {
        private readonly AssemblyDependencyResolver _resolver = new(Path.GetFullPath(mainAssemblyPath));
        private readonly string _folder = Path.GetDirectoryName(Path.GetFullPath(mainAssemblyPath))!;

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, assemblyName.Name + ".dll")))
            {
                return null;
            }

            var path = _resolver.ResolveAssemblyToPath(assemblyName) ?? Path.Combine(_folder, assemblyName.Name + ".dll");
            return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            => _resolver.ResolveUnmanagedDllToPath(unmanagedDllName) is { } path ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
