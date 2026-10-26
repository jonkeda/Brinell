using System.Text;
using System.Text.RegularExpressions;
using Bruinet.Core.Testing;
using StreamJsonRpc;

namespace Bruinet.Bdd.LanguageServer;

// Completion (3.3): keywords and sections, step texts by words with their placeholders as snippet
// stops, and values for the parameter the caret is in: pages, the controls of the page at that
// line, users and apps, enum members, and an outline's Examples columns.
public sealed partial class BddLanguageServer
{
    private const int MostSteps = 200;

    private static readonly string[] Keywords = ["Given", "When", "Then", "And", "But"];

    private static readonly (string Label, string Text, bool StepsFile)[] Sections =
    [
        ("## Scenario:", "## Scenario: ${1:name}", false),
        ("## Scenario Outline:", "## Scenario Outline: ${1:name}", false),
        ("## Background", "## Background", false),
        ("### Examples", "### Examples", false),
        ("## Metadata", "## Metadata", false),
        ("## Step:", "## Step: ${1:phrase}", true),
        ("## Step for each row:", "## Step for each row: ${1:phrase}", true),
        ("```gherkin", "```gherkin\n$0\n```", false),
    ];

    /// <summary>LSP <c>textDocument/completion</c>.</summary>
    [JsonRpcMethod("textDocument/completion")]
    public CompletionList Completion(PositionParams request)
    {
        var file = Context(Key(request.TextDocument.Uri));
        var line = request.Position.Line + 1;
        var text = file.Line(line);
        var column = Math.Clamp(request.Position.Character, 0, text.Length);
        var before = text[..column];
        var structure = file.Structure;
        if (IsConfig(file.Path))
        {
            return ConfigCompletion(file, line, column);
        }

        if (!structure.InGherkin(line))
        {
            return MetadataCompletion(file, line, column) ?? new CompletionList(false, SectionItems(file, line, before));
        }

        if (structure.Blocks.Any(block => block.Kind is BddBlockKind.Table or BddBlockKind.DocString && line >= block.Start && line <= block.End))
        {
            return new CompletionList(false, []);
        }

        if (structure.StepAt(line) is not { } step || column < step.TextColumn)
        {
            var word = before.TrimStart();
            return new CompletionList(false, Keywords
                .Where(keyword => keyword.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                .Select(keyword => new CompletionItem(keyword, 14)
                {
                    TextEdit = new TextEdit(new Range(new Position(line - 1, before.Length - word.Length), new Position(line - 1, column)), keyword + " "),
                })
                .ToList());
        }

        var typed = before[step.TextColumn..];
        var values = ValueItems(file, step, typed, line, column);
        return values.Count > 0
            ? new CompletionList(false, values)
            : new CompletionList(true, StepItems(file, step, typed, line, column));
    }

    private static List<CompletionItem> SectionItems(BddServerFile file, int line, string before)
    {
        if (before.Trim() is { Length: > 0 } written && !written.StartsWith('#') && !written.StartsWith('`'))
        {
            return [];
        }

        var steps = file.Path.EndsWith(BddFileFormat.StepsExtension, StringComparison.OrdinalIgnoreCase);
        var range = new Range(new Position(line - 1, 0), new Position(line - 1, before.Length));
        return Sections
            .Where(section => section.StepsFile == steps || section.Label.StartsWith("```", StringComparison.Ordinal))
            .Select(section => new CompletionItem(section.Label, 15)
            {
                TextEdit = new TextEdit(range, section.Text),
                InsertTextFormat = 2,
                FilterText = before,
            })
            .ToList();
    }

    // Step texts that hold every typed word, the page's own first, as snippets.
    private List<CompletionItem> StepItems(BddServerFile file, BddStepLine step, string typed, int line, int column)
    {
        var words = typed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(word => !word.Contains('"', StringComparison.Ordinal)).ToList();
        var page = PageAt(file, line);
        var range = new Range(new Position(line - 1, step.TextColumn), new Position(line - 1, column));
        List<(string Phrase, string Detail, int Kind, int Rank)> candidates = [];
        if (file.Catalogs is { } catalogs)
        {
            var keyword = Enum.Parse<BddEffectiveStepKeyword>(step.Effective.ToString());
            foreach (var pattern in catalogs.Default.Patterns.Where(pattern => pattern.Keyword == keyword && pattern.Callable is null))
            {
                var onPage = pattern.Page is { } own && page is not null && string.Equals(PageName(own), PageName(page), StringComparison.Ordinal);
                var scope = pattern.Page is { } pageOf ? $" · page {PageName(pageOf)}" : pattern.Namespace.Length > 0 ? $" · {pattern.Namespace}" : string.Empty;
                candidates.Add((pattern.Phrase, pattern.CommandId + scope, 3, onPage ? 0 : pattern.Page is null ? 1 : 2));
            }
        }

        foreach (var callable in VisibleCallables(file))
        {
            candidates.Add((callable.Phrase, $"{callable} ({Path.GetFileName(callable.Source.FilePath)})", 2, 1));
        }

        return candidates
            .Where(candidate => words.All(word => candidate.Phrase.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .DistinctBy(candidate => candidate.Phrase, StringComparer.Ordinal)
            .OrderBy(candidate => candidate.Rank)
            .ThenBy(candidate => candidate.Phrase, StringComparer.OrdinalIgnoreCase)
            .Take(MostSteps)
            .Select((candidate, index) => new CompletionItem(candidate.Phrase, candidate.Kind)
            {
                Detail = candidate.Detail,
                TextEdit = new TextEdit(range, Snippet(candidate.Phrase)),
                InsertTextFormat = 2,
                FilterText = typed,
                SortText = index.ToString("D4", System.Globalization.CultureInfo.InvariantCulture),
            })
            .ToList();
    }

    // The values of the parameter the caret is in, for every step text it could be.
    private List<CompletionItem> ValueItems(BddServerFile file, BddStepLine step, string typed, int line, int column)
    {
        List<(string Name, Type? Type)> parameters = [];
        if (file.Catalogs is { } catalogs)
        {
            var keyword = Enum.Parse<BddEffectiveStepKeyword>(step.Effective.ToString());
            foreach (var pattern in catalogs.Default.Patterns.Where(pattern => pattern.Keyword == keyword))
            {
                if (ParameterAt(pattern.Phrase, typed) is { } name)
                {
                    parameters.Add((name, pattern.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Type));
                }
            }
        }

        foreach (var callable in VisibleCallables(file))
        {
            if (ParameterAt(callable.Phrase, typed) is { } name)
            {
                parameters.Add((name, null));
            }
        }

        // A '<' begun: the outline's columns, bound or not.
        if (parameters.Count == 0)
        {
            var placeholder = Regex.Match(typed, "<[^<>\"]*$");
            return !placeholder.Success
                ? []
                : ExamplesColumns(file, line)
                    .Select(header => new CompletionItem($"<{header}>", 6)
                    {
                        Detail = "Examples column",
                        TextEdit = new TextEdit(new Range(new Position(line - 1, column - placeholder.Length), new Position(line - 1, column)), $"<{header}>"),
                        FilterText = placeholder.Value,
                    })
                    .ToList();
        }

        var partial = Regex.Match(typed, "(\"[^\"]*|[^\\s\"]*)$").Value;
        var quoted = partial.StartsWith('"');
        var range = new Range(new Position(line - 1, column - partial.Length), new Position(line - 1, column));
        List<(string Value, string Detail, int Kind)> values = [];
        foreach (var (name, type) in parameters.Distinct())
        {
            values.AddRange(ValuesOf(file, name, type, line));
        }

        values.AddRange(ExamplesColumns(file, line).Select(header => ($"<{header}>", "Examples column", 6)));
        return values
            .DistinctBy(value => value.Value, StringComparer.Ordinal)
            .Select(value => new CompletionItem(value.Value, value.Kind)
            {
                Detail = value.Detail,
                TextEdit = new TextEdit(range, quoted ? $"\"{value.Value}\"" : value.Value),
                FilterText = partial,
            })
            .ToList();
    }

    private IEnumerable<(string Value, string Detail, int Kind)> ValuesOf(BddServerFile file, string name, Type? type, int line)
    {
        if (file.Catalogs is not { } catalogs)
        {
            yield break;
        }

        var pages = UatPageSet.Of(catalogs.Default);
        switch (name.ToLowerInvariant())
        {
            case "page":
                foreach (var page in pages.Values.OrderBy(page => page.Name, StringComparer.OrdinalIgnoreCase))
                {
                    yield return (page.Name, $"page · {page.PageType.Name}", 7);
                }

                yield break;
            case "control":
                var current = PageAt(file, line) is { } key ? pages.Values.FirstOrDefault(page => page.Key == key || page.Name == PageName(key)) : null;
                foreach (var control in (current is null ? pages.Values.SelectMany(page => page.Controls) : current.Controls).DistinctBy(control => control.Name))
                {
                    yield return (control.Name, $"control · {control.ControlType.Name}{(current is null ? string.Empty : $" · page {current.Name}")}", 5);
                }

                yield break;
            case "user":
                foreach (var user in catalogs.Apps.Select(app => app.User).Distinct(StringComparer.Ordinal))
                {
                    yield return (user, "user", 12);
                }

                yield break;
            case "app":
                foreach (var app in catalogs.Apps)
                {
                    yield return (app.Name, "app", 12);
                }

                yield break;
        }

        var underlying = type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
        if (underlying?.IsEnum == true)
        {
            foreach (var member in Enum.GetNames(underlying))
            {
                yield return (member, underlying.Name, 20);
            }
        }
        else if (underlying == typeof(bool))
        {
            yield return ("true", "bool", 12);
            yield return ("false", "bool", 12);
        }
    }

    // The columns of the Examples tables of the outline around a line.
    private static IEnumerable<string> ExamplesColumns(BddServerFile file, int line)
    {
        var structure = file.Structure;
        var outline = Flatten(structure.Headings).LastOrDefault(heading => heading.Kind == "Scenario Outline" && line >= heading.Line && line <= heading.End);
        if (outline is null)
        {
            return [];
        }

        return Flatten(outline.Children)
            .Where(heading => heading.Kind.StartsWith("Examples", StringComparison.Ordinal))
            .SelectMany(examples => structure.Blocks.Where(block => block.Kind == BddBlockKind.Table && block.Start > examples.Line && block.Start <= examples.End).Take(1))
            .SelectMany(table => file.Line(table.Start).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.Ordinal);
    }

    /// <summary>The page the scenario is on just before a line, as the binder followed it; null when not known.</summary>
    internal string? PageAt(BddServerFile file, int line)
    {
        var analysis = Analyze(file);
        var heading = file.Structure.HeadingAt(line);
        var scenario = analysis.Scenarios.FirstOrDefault(scenario => heading is not null && scenario.Source.Source.LineNumber == heading.Line);
        if (scenario is null)
        {
            return null;
        }

        string? current = null;
        foreach (var invocation in scenario.Invocations.Where(invocation => WrittenLine(invocation) < line))
        {
            var named = invocation.Command.NamesPage ? invocation.Arguments.GetValueOrDefault("page") : null;
            current = invocation.PageAfter ?? named ?? (invocation.Command.ControlVerb?.Navigates == true ? null : invocation.Command.Page ?? current);
        }

        return current;
    }

    /// <summary>The callable steps a file sees: its own, and those of the step files in its folder and above.</summary>
    internal static IReadOnlyList<BddCallable> VisibleCallables(BddServerFile file)
    {
        var own = UatFileAnalysisCallables(file);
        return
        [
            .. own,
            .. BddScenarioFiles.PhraseFilesFor(file.Path)
                .Where(path => !string.Equals(path, file.Path, StringComparison.OrdinalIgnoreCase))
                .SelectMany(path => BddScenarioFiles.ParsePhraseFile(path).Document?.Callables ?? []),
        ];
    }

    private static IReadOnlyList<BddCallable> UatFileAnalysisCallables(BddServerFile file)
        => Bruinet.Bdd.Preview.BddFileAnalysis.IsStepFile(file.Path)
            ? BddScenarioFiles.ParsePhrases(file.Path, file.Text).Document?.Callables ?? []
            : BddMarkdownParser.Parse(file.Text, file.Path).Document?.Callables.Select(callable => callable with { Source = callable.Source with { FilePath = file.Path } }).ToList() ?? [];

    // The line a step is written on: its own, or the outermost call's for a callable's step.
    internal static int WrittenLine(BddStepInvocation invocation)
    {
        if (invocation.Call is not { } frame)
        {
            return invocation.Step.Source.LineNumber;
        }

        while (frame.Parent is { } parent)
        {
            frame = parent;
        }

        return frame.Call.Source.LineNumber;
    }

    /// <summary>The parameter the typed text ends in, for a phrase; null when it is not in one.</summary>
    internal static string? ParameterAt(string phrase, string typed)
    {
        var placeholders = Regex.Matches(phrase, @"\{([A-Za-z][A-Za-z0-9_-]*)\}");
        for (var i = placeholders.Count - 1; i >= 0; i--)
        {
            StringBuilder pattern = new("^");
            var last = 0;
            for (var j = 0; j <= i; j++)
            {
                pattern.Append(Literal(phrase[last..placeholders[j].Index]));
                pattern.Append(j < i ? "(?:.+?)" : "(?<partial>\"[^\"]*|[^\"]*)$");
                last = placeholders[j].Index + placeholders[j].Length;
            }

            if (Regex.IsMatch(typed, pattern.ToString(), RegexOptions.CultureInvariant))
            {
                return placeholders[i].Groups[1].Value;
            }
        }

        return null;
    }

    // A phrase's literal text as the engine matches it: any run of spaces matches any run.
    private static string Literal(string text) => Regex.Replace(Regex.Escape(text), @"(\\ )+|\s+", @"\s+");

    // A phrase as a snippet: each {x} a tab stop named x.
    private static string Snippet(string phrase)
    {
        var index = 0;
        var escaped = phrase.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("$", "\\$", StringComparison.Ordinal);
        return Regex.Replace(escaped, @"\{([A-Za-z][A-Za-z0-9_-]*)\}", match => $"${{{++index}:{match.Groups[1].Value}}}");
    }

    private static string PageName(string page) => page[(page.LastIndexOf('/') + 1)..];
}
