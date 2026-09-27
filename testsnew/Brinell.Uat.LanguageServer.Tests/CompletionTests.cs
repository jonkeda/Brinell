using Xunit;

namespace Brinell.Uat.LanguageServer.Tests;

/// <summary>
/// Completion over the Todo MAUI suite, the text typed into TOD.01 in place of its <c>When I tap Sync</c>.
/// Needs the Todo sample built (<c>dotnet build samples/Todo/Brinell.Samples.Todo.slnx</c>).
/// </summary>
public sealed class CompletionTests : IAsyncLifetime
{
    private static readonly string Feature = Path.Combine(TodoPaths.Project, "Features", "TOD.01-see-my-todos.feature.md");
    private ServerHarness _server = null!;

    public async Task InitializeAsync() => _server = await ServerHarness.StartAsync(TodoPaths.Todo);

    public async Task DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task AtTheStartOfAStep_OffersTheKeywords()
    {
        var items = await CompleteAsync("Wh");

        var when = Assert.Single(items);
        Assert.Equal("When", when.Label);
        Assert.Equal("When ", when.TextEdit!.NewText);
    }

    [Fact]
    public async Task AfterTheKeyword_OffersStepTexts_ByWords_AsSnippets()
    {
        var items = await CompleteAsync("When tap");

        var tap = Assert.Single(items, item => item.Label == "I tap {control}");
        Assert.Equal("I tap ${1:control}", tap.TextEdit!.NewText);
        Assert.Equal(2, tap.InsertTextFormat);
        Assert.Equal("tap", tap.FilterText);
        Assert.All(items, item => Assert.Contains("tap", item.Label, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CallableSteps_AreOffered_ByWords()
    {
        var items = await CompleteAsync("Given synced todos");

        var callable = Assert.Single(items, item => item.Label == "the synced list has todos");
        Assert.Equal(2, callable.Kind);
        Assert.Contains("Todo.steps.md", callable.Detail);
    }

    [Fact]
    public async Task InAPageParameter_OffersThePages()
    {
        var items = await CompleteAsync("Given I am on the ");

        Assert.Contains(items, item => item.Label == "Todo List" && item.Kind == 7);
    }

    [Fact]
    public async Task InAControlParameter_OffersTheControlsOfThePageAtThatLine()
    {
        var items = await CompleteAsync("When I tap ");

        var sync = Assert.Single(items, item => item.Label == "Sync");
        Assert.Contains("page Todo List", sync.Detail);
    }

    [Fact]
    public async Task OutsideAFence_OffersTheSections()
    {
        var text = File.ReadAllText(Feature) + "\n## Sc";
        var uri = await _server.OpenAsync(Feature, text);
        var lines = text.Split('\n');

        var list = await _server.AtAsync<CompletionList>("textDocument/completion", uri, lines.Length - 1, 5);

        Assert.Contains(list.Items, item => item.Label == "## Scenario:" && item.TextEdit!.NewText == "## Scenario: ${1:name}");
    }

    // TOD.01 with its first 'When I tap Sync' replaced by the typed text, the caret at its end.
    private async Task<IReadOnlyList<CompletionItem>> CompleteAsync(string typed)
    {
        var lines = File.ReadAllText(Feature).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var line = Array.IndexOf(lines, "When I tap Sync");
        lines[line] = typed;
        var uri = await _server.OpenAsync(Feature, string.Join('\n', lines));

        return (await _server.AtAsync<CompletionList>("textDocument/completion", uri, line, typed.Length)).Items;
    }
}

/// <summary>Where the Todo sample is.</summary>
internal static class TodoPaths
{
    public static readonly string Todo = Find();

    public static string Project => Path.Combine(Todo, "tests", "Brinell.Samples.Todo.Uat");

    private static string Find()
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
