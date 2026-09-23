namespace Brinell.Blazor.Uat.Tests.Runtime;

[Trait("Category", "UAT")]
[Trait("Target", "BLAZOR")]
public sealed class BlazorUatScenarioTests(BlazorUatFixture fixture)
    : UatScenarioTestBase<BlazorUatFixture>(fixture),
        IClassFixture<BlazorUatFixture>
{
    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "BLAZOR", Fixture: "BlazorUatFixture");

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public Task UatFile_Passes(string filePath)
    {
        return RunUatFileAsync(filePath);
    }
}
