using Xunit;

namespace Brinell.Uat.LanguageServer.Tests;

/// <summary>The outline, folding and semantic colours: from the file's shape, before any build.</summary>
public sealed class StructureTests : IAsyncLifetime
{
    private const string Text = """
        # Feature: Shelf

        ## Metadata

        | Field | Value |
        | --- | --- |
        | Area | Shelf |

        @smoke @shelf
        ## Scenario: One

        ```gherkin
        Given I am on the Shelf page
        When I shelve "Dune" on {shelf}
          | Title |
          | <title> |
        # a comment
        Then the shelf holds 1 book
        ```

        ## Step: I tidy up

        ```gherkin
        When I dust the shelf
        ```
        """;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Brinell", "StructureTests", Guid.NewGuid().ToString("N"));
    private ServerHarness _server = null!;
    private string _uri = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "shelf.feature.md");
        File.WriteAllText(path, Text);
        _server = await ServerHarness.StartAsync(_folder);
        _uri = await _server.OpenAsync(path);
    }

    public async Task DisposeAsync()
    {
        await _server.DisposeAsync();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void Initialize_OffersTheOutline_Folding_AndColours()
    {
        var capabilities = _server.Initialized.Capabilities;

        Assert.True(capabilities.DocumentSymbolProvider);
        Assert.True(capabilities.FoldingRangeProvider);
        Assert.Equal(["keyword", "string", "parameter", "variable", "comment", "macro"], capabilities.SemanticTokensProvider!.Legend.TokenTypes);
    }

    [Fact]
    public async Task TheOutline_IsTheHeadingTree_ByKind()
    {
        var symbols = await _server.OfAsync<IReadOnlyList<DocumentSymbol>>("textDocument/documentSymbol", _uri);

        var feature = Assert.Single(symbols);
        Assert.Equal(("Shelf", "Feature", 2), (feature.Name, feature.Detail, feature.Kind));
        Assert.Equal(["Metadata", "One", "I tidy up"], feature.Children.Select(child => child.Name));
        Assert.Equal([3, 6, 12], feature.Children.Select(child => child.Kind));
        var scenario = feature.Children[1];
        Assert.Equal(9, scenario.SelectionRange.Start.Line);
        Assert.Equal(19, scenario.Range.End.Line);
    }

    [Fact]
    public async Task Folding_CoversSections_Fences_AndTables()
    {
        var ranges = await _server.OfAsync<IReadOnlyList<FoldingRange>>("textDocument/foldingRange", _uri);

        Assert.Contains(new FoldingRange(9, 18), ranges);
        Assert.Contains(new FoldingRange(2, 6), ranges);
        Assert.Contains(new FoldingRange(11, 18), ranges);
        Assert.Contains(new FoldingRange(4, 6), ranges);
        Assert.Contains(new FoldingRange(14, 15), ranges);
    }

    [Fact]
    public async Task Colours_MarkKeywords_Values_Placeholders_Tags_AndComments()
    {
        var tokens = Decode((await _server.OfAsync<SemanticTokens>("textDocument/semanticTokens/full", _uri)).Data);

        Assert.Contains((0, 2, 7, "keyword"), tokens);
        Assert.Contains((8, 0, 6, "macro"), tokens);
        Assert.Contains((8, 7, 6, "macro"), tokens);
        Assert.Contains((12, 0, 5, "keyword"), tokens);
        Assert.Contains((13, 0, 4, "keyword"), tokens);
        Assert.Contains((13, 14, 6, "string"), tokens);
        Assert.Contains((13, 24, 7, "parameter"), tokens);
        Assert.Contains((15, 4, 7, "variable"), tokens);
        Assert.Contains((16, 0, 11, "comment"), tokens);
        Assert.DoesNotContain(tokens, token => token.Line == 4);
    }

    private static List<(int Line, int Column, int Length, string Type)> Decode(IReadOnlyList<int> data)
    {
        string[] types = ["keyword", "string", "parameter", "variable", "comment", "macro"];
        List<(int, int, int, string)> tokens = [];
        var line = 0;
        var column = 0;
        for (var i = 0; i < data.Count; i += 5)
        {
            column = data[i] == 0 ? column + data[i + 1] : data[i + 1];
            line += data[i];
            tokens.Add((line, column, data[i + 2], types[data[i + 3]]));
        }

        return tokens;
    }
}
