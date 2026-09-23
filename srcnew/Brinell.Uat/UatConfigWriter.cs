namespace Brinell.Uat;

/// <summary>
/// Rewrites <c>uat.config.md</c> in place, touching only the rows an edit names.
/// </summary>
/// <remarks>
/// A config is hand-written far more often than it is generated, so an edit that reformats
/// the file costs more than it saves. Everything this does not change survives byte for byte,
/// including unknown sections, prose and column padding: <c>Apply(markdown, Empty)</c> returns
/// its input unchanged.
/// </remarks>
public static class UatConfigWriter
{
    /// <summary>Applies <paramref name="edit" /> to <paramref name="markdown" />.</summary>
    /// <param name="markdown">The config file's current text.</param>
    /// <param name="edit">The rows to set, add or remove.</param>
    /// <returns>The new text, identical to the input where the edit said nothing.</returns>
    public static string Apply(string markdown, UatConfigEdit edit)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(edit);

        if (edit.IsEmpty)
        {
            return markdown;
        }

        var document = MarkdownDocument.Parse(markdown);
        foreach (var fieldEdit in edit.Fields)
        {
            document.SetField(fieldEdit.Section, fieldEdit.Field, fieldEdit.Value);
        }

        foreach (var rowsEdit in edit.Rows)
        {
            document.SetRows(rowsEdit.Section, rowsEdit.Columns, rowsEdit.Rows);
        }

        return document.ToText();
    }

    /// <summary>Applies <paramref name="edit" /> to a config file, writing only when the text changes.</summary>
    /// <param name="filePath">The config file.</param>
    /// <param name="edit">The rows to set, add or remove.</param>
    /// <returns>Whether the file was written.</returns>
    public static bool ApplyToFile(string filePath, UatConfigEdit edit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(edit);

        var original = File.ReadAllText(filePath);
        var updated = Apply(original, edit);
        if (string.Equals(original, updated, StringComparison.Ordinal))
        {
            return false;
        }

        WriteAtomic(filePath, updated);
        return true;
    }

    /// <summary>Creates the text of a new config.</summary>
    /// <param name="template">The project, and the fixture when one has been chosen.</param>
    /// <returns>A minimal config: required fields only, defaults left implicit.</returns>
    public static string Create(UatConfigTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(template.Project);

        List<string> lines =
        [
            "# UAT Config",
            string.Empty,
            "## Runtime",
            string.Empty,
            "| Field | Value |",
            "| --- | --- |",
            $"| {UatConfigFields.Project} | {template.Project.Trim()} |"
        ];

        if (!string.IsNullOrWhiteSpace(template.Fixture))
        {
            lines.Add($"| {UatConfigFields.Fixture} | {template.Fixture.Trim()} |");
        }

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    /// <summary>Writes a new config file, refusing to overwrite an existing one.</summary>
    /// <param name="filePath">The file to create.</param>
    /// <param name="template">The project, and the fixture when one has been chosen.</param>
    public static void CreateFile(string filePath, UatConfigTemplate template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (File.Exists(filePath))
        {
            throw new InvalidOperationException($"A config already exists at {filePath}.");
        }

        WriteAtomic(filePath, Create(template));
    }

    // Through a temporary file, so a crash mid-write cannot leave a truncated config.
    private static void WriteAtomic(string filePath, string contents)
    {
        var temporaryPath = filePath + ".tmp";
        File.WriteAllText(temporaryPath, contents);
        File.Move(temporaryPath, filePath, overwrite: true);
    }
}

/// <summary>The well-known <c>Runtime</c> fields Presenter and the runtime read by name.</summary>
public static class UatConfigFields
{
    /// <summary>The UAT test project, from which the Pages assembly is resolved.</summary>
    public const string Project = "Project";

    /// <summary>The fixture type to construct.</summary>
    public const string Fixture = "Fixture";

    /// <summary>Optional override for an app the fixture cannot find itself.</summary>
    public const string AppPath = "AppPath";

    /// <summary>Optional override for the target, which is otherwise derived from the fixture.</summary>
    public const string Target = "Target";

    /// <summary>The working directory a run uses.</summary>
    public const string WorkingDirectory = "WorkingDirectory";
}

/// <summary>What a new config starts with.</summary>
/// <param name="Project">The UAT test project, relative to the config.</param>
/// <param name="Fixture">The fixture type, when one has been chosen already.</param>
public sealed record UatConfigTemplate(string Project, string? Fixture = null);

/// <summary>One <c>Field</c>/<c>Value</c> row to set or remove.</summary>
/// <param name="Section">The section heading, such as <c>Runtime</c>.</param>
/// <param name="Field">The field name.</param>
/// <param name="Value">The new value, or <see langword="null" /> to remove the row.</param>
public sealed record UatConfigFieldEdit(string Section, string Field, string? Value);

/// <summary>The full contents of a list table, such as <c>Assemblies</c>.</summary>
/// <param name="Section">The section heading.</param>
/// <param name="Columns">The column names, used when the section has to be created.</param>
/// <param name="Rows">The rows, each keyed by column name. An empty list clears the table.</param>
public sealed record UatConfigRowsEdit(
    string Section,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);

/// <summary>A set of changes to apply to a config in one pass.</summary>
public sealed record UatConfigEdit
{
    private UatConfigEdit(
        IReadOnlyList<UatConfigFieldEdit> fields,
        IReadOnlyList<UatConfigRowsEdit> rows)
    {
        Fields = fields;
        Rows = rows;
    }

    /// <summary>An edit that changes nothing.</summary>
    public static UatConfigEdit Empty { get; } = new([], []);

    /// <summary>The <c>Field</c>/<c>Value</c> rows to set or remove, in order.</summary>
    public IReadOnlyList<UatConfigFieldEdit> Fields { get; }

    /// <summary>The list tables to replace, in order.</summary>
    public IReadOnlyList<UatConfigRowsEdit> Rows { get; }

    /// <summary>Whether this edit would change nothing.</summary>
    public bool IsEmpty => Fields.Count == 0 && Rows.Count == 0;

    /// <summary>Sets a <c>Field</c>/<c>Value</c> row, or removes it when <paramref name="value" /> is null.</summary>
    /// <param name="section">The section heading, such as <c>Runtime</c>.</param>
    /// <param name="field">The field name.</param>
    /// <param name="value">The new value, or <see langword="null" /> to remove the row.</param>
    /// <returns>A new edit including this change.</returns>
    public UatConfigEdit WithField(string section, string field, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        return new UatConfigEdit([.. Fields, new UatConfigFieldEdit(section, field, value)], Rows);
    }

    /// <summary>Replaces a list table's rows.</summary>
    /// <param name="section">The section heading, such as <c>Assemblies</c>.</param>
    /// <param name="columns">The column names, used when the section has to be created.</param>
    /// <param name="rows">The rows, each keyed by column name. Empty clears the table.</param>
    /// <returns>A new edit including this change.</returns>
    public UatConfigEdit WithRows(
        string section,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        return new UatConfigEdit(Fields, [.. Rows, new UatConfigRowsEdit(section, columns, rows)]);
    }
}
