namespace Brinell.Wpf.Uat.Tests.Runtime;

[Trait("Category", "UAT")]
[Trait("Target", "WPF")]
public sealed class WpfUatScenarioTests(WpfUatFixture fixture)
    : UatScenarioTestBase<WpfUatFixture>(fixture),
        IClassFixture<WpfUatFixture>
{
    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "WPF", Fixture: "WpfUatFixture");

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public async Task UatFile_Passes(string filePath)
    {
        await RunUatFileAsync(filePath);
    }
}
