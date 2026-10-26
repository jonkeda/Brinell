using Bruinet.Bdd.Workspace;

namespace Bruinet.DataValidator;

// The scenario side of the tool: the suite as Bruinet.Bdd.Workspace loads it for every tool, and the
// same checks the xUnit gate runs (AD-016).
internal static class ScenarioCheck
{
    public static bool CanCheck(string configPath) => Suite(configPath).CanBind;

    public static BddCatalogSet Catalogs(string configPath) => Suite(configPath).Catalogs;

    /// <summary>The folder of feature and step files beside the config.</summary>
    public static string ScenarioFolder(string configPath) => Suite(configPath).FeatureFolder;

    /// <summary>The step files: <c>*.steps.md</c> in the project's Features folder and its subfolders.</summary>
    public static IReadOnlyList<string> PhraseFiles(string configPath) => Suite(configPath).StepFiles;

    public static string? CopyWarning(string configPath) => Suite(configPath).CopyWarning();

    public static IReadOnlyList<string> Files(string configPath) => Suite(configPath).FeatureFiles;

    // One suite per config for the life of the command, so its catalogs are loaded once.
    private static readonly Dictionary<string, BddSuite> Suites = new(StringComparer.OrdinalIgnoreCase);

    private static BddSuite Suite(string configPath)
    {
        var path = Path.GetFullPath(configPath);
        lock (Suites)
        {
            if (!Suites.TryGetValue(path, out var suite) || !File.Exists(path))
            {
                suite = BddSuite.Open(path);
                Suites[path] = suite;
            }

            return suite;
        }
    }
}
