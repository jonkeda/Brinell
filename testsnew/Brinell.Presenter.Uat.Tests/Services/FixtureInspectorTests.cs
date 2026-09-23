using Brinell.Presenter.Services;

namespace Brinell.Presenter.Uat.Tests.Services;

/// <summary>
/// Reading fixtures out of a built assembly without loading it, and what that implies for the
/// target. Runs against this test assembly, which holds a real fixture.
/// </summary>
public sealed class FixtureInspectorTests
{
    private static string ThisAssembly => typeof(FixtureInspectorTests).Assembly.Location;

    [Fact]
    public void Inspect_FindsTheFixtureAndWhatItDerives()
    {
        var inspection = new FixtureInspector().Inspect(ThisAssembly);

        Assert.True(inspection.Succeeded, inspection.Error);
        var fixture = Assert.Single(inspection.Candidates, candidate => candidate.Name == "PresenterFixture");
        Assert.True(fixture.IsUsable);
        Assert.True(fixture.ScansPages);
        Assert.Equal("MauiTestFixtureBase", fixture.FixtureBase);
        Assert.Equal("MAUI", fixture.Target);
    }

    [Fact]
    public void Inspect_RanksTheObviousAnswerFirst()
    {
        var inspection = new FixtureInspector().Inspect(ThisAssembly);

        Assert.Equal("PresenterFixture", inspection.Candidates[0].Name);
        Assert.Equal("PresenterFixture", inspection.Obvious?.Name);
    }

    [Fact]
    public void Inspect_MatchesTheNameTheWayPresenterConstructsIt()
    {
        var inspection = new FixtureInspector().Inspect(ThisAssembly);

        Assert.Equal("PresenterFixture", inspection.Find("PresenterFixture")?.Name);
        Assert.Equal("PresenterFixture", inspection.Find("Brinell.Presenter.Uat.Tests.Runtime.PresenterFixture")?.Name);

        // The "Fixture + 'Fixture'" rule Presenter uses when constructing.
        Assert.Equal("PresenterFixture", inspection.Find("Presenter")?.Name);
        Assert.Null(inspection.Find("FlaUI"));
    }

    /// <summary>
    /// Platforms are declared with an attribute, not a property: the picker reads metadata
    /// before anything is loaded, where a property value cannot be read at all.
    /// </summary>
    [Fact]
    public void Inspect_ReadsDeclaredPlatformsFromMetadata()
    {
        var todo = FindTodoUatAssembly();
        Assert.True(File.Exists(todo), $"Build the Todo UAT project first: {todo}");

        var inspection = new FixtureInspector().Inspect(todo);
        var fixture = Assert.Single(inspection.Candidates, candidate => candidate.Name == "TodoUatFixture");

        Assert.Equal(["Windows", "Android"], fixture.Platforms);
    }

    [Fact]
    public void Inspect_FixtureWithoutTheAttribute_DeclaresNoPlatforms()
    {
        var inspection = new FixtureInspector().Inspect(ThisAssembly);

        Assert.Empty(Assert.Single(
            inspection.Candidates,
            candidate => candidate.Name == "PresenterFixture").Platforms);
    }

    private static string FindTodoUatAssembly()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brinell.sln")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory!.FullName,
            "samples", "Todo", "tests", "Brinell.Samples.Todo.Uat",
            "bin", "Debug", "net10.0-windows", "Brinell.Samples.Todo.Uat.dll");
    }

    [Fact]
    public void Inspect_DoesNotLockTheFile()
    {
        var copy = Path.Combine(Path.GetTempPath(), $"brinell-inspect-{Guid.NewGuid():N}.dll");
        File.Copy(ThisAssembly, copy);

        try
        {
            new FixtureInspector().Inspect(copy);

            // The point of reflection-only: a rebuild after listing fixtures must not fail.
            using var stream = File.Open(copy, FileMode.Open, FileAccess.Write, FileShare.None);
            Assert.True(stream.CanWrite);
        }
        finally
        {
            File.Delete(copy);
        }
    }

    [Fact]
    public void Inspect_UnbuiltAssembly_SaysSoWithoutThrowing()
    {
        var inspection = new FixtureInspector().Inspect(
            Path.Combine(Path.GetTempPath(), "NoSuch", "NotBuilt.dll"));

        Assert.False(inspection.Succeeded);
        Assert.Contains("is not built", inspection.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Inspect_Describes_WhyACandidateCannotBeUsed()
    {
        var inspection = new FixtureInspector().Inspect(ThisAssembly);

        foreach (var candidate in inspection.Candidates.Where(candidate => !candidate.IsUsable))
        {
            Assert.NotEqual(string.Empty, candidate.UnusableReason);
        }
    }
}
