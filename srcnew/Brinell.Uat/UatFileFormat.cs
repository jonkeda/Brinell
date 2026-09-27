namespace Brinell.Uat;

/// <summary>
/// The names of feature and step files, and the rewrite from the forms before them
/// (<c>Scenarios/</c>, <c>*.uat.md</c>, <c># UAT:</c>, <c>*.phrases.md</c>, <c>## Phrase:</c>) that
/// <c>brinell-data validate --fix</c> applies.
/// </summary>
public static class UatFileFormat
{
    /// <summary>The folder of feature and step files, beside the config.</summary>
    public const string FeatureFolder = "Features";

    /// <summary>The name <see cref="FeatureFolder"/> had before feature files.</summary>
    public const string OldFeatureFolder = "Scenarios";

    /// <summary>The name ending of a feature file.</summary>
    public const string FeatureExtension = ".feature.md";

    /// <summary>The search pattern for feature files.</summary>
    public const string FeatureFilePattern = "*" + FeatureExtension;

    /// <summary>The name ending of a step file.</summary>
    public const string StepsExtension = ".steps.md";

    /// <summary>The search pattern for step files.</summary>
    public const string StepFilePattern = "*" + StepsExtension;

    /// <summary>The config of a suite that drives a UI.</summary>
    public const string UiConfigFileName = "uat.config.md";

    /// <summary>The config of a suite below the UI (the <c>service</c> head).</summary>
    public const string ServiceConfigFileName = "bdd.config.md";

    /// <summary>
    /// The config in a folder: <c>uat.config.md</c> or <c>bdd.config.md</c>. The name says which
    /// kind of suite it is; both are read the same way. When neither exists, the
    /// <c>uat.config.md</c> path.
    /// </summary>
    /// <param name="directory">The folder to look in.</param>
    public static string FindConfig(string directory)
    {
        var service = Path.Combine(directory, ServiceConfigFileName);
        var ui = Path.Combine(directory, UiConfigFileName);
        return !File.Exists(ui) && File.Exists(service) ? service : ui;
    }

    /// <summary>Whether a file name is a config's.</summary>
    /// <param name="fileName">A file name, without folder.</param>
    public static bool IsConfigFileName(string fileName) =>
        fileName.Equals(UiConfigFileName, StringComparison.OrdinalIgnoreCase) ||
        fileName.Equals(ServiceConfigFileName, StringComparison.OrdinalIgnoreCase);

    /// <summary>The diagnostic code for a heading or file name in the old form.</summary>
    public const string OldFormCode = "UAT029";

    private static readonly (string Old, string New)[] Extensions =
    [
        (".uat.md", FeatureExtension),
        (".phrases.md", StepsExtension),
    ];

    // Longest first, so '## Phrase for each row:' is not read as '## Phrase:'.
    private static readonly (string Old, string New)[] Headings =
    [
        ("## Phrase for each row:", "## Step for each row:"),
        ("## Phrase:", "## Step:"),
        ("# Phrases:", "# Steps:"),
        ("# UAT:", "# Feature:"),
    ];

    /// <summary>The search patterns of files in the old form.</summary>
    public static IEnumerable<string> OldFilePatterns => Extensions.Select(x => "*" + x.Old);

    /// <summary>The new name of a file in the old form; null when its name is already right.</summary>
    /// <param name="path">A file path.</param>
    public static string? NewFileName(string path)
    {
        foreach (var (old, @new) in Extensions)
        {
            if (path.EndsWith(old, StringComparison.OrdinalIgnoreCase))
            {
                return path[..^old.Length] + @new;
            }
        }

        return null;
    }

    /// <summary>The new form of a heading in the old form; null when it is not one.</summary>
    /// <param name="trimmedLine">A line, trimmed.</param>
    public static string? NewHeading(string trimmedLine)
    {
        foreach (var (old, @new) in Headings)
        {
            if (trimmedLine.StartsWith(old, StringComparison.Ordinal))
            {
                return @new + trimmedLine[old.Length..];
            }
        }

        return null;
    }

    /// <summary>Rewrites every heading in the old form; the rest of the text is kept as it is.</summary>
    /// <param name="text">A feature or step file's text.</param>
    /// <returns>The text, and how many headings changed.</returns>
    public static (string Text, int Count) RewriteHeadings(string text)
    {
        var count = 0;
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var indent = line[..(line.Length - line.TrimStart().Length)];
            if (NewHeading(line.Trim()) is { } heading)
            {
                lines[i] = indent + heading + (line.EndsWith('\r') ? "\r" : string.Empty);
                count++;
            }
        }

        return (string.Join('\n', lines), count);
    }
}
