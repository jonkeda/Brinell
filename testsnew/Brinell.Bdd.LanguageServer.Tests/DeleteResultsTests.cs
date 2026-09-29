using Xunit;

namespace Brinell.Bdd.LanguageServer.Tests;

/// <summary>
/// <c>brinell/deleteResults</c> on a <c>TestResults</c> built here: only scenario results go, a run
/// keeps what is not theirs, a run left empty goes, a running one is left alone.
/// </summary>
public sealed class DeleteResultsTests : IDisposable
{
    private readonly string _solution = Path.Combine(Path.GetTempPath(), "Brinell", "DeleteResultsTests", Guid.NewGuid().ToString("N"));
    private readonly BddLanguageServer _server = new();

    public DeleteResultsTests()
    {
        var features = Directory.CreateDirectory(Path.Combine(Shelf, "Features")).FullName;
        File.WriteAllText(Path.Combine(_solution, "Shelf.sln"), string.Empty);
        File.WriteAllText(Path.Combine(Shelf, "bdd.config.md"), """
            # BDD Config

            ## Runtime

            | Field | Value |
            | --- | --- |
            | Fixture | ShelfFixture |
            """);
        File.WriteAllText(Path.Combine(features, "shelf.feature.md"), """
            # Feature: Shelf

            ## Scenario: One

            ```gherkin
            When I shelve "Dune"
            ```

            ## Scenario: Two

            ```gherkin
            When I shelve "Emma"
            ```
            """);
        File.WriteAllText(Path.Combine(features, "other.feature.md"), """
            # Feature: Other

            ## Scenario: Three

            ```gherkin
            When I shelve "Hamlet"
            ```
            """);
        _server.Initialize(new InitializeParams(null, null, new InitializationOptions(_solution)));
    }

    private string Shelf => Path.Combine(_solution, "Shelf.Steps");

    private string Feature(string file) => Path.Combine(Shelf, "Features", file);

    private string Results => Path.Combine(_solution, "TestResults");

    public void Dispose()
    {
        _server.Dispose();
        Directory.Delete(_solution, recursive: true);
    }

    [Fact]
    public void AScenario_LosesItsReportAndScreenshot_InEveryRun_AndTheRunKeepsTheOthers()
    {
        WriteRun("run-a", ("Shelf.Steps", "shelf", "One"), ("Shelf.Steps", "shelf", "Two"), ("Shelf.Steps", "other", "Three"));
        WriteRun("run-b", ("Shelf.Steps", "shelf", "One"));

        var result = Delete("scenario", Feature("shelf.feature.md"), line: 3);

        Assert.True(result.Done, result.Reason);
        Assert.Equal("One", result.What);
        Assert.Equal((1, 1, 4), (result.RunsChanged, result.RunsDeleted, result.FilesDeleted));
        Assert.Equal(["Three", "Two"], Records("run-a"));
        Assert.False(File.Exists(Report("run-a", "shelf", "One")));
        Assert.False(File.Exists(Screenshot("run-a", "One")));
        Assert.True(File.Exists(Report("run-a", "shelf", "Two")));
        Assert.DoesNotContain("One", File.ReadAllText(Path.Combine(Results, "run-a", "summary.md")));
        Assert.False(Directory.Exists(Path.Combine(Results, "run-b")));
    }

    [Fact]
    public void AFile_LosesEachOfItsScenarios_AndNotAnotherFiles()
    {
        WriteRun("run-a", ("Shelf.Steps", "shelf", "One"), ("Shelf.Steps", "shelf", "Two"), ("Shelf.Steps", "other", "Three"));

        var result = Delete("file", Feature("shelf.feature.md"));

        Assert.Equal("shelf.feature.md", result.What);
        Assert.Equal(["Three"], Records("run-a"));
    }

    [Fact]
    public void ASuite_LosesAllItsResults_AnEmptyRunGoes_AndAnotherSuitesStays()
    {
        WriteRun("run-a", ("Shelf.Steps", "shelf", "One"), ("Shelf.Steps", "other", "Three"));
        WriteRun("run-b", ("Other.Suite", "shelf", "One"));

        var result = Delete("suite", suite: "Shelf.Steps");

        Assert.Equal((0, 1), (result.RunsChanged, result.RunsDeleted));
        Assert.False(Directory.Exists(Path.Combine(Results, "run-a")));
        Assert.Equal(["One"], Records("run-b"));
    }

    [Fact]
    public void ARunWithAnArtifactThatIsNotAScenarios_IsKept_WithIt()
    {
        WriteRun("run-a", extra: true, ("Shelf.Steps", "shelf", "One"));

        var result = Delete("suite", suite: "Shelf.Steps");

        Assert.Equal((1, 0), (result.RunsChanged, result.RunsDeleted));
        Assert.Equal(["page"], Records("run-a"));
        Assert.True(File.Exists(Path.Combine(Results, "run-a", "screens", "page.png")));
    }

    [Fact]
    public void APathOutsideTheRun_IsNeverDeleted()
    {
        var outside = Path.Combine(_solution, "outside.txt");
        File.WriteAllText(outside, "keep me");
        WriteRun("run-a", ("Shelf.Steps", "shelf", "One"));
        var manifest = Path.Combine(Results, "run-a", "manifest.json");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("\"evidencePath\": \"screens\\\\One.png\"", "\"evidencePath\": \"..\\\\..\\\\outside.txt\"", StringComparison.Ordinal));
        File.SetLastWriteTimeUtc(manifest, DateTime.UtcNow.AddMinutes(-10));

        Delete("scenario", Feature("shelf.feature.md"), line: 3);

        Assert.True(File.Exists(outside));
    }

    [Fact]
    public void ARunWrittenJustNow_IsSkipped_SayingWhy()
    {
        WriteRun("run-a", age: TimeSpan.Zero, entries: ("Shelf.Steps", "shelf", "One"));

        var result = Delete("suite", suite: "Shelf.Steps");

        Assert.Equal("run-a", Assert.Single(result.Skipped).Run);
        Assert.Equal(["One"], Records("run-a"));
    }

    [Fact]
    public void ADryRun_CountsTheSame_AndChangesNothing()
    {
        WriteRun("run-a", ("Shelf.Steps", "shelf", "One"), ("Shelf.Steps", "shelf", "Two"));

        var dry = Delete("file", Feature("shelf.feature.md"), dryRun: true);

        Assert.Equal((0, 1, 4), (dry.RunsChanged, dry.RunsDeleted, dry.FilesDeleted));
        Assert.Equal(["One", "Two"], Records("run-a"));
        Assert.True(File.Exists(Report("run-a", "shelf", "One")));
    }

    [Fact]
    public void AnUnknownScope_OrNoHeading_SaysSo()
    {
        Assert.False(Delete("everything").Done);
        Assert.Contains("no scenario heading at line 4", Delete("scenario", Feature("shelf.feature.md"), line: 4).Reason, StringComparison.OrdinalIgnoreCase);
    }

    private DeleteResultsResult Delete(string scope, string? file = null, int? line = null, string? suite = null, bool dryRun = false)
        => _server.DeleteResults(new DeleteResultsParams(scope, file is null ? null : new Uri(file).AbsoluteUri, line, suite, dryRun));

    private void WriteRun(string runId, params (string Suite, string File, string Scenario)[] entries)
        => WriteRun(runId, extra: false, entries);

    private void WriteRun(string runId, bool extra, params (string Suite, string File, string Scenario)[] entries)
        => WriteRun(runId, TimeSpan.FromMinutes(10), extra, entries);

    private void WriteRun(string runId, TimeSpan age, params (string Suite, string File, string Scenario)[] entries)
        => WriteRun(runId, age, extra: false, entries);

    // A run as a test process writes it: a report and a screenshot per scenario, a manifest and a summary;
    // with extra, a UI test's screenshot that is no scenario's.
    private void WriteRun(string runId, TimeSpan age, bool extra, (string Suite, string File, string Scenario)[] entries)
    {
        var run = Directory.CreateDirectory(Path.Combine(Results, runId)).FullName;
        List<string> records = [];
        foreach (var (suite, file, scenario) in entries)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Report(runId, file, scenario))!);
            File.WriteAllText(Report(runId, file, scenario), "{}");
            Directory.CreateDirectory(Path.Combine(run, "screens"));
            File.WriteAllText(Screenshot(runId, scenario), "png");
            records.Add($$"""
                { "Kind": "uat-scenario", "Name": "{{scenario}}", "Path": "suites\\{{suite}}\\uat\\{{file}}__{{scenario}}__result.json", "Reason": "passed", "Status": "passed",
                  "CreatedAtUtc": "2026-01-01T00:00:00Z", "Metadata": { "suite": "{{suite}}", "file": "Features/{{file}}.feature.md", "evidencePath": "screens\\{{scenario}}.png" } }
                """);
        }

        if (extra)
        {
            Directory.CreateDirectory(Path.Combine(run, "screens"));
            File.WriteAllText(Path.Combine(run, "screens", "page.png"), "png");
            records.Add("""{ "Kind": "screenshot", "Name": "page", "Path": "screens\\page.png", "Reason": "failure", "Status": "failed", "CreatedAtUtc": "2026-01-01T00:00:00Z", "Metadata": {} }""");
        }

        File.WriteAllText(Path.Combine(run, "manifest.json"), $$"""{ "RunId": "{{runId}}", "Suites": [ { "Name": "testhost", "Artifacts": [ {{string.Join(",", records)}} ] } ] }""");
        File.WriteAllText(Path.Combine(run, "summary.md"), string.Join("\n", entries.Select(entry => entry.Scenario)));
        File.SetLastWriteTimeUtc(Path.Combine(run, "manifest.json"), DateTime.UtcNow - age);
    }

    private string Report(string runId, string file, string scenario)
        => Path.Combine(Results, runId, "suites", "Shelf.Steps", "uat", $"{file}__{scenario}__result.json");

    private string Screenshot(string runId, string scenario) => Path.Combine(Results, runId, "screens", $"{scenario}.png");

    private List<string> Records(string runId)
        => Brinell.Core.Artifacts.TestArtifactManifestWriter.Read(Path.Combine(Results, runId))!.Suites
            .SelectMany(suite => suite.Artifacts).Select(record => record.Name).Order(StringComparer.Ordinal).ToList();
}
