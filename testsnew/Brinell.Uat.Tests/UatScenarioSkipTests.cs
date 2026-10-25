using Brinell.Bdd;
using Xunit;

namespace Brinell.Uat.Tests;

/// <summary>A scenario its config skips is reported skipped at discovery, decided as the runner decides it.</summary>
public sealed class UatScenarioSkipTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("brinell-skip-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Feature()
    {
        var path = Path.Combine(_folder, "skips.feature.md");
        File.WriteAllText(path, """
            # Feature: Skips

            @browser
            ## Scenario: Needs a browser

            ```gherkin
            Given something
            ```

            ## Scenario: Runs anywhere

            ```gherkin
            Given something
            ```
            """);
        return path;
    }

    private static readonly BddConfig Config = BddConfigParser.Parse("# UAT Config");

    [Fact]
    public void Of_ABrowserScenarioOnTheBunitHead_IsSkippedWithTheReason()
    {
        var reason = UatScenarioSkip.Of(Config, Feature(), "Needs a browser", name => name == "BRINELL_HEAD" ? "bunit" : null);

        Assert.NotNull(reason);
        Assert.Contains("@browser", reason);
    }

    [Fact]
    public void Of_ABrowserScenarioInTheBrowser_Runs()
        => Assert.Null(UatScenarioSkip.Of(Config, Feature(), "Needs a browser", _ => null));

    [Fact]
    public void Of_AnUntaggedScenarioOnTheBunitHead_Runs()
        => Assert.Null(UatScenarioSkip.Of(Config, Feature(), "Runs anywhere", name => name == "BRINELL_HEAD" ? "bunit" : null));

    [Fact]
    public void Of_AScenarioNotInTheFile_Runs()
        => Assert.Null(UatScenarioSkip.Of(Config, Feature(), "Not there", _ => "bunit"));
}
