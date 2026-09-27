using System.Reflection;
using System.Text.Json;
using Brinell.TestData;

namespace Brinell.Uat.Workspace;

// A test-data definition loaded from the shadow copy, placed where its build is: a definition that
// finds its folder from its assembly's location would otherwise look in %TEMP%. Everything else is
// the definition's own.
internal sealed class UatTestDataDefinition : ITestDataDefinition
{
    private readonly ITestDataDefinition _loaded;
    private readonly string _folder;

    private UatTestDataDefinition(ITestDataDefinition loaded, string folder, string? projectDirectory)
    {
        _loaded = loaded;
        _folder = folder;
        ProjectDirectory = projectDirectory;
    }

    public string Name => _loaded.Name;

    public Type SetType => _loaded.SetType;

    public string Folder => _folder;

    public string? ProjectDirectory { get; }

    public JsonSerializerOptions JsonOptions => _loaded.JsonOptions;

    public IReadOnlyList<Type> TableTypes => _loaded.TableTypes;

    public IReadOnlyList<string> TestSetNames => Directory.Exists(_folder)
        ? Directory.EnumerateFiles(_folder, "*.json")
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Order(StringComparer.Ordinal)
            .ToList()
        : [];

    public string FileOf(string name) => Path.Combine(_folder, name + ".json");

    public IReadOnlyList<TestDataDiagnostic> ValidateFile(string path) => _loaded.ValidateFile(path);

    /// <summary>
    /// The definition as its build places it: kept when its folder is outside the shadow copy
    /// (found beside its source file), else found from the project of <paramref name="builtAssembly"/>.
    /// </summary>
    public static ITestDataDefinition Placed(ITestDataDefinition loaded, string builtAssembly, string shadowCopyRoot)
    {
        if (!loaded.Folder.StartsWith(shadowCopyRoot, StringComparison.OrdinalIgnoreCase))
        {
            return loaded;
        }

        // RelativeFolder is how every TestDataDefinition<T> names its folder; it is protected.
        var relative = loaded.GetType().GetProperty("RelativeFolder", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(loaded) as string;
        var project = ProjectDirectoryOf(Path.GetFileNameWithoutExtension(builtAssembly), Path.GetDirectoryName(builtAssembly)!);
        return relative is null || project is null
            ? loaded
            : new UatTestDataDefinition(loaded, Path.GetFullPath(Path.Combine(project, relative)), project);
    }

    // The same walk TestDataDefinition makes: <name>.csproj on the way up, or <name>/<name>.csproj beside it.
    private static string? ProjectDirectoryOf(string assemblyName, string start)
    {
        var projectFile = assemblyName + ".csproj";
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, projectFile)))
            {
                return directory.FullName;
            }

            var sibling = Path.Combine(directory.FullName, assemblyName);
            if (File.Exists(Path.Combine(sibling, projectFile)))
            {
                return sibling;
            }
        }

        return null;
    }
}
