namespace Brinell.Stride.Uat.Tests.Runtime;

[Trait("Category", "UAT")]
[Trait("Target", "STRIDE")]
public sealed class StrideUatScenarioTests(StrideUatFixture fixture)
    : UatScenarioTestBase<StrideUatFixture>(fixture),
        IClassFixture<StrideUatFixture>
{
    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "STRIDE", Fixture: "StrideUatFixture");

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public Task UatFile_Passes(string filePath)
    {
        return RunUatFileAsync(filePath);
    }
}
