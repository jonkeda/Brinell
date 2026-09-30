using Brinell.Core.Utilities;

namespace Brinell.Scraper.Projects;

/// <summary>
/// One change to a <c>scraper.config.md</c>: a field set, a row added or a row removed. Everything else
/// in the file (prose, other sections, line endings) stays as it was, so a person's file is not
/// rewritten by the view.
/// </summary>
public static class ScraperConfigWriter
{
    /// <summary>Sets a field of a <c>| Field | Value |</c> section (<c>## App</c>, <c>## Explore</c>), adding the row or the section when missing.</summary>
    public static ConfigEdit SetField(string markdown, string section, string field, string value)
    {
        Check(value);
        var doc = new Doc(markdown);
        var table = doc.Table(section);
        if (table is null)
        {
            doc.AppendSection(section, ["Field", "Value"], [[field, value]]);
            return new(doc.Text, true, $"Added ## {section} with {field} = {value}.");
        }
        var row = table.Rows.FirstOrDefault(r => r.Cells.GetValueOrDefault("Field", "") == field);
        if (row is not null)
        {
            if (row.Cells.GetValueOrDefault("Value", "") == value) return new(markdown, false, $"{field} is {value} already.");
            doc.Replace(row.LineNumber, Row([field, value]));
            return new(doc.Text, true, $"Set {field} to {value} in ## {section}.");
        }
        doc.Insert(table.EndLineNumber, Row([field, value]));
        return new(doc.Text, true, $"Added {field} = {value} to ## {section}.");
    }

    /// <summary>Adds a row to a section's table (<c>## Aliases</c>, <c>## Explore hosts</c>, <c>## Never click</c>…), unless a row with the same first cell is there; adds the section when missing.</summary>
    public static ConfigEdit AddRow(string markdown, string section, IReadOnlyList<string> columns, IReadOnlyList<string> values)
    {
        if (columns.Count != values.Count || columns.Count == 0) throw new ArgumentException("One value per column.", nameof(values));
        foreach (var value in values) Check(value);
        var doc = new Doc(markdown);
        var table = doc.Table(section);
        if (table is null)
        {
            doc.AppendSection(section, columns, [values]);
            return new(doc.Text, true, $"Added ## {section} with {string.Join(", ", values)}.");
        }
        if (table.Rows.Any(r => r.Cells.GetValueOrDefault(table.Columns[0], "") == values[0]))
            return new(markdown, false, $"{values[0]} is in ## {section} already.");
        var ordered = table.Columns.Select(c => columns.Contains(c) ? values[columns.ToList().IndexOf(c)] : "").ToArray();
        doc.Insert(table.EndLineNumber, Row(ordered));
        return new(doc.Text, true, $"Added {values[0]} to ## {section}.");
    }

    /// <summary>Removes the rows of a section's table whose first cell is <paramref name="first"/>.</summary>
    public static ConfigEdit RemoveRow(string markdown, string section, string first)
    {
        var doc = new Doc(markdown);
        var table = doc.Table(section);
        var rows = table?.Rows.Where(r => r.Cells.GetValueOrDefault(table.Columns[0], "") == first).ToArray() ?? [];
        if (rows.Length == 0) return new(markdown, false, $"{first} is not in ## {section}.");
        foreach (var row in rows.OrderByDescending(r => r.LineNumber)) doc.Remove(row.LineNumber);
        return new(doc.Text, true, $"Removed {first} from ## {section}.");
    }

    /// <summary>A new config for a project: <c>## App</c> with the start URL, and the rest left to the defaults.</summary>
    public static string New(string startUrl, string platform = "html") =>
        "# Scraper Config\n\n## App\n\n| Field | Value |\n| --- | --- |\n" + Row(["Platform", platform]) + "\n" + Row(["StartUrl", startUrl]) + "\n";

    private static string Row(IReadOnlyList<string> cells) => "| " + string.Join(" | ", cells) + " |";

    private static void Check(string value)
    {
        if (value.Contains('|', StringComparison.Ordinal) || value.Contains('\n', StringComparison.Ordinal))
            throw new ArgumentException($"'{value}' cannot go in a table cell: no '|' and no line break.");
    }

    // The file as lines, with its own line ending; line numbers are 1-based as MarkdownTables gives them.
    private sealed class Doc
    {
        private readonly List<string> _lines;
        private readonly string _newline;
        private readonly bool _endsWithNewline;

        public Doc(string markdown)
        {
            _newline = markdown.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            var text = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
            _endsWithNewline = text.EndsWith('\n');
            _lines = [.. (_endsWithNewline ? text[..^1] : text).Split('\n')];
        }

        public string Text => string.Join(_newline, _lines) + (_endsWithNewline ? _newline : "");

        public MarkdownTable? Table(string section) =>
            MarkdownTables.ReadSections(string.Join('\n', _lines)).TryGetValue(section, out var table) ? table : null;

        public void Replace(int line, string text) => _lines[line - 1] = text;

        public void Insert(int line, string text) => _lines.Insert(line - 1, text);

        public void Remove(int line) => _lines.RemoveAt(line - 1);

        public void AppendSection(string section, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            while (_lines.Count > 0 && _lines[^1].Trim().Length == 0) _lines.RemoveAt(_lines.Count - 1);
            if (_lines.Count > 0) _lines.Add("");
            _lines.Add("## " + section);
            _lines.Add("");
            _lines.Add(Row(columns));
            _lines.Add(Row(columns.Select(_ => "---").ToArray()));
            _lines.AddRange(rows.Select(Row));
        }
    }
}

/// <summary>What a change did: the new text, whether anything changed, and a sentence saying what.</summary>
public sealed record ConfigEdit(string Text, bool Changed, string Description);
