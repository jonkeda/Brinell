namespace Brinell.Wpf.Uat.Tests.Runtime;

internal static class WpfUatScenarioSource
{
    public static IEnumerable<object[]> GetScenarioFiles()
    {
        var scenarioDirectory = Path.Combine(AppContext.BaseDirectory, "Scenarios");
        if (!Directory.Exists(scenarioDirectory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(scenarioDirectory, "*.uat.md", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            yield return [file];
        }
    }
}
