using System.Text.RegularExpressions;

namespace Brinell.Uat;

public static class UatMarkdownParser
{
    private const string DocStringDelimiter = "\"\"\"";

    private const string TableMarker = "<table>";
    private const string DocStringMarker = "<doc string>";

    public static UatParseResult Parse(string markdown, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var parser = new Parser(markdown, filePath);
        return parser.Parse();
    }

    public static UatParseResult ParseFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Parse(File.ReadAllText(filePath), filePath);
    }

    /// <summary>Parses a <c>*.phrases.md</c> file: one <c># Phrases:</c> heading and only phrase sections.</summary>
    /// <param name="markdown">The file's text.</param>
    /// <param name="filePath">The file, for locations.</param>
    public static UatPhrasesParseResult ParsePhrases(string markdown, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        return new Parser(markdown, filePath).ParsePhrases();
    }

    private sealed class Parser
    {
        private readonly List<UatDiagnostic> _diagnostics = [];
        private readonly string? _filePath;
        private readonly Line[] _lines;
        private int _index;

        public Parser(string markdown, string? filePath)
        {
            _filePath = filePath;
            _lines = SplitLines(markdown);
        }

        public UatParseResult Parse()
        {
            string? title = null;
            UatSourceLocation? titleSource = null;
            Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
            List<UatStep> background = [];
            List<UatNamedDataTable> dataTables = [];
            List<UatScenario> scenarios = [];
            List<UatCallable> callables = [];
            List<string> pendingTags = [];

            while (!End)
            {
                SkipBlankLines();
                if (End)
                {
                    break;
                }

                var line = Current;
                var trimmed = line.Text.Trim();

                if (TryParseTags(trimmed, out var tags))
                {
                    pendingTags.AddRange(tags);
                    _index++;
                    continue;
                }

                if (pendingTags.Count > 0 && !IsScenarioHeading(trimmed))
                {
                    AddError("UAT020", "Tag lines must be immediately followed by a scenario heading.", line);
                    pendingTags.Clear();
                }

                if (!TryParseHeading(trimmed, out var level, out var headingText))
                {
                    AddError("UAT021", $"Unexpected content outside a UAT section: '{trimmed}'.", line);
                    _index++;
                    continue;
                }

                if (level == 1 && headingText.StartsWith("UAT:", StringComparison.Ordinal))
                {
                    if (title is not null)
                    {
                        AddError("UAT001", "Document must contain exactly one '# UAT:' heading.", line);
                    }

                    title = headingText["UAT:".Length..].Trim();
                    titleSource = Location(line);
                    if (title.Length == 0)
                    {
                        AddError("UAT002", "UAT heading must include a title.", line);
                    }

                    _index++;
                    continue;
                }

                if (title is null)
                {
                    AddError("UAT003", "Document must start with a '# UAT:' heading before other sections.", line);
                }

                if (level == 2 && headingText.Equals("Metadata", StringComparison.Ordinal))
                {
                    _index++;
                    metadata = ParseMetadataTable();
                    continue;
                }

                if (level == 2 && headingText.Equals("Background", StringComparison.Ordinal))
                {
                    _index++;
                    background = ParseStepBlock(stopAtExamples: false);
                    if (background.Count == 0)
                    {
                        AddError("UAT004", "Background section must contain at least one step.", line);
                    }

                    continue;
                }

                if (level == 2 && headingText.StartsWith("Data:", StringComparison.Ordinal))
                {
                    var dataName = headingText["Data:".Length..].Trim();
                    if (dataName.Length == 0)
                    {
                        AddError("UAT005", "Data section must include a name.", line);
                    }

                    _index++;
                    var table = ParseRequiredTable("Data section must contain a Markdown table.");
                    if (table is not null)
                    {
                        dataTables.Add(new UatNamedDataTable(dataName, table, Location(line)));
                    }

                    continue;
                }

                if (level == 2 && TryParsePhraseHeading(headingText, out var kind, out var phrase))
                {
                    _index++;
                    if (ParsePhraseSection(kind, phrase, line) is { } callable)
                    {
                        callables.Add(callable);
                    }

                    continue;
                }

                if (level == 2 && headingText.StartsWith("Scenario Outline:", StringComparison.Ordinal))
                {
                    var scenarioName = headingText["Scenario Outline:".Length..].Trim();
                    _index++;
                    var outlineSteps = ParseStepBlock(stopAtExamples: true);
                    var examples = ParseExamplesTable();
                    scenarios.AddRange(ExpandOutline(scenarioName, pendingTags, outlineSteps, examples, line));
                    pendingTags.Clear();
                    continue;
                }

                if (level == 2 && headingText.StartsWith("Scenario:", StringComparison.Ordinal))
                {
                    var scenarioName = headingText["Scenario:".Length..].Trim();
                    if (scenarioName.Length == 0)
                    {
                        AddError("UAT006", "Scenario heading must include a name.", line);
                    }

                    _index++;
                    var steps = ParseStepBlock(stopAtExamples: false);
                    if (steps.Count == 0)
                    {
                        AddError("UAT007", "Scenario section must contain at least one step.", line);
                    }

                    scenarios.Add(new UatScenario(scenarioName, [.. pendingTags], steps, Location(line)));
                    pendingTags.Clear();
                    continue;
                }

                AddError("UAT022", $"Unsupported heading '{trimmed}'.", line);
                _index++;
            }

            if (pendingTags.Count > 0)
            {
                var location = _lines.Length == 0 ? new UatSourceLocation(_filePath, 1) : Location(_lines[^1]);
                _diagnostics.Add(new UatDiagnostic(
                    UatDiagnosticSeverity.Error,
                    "UAT020",
                    "Tag lines must be immediately followed by a scenario heading.",
                    location));
            }

            if (title is null)
            {
                _diagnostics.Add(new UatDiagnostic(
                    UatDiagnosticSeverity.Error,
                    "UAT000",
                    "Document must contain exactly one '# UAT:' heading.",
                    new UatSourceLocation(_filePath, 1)));
            }

            if (scenarios.Count == 0)
            {
                _diagnostics.Add(new UatDiagnostic(
                    UatDiagnosticSeverity.Error,
                    "UAT008",
                    "Document must contain at least one scenario.",
                    titleSource ?? new UatSourceLocation(_filePath, 1)));
            }

            var document = title is null
                ? null
                : new UatDocument(
                    title,
                    metadata,
                    background,
                    dataTables,
                    scenarios,
                    titleSource ?? new UatSourceLocation(_filePath, 1))
                {
                    Callables = callables,
                };

            return new UatParseResult(document, _diagnostics);
        }

        public UatPhrasesParseResult ParsePhrases()
        {
            string? title = null;
            UatSourceLocation? titleSource = null;
            List<UatCallable> callables = [];

            while (!End)
            {
                SkipBlankLines();
                if (End)
                {
                    break;
                }

                var line = Current;
                var trimmed = line.Text.Trim();
                if (!TryParseHeading(trimmed, out var level, out var headingText))
                {
                    AddError("UAT028", $"A phrase file holds only '## Phrase:' sections; found '{trimmed}'.", line);
                    _index++;
                    continue;
                }

                if (level == 1 && headingText.StartsWith("Phrases:", StringComparison.Ordinal))
                {
                    if (title is not null)
                    {
                        AddError("UAT028", "A phrase file has exactly one '# Phrases:' heading.", line);
                    }

                    title = headingText["Phrases:".Length..].Trim();
                    titleSource = Location(line);
                    _index++;
                    continue;
                }

                if (title is null)
                {
                    AddError("UAT028", "A phrase file starts with a '# Phrases:' heading.", line);
                    title = string.Empty;
                }

                if (level == 2 && TryParsePhraseHeading(headingText, out var kind, out var phrase))
                {
                    _index++;
                    if (ParsePhraseSection(kind, phrase, line) is { } callable)
                    {
                        callables.Add(callable);
                    }

                    continue;
                }

                AddError("UAT028", $"A phrase file holds only phrase sections; '{trimmed}' belongs in a .uat.md file.", line);
                _index++;
                SkipToNextSection();
            }

            if (title is null)
            {
                AddError("UAT028", "A phrase file starts with a '# Phrases:' heading.", new UatSourceLocation(_filePath, 1));
            }

            var document = new UatPhrasesDocument(title ?? string.Empty, callables, titleSource ?? new UatSourceLocation(_filePath, 1));
            return new UatPhrasesParseResult(document, _diagnostics);
        }

        private void SkipToNextSection()
        {
            while (!End && !(TryParseHeading(Current.Text.Trim(), out var level, out _) && level <= 2))
            {
                _index++;
            }
        }

        private static bool TryParsePhraseHeading(string headingText, out UatCallableKind kind, out string phrase)
        {
            foreach (var (prefix, candidate) in new[] { ("Phrase for each row:", UatCallableKind.EachRow), ("Phrase:", UatCallableKind.Once) })
            {
                if (headingText.StartsWith(prefix, StringComparison.Ordinal))
                {
                    kind = candidate;
                    phrase = headingText[prefix.Length..].Trim();
                    return true;
                }
            }

            kind = default;
            phrase = string.Empty;
            return false;
        }

        // An optional Parameters table, then the body. What needs the catalog is checked later, as
        // the callable's problems, so a wrong callable does not stop the file (AD-016 A10).
        private UatCallable? ParsePhraseSection(UatCallableKind kind, string phrase, Line heading)
        {
            if (phrase.Length == 0)
            {
                AddError("UAT025", "A '## Phrase:' heading names the phrase: '## Phrase: a project {name}'.", heading);
            }

            SkipBlankLines();
            IReadOnlyList<UatCallableParameterDeclaration> declared = [];
            if (!End && IsTableRow(Current.Text) && SplitTableRow(Current.Text) is { Count: > 0 } header &&
                header[0].Equals("Parameter", StringComparison.OrdinalIgnoreCase))
            {
                var table = TryParseCurrentTable();
                if (table is null)
                {
                    AddError("UAT027", "A Parameters table needs a separator row under its header.", Current);
                    _index++;
                }
                else
                {
                    declared = ReadParameters(table);
                }
            }

            var steps = ParseStepBlock(stopAtExamples: false, inPhrase: true);
            if (steps.Count == 0)
            {
                AddError("UAT026", $"The phrase '{phrase}' has no steps.", heading);
            }

            return phrase.Length == 0 ? null : new UatCallable(kind, phrase, declared, steps, Location(heading));
        }

        private List<UatCallableParameterDeclaration> ReadParameters(UatTable table)
        {
            string? Column(string name) => table.Columns.FirstOrDefault(column => column.Equals(name, StringComparison.OrdinalIgnoreCase));

            List<UatCallableParameterDeclaration> declared = [];
            if (Column("Parameter") is not { } parameter || Column("Type") is not { } type)
            {
                AddError("UAT027", "A Parameters table has the columns 'Parameter' and 'Type', and optionally 'Example'.", table.Source);
                return declared;
            }

            var example = Column("Example");
            foreach (var row in table.Rows)
            {
                var name = row.Cells[parameter];
                if (name.Length == 0 || row.Cells[type].Length == 0)
                {
                    AddError("UAT027", "A Parameters row names a parameter and its type.", row.Source);
                    continue;
                }

                if (declared.Any(existing => existing.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    AddError("UAT027", $"The Parameters table lists '{name}' twice.", row.Source);
                    continue;
                }

                declared.Add(new UatCallableParameterDeclaration(name, row.Cells[type], example is null ? null : row.Cells[example], row.Source));
            }

            return declared;
        }

        private Dictionary<string, string> ParseMetadataTable()
        {
            var table = ParseRequiredTable("Metadata section must contain a Markdown table.");
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (table is null)
            {
                return metadata;
            }

            if (table.Columns.Count != 2 ||
                !table.Columns[0].Equals("Field", StringComparison.OrdinalIgnoreCase) ||
                !table.Columns[1].Equals("Value", StringComparison.OrdinalIgnoreCase))
            {
                AddError("UAT009", "Metadata table must have columns 'Field' and 'Value'.", table.Source);
                return metadata;
            }

            foreach (var row in table.Rows)
            {
                var field = row.Cells["Field"];
                if (field.Length == 0)
                {
                    AddError("UAT010", "Metadata field name cannot be empty.", row.Source);
                    continue;
                }

                metadata[field] = row.Cells["Value"];
            }

            return metadata;
        }

        private List<UatStep> ParseStepBlock(bool stopAtExamples, bool inPhrase = false)
        {
            List<UatStep> steps = [];
            UatEffectiveStepKeyword? currentEffectiveKeyword = null;

            while (!End)
            {
                SkipBlankLines();
                if (End)
                {
                    break;
                }

                var line = Current;
                var trimmed = line.Text.Trim();

                if (TryParseHeading(trimmed, out var level, out var headingText))
                {
                    if (stopAtExamples && level == 3 && headingText.Equals("Examples", StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (level <= 2)
                    {
                        break;
                    }
                }

                if (TryParseTags(trimmed, out _))
                {
                    break;
                }

                if (trimmed is TableMarker or DocStringMarker)
                {
                    AddError("UAT024", $"'{trimmed}' goes directly under a step in a '## Phrase:' section.", line);
                    _index++;
                    continue;
                }

                if (!TryParseStep(trimmed, out var keyword, out var text))
                {
                    AddError("UAT011", $"Expected a UAT step but found '{trimmed}'.", line);
                    _index++;
                    continue;
                }

                var effectiveKeyword = ResolveEffectiveKeyword(keyword, currentEffectiveKeyword, line);
                currentEffectiveKeyword = effectiveKeyword;
                _index++;

                var table = TryParseCurrentTable();
                var docString = TryParseDocString();
                var passes = UatPassedBlock.None;
                if (table is null && docString is null && !End && Current.Text.Trim() is TableMarker or DocStringMarker)
                {
                    if (inPhrase)
                    {
                        passes = Current.Text.Trim() == TableMarker ? UatPassedBlock.Table : UatPassedBlock.DocString;
                    }
                    else
                    {
                        AddError("UAT024", $"'{Current.Text.Trim()}' passes a call's block, so it is only allowed in a '## Phrase:' section.", Current);
                    }

                    _index++;
                }

                steps.Add(new UatStep(keyword, effectiveKeyword, text, table, Location(line), docString) { Passes = passes });
            }

            return steps;
        }

        private UatTable? ParseExamplesTable()
        {
            SkipBlankLines();
            if (End || !TryParseHeading(Current.Text.Trim(), out var level, out var headingText) ||
                level != 3 ||
                !headingText.Equals("Examples", StringComparison.Ordinal))
            {
                var location = End ? new UatSourceLocation(_filePath, _lines.Length + 1) : Location(Current);
                _diagnostics.Add(new UatDiagnostic(
                    UatDiagnosticSeverity.Error,
                    "UAT012",
                    "Scenario Outline must contain a '### Examples' section.",
                    location));
                return null;
            }

            _index++;
            return ParseRequiredTable("Examples section must contain a Markdown table.");
        }

        private IReadOnlyList<UatScenario> ExpandOutline(
            string scenarioName,
            IReadOnlyList<string> tags,
            IReadOnlyList<UatStep> outlineSteps,
            UatTable? examples,
            Line headingLine)
        {
            if (scenarioName.Length == 0)
            {
                AddError("UAT013", "Scenario Outline heading must include a name.", headingLine);
            }

            if (outlineSteps.Count == 0)
            {
                AddError("UAT014", "Scenario Outline section must contain at least one step.", headingLine);
            }

            if (examples is null)
            {
                return [];
            }

            var columns = new HashSet<string>(examples.Columns, StringComparer.Ordinal);
            foreach (var step in outlineSteps)
            {
                foreach (var parameter in UatOutline.Placeholders(step))
                {
                    if (!columns.Contains(parameter))
                    {
                        _diagnostics.Add(new UatDiagnostic(
                            UatDiagnosticSeverity.Error,
                            "UAT015",
                            $"Scenario Outline parameter '<{parameter}>' has no matching Examples column.",
                            step.Source));
                    }
                }
            }

            List<UatScenario> scenarios = [];
            for (var i = 0; i < examples.Rows.Count; i++)
            {
                var row = examples.Rows[i];
                var expandedSteps = outlineSteps
                    .Select(step => UatOutline.Substitute(step, row.Cells))
                    .ToArray();

                scenarios.Add(new UatScenario(
                    $"{scenarioName} [{i + 1}]",
                    [.. tags],
                    expandedSteps,
                    Location(headingLine),
                    scenarioName,
                    i + 1));
            }

            return scenarios;
        }

        private UatEffectiveStepKeyword ResolveEffectiveKeyword(
            UatStepKeyword keyword,
            UatEffectiveStepKeyword? currentEffectiveKeyword,
            Line line)
        {
            return keyword switch
            {
                UatStepKeyword.Given => UatEffectiveStepKeyword.Given,
                UatStepKeyword.When => UatEffectiveStepKeyword.When,
                UatStepKeyword.Then => UatEffectiveStepKeyword.Then,
                UatStepKeyword.And or UatStepKeyword.But when currentEffectiveKeyword.HasValue => currentEffectiveKeyword.Value,
                _ => AddAndReturnStepKeywordError(line)
            };
        }

        private UatEffectiveStepKeyword AddAndReturnStepKeywordError(Line line)
        {
            AddError("UAT016", "'And' and 'But' steps must follow Given, When, or Then.", line);
            return UatEffectiveStepKeyword.Given;
        }

        private UatTable? ParseRequiredTable(string message)
        {
            SkipBlankLines();
            var table = TryParseCurrentTable();
            if (table is null)
            {
                var location = End ? new UatSourceLocation(_filePath, _lines.Length + 1) : Location(Current);
                _diagnostics.Add(new UatDiagnostic(UatDiagnosticSeverity.Error, "UAT017", message, location));
            }

            return table;
        }

        private UatTable? TryParseCurrentTable()
        {
            if (End || _index + 1 >= _lines.Length || !IsTableRow(Current.Text) || !IsSeparatorRow(_lines[_index + 1].Text))
            {
                return null;
            }

            var source = Location(Current);
            var columns = SplitTableRow(Current.Text);
            _index += 2;

            List<UatTableRow> rows = [];
            while (!End && IsTableRow(Current.Text))
            {
                var rowLine = Current;
                var cells = SplitTableRow(rowLine.Text);
                if (cells.Count != columns.Count)
                {
                    AddError("UAT018", "Markdown table row must have the same number of cells as the header.", rowLine);
                    _index++;
                    continue;
                }

                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var i = 0; i < columns.Count; i++)
                {
                    row[columns[i]] = cells[i];
                }

                rows.Add(new UatTableRow(row, Location(rowLine)));
                _index++;
            }

            if (rows.Count == 0)
            {
                AddError("UAT019", "Markdown table must contain at least one data row.", source);
            }

            return new UatTable(columns, rows, source);
        }

        // A """ block under a step: its lines lose the opening delimiter's indentation.
        private string? TryParseDocString()
        {
            if (End || Current.Text.Trim() != DocStringDelimiter)
            {
                return null;
            }

            var opening = Current;
            var indent = opening.Text.Length - opening.Text.TrimStart().Length;
            _index++;
            List<string> lines = [];
            while (!End && Current.Text.Trim() != DocStringDelimiter)
            {
                var text = Current.Text;
                lines.Add(text.Length >= indent && string.IsNullOrWhiteSpace(text[..indent]) ? text[indent..] : text.TrimStart());
                _index++;
            }

            if (End)
            {
                AddError("UAT023", "A doc string (\"\"\") is not closed.", opening);
            }
            else
            {
                _index++;
            }

            return string.Join('\n', lines);
        }

        private bool End => _index >= _lines.Length;

        private Line Current => _lines[_index];

        private void SkipBlankLines()
        {
            while (!End && string.IsNullOrWhiteSpace(Current.Text))
            {
                _index++;
            }
        }

        private UatSourceLocation Location(Line line)
        {
            return new UatSourceLocation(_filePath, line.Number);
        }

        private void AddError(string code, string message, Line line)
        {
            _diagnostics.Add(new UatDiagnostic(UatDiagnosticSeverity.Error, code, message, Location(line)));
        }

        private void AddError(string code, string message, UatSourceLocation location)
        {
            _diagnostics.Add(new UatDiagnostic(UatDiagnosticSeverity.Error, code, message, location));
        }

        private static bool IsScenarioHeading(string line)
        {
            return line.StartsWith("## Scenario:", StringComparison.Ordinal) ||
                   line.StartsWith("## Scenario Outline:", StringComparison.Ordinal);
        }

        private static bool TryParseHeading(string line, out int level, out string text)
        {
            level = 0;
            text = string.Empty;

            while (level < line.Length && line[level] == '#')
            {
                level++;
            }

            if (level == 0 || level >= line.Length || line[level] != ' ')
            {
                return false;
            }

            text = line[(level + 1)..].Trim();
            return text.Length > 0;
        }

        private static bool TryParseTags(string line, out IReadOnlyList<string> tags)
        {
            tags = [];
            if (!line.StartsWith('@'))
            {
                return false;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || parts.Any(x => x.Length < 2 || x[0] != '@'))
            {
                return false;
            }

            tags = parts.Select(x => x[1..]).ToArray();
            return true;
        }

        private static bool TryParseStep(string line, out UatStepKeyword keyword, out string text)
        {
            foreach (var name in Enum.GetNames<UatStepKeyword>())
            {
                if (line.StartsWith(name + " ", StringComparison.Ordinal))
                {
                    keyword = Enum.Parse<UatStepKeyword>(name);
                    text = line[(name.Length + 1)..].Trim();
                    return text.Length > 0;
                }
            }

            keyword = default;
            text = string.Empty;
            return false;
        }

        private static bool IsTableRow(string line)
        {
            var trimmed = line.Trim();
            return trimmed.StartsWith('|') && trimmed.EndsWith('|') && trimmed.Count(x => x == '|') >= 2;
        }

        private static bool IsSeparatorRow(string line)
        {
            if (!IsTableRow(line))
            {
                return false;
            }

            var cells = SplitTableRow(line);
            return cells.Count > 0 && cells.All(cell =>
            {
                var trimmed = cell.Replace(" ", string.Empty, StringComparison.Ordinal);
                return trimmed.Length >= 3 && trimmed.All(x => x is '-' or ':');
            });
        }

        private static IReadOnlyList<string> SplitTableRow(string line)
        {
            var trimmed = line.Trim();
            var inner = trimmed[1..^1];
            return inner.Split('|').Select(x => x.Trim()).ToArray();
        }

        private static Line[] SplitLines(string markdown)
        {
            // Fence delimiters are dropped here, so a scenario whose steps are fenced for
            // rendering parses exactly like a bare one. Numbers are assigned before the drop,
            // which keeps diagnostics pointing at the line the author sees.
            return markdown
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n')
                .Select((text, index) => new Line(text, index + 1))
                .Where(line => !IsCodeFenceDelimiter(line.Text))
                .ToArray();
        }

        private static bool IsCodeFenceDelimiter(string text)
        {
            var trimmed = text.Trim();
            return trimmed.StartsWith("```", StringComparison.Ordinal) ||
                   trimmed.StartsWith("~~~", StringComparison.Ordinal);
        }
    }

    private sealed record Line(string Text, int Number);
}
