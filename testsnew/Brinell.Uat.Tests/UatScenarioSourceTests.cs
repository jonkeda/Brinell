using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatScenarioSourceTests
{
    [Fact]
    public void EnumerateScenarioFiles_RecursesModuleFolders()
    {
        var directory = CreateTempDirectory();
        try
        {
            WriteScenario(directory, "Scenarios", "Contacts", "00-smoke.uat.md");
            WriteScenario(directory, "Scenarios", "Contacts", "01-list-basic.uat.md");
            WriteScenario(directory, "Scenarios", "Projects", "00-smoke.uat.md");

            var files = UatScenarioSource.EnumerateScenarioFiles(baseDirectory: directory).ToArray();

            Assert.Equal(3, files.Length);
            Assert.Contains(files, path => path.Contains(Path.Combine("Contacts", "00-smoke.uat.md"), StringComparison.Ordinal));
            Assert.Contains(files, path => path.Contains(Path.Combine("Contacts", "01-list-basic.uat.md"), StringComparison.Ordinal));
            Assert.Contains(files, path => path.Contains(Path.Combine("Projects", "00-smoke.uat.md"), StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ApplyFilter_MatchesModuleFolder()
    {
        var directory = CreateTempDirectory();
        try
        {
            WriteScenario(directory, "Scenarios", "Contacts", "00-smoke.uat.md");
            WriteScenario(directory, "Scenarios", "Projects", "00-smoke.uat.md");

            var files = UatScenarioSource
                .ApplyFilter(
                    UatScenarioSource.EnumerateScenarioFiles(baseDirectory: directory),
                    "Contacts")
                .ToArray();

            var file = Assert.Single(files);
            Assert.Contains(Path.Combine("Contacts", "00-smoke.uat.md"), file, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"brinell-uat-source-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteScenario(
        string root,
        string scenarioFolder,
        string moduleFolder,
        string fileName)
    {
        var directory = Path.Combine(root, scenarioFolder, moduleFolder);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), """
            # UAT: Source

            @smoke
            ## Scenario: Smoke

            When I run
            """);
    }
}

