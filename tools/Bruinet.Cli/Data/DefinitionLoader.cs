using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Bruinet.TestData;

namespace Bruinet.DataValidator;

internal static partial class DefinitionLoader
{
    private static readonly string SharedAssembly = typeof(ITestDataDefinition).Assembly.GetName().Name!;

    public static IReadOnlyList<ITestDataDefinition> Load(string assemblyPath, TextWriter warnings)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new ToolException($"'{assemblyPath}' does not exist. Build the project that holds the test-data definition first.");
        }

        Assembly assembly;
        try
        {
            assembly = new DefinitionLoadContext(assemblyPath).LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
        }
        catch (Exception exception) when (exception is BadImageFormatException or FileLoadException)
        {
            throw new ToolException(
                $"'{assemblyPath}' cannot be loaded ({exception.Message}). The tool runs on net10.0: keep test-data definitions in an assembly without a platform or MAUI target.");
        }

        var definitions = TypesOf(assembly)
            .Where(type => typeof(ITestDataDefinition).IsAssignableFrom(type) && type is { IsAbstract: false, IsPublic: true })
            .Where(type => type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (ITestDataDefinition)Activator.CreateInstance(type)!)
            .ToList();

        if (definitions.Count == 0)
        {
            throw new ToolException($"'{assemblyPath}' has no public test-data definition (a class deriving from TestDataDefinition<T> with a parameterless constructor).");
        }

        foreach (var definition in definitions)
        {
            WarnIfStale(assemblyPath, definition, warnings);
        }

        return definitions;
    }

    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    // A model that has just changed a DTO would otherwise validate against the old one.
    private static void WarnIfStale(string assemblyPath, ITestDataDefinition definition, TextWriter warnings)
    {
        if (definition.ProjectDirectory is not { } project)
        {
            return;
        }

        var built = File.GetLastWriteTimeUtc(assemblyPath);
        var newer = SourcesOf(project, [])
            .Where(file => File.GetLastWriteTimeUtc(file) > built)
            .Select(file => Path.GetFileName(file))
            .Take(3)
            .ToList();

        if (newer.Count > 0)
        {
            warnings.WriteLine(
                $"warning: {Path.GetFileName(assemblyPath)} is older than its sources ({string.Join(", ", newer)}). Rebuild it, or the check runs against the old types.");
        }
    }

    private static IEnumerable<string> SourcesOf(string projectDirectory, HashSet<string> visited)
    {
        if (!visited.Add(Path.GetFullPath(projectDirectory)) || !Directory.Exists(projectDirectory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(projectDirectory, file);
            if (relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }

        // Referenced projects hold the DTOs (a Contracts project), so they count too.
        foreach (var projectFile in Directory.EnumerateFiles(projectDirectory, "*.csproj"))
        {
            foreach (Match reference in ProjectReferenceRegex().Matches(File.ReadAllText(projectFile)))
            {
                var referenced = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(projectDirectory, reference.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar))));
                if (referenced is not null)
                {
                    foreach (var file in SourcesOf(referenced, visited))
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    [GeneratedRegex("<ProjectReference\\s+Include=\"(?<path>[^\"]+)\"")]
    private static partial Regex ProjectReferenceRegex();

    private sealed class DefinitionLoadContext(string mainAssemblyPath) : AssemblyLoadContext(isCollectible: false)
    {
        private readonly AssemblyDependencyResolver _resolver = new(Path.GetFullPath(mainAssemblyPath));

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Shared from the tool's context: loaded twice, the app's ITestDataDefinition would be a different type.
            if (assemblyName.Name == SharedAssembly)
            {
                return null;
            }

            return _resolver.ResolveAssemblyToPath(assemblyName) is { } path ? LoadFromAssemblyPath(path) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            => _resolver.ResolveUnmanagedDllToPath(unmanagedDllName) is { } path ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}

internal sealed class ToolException(string message) : Exception(message);
