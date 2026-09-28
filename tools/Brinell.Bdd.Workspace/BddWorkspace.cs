namespace Brinell.Bdd.Workspace;

/// <summary>
/// The suites under a folder (a solution's), and which of them a file belongs to. A file linked
/// into several suites belongs to each (AD-014).
/// </summary>
public sealed class BddWorkspace : IDisposable
{
    private static readonly string[] SkippedFolders = ["bin", "obj", "node_modules", "TestResults"];

    private BddWorkspace(string root, IReadOnlyList<BddSuite> suites)
    {
        Root = root;
        Suites = suites;
    }

    /// <summary>The folder searched.</summary>
    public string Root { get; }

    /// <summary>The suites found, by config path.</summary>
    public IReadOnlyList<BddSuite> Suites { get; }

    /// <summary>Opens every suite under <paramref name="root"/>. Nothing is loaded until a suite's catalogs are asked for.</summary>
    /// <param name="root">A solution folder.</param>
    public static BddWorkspace Open(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var full = Path.GetFullPath(root);
        return new BddWorkspace(full, FindConfigs(full).Select(BddSuite.Open).ToList());
    }

    /// <summary>
    /// The configs under a folder: one per project, <c>uat.config.md</c> before <c>bdd.config.md</c>
    /// (<see cref="BddFileFormat.FindConfig"/>). Build output, hidden folders and test results are skipped.
    /// </summary>
    /// <param name="root">The folder to search.</param>
    public static IReadOnlyList<string> FindConfigs(string root)
    {
        List<string> configs = [];
        Stack<string> folders = new([Path.GetFullPath(root)]);
        while (folders.Count > 0)
        {
            var folder = folders.Pop();
            var config = BddFileFormat.FindConfig(folder);
            if (File.Exists(config))
            {
                configs.Add(config);
            }

            foreach (var child in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(child);
                if (!name.StartsWith('.') && !SkippedFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    folders.Push(child);
                }
            }
        }

        return configs.Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>The suites a feature or step file belongs to, by config path; empty when none.</summary>
    /// <param name="filePath">A feature or step file.</param>
    public IReadOnlyList<BddSuite> SuitesFor(string filePath) => Suites.Where(suite => suite.Contains(filePath)).ToList();

    /// <summary>Drops every suite's catalogs, for after a build.</summary>
    public void Reload()
    {
        foreach (var suite in Suites)
        {
            suite.Reload();
        }
    }

    /// <inheritdoc />
    public void Dispose() => Reload();
}
