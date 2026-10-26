using Bruinet.Core.Artifacts;

namespace Bruinet.Narrator;

/// <summary>Where recordings go, and the narration a scenario has.</summary>
public static class BddRecordings
{
    /// <summary>
    /// The Recordings folder when no setting names one: <c>recordings</c> in the <c>TestResults</c>
    /// folder a suite's runs write (<c>BRUINET_TEST_RESULTS_DIR</c>, else at the nearest folder above
    /// the project with <c>.git</c> or a <c>.sln</c>).
    /// </summary>
    /// <param name="projectDirectory">The suite's project folder.</param>
    public static string DefaultFolder(string projectDirectory)
    {
        if (Environment.GetEnvironmentVariable(TestArtifactOptions.RootDirectoryEnvironmentVariable) is { Length: > 0 } configured)
        {
            return Path.Combine(configured, "recordings");
        }

        for (var folder = new DirectoryInfo(projectDirectory); folder is not null; folder = folder.Parent)
        {
            if (Directory.Exists(Path.Combine(folder.FullName, ".git")) || folder.EnumerateFiles("*.sln").Any())
            {
                return Path.Combine(folder.FullName, "TestResults", "recordings");
            }
        }

        return Path.Combine(projectDirectory, "TestResults", "recordings");
    }

    /// <summary>
    /// The scene a video script of the suite has for a scenario, so a scenario recorded on its own
    /// is narrated as in the video; null when no script has one. The first script by name wins.
    /// </summary>
    /// <param name="configDirectory">The suite's folder.</param>
    /// <param name="featureFile">The scenario's feature file.</param>
    /// <param name="scenarioName">The scenario's name.</param>
    public static (BddVideoScript Script, BddVideoScenePlan Scene)? FindScene(string configDirectory, string featureFile, string scenarioName)
    {
        var address = BddScenarioAddress.Of(Path.GetRelativePath(configDirectory, featureFile), scenarioName);
        var scripts = Directory.EnumerateFiles(configDirectory, "*" + BddVideoScript.Extension, SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(configDirectory, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Order(StringComparer.OrdinalIgnoreCase);
        foreach (var path in scripts)
        {
            var check = BddVideoScriptCheck.Check(File.ReadAllText(path), path, configDirectory);
            var scene = check.Scenes.FirstOrDefault(plan => string.Equals(
                BddScenarioAddress.Of(Path.GetRelativePath(configDirectory, plan.FeatureFile), plan.Scenario.Name), address, StringComparison.OrdinalIgnoreCase));
            if (check.Script is { } script && scene is not null)
            {
                return (script, scene);
            }
        }

        return null;
    }
}
