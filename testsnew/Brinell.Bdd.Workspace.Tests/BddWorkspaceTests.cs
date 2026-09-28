using Xunit;

namespace Brinell.Bdd.Workspace.Tests;

/// <summary>The Todo sample's suites, found and told apart by their files, without a build.</summary>
public sealed class BddWorkspaceTests
{
    private static readonly string Todo = FindTodo();

    [Fact]
    public void FindConfigs_FindsEveryTodoSuite_UiAndService()
    {
        var configs = BddWorkspace.FindConfigs(Todo).Select(path => Path.GetRelativePath(Todo, path).Replace('\\', '/')).ToList();

        Assert.Equal(
            [
                "tests/Brinell.Samples.Todo.IntegrationTests/bdd.config.md",
                "tests/Brinell.Samples.Todo.Mixed.Uat/uat.config.md",
                "tests/Brinell.Samples.Todo.Uat/uat.config.md",
                "tests/Brinell.Samples.Todo.Web.Uat/uat.config.md",
            ],
            configs);
    }

    [Fact]
    public void SuitesFor_AFileLinkedIntoSeveralSuites_NamesEachOfThem()
    {
        using var workspace = BddWorkspace.Open(Todo);

        var suites = workspace.SuitesFor(Feature("TOD.01-see-my-todos.feature.md")).Select(suite => suite.Name).ToList();

        Assert.Contains("Brinell.Samples.Todo.Uat", suites);
        Assert.Contains("Brinell.Samples.Todo.Web.Uat", suites);
        Assert.Contains("Brinell.Samples.Todo.IntegrationTests", suites);
    }

    [Fact]
    public void SuitesFor_AFileTheWebSuiteExcludes_IsNotTheWebSuites()
    {
        using var workspace = BddWorkspace.Open(Todo);

        var suites = workspace.SuitesFor(Feature("TOD.11-manage-users.feature.md")).Select(suite => suite.Name).ToList();

        Assert.Contains("Brinell.Samples.Todo.Uat", suites);
        Assert.DoesNotContain("Brinell.Samples.Todo.Web.Uat", suites);
    }

    [Fact]
    public void SuitesFor_AStepFile_FollowsItsLinks()
    {
        using var workspace = BddWorkspace.Open(Todo);

        var suites = workspace.SuitesFor(Feature("Todo.steps.md")).Select(suite => suite.Name).ToList();

        Assert.Contains("Brinell.Samples.Todo.Web.Uat", suites);
    }

    private static string Feature(string name) => Path.Combine(Todo, "tests", "Brinell.Samples.Todo.Uat", "Features", name);

    private static string FindTodo()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var todo = Path.Combine(folder.FullName, "samples", "Todo");
            if (Directory.Exists(todo))
            {
                return todo;
            }
        }

        throw new DirectoryNotFoundException("samples/Todo is not above the test's folder.");
    }
}
