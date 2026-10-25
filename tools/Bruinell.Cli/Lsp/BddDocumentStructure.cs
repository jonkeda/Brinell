using System.Text.RegularExpressions;

namespace Bruinell.Bdd.LanguageServer;

/// <summary>
/// The shape of a feature or step file, read line by line, the way an editor needs it even while the
/// file does not parse: its headings as a tree, its fences and tables, and each step line in a gherkin
/// fence with its keyword. Lines are 1-based, as the engine counts them.
/// </summary>
internal sealed partial class BddDocumentStructure
{
    private BddDocumentStructure(IReadOnlyList<BddHeading> headings, IReadOnlyList<BddBlock> blocks, IReadOnlyList<BddStepLine> steps, IReadOnlyList<int> tagLines)
    {
        Headings = headings;
        Blocks = blocks;
        Steps = steps;
        TagLines = tagLines;
    }

    /// <summary>The top-level headings, each with the ones under it.</summary>
    public IReadOnlyList<BddHeading> Headings { get; }

    /// <summary>The fences and tables.</summary>
    public IReadOnlyList<BddBlock> Blocks { get; }

    /// <summary>Each step line in a gherkin fence.</summary>
    public IReadOnlyList<BddStepLine> Steps { get; }

    /// <summary>The lines that hold only tags (<c>@smoke @maui</c>).</summary>
    public IReadOnlyList<int> TagLines { get; }

    /// <summary>Whether a line is inside a gherkin fence (between its fence lines).</summary>
    public bool InGherkin(int line) => Blocks.Any(block => block.Kind == BddBlockKind.Gherkin && line > block.Start && line < block.End);

    /// <summary>The step line at a line; null when the line is not a step.</summary>
    public BddStepLine? StepAt(int line) => Steps.FirstOrDefault(step => step.Line == line);

    /// <summary>The innermost heading that holds a line.</summary>
    public BddHeading? HeadingAt(int line) => Deepest(Headings, line);

    /// <summary>Reads a file's lines.</summary>
    /// <param name="lines">The text's lines, without their line breaks.</param>
    public static BddDocumentStructure Read(IReadOnlyList<string> lines)
    {
        List<(int Level, string Text, int Line)> flat = [];
        List<BddBlock> blocks = [];
        List<BddStepLine> steps = [];
        List<int> tagLines = [];
        int? fence = null;
        var gherkin = false;
        int? docString = null;
        int? table = null;
        BddStepKeywordKind previous = BddStepKeywordKind.Given;
        for (var i = 0; i < lines.Count; i++)
        {
            var number = i + 1;
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                if (fence is { } start)
                {
                    blocks.Add(new BddBlock(gherkin ? BddBlockKind.Gherkin : BddBlockKind.Fence, start, number));
                    fence = null;
                }
                else
                {
                    CloseTable(number - 1);
                    fence = number;
                    gherkin = trimmed[3..].Trim().Equals("gherkin", StringComparison.OrdinalIgnoreCase);
                    previous = BddStepKeywordKind.Given;
                }

                continue;
            }

            if (fence is not null)
            {
                if (!gherkin)
                {
                    continue;
                }

                // A doc string: text, not steps.
                if (trimmed.StartsWith("\"\"\"", StringComparison.Ordinal))
                {
                    if (docString is { } opened)
                    {
                        blocks.Add(new BddBlock(BddBlockKind.DocString, opened, number));
                        docString = null;
                    }
                    else
                    {
                        docString = number;
                    }

                    continue;
                }

                if (docString is not null)
                {
                    continue;
                }

                if (trimmed.StartsWith('|'))
                {
                    table ??= number;
                    continue;
                }

                CloseTable(number - 1);
                if (StepKeyword().Match(line) is { Success: true } keyword)
                {
                    var word = keyword.Groups["keyword"];
                    var kind = Kind(word.Value, previous);
                    previous = kind;
                    var text = keyword.Groups["text"];
                    steps.Add(new BddStepLine(number, word.Value, word.Index, kind, text.Value.TrimEnd(), text.Index));
                }

                continue;
            }

            if (trimmed.StartsWith('|'))
            {
                table ??= number;
                continue;
            }

            CloseTable(number - 1);
            if (Heading().Match(line) is { Success: true } heading)
            {
                flat.Add((heading.Groups["hashes"].Length, heading.Groups["text"].Value.Trim(), number));
            }
            else if (trimmed.StartsWith('@') && trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(word => word.StartsWith('@')))
            {
                tagLines.Add(number);
            }
        }

        CloseTable(lines.Count);
        if (fence is { } unclosed)
        {
            blocks.Add(new BddBlock(gherkin ? BddBlockKind.Gherkin : BddBlockKind.Fence, unclosed, lines.Count));
        }

        return new BddDocumentStructure(Tree(flat, lines.Count, tagLines), blocks, steps, tagLines);

        void CloseTable(int last)
        {
            if (table is { } first)
            {
                blocks.Add(new BddBlock(BddBlockKind.Table, first, last));
                table = null;
            }
        }
    }

    // And and But take the keyword before them, as the parser reads them.
    private static BddStepKeywordKind Kind(string keyword, BddStepKeywordKind previous) => keyword switch
    {
        "Given" => BddStepKeywordKind.Given,
        "When" => BddStepKeywordKind.When,
        "Then" => BddStepKeywordKind.Then,
        _ => previous,
    };

    // Each heading ends where the next one at its level or above starts, before that one's tags.
    private static IReadOnlyList<BddHeading> Tree(List<(int Level, string Text, int Line)> flat, int lastLine, List<int> tagLines)
    {
        List<BddHeading> top = [];
        Stack<BddHeading> open = new();
        for (var i = 0; i < flat.Count; i++)
        {
            var (level, text, line) = flat[i];
            var end = lastLine;
            for (var j = i + 1; j < flat.Count; j++)
            {
                if (flat[j].Level <= level)
                {
                    end = flat[j].Line - 1;
                    while (end > line && tagLines.Contains(end))
                    {
                        end--;
                    }

                    break;
                }
            }

            var heading = new BddHeading(level, text, line, end);
            while (open.Count > 0 && open.Peek().Level >= level)
            {
                open.Pop();
            }

            (open.Count == 0 ? top : open.Peek().Children).Add(heading);
            open.Push(heading);
        }

        return top;
    }

    private static BddHeading? Deepest(IReadOnlyList<BddHeading> headings, int line)
    {
        foreach (var heading in headings)
        {
            if (line >= heading.Line && line <= heading.End)
            {
                return Deepest(heading.Children, line) ?? heading;
            }
        }

        return null;
    }

    [GeneratedRegex(@"^(?<hashes>#{1,6})\s+(?<text>.*)$")]
    private static partial Regex Heading();

    [GeneratedRegex(@"^\s*(?<keyword>Given|When|Then|And|But)\s+(?<text>.*)$")]
    private static partial Regex StepKeyword();
}

/// <summary>A heading and the headings under it.</summary>
/// <param name="Level">1 for <c>#</c>.</param>
/// <param name="Text">The heading's text: <c>Scenario: Opening the app</c>.</param>
/// <param name="Line">Its line.</param>
/// <param name="End">The last line of its section.</param>
internal sealed record BddHeading(int Level, string Text, int Line, int End)
{
    public List<BddHeading> Children { get; } = [];

    /// <summary>The section's kind, from the word before the colon.</summary>
    public string Kind => Text.IndexOf(':') is var colon and > 0 ? Text[..colon].Trim() : Text;

    /// <summary>The name after the colon; the whole text when there is none.</summary>
    public string Name => Text.IndexOf(':') is var colon and > 0 ? Text[(colon + 1)..].Trim() : Text;
}

internal enum BddBlockKind
{
    Gherkin,
    Fence,
    Table,
    DocString,
}

/// <summary>A fence (its two fence lines), a table or a doc string.</summary>
internal sealed record BddBlock(BddBlockKind Kind, int Start, int End);

internal enum BddStepKeywordKind
{
    Given,
    When,
    Then,
}

/// <summary>A step line.</summary>
/// <param name="Line">Its line.</param>
/// <param name="Keyword">The keyword as written.</param>
/// <param name="KeywordColumn">The keyword's 0-based column.</param>
/// <param name="Effective">Given, When or Then: And and But take the one before them.</param>
/// <param name="Text">The step's text, after the keyword.</param>
/// <param name="TextColumn">The text's 0-based column.</param>
internal sealed record BddStepLine(int Line, string Keyword, int KeywordColumn, BddStepKeywordKind Effective, string Text, int TextColumn);
