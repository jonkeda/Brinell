using System.Reflection;
using System.Runtime.Loader;

namespace Brinell.Presenter.Services;

/// <summary>
/// The load context one run's page-object assemblies live in, so they can be released again.
/// </summary>
/// <remarks>
/// <para>
/// The default context cannot unload, and <c>LoadFromAssemblyPath</c> maps the file for the life
/// of the process: a workspace that has been run once could not be rebuilt without restarting
/// Presenter. A collectible context per session releases the file when the session ends.
/// </para>
/// <para>
/// Anything the host already has — Brinell.Maui, Brinell.Uat, the framework — is deliberately
/// shared rather than loaded again. Two copies of <c>IMauiTestContext</c> are two different
/// types, and every cast across the boundary would fail.
/// </para>
/// </remarks>
/// <param name="resolvePath">Finds an assembly file by name, or returns null.</param>
internal sealed class BddSessionLoadContext(Func<string, string?> resolvePath)
    : AssemblyLoadContext(name: "BrinellUatSession", isCollectible: true)
{
    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Null hands the request to the default context, which is what shared types need.
        if (assemblyName.Name is null)
        {
            return null;
        }

        // The host first, for anything it can load at all — not merely what it has loaded
        // already. Probing instead would search the whole tree by file name, and that tree
        // holds Android and iOS build output: picking one of those copies of a dependency
        // fails later with a missing native library, naming neither the assembly nor the
        // platform it came from.
        if (CanHostLoad(assemblyName))
        {
            return null;
        }

        var path = resolvePath(assemblyName.Name + ".dll");
        return path is null ? null : LoadSuiteAssembly(path);
    }

    /// <summary>
    /// Loads a suite's assembly. One built here (its symbols are beside it) is read into memory:
    /// its file stays free, so the next build can overwrite it even when the context cannot
    /// unload (ASP.NET Core and SQLite leave timers and handlers behind that hold it). Anything
    /// else is loaded from its path, which libraries that look beside themselves need.
    /// </summary>
    /// <param name="path">The assembly file.</param>
    internal Assembly LoadSuiteAssembly(string path)
    {
        var symbols = Path.ChangeExtension(path, ".pdb");
        if (!File.Exists(symbols))
        {
            return LoadFromAssemblyPath(path);
        }

        using var image = new MemoryStream(File.ReadAllBytes(path));
        using var debug = new MemoryStream(File.ReadAllBytes(symbols));
        var assembly = LoadFromStream(image, debug);
        Brinell.Core.Composition.AssemblyFile.Record(assembly, path);
        return assembly;
    }

    // What the host ships: the framework and Presenter's own folder, or what it has loaded. Not
    // Default.LoadFromAssemblyName, which raises the default context's Resolving event: the
    // workspace check's shadow copy answers there (BddSuiteLoad), and a copy from it is not the
    // suite's own - Playwright loaded from it found no browser driver beside it.
    private static readonly Lazy<HashSet<string>> HostAssemblies = new(() =>
    {
        var trusted = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var own = Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll");
        return trusted.Concat(own).Select(Path.GetFileNameWithoutExtension).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
    });

    private static bool CanHostLoad(AssemblyName assemblyName)
        => HostAssemblies.Value.Contains(assemblyName.Name!)
           || Default.Assemblies.Any(assembly => string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
}
