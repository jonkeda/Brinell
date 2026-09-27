using Xunit;

namespace Brinell.Uat.LanguageServer.Tests;

/// <summary>
/// Hover, go to definition and find references over the Todo MAUI suite. Needs the Todo sample built
/// (<c>dotnet build samples/Todo/Brinell.Samples.Todo.slnx</c>).
/// </summary>
public sealed class NavigationTests : IAsyncLifetime
{
    private static readonly string Feature = Path.Combine(TodoPaths.Project, "Features", "TOD.01-see-my-todos.feature.md");
    private static readonly string Steps = Path.Combine(TodoPaths.Project, "Features", "Todo.steps.md");
    private static readonly string[] Lines = File.ReadAllLines(Feature);
    private ServerHarness _server = null!;
    private string _uri = null!;

    public async Task InitializeAsync()
    {
        _server = await ServerHarness.StartAsync(TodoPaths.Todo);
        _uri = await _server.OpenAsync(Feature);
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task Hover_OnAControlVerb_NamesThePageItRunsOn()
    {
        var hover = await _server.AtAsync<Hover>("textDocument/hover", _uri, LineOf("When I tap Sync"), 3);

        Assert.Contains("**binds to**", hover.Contents.Value);
        Assert.Contains("page Todo List", hover.Contents.Value);
    }

    [Fact]
    public async Task Hover_OnACall_ShowsTheCallableAndItsBody()
    {
        var hover = await _server.AtAsync<Hover>("textDocument/hover", _uri, LineOf("Given the synced list has todos"), 3);

        Assert.Contains("**calls** `Step: the synced list has todos` (Todo.steps.md:3)", hover.Contents.Value);
        Assert.Contains("- When I tap Sync", hover.Contents.Value);
    }

    [Fact]
    public async Task Definition_OfACall_IsTheStepHeading()
    {
        var location = Assert.Single(await _server.AtAsync<IReadOnlyList<Location>>("textDocument/definition", _uri, LineOf("Given the synced list has todos"), 3));

        Assert.Equal(new Uri(Steps).AbsoluteUri, location.Uri);
        Assert.Equal(2, location.Range.Start.Line);
    }

    [Fact]
    public async Task Definition_OfAControlVerb_IsTheControlOnThePage()
    {
        var location = Assert.Single(await _server.AtAsync<IReadOnlyList<Location>>("textDocument/definition", _uri, LineOf("When I tap Sync"), 3));

        var path = new Uri(location.Uri).LocalPath;
        Assert.EndsWith(".cs", path);
        Assert.Contains("Sync", File.ReadAllLines(path)[location.Range.Start.Line]);
    }

    [Fact]
    public async Task Definition_OfAPageStep_IsThePageClass()
    {
        var location = Assert.Single(await _server.AtAsync<IReadOnlyList<Location>>("textDocument/definition", _uri, LineOf("Given I am on the Todo List page"), 3));

        Assert.Matches(@"\b(class|record)\s+\w+", File.ReadAllLines(new Uri(location.Uri).LocalPath)[location.Range.Start.Line]);
    }

    [Fact]
    public async Task References_OfAStep_AreEveryStepAcrossTheSuiteBindingToIt()
    {
        var locations = await _server.AtAsync<IReadOnlyList<Location>>("textDocument/references", _uri, LineOf("When I tap Sync"), 3, new { includeDeclaration = false });

        Assert.True(locations.Select(location => location.Uri).Distinct().Count() > 1);
        Assert.All(locations, location => Assert.Contains(" tap", File.ReadAllLines(new Uri(location.Uri).LocalPath)[location.Range.Start.Line]));
    }

    [Fact]
    public async Task References_OfAStepHeading_AreItsCalls()
    {
        var steps = await _server.OpenAsync(Steps);

        var locations = await _server.AtAsync<IReadOnlyList<Location>>("textDocument/references", steps, 2, 5, new { includeDeclaration = true });

        Assert.Contains(locations, location => location.Uri == _uri && location.Range.Start.Line == LineOf("Given the synced list has todos"));
        Assert.Contains(locations, location => location.Uri == steps && location.Range.Start.Line == 2);
    }

    private static int LineOf(string text) => Array.IndexOf(Lines, text);
}
