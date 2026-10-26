using StreamJsonRpc;

namespace Bruinet.Bdd.LanguageServer;

// The outline, folding and semantic colours (3.2), from the file's shape alone: they work before a
// build and while the file does not parse. Visual Studio has no TextMate grammar for these files,
// so the colours come from here.
public sealed partial class BddLanguageServer
{
    // The legend's order is the token type's number in the data.
    private static readonly string[] TokenTypes = ["keyword", "string", "parameter", "variable", "comment", "macro"];

    private static readonly HashSet<string> SectionKeywords = new(StringComparer.Ordinal)
    {
        "Feature", "Steps", "Scenario", "Scenario Outline", "Background", "Examples", "Step", "Step for each row", "Rule",
    };

    /// <summary>LSP <c>textDocument/documentSymbol</c>: the headings as a tree.</summary>
    [JsonRpcMethod("textDocument/documentSymbol")]
    public IReadOnlyList<DocumentSymbol> DocumentSymbols(DocumentParams request)
    {
        var file = Context(Key(request.TextDocument.Uri));
        return file.Structure.Headings.Select(heading => Symbol(file, heading)).ToList();
    }

    /// <summary>LSP <c>textDocument/foldingRange</c>: sections, fences, tables and doc strings.</summary>
    [JsonRpcMethod("textDocument/foldingRange")]
    public IReadOnlyList<FoldingRange> FoldingRanges(DocumentParams request)
    {
        var file = Context(Key(request.TextDocument.Uri));
        var structure = file.Structure;
        return Flatten(structure.Headings)
            .Select(heading => (Start: heading.Line, End: LastNonBlank(heading)))
            .Concat(structure.Blocks.Select(block => (block.Start, block.End)))
            .Where(range => range.End > range.Start)
            .Distinct()
            .OrderBy(range => range.Start)
            .Select(range => new FoldingRange(range.Start - 1, range.End - 1))
            .ToList();

        int LastNonBlank(BddHeading heading)
        {
            var end = heading.End;
            while (end > heading.Line && string.IsNullOrWhiteSpace(file.Line(end)))
            {
                end--;
            }

            return end;
        }
    }

    /// <summary>LSP <c>textDocument/semanticTokens/full</c>.</summary>
    [JsonRpcMethod("textDocument/semanticTokens/full")]
    public SemanticTokens SemanticTokensFull(DocumentParams request)
    {
        var file = Context(Key(request.TextDocument.Uri));
        return new SemanticTokens(Encode(Tokens(file)));
    }

    internal static IReadOnlyList<(int Line, int Column, int Length, int Type)> Tokens(BddServerFile file)
    {
        var structure = file.Structure;
        List<(int Line, int Column, int Length, int Type)> tokens = [];
        foreach (var heading in Flatten(structure.Headings))
        {
            if (SectionKeywords.Contains(heading.Kind) && heading.Text.Contains(':', StringComparison.Ordinal))
            {
                var text = file.Line(heading.Line);
                var column = text.IndexOf(heading.Kind, StringComparison.Ordinal);
                tokens.Add((heading.Line, column, heading.Kind.Length, Type("keyword")));
            }
        }

        foreach (var line in structure.TagLines)
        {
            Words(line, file.Line(line), '@', Type("macro"));
        }

        foreach (var step in structure.Steps)
        {
            tokens.Add((step.Line, step.KeywordColumn, step.Keyword.Length, Type("keyword")));
            Values(step.Line, file.Line(step.Line), step.TextColumn);
        }

        // Placeholders in an outline's or a callable's tables, and comments, inside gherkin fences.
        foreach (var block in structure.Blocks.Where(block => block.Kind == BddBlockKind.Gherkin))
        {
            for (var line = block.Start + 1; line < block.End; line++)
            {
                var text = file.Line(line);
                if (text.TrimStart().StartsWith('#'))
                {
                    tokens.Add((line, text.Length - text.TrimStart().Length, text.Trim().Length, Type("comment")));
                }
                else if (text.TrimStart().StartsWith('|') || text.Trim() is ['<', .., '>'])
                {
                    Values(line, text, 0);
                }
            }
        }

        return tokens.Where(token => token.Length > 0).OrderBy(token => token.Line).ThenBy(token => token.Column).ToList();

        // Quoted values, {x} parameters and <x> placeholders, left to right, never overlapping.
        void Values(int line, string text, int from)
        {
            for (var i = from; i < text.Length; i++)
            {
                if (text[i] == '"' && text.IndexOf('"', i + 1) is var close and > 0)
                {
                    tokens.Add((line, i, close - i + 1, Type("string")));
                    i = close;
                }
                else if (text[i] is '{' or '<' && Placeholder(text, i) is > 0 and var length)
                {
                    tokens.Add((line, i, length, Type(text[i] == '{' ? "parameter" : "variable")));
                    i += length - 1;
                }
            }
        }

        void Words(int line, string text, char start, int type)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == start)
                {
                    var end = i;
                    while (end < text.Length && !char.IsWhiteSpace(text[end]))
                    {
                        end++;
                    }

                    tokens.Add((line, i, end - i, type));
                    i = end;
                }
            }
        }
    }

    // {name} or <name>: the whole placeholder's length, or 0 when it is not one.
    private static int Placeholder(string text, int start)
    {
        var close = text[start] == '{' ? '}' : '>';
        var end = start + 1;
        while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] is '_' or '-' or ' ' or ':'))
        {
            end++;
        }

        return end < text.Length && text[end] == close && end > start + 1 ? end - start + 1 : 0;
    }

    private static int Type(string name) => Array.IndexOf(TokenTypes, name);

    // LSP's relative encoding: line and start relative to the token before; 1-based lines to 0-based.
    private static IReadOnlyList<int> Encode(IReadOnlyList<(int Line, int Column, int Length, int Type)> tokens)
    {
        List<int> data = new(tokens.Count * 5);
        var line = 0;
        var column = 0;
        foreach (var token in tokens)
        {
            var tokenLine = token.Line - 1;
            data.Add(tokenLine - line);
            data.Add(tokenLine == line ? token.Column - column : token.Column);
            data.Add(token.Length);
            data.Add(token.Type);
            data.Add(0);
            line = tokenLine;
            column = token.Column;
        }

        return data;
    }

    private static DocumentSymbol Symbol(BddServerFile file, BddHeading heading)
    {
        var kind = heading.Kind switch
        {
            "Feature" or "Steps" => 2,
            "Scenario" or "Scenario Outline" => 6,
            "Background" => 9,
            "Examples" => 18,
            "Step" or "Step for each row" => 12,
            _ => 3,
        };
        var named = heading.Text.Contains(':', StringComparison.Ordinal) && heading.Name.Length > 0;
        var last = Math.Max(heading.Line, heading.End);
        return new DocumentSymbol(
            named ? heading.Name : heading.Text,
            named ? heading.Kind : null,
            kind,
            new Range(new Position(heading.Line - 1, 0), new Position(last - 1, file.Line(last).Length)),
            new Range(new Position(heading.Line - 1, 0), new Position(heading.Line - 1, file.Line(heading.Line).Length)),
            heading.Children.Select(child => Symbol(file, child)).ToList());
    }

    private static IEnumerable<BddHeading> Flatten(IEnumerable<BddHeading> headings)
        => headings.SelectMany(heading => Flatten(heading.Children).Prepend(heading));
}
