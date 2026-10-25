using System.Reflection;

namespace Bruinell.Recording;

/// <summary>
/// Loads the platform packages that bring recorders and voices, when a run does not reference them:
/// from the run's own folder, else from the folder <c>BRUINELL_RECORDERS</c> names. A suite that
/// targets <c>net10.0-windows</c> cannot reference the Windows packages (they need the Windows 10
/// SDK's projections), so a host that records points this at a folder that has them.
/// </summary>
public static class RecordingPackages
{
    /// <summary>The variable that names the folder with the recorder packages.</summary>
    public const string Variable = "BRUINELL_RECORDERS";

    /// <summary>Loads each package that is not loaded yet; a package that is nowhere is left out.</summary>
    /// <param name="names">The packages' assembly names.</param>
    /// <param name="read">Reads a process variable; <see cref="Environment.GetEnvironmentVariable(string)"/> by default.</param>
    /// <returns>The packages that are loaded now.</returns>
    public static IReadOnlyList<Assembly> Load(IEnumerable<string> names, Func<string, string?>? read = null)
    {
        ArgumentNullException.ThrowIfNull(names);
        var folder = (read ?? Environment.GetEnvironmentVariable)(Variable);
        var loaded = new List<Assembly>();
        foreach (var name in names)
        {
            if ((Loaded(name) ?? TryLoad(name, folder)) is { } assembly)
            {
                loaded.Add(assembly);
            }
        }

        return loaded;
    }

    private static Assembly? Loaded(string name)
        => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase));

    // Not being there means its recorders are not offered; any other failure is a real problem.
    private static Assembly? TryLoad(string name, string? folder)
    {
        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch (FileNotFoundException)
        {
            // Its dependencies (the Windows SDK's projections) are found beside it: LoadFrom looks there.
            var path = folder is { Length: > 0 } ? Path.Combine(folder, name + ".dll") : null;
            if (path is not null && File.Exists(path))
            {
                return Assembly.LoadFrom(path);
            }

            Debug.WriteLine($"Recording: {name} is not in the run's folder{(path is null ? string.Empty : $" or {folder}")}.");
            return null;
        }
    }
}
