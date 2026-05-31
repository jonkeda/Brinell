namespace Brinell.Blazor.Uat.Tests.Runtime;

[Trait("Category", "UAT")]
[Trait("Target", "BLAZOR")]
public sealed class BlazorUatScenarioTests : IDisposable
{
    private readonly BlazorUatFixture _fixture = new();

    public static IEnumerable<object[]> ScenarioFiles => BlazorUatScenarioSource.GetScenarioFiles();

    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public async Task UatFile_Passes(string filePath)
    {
        var parse = UatMarkdownParser.ParseFile(filePath);
        Assert.True(parse.Success, FormatDiagnostics(parse.Diagnostics));
        Assert.NotNull(parse.Document);

        var runtime = UatReflectionRuntime.FromRoot(_fixture);
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

    public void Dispose()
    {
        _fixture.Dispose();
    }

    private static string FormatDiagnostics(IEnumerable<UatDiagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(x => $"{x.Location}: {x.Code} {x.Message}"));
    }

    private static string FormatBindFailure(
        IEnumerable<UatDiagnostic> diagnostics,
        UatReflectionRuntime runtime,
        UatCommandCatalog catalog)
    {
        return string.Join(Environment.NewLine, [FormatDiagnostics(diagnostics), .. runtime.DescribeDiscovery(), FormatCatalog(catalog)]);
    }

    private static string FormatResults(
        UatScenarioRunResult result,
        UatExecutionContext context,
        UatReflectionRuntime runtime,
        UatCommandCatalog catalog)
    {
        List<string> lines =
        [
            .. result.Steps.Select(x =>
                $"{x.Status}: {x.Invocation.Step.Source}: {x.Invocation.CommandId}: {x.Invocation.Step.Text} {x.Message}"),
            "Runtime trace:"
        ];
        lines.AddRange(context.Diagnostics);
        lines.AddRange(runtime.DescribeDiscovery());
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
