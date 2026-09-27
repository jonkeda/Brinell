using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Brinell.Uat.Preview;

// One render: the engine's parse for the diagnostics, Markdig's for the blocks. Gherkin fences,
// feature, scenario and step headings, and tag lines are drawn here; everything else is Markdig's.
internal sealed partial class UatPreviewWriter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UsePreciseSourceLocation()
        .DisableHtml()
        .Build();

    private readonly string _text;
    private readonly string _filePath;
    private readonly UatPreviewOptions _options;
    private readonly string[] _lines;
    private readonly bool _isSteps;
    private readonly List<UatDiagnostic> _diagnostics = [];
    private readonly HashSet<UatDiagnostic> _shown = [];
    private readonly StringBuilder _html = new();
    private string? _nextTableClass;

    public UatPreviewWriter(string text, string filePath, UatPreviewOptions options)
    {
        _text = text;
        _filePath = filePath;
        _options = options;
        _lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        _isSteps = filePath.EndsWith(UatFileFormat.StepsExtension, StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".phrases.md", StringComparison.OrdinalIgnoreCase);
    }

    public UatPreviewResult Write()
    {
        Parse();
        var name = Path.GetFileName(_filePath);
        _html.Append($"<article class=\"uat-doc\" data-kind=\"{(_isSteps ? "steps" : "feature")}\" data-file=\"{Encode(name)}\">\n");
        if (_options.ParseOnlyReason is { } reason)
        {
            _html.Append($"<p class=\"uat-banner\" data-mode=\"parse-only\">Parsed only, not bound: {Encode(reason)}</p>\n");
        }

        var document = Markdown.Parse(_text, Pipeline);
        var blocks = document.ToList();
        WriteDiagnostics(line => blocks.Count == 0 || line < blocks[0].Line + 1);
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            var first = block.Line + 1;
            var last = i + 1 < blocks.Count ? blocks[i + 1].Line : _lines.Length;
            WriteBlock(block);
            WriteDiagnostics(line => line >= first && line <= last);
        }

        WriteDiagnostics(_ => true);
        _html.Append("</article>\n");
        return new UatPreviewResult(_html.ToString(), _diagnostics);
    }

    private void Parse()
    {
        if (UatFileFormat.NewFileName(_filePath) is { } renamed)
        {
            _diagnostics.Add(new UatDiagnostic(
                UatDiagnosticSeverity.Error,
                UatFileFormat.OldFormCode,
                $"'{Path.GetFileName(_filePath)}' is the old name of '{Path.GetFileName(renamed)}'. Run 'brinell-data validate --fix' to rename it.",
                new UatSourceLocation(_filePath, 1)));
        }

        _diagnostics.AddRange(_isSteps
            ? UatMarkdownParser.ParsePhrases(_text, _filePath).Diagnostics
            : UatMarkdownParser.Parse(_text, _filePath).Diagnostics);
        _diagnostics.Sort((a, b) => a.Location.LineNumber.CompareTo(b.Location.LineNumber));
    }

    private void WriteBlock(Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                WriteHeading(heading);
                break;
            case FencedCodeBlock fence when string.Equals(fence.Info, "gherkin", StringComparison.OrdinalIgnoreCase):
                WriteSteps(fence);
                break;
            case ParagraphBlock paragraph when TagsOf(paragraph) is { } tags:
                _html.Append($"<p class=\"uat-tags\" data-line=\"{paragraph.Line + 1}\">");
                _html.AppendJoin(' ', tags.Select(tag => $"<span class=\"uat-tag\">{Encode(tag)}</span>"));
                _html.Append("</p>\n");
                break;
            case Table table:
                if (_nextTableClass is { } tableClass)
                {
                    table.GetAttributes().AddClass(tableClass);
                }

                _nextTableClass = null;
                WriteMarkdig(table);
                break;
            default:
                WriteMarkdig(block);
                break;
        }
    }

    private void WriteHeading(HeadingBlock heading)
    {
        var line = heading.Line + 1;
        var raw = _lines[heading.Line].Trim();
        var text = (UatFileFormat.NewHeading(raw) ?? raw).TrimStart('#').Trim();
        _nextTableClass = null;
        switch (heading.Level)
        {
            case 1 when TrySplit(text, "Feature:", out var title) || TrySplit(text, "Steps:", out title):
                var keyword = text[..text.IndexOf(':', StringComparison.Ordinal)];
                _html.Append($"<h1 class=\"uat-title\" data-line=\"{line}\"><span class=\"uat-kw\">{keyword}</span> {Encode(title)}</h1>\n");
                return;
            case 2 when TrySplit(text, "Scenario Outline:", out var outline):
                _html.Append($"<h2 class=\"uat-scenario\" data-line=\"{line}\" data-scenario=\"{Encode(outline)}\"><span class=\"uat-kw\">Scenario Outline</span> {Encode(outline)}</h2>\n");
                return;
            case 2 when TrySplit(text, "Scenario:", out var scenario):
                _html.Append($"<h2 class=\"uat-scenario\" data-line=\"{line}\" data-scenario=\"{Encode(scenario)}\"><span class=\"uat-kw\">Scenario</span> {Encode(scenario)}</h2>\n");
                return;
            case 2 when text == "Background":
                _html.Append($"<h2 class=\"uat-background\" data-line=\"{line}\"><span class=\"uat-kw\">Background</span></h2>\n");
                return;
            case 2 when TrySplit(text, "Step for each row:", out var eachRow):
                _html.Append($"<h2 class=\"uat-callable\" data-line=\"{line}\" data-kind=\"each-row\"><span class=\"uat-kw\">Step for each row</span> {Mark(eachRow)}</h2>\n");
                _nextTableClass = "uat-parameters";
                return;
            case 2 when TrySplit(text, "Step:", out var step):
                _html.Append($"<h2 class=\"uat-callable\" data-line=\"{line}\"><span class=\"uat-kw\">Step</span> {Mark(step)}</h2>\n");
                _nextTableClass = "uat-parameters";
                return;
            case 2 when text == "Metadata":
                _nextTableClass = "uat-metadata";
                break;
            case 3 when text == "Examples":
                _nextTableClass = "uat-examples";
                break;
        }

        WriteMarkdig(heading);
    }

    // A fence of steps: one row per step, the keyword in a gutter, a step's table or doc string
    // under it. Lines that are not steps are shown as they are; the parser's diagnostic says why.
    private void WriteSteps(FencedCodeBlock fence)
    {
        _html.Append($"<ol class=\"uat-steps\" data-line=\"{fence.Line + 1}\">\n");
        var lines = fence.Lines.Lines.Take(fence.Lines.Count).Select(line => (Number: line.Line + 1, Text: line.Slice.ToString())).ToList();
        var open = false;
        for (var i = 0; i < lines.Count; i++)
        {
            var (number, text) = lines[i];
            var trimmed = text.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith('|'))
            {
                var rows = new List<(int Number, string Text)>();
                for (; i < lines.Count && lines[i].Text.Trim().StartsWith('|'); i++)
                {
                    rows.Add((lines[i].Number, lines[i].Text.Trim()));
                }

                i--;
                WriteStepTable(rows);
                continue;
            }

            if (trimmed.StartsWith("\"\"\"", StringComparison.Ordinal))
            {
                var body = new List<string>();
                for (i++; i < lines.Count && !lines[i].Text.Trim().StartsWith("\"\"\"", StringComparison.Ordinal); i++)
                {
                    body.Add(lines[i].Text);
                }

                _html.Append($"<pre class=\"uat-docstring\" data-line=\"{number}\">{Encode(string.Join('\n', Dedent(body)))}</pre>\n");
                continue;
            }

            if (trimmed is "<table>" or "<doc string>")
            {
                _html.Append($"<p class=\"uat-passes\" data-line=\"{number}\">passes the call's {(trimmed == "<table>" ? "table" : "doc string")}</p>\n");
                continue;
            }

            if (open)
            {
                _html.Append("</li>\n");
            }

            open = true;
            var step = StepRegex().Match(trimmed);
            _html.Append(step.Success
                ? $"<li class=\"uat-step\" data-line=\"{number}\"><span class=\"uat-kw\">{step.Groups["keyword"].Value}</span><span class=\"uat-text\">{Mark(step.Groups["text"].Value)}</span>\n"
                : $"<li class=\"uat-step uat-not-a-step\" data-line=\"{number}\"><span class=\"uat-kw\"></span><span class=\"uat-text\">{Encode(trimmed)}</span>\n");
            WriteDiagnostics(line => line == number);
        }

        if (open)
        {
            _html.Append("</li>\n");
        }

        _html.Append("</ol>\n");
    }

    private void WriteStepTable(List<(int Number, string Text)> rows)
    {
        var cells = rows.Where(row => !SeparatorRegex().IsMatch(row.Text)).Select(row => (row.Number, Cells: Cells(row.Text))).ToList();
        if (cells.Count == 0)
        {
            return;
        }

        _html.Append($"<table class=\"uat-table\" data-line=\"{cells[0].Number}\">\n<thead><tr>");
        _html.AppendJoin(string.Empty, cells[0].Cells.Select(cell => $"<th>{Mark(cell)}</th>"));
        _html.Append("</tr></thead>\n<tbody>\n");
        foreach (var (number, row) in cells.Skip(1))
        {
            _html.Append($"<tr data-line=\"{number}\">");
            _html.AppendJoin(string.Empty, row.Select(cell => $"<td>{Mark(cell)}</td>"));
            _html.Append("</tr>\n");
        }

        _html.Append("</tbody>\n</table>\n");
        foreach (var (number, _) in cells)
        {
            WriteDiagnostics(line => line == number);
        }
    }

    private void WriteMarkdig(Block block)
    {
        block.GetAttributes().AddProperty("data-line", (block.Line + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        Pipeline.Setup(renderer);
        renderer.Render(block);
        writer.Flush();
        _html.Append(writer.ToString());
    }

    private void WriteDiagnostics(Func<int, bool> atLine)
    {
        foreach (var diagnostic in _diagnostics.Where(d => !_shown.Contains(d) && atLine(d.Location.LineNumber)).ToList())
        {
            _shown.Add(diagnostic);
            var severity = diagnostic.Severity == UatDiagnosticSeverity.Error ? "error" : "warning";
            _html.Append($"<div class=\"uat-diag\" data-severity=\"{severity}\" data-code=\"{Encode(diagnostic.Code)}\" data-line=\"{diagnostic.Location.LineNumber}\">");
            _html.Append($"<span class=\"uat-diag-code\">{Encode(diagnostic.Code)}</span> {Encode(diagnostic.Message)}</div>\n");
        }
    }

    private IReadOnlyList<string>? TagsOf(ParagraphBlock paragraph)
    {
        var raw = _text.Substring(paragraph.Span.Start, paragraph.Span.Length);
        var tags = raw.Split((char[])[' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return tags.Length > 0 && tags.All(tag => tag.StartsWith('@') && tag.Length > 1) ? tags : null;
    }

    private static bool TrySplit(string text, string prefix, out string rest)
    {
        rest = text.StartsWith(prefix, StringComparison.Ordinal) ? text[prefix.Length..].Trim() : string.Empty;
        return text.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> Cells(string row)
    {
        var trimmed = row.Trim();
        trimmed = trimmed.StartsWith('|') ? trimmed[1..] : trimmed;
        trimmed = trimmed.EndsWith('|') ? trimmed[..^1] : trimmed;
        return trimmed.Split('|').Select(cell => cell.Trim()).ToList();
    }

    private static IEnumerable<string> Dedent(List<string> lines)
    {
        var indent = lines.Where(line => line.Trim().Length > 0).Select(line => line.Length - line.TrimStart().Length).DefaultIfEmpty(0).Min();
        return lines.Select(line => line.Length >= indent ? line[indent..] : line.TrimStart());
    }

    // Quoted values, {x} arguments and <x> placeholders, marked; everything encoded.
    internal static string Mark(string text)
    {
        var builder = new StringBuilder();
        var at = 0;
        foreach (Match match in TokenRegex().Matches(text))
        {
            builder.Append(Encode(text[at..match.Index]));
            var kind = match.Groups["quoted"].Success ? "uat-value" : match.Groups["argument"].Success ? "uat-argument" : "uat-placeholder";
            builder.Append($"<span class=\"{kind}\">{Encode(match.Value)}</span>");
            at = match.Index + match.Length;
        }

        builder.Append(Encode(text[at..]));
        return builder.ToString();
    }

    internal static string Encode(string text) => WebUtility.HtmlEncode(text);

    [GeneratedRegex(@"^(?<keyword>Given|When|Then|And|But)\s+(?<text>.*)$")]
    private static partial Regex StepRegex();

    [GeneratedRegex(@"^\|?\s*:?-+:?\s*(\|\s*:?-+:?\s*)*\|?$")]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"(?<quoted>""(?:[^""\\]|\\.)*"")|(?<argument>\{[^{}\s]+\})|(?<placeholder><[^<>\s][^<>]*>)")]
    private static partial Regex TokenRegex();
}
