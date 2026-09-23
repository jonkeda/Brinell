namespace Brinell.Uat;

/// <summary>
/// A config file as lines, edited row by row so untouched text survives byte for byte.
/// </summary>
internal sealed class MarkdownDocument
{
    // The order a section is created in when it is missing; an unknown section is appended last.
    private static readonly string[] CanonicalSections =
        ["Runtime", "Assemblies", "Discovery", "Reporting", "Settings", "Skip Rules"];

    private readonly List<string> _lines;
    private readonly string _newLine;
    private readonly bool _endsWithNewLine;

    private MarkdownDocument(List<string> lines, string newLine, bool endsWithNewLine)
    {
        _lines = lines;
        _newLine = newLine;
        _endsWithNewLine = endsWithNewLine;
    }

    public static MarkdownDocument Parse(string markdown)
    {
        var newLine = markdown.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var endsWithNewLine = markdown.EndsWith('\n');

        List<string> lines = [.. markdown.Split('\n').Select(line => line.TrimEnd('\r'))];
        if (endsWithNewLine && lines.Count > 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return new MarkdownDocument(lines, newLine, endsWithNewLine);
    }

    public string ToText()
    {
        var text = string.Join(_newLine, _lines);
        return _endsWithNewLine ? text + _newLine : text;
    }

    public void SetField(string section, string field, string? value)
    {
        var table = FindTable(section);
        if (table is null)
        {
            if (value is not null)
            {
                CreateSection(section, ["Field", "Value"], [Row(("Field", field), ("Value", value))]);
            }

            return;
        }

        var rowIndex = table.FindRow("Field", field);
        if (rowIndex is null)
        {
            if (value is not null)
            {
                _lines.Insert(table.RowEndIndex + 1, table.Format(Row(("Field", field), ("Value", value))));
            }

            return;
        }

        if (value is null)
        {
            _lines.RemoveAt(rowIndex.Value);
            return;
        }

        var updated = table.Format(Row(("Field", field), ("Value", value)), _lines[rowIndex.Value]);
        if (!string.Equals(_lines[rowIndex.Value], updated, StringComparison.Ordinal))
        {
            _lines[rowIndex.Value] = updated;
        }
    }

    public void SetRows(
        string section,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        var table = FindTable(section);
        if (table is null)
        {
            if (rows.Count > 0)
            {
                CreateSection(section, columns, rows);
            }

            return;
        }

        List<string> replacement = [.. rows.Select(row => table.Format(row))];
        var existing = _lines.GetRange(table.FirstRowIndex, table.RowCount);
        if (existing.SequenceEqual(replacement, StringComparer.Ordinal))
        {
            return;
        }

        _lines.RemoveRange(table.FirstRowIndex, table.RowCount);
        _lines.InsertRange(table.FirstRowIndex, replacement);
    }

    private static Dictionary<string, string> Row(params (string Column, string Value)[] cells)
    {
        Dictionary<string, string> row = new(StringComparer.OrdinalIgnoreCase);
        foreach (var (column, value) in cells)
        {
            row[column] = value;
        }

        return row;
    }

    private TableInfo? FindTable(string section)
    {
        var headingIndex = FindHeading(section);
        if (headingIndex is null)
        {
            return null;
        }

        for (var i = headingIndex.Value + 1; i < _lines.Count; i++)
        {
            var line = _lines[i].Trim();
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                return null;
            }

            if (!UatConfigParser.IsTableRow(line) ||
                i + 1 >= _lines.Count ||
                !UatConfigParser.IsSeparatorRow(_lines[i + 1].Trim()))
            {
                continue;
            }

            var firstRow = i + 2;
            var rowCount = 0;
            while (firstRow + rowCount < _lines.Count &&
                   UatConfigParser.IsTableRow(_lines[firstRow + rowCount].Trim()))
            {
                rowCount++;
            }

            return new TableInfo(_lines, i, firstRow, rowCount);
        }

        return null;
    }

    private int? FindHeading(string section)
    {
        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i].Trim();
            if (line.StartsWith("## ", StringComparison.Ordinal) &&
                line[3..].Trim().Equals(section, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }

    private void CreateSection(
        string section,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        List<string> block =
        [
            string.Empty,
            $"## {section}",
            string.Empty,
            "| " + string.Join(" | ", columns) + " |",
            "| " + string.Join(" | ", columns.Select(_ => "---")) + " |"
        ];

        foreach (var row in rows)
        {
            block.Add("| " + string.Join(" | ", columns.Select(column =>
                row.TryGetValue(column, out var value) ? value : string.Empty)) + " |");
        }

        var insertIndex = FindInsertIndex(section);
        while (insertIndex > 0 && _lines[insertIndex - 1].Trim().Length == 0)
        {
            insertIndex--;
        }

        _lines.InsertRange(insertIndex, block);
    }

    // After the last section that precedes this one in canonical order, so a generated
    // section lands where a reader expects it rather than at the end.
    private int FindInsertIndex(string section)
    {
        var position = Array.FindIndex(
            CanonicalSections,
            name => name.Equals(section, StringComparison.OrdinalIgnoreCase));
        if (position < 0)
        {
            return _lines.Count;
        }

        var insertIndex = _lines.Count;
        for (var i = position - 1; i >= 0; i--)
        {
            var headingIndex = FindHeading(CanonicalSections[i]);
            if (headingIndex is null)
            {
                continue;
            }

            insertIndex = SectionEnd(headingIndex.Value);
            break;
        }

        return insertIndex;
    }

    private int SectionEnd(int headingIndex)
    {
        for (var i = headingIndex + 1; i < _lines.Count; i++)
        {
            if (_lines[i].Trim().StartsWith("## ", StringComparison.Ordinal))
            {
                return i;
            }
        }

        return _lines.Count;
    }

    /// <summary>One markdown table, and the layout a new row has to match.</summary>
    private sealed class TableInfo
    {
        private readonly List<string> _lines;
        private readonly IReadOnlyList<int> _widths;
        private readonly string _indent;

        public TableInfo(List<string> lines, int headerIndex, int firstRowIndex, int rowCount)
        {
            _lines = lines;
            FirstRowIndex = firstRowIndex;
            RowCount = rowCount;
            Columns = UatConfigParser.SplitTableRow(lines[headerIndex].Trim());
            _indent = lines[headerIndex][..(lines[headerIndex].Length - lines[headerIndex].TrimStart().Length)];

            // A padded table keeps every line the same length, separator included; an unpadded
            // one does not. Widths only matter for the first, so a new row matches its neighbours.
            var blockLines = Enumerable.Range(headerIndex, rowCount + 2)
                .Where(index => index < lines.Count)
                .Select(index => lines[index].Trim())
                .ToArray();
            var aligned = blockLines.Length > 2 && blockLines.Select(line => line.Length).Distinct().Count() == 1;
            _widths = aligned
                ? [.. RawCells(lines[headerIndex].Trim()).Select(cell => Math.Max(cell.Length - 2, 0))]
                : [];
        }

        public IReadOnlyList<string> Columns { get; }

        public int FirstRowIndex { get; }

        public int RowCount { get; }

        public int RowEndIndex => FirstRowIndex + RowCount - 1;

        public int? FindRow(string column, string value)
        {
            for (var i = FirstRowIndex; i < FirstRowIndex + RowCount; i++)
            {
                var cells = UatConfigParser.SplitTableRow(_lines[i].Trim());
                var columnIndex = IndexOfColumn(column);
                if (columnIndex >= 0 &&
                    columnIndex < cells.Count &&
                    cells[columnIndex].Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return null;
        }

        /// <summary>Renders a row with this table's column order, padding and indentation.</summary>
        /// <param name="row">The cell values, keyed by column name.</param>
        /// <param name="existingLine">The line being replaced, whose indentation is kept.</param>
        /// <returns>The rendered line.</returns>
        public string Format(IReadOnlyDictionary<string, string> row, string? existingLine = null)
        {
            var indent = existingLine is null
                ? _indent
                : existingLine[..(existingLine.Length - existingLine.TrimStart().Length)];

            var cells = Columns.Select((column, index) =>
            {
                var value = row.TryGetValue(column, out var cell) ? cell : string.Empty;

                // A value wider than the column widens that one row rather than reformatting
                // the whole table: a one-line diff, and the nudge is visible.
                return index < _widths.Count ? value.PadRight(_widths[index]) : value;
            });

            return indent + "| " + string.Join(" | ", cells) + " |";
        }

        private int IndexOfColumn(string column)
        {
            for (var i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].Equals(column, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static IReadOnlyList<string> RawCells(string line)
        {
            return line[1..^1].Split('|');
        }
    }
}
