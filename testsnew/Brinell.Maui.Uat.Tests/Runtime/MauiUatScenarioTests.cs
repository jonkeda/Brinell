using Brinell.Maui.UITests;
using Brinell.Uat;

namespace Brinell.Maui.Uat.Tests.Runtime;

[Collection(MauiUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "MAUI")]
public sealed class MauiUatScenarioTests
    : UatScenarioTestBase<AppiumFixture>
{
    public MauiUatScenarioTests(AppiumFixture fixture)
        : base(fixture)
    {
    }

    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    public static IEnumerable<object[]> ExpectedFailureScenarioFiles => GetScenarioFiles("ExpectedFailures");

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public Task UatFile_Passes(string filePath) => RunUatFileAsync(filePath);

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ExpectedFailureScenarioFiles))]
    public Task ExpectedFailureUatFile_ReturnsUsefulDiagnostics(string filePath) =>
        RunExpectedFailureUatFileAsync(
            filePath,
            "Imaginary Button",
            "Available controls",
            Path.GetFileName(filePath));

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "MAUI", Fixture: "Appium");
}
