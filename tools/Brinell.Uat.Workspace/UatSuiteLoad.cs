using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;

namespace Brinell.Uat.Workspace;

// One load of a suite's assemblies: each build-output folder copied once, loaded into one collectible
// context, and unloaded together. The copy is what keeps a build from meeting a locked file (AD-018);
// the collectible context is what lets a long-running tool load the next build without growing.
internal sealed class UatSuiteLoad : IDisposable
{
    private readonly Dictionary<string, string> _copies = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Func<AssemblyLoadContext, AssemblyName, Assembly?>> _resolvers = [];
    private SuiteLoadContext? _context = new();

    public UatCatalogSet Catalogs { get; set; } = null!;

    // The context, for a test that the load is released.
    internal WeakReference Context => new(_context);

    public IEnumerable<Type> TypesOf(string assemblyPath)
    {
        var context = _context ?? throw new ObjectDisposedException(nameof(UatSuiteLoad));
        var copy = Path.Combine(CopyOf(Path.GetDirectoryName(assemblyPath)!), Path.GetFileName(assemblyPath));
        Assembly assembly;
        try
        {
            context.AddFolder(copy);
            assembly = context.LoadFromAssemblyPath(copy);
        }
        catch (Exception exception) when (exception is BadImageFormatException or FileLoadException)
        {
            throw new UatWorkspaceException($"'{assemblyPath}' cannot be loaded ({exception.Message}).");
        }

        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    public void Dispose()
    {
        foreach (var resolver in _resolvers)
        {
            AssemblyLoadContext.Default.Resolving -= resolver;
        }

        _resolvers.Clear();
        Catalogs = null!;
        _context?.Unload();
        _context = null;
    }

    private string CopyOf(string folder)
    {
        if (_copies.TryGetValue(folder, out var copy))
        {
            return copy;
        }

        copy = UatShadowCopy.Of(folder);
        _copies[folder] = copy;

        // The tool's own Brinell assemblies may need what only the suite ships (xunit for Brinell.Core).
        Func<AssemblyLoadContext, AssemblyName, Assembly?> resolver = (context, name) =>
            File.Exists(Path.Combine(copy, name.Name + ".dll")) ? context.LoadFromAssemblyPath(Path.Combine(copy, name.Name + ".dll")) : null;
        AssemblyLoadContext.Default.Resolving += resolver;
        _resolvers.Add(resolver);
        return copy;
    }

    // An assembly the tool ships (Brinell.Uat, Brinell.Core, DI) comes from the tool, so the suite's
    // attributes and interfaces are the tool's types; everything else from the suite's own folder.
    private sealed class SuiteLoadContext() : AssemblyLoadContext("BrinellUatSuite", isCollectible: true)
    {
        private readonly List<(string Folder, AssemblyDependencyResolver Resolver)> _folders = [];

        public void AddFolder(string mainAssemblyPath)
        {
            var folder = Path.GetDirectoryName(mainAssemblyPath)!;
            if (!_folders.Any(entry => string.Equals(entry.Folder, folder, StringComparison.OrdinalIgnoreCase)))
            {
                _folders.Add((folder, new AssemblyDependencyResolver(mainAssemblyPath)));
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, assemblyName.Name + ".dll")))
            {
                return null;
            }

            foreach (var (folder, resolver) in _folders)
            {
                var path = resolver.ResolveAssemblyToPath(assemblyName) ?? Path.Combine(folder, assemblyName.Name + ".dll");
                if (File.Exists(path))
                {
                    return LoadFromAssemblyPath(path);
                }
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            foreach (var (_, resolver) in _folders)
            {
                if (resolver.ResolveUnmanagedDllToPath(unmanagedDllName) is { } path)
                {
                    return LoadUnmanagedDllFromPath(path);
                }
            }

            return IntPtr.Zero;
        }
    }
}

// A copy of a build-output folder's assemblies under %TEMP%, one per build: named by the folder and
// the newest write time of what it copies, so an unchanged build is not copied again, even by the
// next process. Older copies are deleted when they are no longer in use.
internal static class UatShadowCopy
{
    private const string DoneMarker = ".copied";

    private static readonly string[] Extensions = [".dll", ".pdb", ".json"];

    public static string Root { get; set; } = Path.Combine(Path.GetTempPath(), "Brinell", "UatWorkspace");

    public static string Of(string folder)
    {
        var files = Directory.EnumerateFiles(folder)
            .Where(file => Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToList();
        var stamp = files.Count == 0 ? 0 : files.Max(file => File.GetLastWriteTimeUtc(file).Ticks);
        var parent = Path.Combine(Root, Hash(Path.GetFullPath(folder)));
        var copy = Path.Combine(parent, stamp.ToString("x", System.Globalization.CultureInfo.InvariantCulture));
        if (File.Exists(Path.Combine(copy, DoneMarker)))
        {
            return copy;
        }

        // Copied beside, then moved into place: two processes copying the same build both end with
        // a complete copy.
        var staging = copy + "." + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(staging);
        foreach (var file in files)
        {
            File.Copy(file, Path.Combine(staging, Path.GetFileName(file)));
        }

        File.WriteAllText(Path.Combine(staging, DoneMarker), folder);
        if (!TryMove(staging, copy))
        {
            // Another process moved its copy first, or a virus scanner still has a file open: the
            // staged copy is complete too, and the next copy of this folder cleans it up.
            return File.Exists(Path.Combine(copy, DoneMarker)) && TryDelete(staging) ? copy : staging;
        }

        // Another process's staging folder is left alone while it may still be copying.
        foreach (var older in Directory.EnumerateDirectories(parent)
            .Where(other => !string.Equals(other, copy, StringComparison.OrdinalIgnoreCase))
            .Where(other => !Path.GetFileName(other).Contains('.') || Directory.GetCreationTimeUtc(other) < DateTime.UtcNow.AddMinutes(-10)))
        {
            TryDelete(older);
        }

        return copy;
    }

    // A copy another process (or this one, before its context is collected) still has loaded stays
    // until the next build's copy cleans up again.
    private static bool TryDelete(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryMove(string from, string to)
    {
        try
        {
            Directory.Move(from, to);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string Hash(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToUpperInvariant())))[..16].ToLowerInvariant();
}
