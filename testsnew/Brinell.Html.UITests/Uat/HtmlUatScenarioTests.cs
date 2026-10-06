namespace Brinell.Html.UITests.Uat;

using Brinell.Html.UITests.Uat.Pages;

[Trait("Category", "UAT")]
[Trait("Target", "HTML")]
public sealed class HtmlUatScenarioTests(HtmlUatFixture fixture)
    : UatScenarioTestBase<HtmlUatFixture>(fixture),
        IClassFixture<HtmlUatFixture>
{
    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "HTML", Fixture: "HtmlUatFixture");

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public Task UatFile_Passes(string filePath)
    {
        return RunUatFileAsync(filePath);
    }
}
