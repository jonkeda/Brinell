namespace Bruinell.Core.Tests;

public sealed class SolutionTests
{
    // Not product code: scratch projects of work in flight, and build output.
    private static readonly string[] SkippedFolders = [".git", ".my", ".vs", "bin", "obj", "node_modules", "TestResults"];

    [Fact]
    public void EveryProjectInTheRepository_IsInBruinellSln()
    {
        var root = FindRepositoryRoot();
        Assert.NotNull(root);
        var solution = File.ReadAllText(Path.Combine(root, "Bruinell.sln")).Replace('\\', '/');

        var missing = Projects(root)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !solution.Contains($"\"{path}\"", StringComparison.OrdinalIgnoreCase))
            .Order()
            .ToList();

        Assert.True(missing.Count == 0, $"Add these projects to Bruinell.sln, in their area's folder: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// The call-model stacks stand on Core alone: the first model's interfaces and waits are
    /// <c>Bruinell.Core.Legacy</c>, for WPF, WinForms and Stride (AD-029).
    /// </summary>
    [Fact]
    public void NoCallModelStack_ReferencesTheFirstModel()
    {
        var root = FindRepositoryRoot();
        Assert.NotNull(root);
        string[] callModel = ["Bruinell.Html", "Bruinell.Html.Playwright", "Bruinell.Blazor", "Bruinell.Maui", "Bruinell.Maui.Appium",
            "Bruinell.Maui.FlaUI", "Bruinell.Maui.CommunityToolkit", "Bruinell.NativeAndroid", "Bruinell.Appium", "Bruinell.Uat", "Bruinell.Bdd"];

        var offenders = callModel
            .Select(name => Path.Combine(root, "srcnew", name, $"{name}.csproj"))
            .Where(File.Exists)
            .Where(project => File.ReadAllText(project).Contains("Bruinell.Core.Legacy", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(offenders.Count == 0, $"These call-model projects reference Bruinell.Core.Legacy: {string.Join(", ", offenders)}");
        Assert.True(callModel.Count(name => File.Exists(Path.Combine(root, "srcnew", name, $"{name}.csproj"))) >= 8,
            "The list of call-model projects no longer matches srcnew.");
    }

    /// <summary>
    /// MAUI's controls and containers ask the driver what the platform's tree is like
    /// (<c>IMauiDriver.OmitsOffscreenElements</c>, <c>ChildrenPath</c>, <c>AlertButtons</c> ...),
    /// as <c>ShellChrome</c> does; none branches on the platform or names a platform's element type.
    /// <c>ElementMatch</c> normalizes type names from every platform and is the one exception.
    /// </summary>
    [Fact]
    public void MauiControls_HoldNoPlatformBranchesOrPlatformNames()
    {
        var root = FindRepositoryRoot();
        Assert.NotNull(root);
        var pattern = new System.Text.RegularExpressions.Regex(@"MauiPlatform\.(Android|iOS|Windows)|XCUIElementType|android\.widget|android:id");
        string[] folders = ["srcnew/Bruinell.Maui/Controls", "srcnew/Bruinell.Maui/Containers", "srcnew/Bruinell.Maui.CommunityToolkit/Controls"];

        var offenders = folders
            .Select(folder => Path.Combine(root, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.EndsWith(".gen.cs", StringComparison.Ordinal) && Path.GetFileName(file) != "ElementMatch.cs")
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => (file, line: line.Trim(), number: index + 1)))
            .Where(entry => !entry.line.StartsWith("//", StringComparison.Ordinal) && pattern.IsMatch(entry.line))
            .Select(entry => $"{Path.GetRelativePath(root, entry.file)}:{entry.number}")
            .ToList();

        Assert.True(offenders.Count == 0, $"Ask the driver instead (IMauiDriver, 'What the platform's tree is like'): {string.Join(", ", offenders)}");
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
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bruinell.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName;
    }
}
