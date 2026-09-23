using Brinell.Core.Artifacts;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Todo.JourneyCoverage;

/// <summary>
/// Keeps <c>journeys.md</c> and the tests in step: every subtask is tested in the tier it is
/// assigned to, and every test says which subtask and tier it is.
/// </summary>
/// <remarks>
/// Reads the test assemblies by reflection, so it runs in a second and launches nothing. It writes
/// the matrix to <c>TestResults/&lt;run-id&gt;/suites/TodoJourneyCoverage/journey-coverage.md</c>
/// (AD-007).
/// </remarks>
[Trait("Module", "Todo")]
[Trait("Category", "Coverage")]
public sealed class JourneyCoverageTests
{
    private static readonly Lazy<CoverageReport> Report = new(() => CoverageReport.Build(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "journeys.md")),
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "charters.md")),
        TestCatalog.Load()));

    private readonly ITestOutputHelper _output;

    public JourneyCoverageTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void JourneysMd_IsRead()
    {
        Assert.True(Report.Value.Subtasks.Count >= 50, $"Only {Report.Value.Subtasks.Count} subtasks read from journeys.md.");
        Assert.True(Report.Value.Tests.Count > 0, "No tests found in the test assemblies.");
    }

    [Fact]
    public void EveryAutomatedAssignment_HasATestInItsTier()
    {
        var missing = Report.Value.Assignments
            .Where(a => a.Tier != Tier.Manual && !a.IsCovered)
            .Select(a => $"{a.Subtask.Id} ({a.Tier}): {a.Subtask.Text}")
            .ToList();

        Assert.True(missing.Count == 0,
            "journeys.md assigns these subtasks to a tier with no test carrying their Journey trait:\n  "
            + string.Join("\n  ", missing));
    }

    [Fact]
    public void EveryManualAssignment_HasACharter()
    {
        var missing = Report.Value.Assignments
            .Where(a => a.Tier == Tier.Manual && !a.IsCovered)
            .Select(a => $"{a.Subtask.Id}: {a.Subtask.Text}")
            .ToList();

        Assert.True(missing.Count == 0, "No charter in manual/charters.md names:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void EveryJourneyTrait_NamesASubtaskOfJourneysMd()
    {
        var known = Report.Value.Subtasks.Select(s => s.Id).ToHashSet();
        var unknown = Report.Value.Tests
            .SelectMany(t => t.Journeys.Where(id => !known.Contains(id)).Select(id => $"{t.Name}: {id}"))
            .ToList();

        Assert.True(unknown.Count == 0, "These Journey traits name no subtask:\n  " + string.Join("\n  ", unknown));
    }

    [Fact]
    public void EveryTest_SaysWhichTierItIsIn()
    {
        var untiered = Report.Value.Tests.Where(t => t.Pyramid == null).Select(t => t.Name).ToList();

        Assert.True(untiered.Count == 0, "These tests have no Pyramid trait:\n  " + string.Join("\n  ", untiered));
    }

    [Fact]
    public void TheCaseCounts_HaveThePyramidsShape()
    {
        // journeys.md: subtask assignments may lean to the UI, but the cases must not. If this fails,
        // look for hermetic tests that repeat a variant a unit test already covers.
        var unit = Report.Value.CasesIn("Unit");
        var hermetic = Report.Value.CasesIn("UiHermetic");
        var live = Report.Value.CasesIn("UiLive");

        Assert.True(unit > hermetic, $"Unit cases ({unit}) should outnumber hermetic UI cases ({hermetic}).");
        Assert.True(hermetic > live, $"Hermetic UI cases ({hermetic}) should outnumber live UI cases ({live}).");
    }

    [Fact]
    public void TheMatrix_IsWrittenToTheArtifacts()
    {
        var folder = DefaultTestArtifactPathProvider.Create("TodoJourneyCoverage").SuiteDirectory;
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "journey-coverage.md");

        File.WriteAllText(file, Report.Value.ToMarkdown());

        _output.WriteLine(file);
        Assert.True(new FileInfo(file).Length > 0);
    }
}
