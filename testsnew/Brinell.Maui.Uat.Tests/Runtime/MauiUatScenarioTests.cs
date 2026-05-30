using Brinell.Maui.UITests;
using Brinell.Uat;

namespace Brinell.Maui.Uat.Tests.Runtime;

[Collection(MauiUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "MAUI")]
public sealed class MauiUatScenarioTests
{
    private readonly AppiumFixture _fixture;

    public MauiUatScenarioTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    public static IEnumerable<object[]> ScenarioFiles => MauiUatScenarioSource.GetScenarioFiles();

    public static IEnumerable<object[]> ExpectedFailureScenarioFiles => MauiUatScenarioSource.GetExpectedFailureScenarioFiles();

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public async Task UatFile_Passes(string filePath)
    {
        var parse = UatMarkdownParser.ParseFile(filePath);
        Assert.True(parse.Success, FormatDiagnostics(parse.Diagnostics));
        Assert.NotNull(parse.Document);

        var runtime = new MauiUatRuntime(_fixture, MauiUatScenarioSource.ConfigFilePath);
        var catalog = runtime.CreateCommandCatalog();
        var bind = UatBinder.Bind(parse.Document, catalog);
        Assert.True(bind.Success, FormatBindFailure(bind.Diagnostics, runtime, catalog));
        Assert.NotNull(bind.Document);

        foreach (var scenario in bind.Document.Scenarios)
        {
            var runner = new UatScenarioRunner();
            var result = await runner.RunAsync(scenario);
            Assert.True(result.Passed, FormatResults(result, runner.Context, runtime, catalog));
        }
    }

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ExpectedFailureScenarioFiles))]
    public async Task ExpectedFailureUatFile_ReturnsUsefulDiagnostics(string filePath)
    {
        var parse = UatMarkdownParser.ParseFile(filePath);
        Assert.True(parse.Success, FormatDiagnostics(parse.Diagnostics));
        Assert.NotNull(parse.Document);

        var runtime = new MauiUatRuntime(_fixture, MauiUatScenarioSource.ConfigFilePath);
        var catalog = runtime.CreateCommandCatalog();
        var bind = UatBinder.Bind(parse.Document, catalog);
        Assert.True(bind.Success, FormatBindFailure(bind.Diagnostics, runtime, catalog));
        Assert.NotNull(bind.Document);

        var scenario = Assert.Single(bind.Document.Scenarios);
        var runner = new UatScenarioRunner();
        var result = await runner.RunAsync(scenario);
        var details = FormatResults(result, runner.Context, runtime, catalog);

        Assert.False(result.Passed, details);
        Assert.Contains("Imaginary Button", details);
        Assert.Contains("Available controls", details);
        Assert.Contains(Path.GetFileName(filePath), details);
    }

    private static string FormatDiagnostics(IEnumerable<UatDiagnostic> diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.Select(x => $"{x.Location}: {x.Code} {x.Message}"));
    }

    private static string FormatBindFailure(
        IEnumerable<UatDiagnostic> diagnostics,
        MauiUatRuntime runtime,
        UatCommandCatalog catalog)
    {
        return string.Join(
            Environment.NewLine,
            [
                FormatDiagnostics(diagnostics),
                runtime.DiscoveryReport,
                FormatCatalog(catalog)
            ]);
    }

    private static string FormatResults(
        UatScenarioRunResult result,
        UatExecutionContext context,
        MauiUatRuntime runtime,
        UatCommandCatalog catalog)
    {
        List<string> lines =
        [
            .. result.Steps.Select(x =>
                $"{x.Status}: {x.Invocation.Step.Source}: {x.Invocation.CommandId}: {x.Invocation.Step.Text} {x.Message}"),
            "Runtime trace:"
        ];
        lines.AddRange(context.Diagnostics);
        lines.Add(runtime.DiscoveryReport);
        lines.Add(FormatCatalog(catalog));
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatCatalog(UatCommandCatalog catalog)
    {
        return "Command catalog:" + Environment.NewLine + string.Join(
            Environment.NewLine,
            catalog.Patterns
                .OrderBy(pattern => pattern.Keyword)
                .ThenBy(pattern => pattern.Phrase, StringComparer.Ordinal)
                .Select(pattern => $"- {pattern.Keyword}: {pattern.Phrase} -> {pattern.CommandId}"));
    }
}
