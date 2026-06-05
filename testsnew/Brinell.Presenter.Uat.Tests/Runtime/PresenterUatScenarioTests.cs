using Brinell.Uat;
using Brinell.Presenter.Uat.Tests.PageObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Presenter.Uat.Tests.Runtime;

[Collection(PresenterUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "Presenter")]
public sealed class PresenterUatScenarioTests(PresenterFixture fixture)
    : UatScenarioTestBase<PresenterFixture>(fixture)
{
    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "MAUI", Fixture: "FlaUI");

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public Task UatFile_Passes(string filePath)
    {
        return RunUatFileAsync(filePath);
    }

    protected override void BeforeScenario(UatBoundScenario scenario)
    {
        ResetPresenter();
    }

    private void ResetPresenter()
    {
        using var scope = Fixture.Composition.CreateScope();
        var presenterPage = scope.ServiceProvider.GetRequiredService<PresenterPage>();

        if (!presenterPage.IsLoaded(timeoutMs: 30000))
        {
            return;
        }

        presenterPage.StopButton.Click();
        presenterPage.ReloadButton.Click();
        presenterPage.StatusSummary.AssertTextContains("Ready", timeoutMs: 30000);
        presenterPage.RunButton.AssertClickable(timeoutMs: 30000);
    }
}
