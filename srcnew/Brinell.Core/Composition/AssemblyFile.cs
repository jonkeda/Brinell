using System.Reflection;

namespace Brinell.Core.Composition;

/// <summary>
/// Where an assembly's file is. A suite finds its files (its config, its test data, its app) from
/// <see cref="FolderOf"/>: the folder it was built into, whichever process runs it. See AD-025.
/// </summary>
public static class AssemblyFile
{
    /// <summary>The folder the assembly was built into; the process's folder when it has no file.</summary>
    /// <param name="assembly">The assembly.</param>
    public static string FolderOf(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return Path.GetDirectoryName(assembly.Location) is { Length: > 0 } folder ? folder : AppContext.BaseDirectory;
    }
}
