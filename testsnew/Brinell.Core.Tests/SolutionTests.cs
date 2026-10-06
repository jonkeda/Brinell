namespace Brinell.Core.Tests;

public sealed class SolutionTests
{
    // Not product code: scratch projects of work in flight, and build output.
    private static readonly string[] SkippedFolders = [".git", ".my", ".vs", "bin", "obj", "node_modules", "TestResults"];

    [Fact]
    public void EveryProjectInTheRepository_IsInBrinellSln()
    {
        var root = FindRepositoryRoot();
        Assert.NotNull(root);
        var solution = File.ReadAllText(Path.Combine(root, "Brinell.sln")).Replace('\\', '/');

        var missing = Projects(root)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !solution.Contains($"\"{path}\"", StringComparison.OrdinalIgnoreCase))
            .Order()
            .ToList();

        Assert.True(missing.Count == 0, $"Add these projects to Brinell.sln, in their area's folder: {string.Join(", ", missing)}");
    }

    private static IEnumerable<string> Projects(string directory)
    {
        foreach (var project in Directory.EnumerateFiles(directory, "*.csproj"))
        {
            yield return project;
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            if (SkippedFolders.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var project in Projects(child))
            {
                yield return project;
            }
        }
    }

    private static string? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brinell.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName;
    }
}
