namespace Brinell.WinForms.Uat.Tests.Runtime;

[Trait("Category", "UAT")]
[Trait("Target", "WINFORMS")]
public sealed class WinFormsUatScenarioTests(WinFormsUatFixture fixture)
    : UatScenarioTestBase<WinFormsUatFixture>(fixture),
        IClassFixture<WinFormsUatFixture>
{
    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "WINFORMS", Fixture: "WinFormsUatFixture");

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public async Task UatFile_Passes(string filePath)
    {
        await RunUatFileAsync(filePath);
    }
}
