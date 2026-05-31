using Brinell.Uat;

namespace Brinell.Presenter.Uat.Tests.Runtime;

[Collection(PresenterUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "Presenter")]
public sealed class PresenterUatScenarioTests
{
    private readonly PresenterFixture _fixture;

    public PresenterUatScenarioTests(PresenterFixture fixture)
    {
        _fixture = fixture;
    }

    public static IEnumerable<object[]> ScenarioFiles => PresenterUatScenarioSource.GetScenarioFiles();

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public async Task UatFile_Passes(string filePath)
    {
        var parse = UatMarkdownParser.ParseFile(filePath);
        Assert.True(parse.Success, FormatDiagnostics(parse.Diagnostics));
        Assert.NotNull(parse.Document);

        var runtime = new PresenterUatRuntime(_fixture);
        var bind = UatBinder.Bind(parse.Document, runtime.CreateCommandCatalog());
        Assert.True(bind.Success, FormatDiagnostics(bind.Diagnostics));
        Assert.NotNull(bind.Document);

        foreach (var scenario in bind.Document.Scenarios)
        {
            ResetPresenter();
            var runner = new UatScenarioRunner();
            var result = await runner.RunAsync(scenario);
            Assert.True(result.Passed, FormatResults(result, runner.Context, runtime));
        }
    }

    private void ResetPresenter()
    {
        if (!_fixture.PresenterPage.IsLoaded(timeoutMs: 30000))
        {
            return;
        }

        _fixture.PresenterPage.StopButton.Click();
        _fixture.PresenterPage.ReloadButton.Click();
        _fixture.PresenterPage.StatusSummary.AssertTextContains("Ready", timeoutMs: 30000);
        _fixture.PresenterPage.RunButton.AssertClickable(timeoutMs: 30000);
    }

    private static string FormatDiagnostics(IEnumerable<UatDiagnostic> diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.Select(x => $"{x.Location}: {x.Code} {x.Message}"));
    }

    private static string FormatResults(
        UatScenarioRunResult result,
        UatExecutionContext context,
        PresenterUatRuntime runtime)
    {
        List<string> lines =
        [
            .. result.Steps.Select(x =>
                $"{x.Status}: {x.Invocation.Step.Source}: {x.Invocation.CommandId}: {x.Invocation.Step.Text} {x.Message}"),
            "Runtime trace:"
        ];
        lines.AddRange(context.Diagnostics);
        lines.Add(runtime.DiscoveryReport);
        return string.Join(Environment.NewLine, lines);
    }
}
