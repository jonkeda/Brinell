using System.Reflection;

namespace Bruinell.Core.Composition;

/// <summary>
/// Where an assembly's file is. A suite finds its files (its config, its test data, its app) from
/// <see cref="FolderOf"/>: the folder it was built into, whichever process runs it. See AD-025.
/// </summary>
public static class AssemblyFile
{
    /// <summary>
    /// Set for a runner started from a copy of a suite's build output (a warm runner, AD-025) to the
    /// output it was copied from: <see cref="FolderOf"/> answers with that.
    /// </summary>
    public const string BuiltOutputEnvironmentVariable = "BRUINELL_BUILT_OUTPUT";

    /// <summary>The folder the assembly was built into; the process's folder when it has no file.</summary>
    /// <param name="assembly">The assembly.</param>
    public static string FolderOf(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var folder = Path.GetDirectoryName(assembly.Location) is { Length: > 0 } located ? located : AppContext.BaseDirectory;
        return BuiltFolder(folder) ?? folder;
    }

    // In a copy of the output (the process's own folder), the same folder of the output it was copied from.
    private static string? BuiltFolder(string folder)
    {
        if (Environment.GetEnvironmentVariable(BuiltOutputEnvironmentVariable) is not { Length: > 0 } built)
        {
            return null;
        }

        var copy = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);
        if (!full.Equals(copy, StringComparison.OrdinalIgnoreCase) && !full.StartsWith(copy + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relative = Path.GetRelativePath(copy, full);
        return relative == "." ? built : Path.Combine(built, relative);
    }
}
