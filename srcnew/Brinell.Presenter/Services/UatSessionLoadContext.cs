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
internal sealed class UatSessionLoadContext(Func<string, string?> resolvePath)
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
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    private static bool CanHostLoad(AssemblyName assemblyName)
    {
        try
        {
            return Default.LoadFromAssemblyName(assemblyName) is not null;
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return false;
        }
    }
}
