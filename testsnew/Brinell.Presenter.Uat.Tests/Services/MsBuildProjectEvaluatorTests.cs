using Brinell.Presenter.Services;
using Brinell.Uat;

namespace Brinell.Presenter.Uat.Tests.Services;

/// <summary>
/// The real evaluator against real projects. These spawn <c>dotnet msbuild</c>, which is
/// evaluation only — no build, no restore — and takes about half a second.
/// </summary>
public sealed class MsBuildProjectEvaluatorTests
{
    [Fact]
    public void Evaluate_SingleTargetProject_ReturnsItsOutputPath()
    {
        var project = FindProject("testsnew", "Brinell.Presenter.Uat.Tests", "Brinell.Presenter.Uat.Tests.csproj");

        var evaluation = new MsBuildProjectEvaluator().Evaluate(project);

        Assert.True(evaluation.Succeeded, evaluation.Error);
        Assert.EndsWith("Brinell.Presenter.Uat.Tests.dll", evaluation.TargetPath, StringComparison.Ordinal);
        Assert.False(evaluation.IsAmbiguous);
    }

    /// <summary>
    /// A multi-targeted project returns an empty TargetPath rather than an error, which is the
    /// sharp edge this resolution has to name rather than pass on.
    /// </summary>
    [Fact]
    public void Evaluate_MultiTargetedProjectWithoutAFramework_IsAmbiguousRatherThanEmpty()
    {
        var project = FindProject("srcnew", "Brinell.Uat", "Brinell.Uat.csproj");
        var evaluator = new MsBuildProjectEvaluator();

        var ambiguous = evaluator.Evaluate(project);

        Assert.True(ambiguous.Succeeded, ambiguous.Error);
        Assert.True(ambiguous.TargetFrameworks.Count > 1, "Brinell.Uat multi-targets.");
        Assert.True(ambiguous.IsAmbiguous);
        Assert.Empty(ambiguous.TargetPath);

        var resolved = evaluator.Evaluate(project, ambiguous.TargetFrameworks[^1]);

        Assert.False(resolved.IsAmbiguous);
        Assert.EndsWith("Brinell.Uat.dll", resolved.TargetPath, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_MissingProject_FailsWithoutThrowing()
    {
        var evaluation = new MsBuildProjectEvaluator()
            .Evaluate(Path.Combine(Path.GetTempPath(), "NoSuch", "Missing.csproj"));

        Assert.False(evaluation.Succeeded);
        Assert.Contains("was not found", evaluation.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_SameProjectTwice_IsServedFromTheCache()
    {
        var project = FindProject("srcnew", "Brinell.Presenter", "Brinell.Presenter.csproj");
        var evaluator = new MsBuildProjectEvaluator();

        var first = evaluator.Evaluate(project, "net10.0-windows10.0.19041.0");

        var start = DateTime.UtcNow;
        var second = evaluator.Evaluate(project, "net10.0-windows10.0.19041.0");
        var elapsed = DateTime.UtcNow - start;

        Assert.Equal(first.TargetPath, second.TargetPath);
        Assert.True(
            elapsed < TimeSpan.FromMilliseconds(100),
            $"A cached evaluation should be immediate, took {elapsed.TotalMilliseconds:0} ms.");
    }

    /// <summary>
    /// The design's claim, on a real workspace: a config of two rows — the project and the
    /// fixture — resolves the Pages assembly and reports no errors. No AppPath, no assembly
    /// registrations, no Target.
    /// </summary>
    [Fact]
    public void TwoRowConfig_AgainstTheRealTodoProject_ResolvesWithNoErrors()
    {
        var project = FindProject(
            "samples", "Todo", "tests", "Brinell.Samples.Todo.Uat", "Brinell.Samples.Todo.Uat.csproj");
        var root = Path.Combine(Path.GetTempPath(), "BrinellTwoRowConfig", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, "uat.config.md"),
                UatConfigWriter.Create(new UatConfigTemplate(project, "TodoUatFixture")));

            var config = new UatWorkspaceService().LoadFolder(root).Config;

            Assert.Equal("TodoUatFixture", config.Fixture);
            Assert.EndsWith(
                "Brinell.Samples.Todo.Uat.dll",
                config.Project?.PagesAssemblyPath ?? string.Empty,
                StringComparison.Ordinal);
            Assert.True(
                config.Project?.PagesAssemblyExists,
                "The Todo UAT project should be built; run dotnet build on it first.");
            Assert.False(config.HasErrors, string.Join(Environment.NewLine, config.Diagnostics));
            Assert.Contains("App from fixture", config.Summary);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string FindProject(params string[] relativeSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brinell.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var path = Path.Combine([directory.FullName, .. relativeSegments]);
        Assert.True(File.Exists(path), $"Expected a project at {path}.");
        return path;
    }
}
