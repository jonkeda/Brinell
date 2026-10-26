namespace Bruinet.Html.UITests.Uat;

using Bruinet.Html.UITests.Uat.Pages;

[Trait("Category", "UAT")]
[Trait("Target", "HTML")]
public sealed class HtmlUatScenarioTests(HtmlUatFixture fixture)
    : UatScenarioTestBase<HtmlUatFixture>(fixture),
        IClassFixture<HtmlUatFixture>
{
    // One case per scenario, each with its BddScenario trait: one scenario runs by filter (bruinet affected).
    public static IEnumerable<object[]> Scenarios => GetScenarioTheoryData();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "HTML", Fixture: "HtmlUatFixture");

    [UatScenarioTheory(Timeout = 120000)]
    [MemberData(nameof(Scenarios))]
    public Task Scenario_Passes(string filePath, string scenarioName)
    {
        return RunUatScenarioAsync(filePath, scenarioName);
    }
}
