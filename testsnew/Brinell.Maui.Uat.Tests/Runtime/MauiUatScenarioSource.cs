namespace Brinell.Maui.Uat.Tests.Runtime;

internal static class MauiUatScenarioSource
{
    public static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "uat.config.md");

    public static IEnumerable<object[]> GetScenarioFiles()
    {
        foreach (var filePath in EnumerateUatFiles("Scenarios"))
        {
            yield return [filePath];
        }
    }

    public static IEnumerable<object[]> GetExpectedFailureScenarioFiles()
    {
        foreach (var filePath in EnumerateUatFiles("ExpectedFailures"))
        {
            yield return [filePath];
        }
    }

    private static IEnumerable<string> EnumerateUatFiles(string folderName)
    {
        var scenarioDirectory = Path.Combine(AppContext.BaseDirectory, folderName);
        return Directory.Exists(scenarioDirectory)
            ? Directory.EnumerateFiles(scenarioDirectory, "*.uat.md").Order(StringComparer.OrdinalIgnoreCase)
            : [];
    }
}
