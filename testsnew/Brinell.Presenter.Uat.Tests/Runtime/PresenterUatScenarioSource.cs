namespace Brinell.Presenter.Uat.Tests.Runtime;

internal static class PresenterUatScenarioSource
{
    public static IEnumerable<object[]> GetScenarioFiles()
    {
        var scenarioDirectory = Path.Combine(AppContext.BaseDirectory, "Scenarios");
        if (!Directory.Exists(scenarioDirectory))
        {
            yield break;
        }

        foreach (var filePath in Directory.EnumerateFiles(scenarioDirectory, "*.uat.md").Order(StringComparer.OrdinalIgnoreCase))
        {
            yield return [filePath];
        }
    }
}
