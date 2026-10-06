using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;

namespace Brinell.Bdd.VisualStudio;

/// <summary>
/// Whether a project needs a build before a run on the suite's runner, which reads the scenario files
/// from the source (I1 of the debugger plan): only a change to anything else since the project last
/// built, in the folders of the solution's projects, or a document not saved yet, needs one.
/// </summary>
internal sealed class BddCodeChanges : IDisposable
{
    // What the runner reads from the source folder; the same list as the VS Code extension's.
    private static readonly Regex ReadFromSource = new(@"(\.feature\.md|\.steps\.md|\.video\.md|[\\/](uat|bdd)\.config\.md)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Output = new(@"[\\/](bin|obj|TestResults|node_modules|\.git|\.vs)[\\/]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly object _gate = new();
    private readonly Dictionary<string, DateTime> _built = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FileSystemWatcher> _watchers = [];
    private string? _solution;
    private DateTime _lastChange = DateTime.UtcNow;

    /// <summary>A build of the project that started at <paramref name="started"/> succeeded.</summary>
    public void Built(string project, DateTime started)
    {
        lock (_gate)
        {
            _built[project] = started;
        }
    }

    /// <summary>Whether the project built after the last change that a runner run needs built; starts watching on first use.</summary>
    /// <param name="dte">Visual Studio, for the solution's projects and its unsaved documents.</param>
    /// <param name="project">The suite's project file.</param>
    public bool UpToDate(EnvDTE80.DTE2 dte, string project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Watch(dte);
        foreach (EnvDTE.Document document in dte.Documents)
        {
            if (!document.Saved && !ReadFromSource.IsMatch(document.FullName))
            {
                return false;
            }
        }

        lock (_gate)
        {
            return _built.TryGetValue(project, out var built) && built > _lastChange;
        }
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
    }

    // The folders of the solution's projects, the outermost of each, watched again when another
    // solution opens. A project outside every folder counts too (MauiCore beside Construction).
    private void Watch(EnvDTE80.DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var solution = dte.Solution?.FullName;
        if (string.Equals(solution, _solution, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Dispose();
        _solution = solution;
        Changed();
        List<string> folders = [];
        foreach (var file in ProjectFiles(dte.Solution?.Projects?.Cast<EnvDTE.Project>() ?? []).Append(solution ?? string.Empty))
        {
            if (Path.GetDirectoryName(file) is { Length: > 0 } folder && Directory.Exists(folder))
            {
                folders.Add(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            }
        }

        foreach (var folder in folders.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(folder => folder.Length).ToList())
        {
            if (_watchers.Any(watcher => folder.StartsWith(watcher.Path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var watcher = new FileSystemWatcher(folder) { IncludeSubdirectories = true, InternalBufferSize = 64 * 1024 };
            watcher.Changed += OnFile;
            watcher.Created += OnFile;
            watcher.Deleted += OnFile;
            watcher.Renamed += OnFile;

            // Missed events count as a change: the next run builds.
            watcher.Error += (_, _) => Changed();
            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
        }
    }

    private void OnFile(object sender, FileSystemEventArgs change)
    {
        if (!Output.IsMatch(change.FullPath) && !ReadFromSource.IsMatch(change.FullPath))
        {
            Changed();
        }
    }

    private void Changed()
    {
        lock (_gate)
        {
            _lastChange = DateTime.UtcNow;
        }
    }

    private static IEnumerable<string> ProjectFiles(IEnumerable<EnvDTE.Project> projects)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        foreach (var project in projects)
        {
            if (project.Kind == EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder)
            {
                List<EnvDTE.Project> children = [];
                foreach (EnvDTE.ProjectItem item in project.ProjectItems)
                {
                    if (item.SubProject is { } child)
                    {
                        children.Add(child);
                    }
                }

                foreach (var file in ProjectFiles(children))
                {
                    yield return file;
                }
            }
            else if (TryFullName(project) is { Length: > 0 } file)
            {
                yield return file;
            }
        }
    }

    // A project that failed to load has no file name.
    private static string? TryFullName(EnvDTE.Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            return project.FullName;
        }
        catch (Exception exception) when (exception is System.Runtime.InteropServices.COMException or NotImplementedException)
        {
            return null;
        }
    }
}
