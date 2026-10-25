using Bruinell.Bdd;
using Xunit;

namespace Bruinell.Uat.Tests;

/// <summary>A scenario its config skips is reported skipped at discovery, decided as the runner decides it.</summary>
public sealed class UatScenarioSkipTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("bruinell-skip-").FullName;

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
        var reason = UatScenarioSkip.Of(Config, Feature(), "Needs a browser", name => name == "BRUINELL_HEAD" ? "bunit" : null);

        Assert.NotNull(reason);
        Assert.Contains("@browser", reason);
    }

    [Fact]
    public void Of_ABrowserScenarioInTheBrowser_Runs()
        => Assert.Null(UatScenarioSkip.Of(Config, Feature(), "Needs a browser", _ => null));

    [Fact]
    public void Of_AnUntaggedScenarioOnTheBunitHead_Runs()
        => Assert.Null(UatScenarioSkip.Of(Config, Feature(), "Runs anywhere", name => name == "BRUINELL_HEAD" ? "bunit" : null));

    // The fixture may set a rule's variable as it starts, after discovery: the run decides it.
    [Fact]
    public void Of_ASkipRuleWhoseVariableIsUnset_IsLeftToTheRun()
    {
        var config = BddConfigParser.Parse("""
            # UAT Config

            ## Skip Rules

            | Tag | EnvironmentVariable |
            | --- | --- |
            | browser | EXAMPLE_OWN |
            """);

        Assert.Null(UatScenarioSkip.Of(config, Feature(), "Needs a browser", _ => null));
    }

    [Fact]
    public void Of_AScenarioNotInTheFile_Runs()
        => Assert.Null(UatScenarioSkip.Of(Config, Feature(), "Not there", _ => "bunit"));
}
