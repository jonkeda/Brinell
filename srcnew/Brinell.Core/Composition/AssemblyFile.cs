using System.Reflection;

namespace Brinell.Core.Composition;

/// <summary>
/// Where an assembly's file is. A host that loads a suite from memory, so a build can overwrite
/// the file while the host runs (Presenter), records the path here: <c>Assembly.Location</c> is
/// empty for such an assembly. A suite finds its files (its config, its test data, its app) from
/// <see cref="FolderOf"/>, never from <c>AppContext.BaseDirectory</c>, which is the host's folder.
/// </summary>
public static class AssemblyFile
{
    /// <summary>The prefix of the <see cref="AppContext"/> data that holds a recorded path, followed by the assembly's name.</summary>
    public const string DataPrefix = "Brinell.AssemblyFile:";

    /// <summary>The assembly's file; empty when it has none and no host recorded one.</summary>
    /// <param name="assembly">The assembly.</param>
    public static string PathOf(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.Location is { Length: > 0 } location
            ? location
            : AppContext.GetData(DataPrefix + assembly.GetName().Name) as string ?? string.Empty;
    }

    /// <summary>The folder the assembly was built into; the host's folder when it is not known.</summary>
    /// <param name="assembly">The assembly.</param>
    public static string FolderOf(Assembly assembly)
        => Path.GetDirectoryName(PathOf(assembly)) is { Length: > 0 } folder ? folder : AppContext.BaseDirectory;

    /// <summary>Records where an assembly loaded from memory was read from.</summary>
    /// <param name="assembly">The assembly, loaded from a stream.</param>
    /// <param name="path">The file it was read from.</param>
    public static void Record(Assembly assembly, string path)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        AppContext.SetData(DataPrefix + assembly.GetName().Name, Path.GetFullPath(path));
    }
}
