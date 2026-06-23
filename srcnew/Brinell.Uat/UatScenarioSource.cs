namespace Brinell.Uat;

public static class UatScenarioSource
{
    public static string GetConfigFilePath(
        string? baseDirectory = null,
        string configFileName = "uat.config.md")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configFileName);
        return Path.Combine(baseDirectory ?? AppContext.BaseDirectory, configFileName);
    }

    /// <summary>
    /// Enumerates scenario files, optionally filtered by configuration.
    /// </summary>
    public static IEnumerable<string> EnumerateScenarioFiles(
        string folderName = "Scenarios",
        string? baseDirectory = null,
        string? configurationFilter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);

        var scenarioDirectory = Path.Combine(baseDirectory ?? AppContext.BaseDirectory, folderName);
        IEnumerable<string> files = Directory.Exists(scenarioDirectory)
            ? Directory
                .EnumerateFiles(scenarioDirectory, "*.uat.md", SearchOption.AllDirectories)
                .Order(StringComparer.OrdinalIgnoreCase)
            : Array.Empty<string>();

        // Use configuration filter if provided
        return ApplyFilter(files, configurationFilter);
    }

    /// <summary>
    /// Gets scenario files as theory data, optionally filtered by configuration.
    /// </summary>
    public static IEnumerable<object[]> GetScenarioFileTheoryData(
        string folderName = "Scenarios",
        string? baseDirectory = null,
        string? configurationFilter = null)
    {
        foreach (var filePath in EnumerateScenarioFiles(folderName, baseDirectory, configurationFilter))
        {
            yield return [filePath];
        }
    }

    public static IEnumerable<string> ApplyFilter(IEnumerable<string> filePaths, string? filter)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        if (string.IsNullOrWhiteSpace(filter))
            return filePaths;

        var terms = filter.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return terms.Length == 0
            ? filePaths
            : filePaths.Where(path => terms.Any(term =>
                path.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(path).Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}
