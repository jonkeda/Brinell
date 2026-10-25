namespace Bruinell.Blazor.UITests.Uat;

[Trait("Category", "UAT")]
[Trait("Target", "BLAZOR")]
public sealed class BlazorUatScenarioTests(BlazorUatFixture fixture)
    : UatScenarioTestBase<BlazorUatFixture>(fixture),
        IClassFixture<BlazorUatFixture>
{
    // One case per scenario, each with its BddScenario trait: one scenario runs by filter (bruinell affected).
    public static IEnumerable<object[]> Scenarios => GetScenarioTheoryData();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "BLAZOR", Fixture: "BlazorUatFixture");

    [UatScenarioTheory(Timeout = 120000)]
    [MemberData(nameof(Scenarios))]
    public Task Scenario_Passes(string filePath, string scenarioName)
    {
        return RunUatScenarioAsync(filePath, scenarioName);
    }
}
