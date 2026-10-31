using System.Text;
using System.Text.RegularExpressions;

namespace Bruinet.Narrator;

/// <summary>A name and how it is said.</summary>
/// <param name="Term">The word as scripts write it; matched whole and case-sensitively.</param>
/// <param name="Say">What is said instead, as plain words (<c>Brew-ee-net</c>); null when only <paramref name="Ipa"/> is given.</param>
/// <param name="Ipa">The pronunciation in IPA, for a voice that follows <c>phoneme</c>; null for none.</param>
/// <param name="Line">The row's line in the file.</param>
public sealed record Pronunciation(string Term, string? Say, string? Ipa, int Line);

/// <summary>
/// How a suite's names are said, once: <c>pronunciations.md</c> beside <c>uat.config.md</c>, a table
/// <c>Term | Say | IPA</c>. Before a line is spoken each whole-word occurrence of a term in its plain text is wrapped in
/// <c>&lt;sub alias="Say"&gt;</c> (or <c>&lt;phoneme alphabet="ipa" ph="IPA"&gt;</c> when there is no Say), so no script carries tags
/// for it and every voice, one that follows no markup included, says the alias. Terms inside a tag the author wrote are left alone.
/// </summary>
public sealed class Pronunciations
{
    /// <summary>The file's name.</summary>
    public const string FileName = "pronunciations.md";

    /// <summary>A term is listed twice, or a row has nothing to say.</summary>
    public const string ProblemCode = "VID025";

    private readonly Regex? _terms;
    private readonly Dictionary<string, Pronunciation> _byTerm;

    private Pronunciations(string path, IReadOnlyList<Pronunciation> entries, IReadOnlyList<(int Line, string Message)> problems)
    {
        Path = path;
        Entries = entries;
        Problems = problems;
        _byTerm = entries.ToDictionary(entry => entry.Term, StringComparer.Ordinal);
        if (entries.Count > 0)
        {
            var alternatives = entries.OrderByDescending(entry => entry.Term.Length).Select(entry => Regex.Escape(entry.Term));
            _terms = new Regex($@"(?<![\p{{L}}\p{{N}}_])(?:{string.Join("|", alternatives)})(?![\p{{L}}\p{{N}}_])", RegexOptions.CultureInvariant);
        }
    }

    /// <summary>The file the terms were read from.</summary>
    public string Path { get; }

    /// <summary>The terms with something to say, in the file's order.</summary>
    public IReadOnlyList<Pronunciation> Entries { get; }

    /// <summary>What is wrong with the file (<see cref="ProblemCode"/>), by line.</summary>
    public IReadOnlyList<(int Line, string Message)> Problems { get; }

    /// <summary>What a kept scene's hash holds, so a scene is out of date when a term changes: the terms and what they say.</summary>
    public string Fingerprint => string.Join("\n", Entries.Select(entry => $"{entry.Term}|{entry.Say}|{entry.Ipa}"));

    /// <summary>The suite's pronunciations, or null when there is no file or it holds nothing.</summary>
    /// <param name="configDirectory">The suite's folder; null for none.</param>
    public static Pronunciations? Load(string? configDirectory)
    {
        if (configDirectory is null)
        {
            return null;
        }

        var path = System.IO.Path.Combine(configDirectory, FileName);
        if (!File.Exists(path))
        {
            return null;
        }

        var found = Parse(File.ReadAllText(path), path);
        return found.Entries.Count == 0 && found.Problems.Count == 0 ? null : found;
    }

    /// <summary>Reads a file's text: a table with <c>Term</c>, <c>Say</c> and <c>IPA</c> columns; other lines are notes.</summary>
    /// <param name="text">The file's text.</param>
    /// <param name="path">The file's path.</param>
    public static Pronunciations Parse(string text, string path)
    {
        ArgumentNullException.ThrowIfNull(text);
        List<Pronunciation> entries = [];
        List<(int Line, string Message)> problems = [];
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (!line.StartsWith('|'))
            {
                continue;
            }

            var cells = line.Trim('|').Split('|').Select(cell => cell.Trim()).ToList();
            if (cells.All(cell => cell.Length == 0 || cell.All(character => character is '-' or ':')) || string.Equals(cells[0], "Term", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var term = cells[0];
            var say = cells.Count > 1 && cells[1].Length > 0 ? cells[1] : null;
            var ipa = cells.Count > 2 && cells[2].Length > 0 ? cells[2] : null;
            if (term.Length == 0)
            {
                continue;
            }

            if (say is null && ipa is null)
            {
                problems.Add((index + 1, $"'{term}' has nothing to say: give it a Say or an IPA."));
            }
            else if (entries.Any(entry => entry.Term == term))
            {
                problems.Add((index + 1, $"'{term}' is listed twice."));
            }
            else
            {
                entries.Add(new Pronunciation(term, say, ipa, index + 1));
            }
        }

        return new Pronunciations(path, entries, problems);
    }

    /// <summary>The line with each whole-word term in its plain text wrapped for its pronunciation; terms inside <c>sub</c>, <c>phoneme</c> and <c>say-as</c> are left alone.</summary>
    /// <param name="line">What the script says.</param>
    public string Apply(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return _terms is null ? line : SpokenLine.MapText(line, text => _terms.Replace(text, match => Wrap(_byTerm[match.Value])));
    }

    private static string Wrap(Pronunciation entry)
        => entry.Say is { } say
            ? $"<sub alias=\"{Attribute(say)}\">{entry.Term}</sub>"
            : $"<phoneme alphabet=\"ipa\" ph=\"{Attribute(entry.Ipa!)}\">{entry.Term}</phoneme>";

    private static string Attribute(string value)
    {
        var text = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            text.Append(character switch { '&' => "&amp;", '"' => "&quot;", '<' => "&lt;", '>' => "&gt;", _ => character.ToString() });
        }

        return text.ToString();
    }
}
