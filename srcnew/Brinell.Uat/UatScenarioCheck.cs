using System.Runtime.CompilerServices;
using System.Text;
using Brinell.Core.Composition;
using System.Text.RegularExpressions;

namespace Brinell.Uat;

/// <summary>A value rewritten in place: the one right answer for a near miss (<c>2026-05-12</c> to <c>12-May-2026</c>).</summary>
/// <param name="Original">The value as written.</param>
/// <param name="Replacement">The value as it should be written.</param>
public sealed record UatTextFix(string Original, string Replacement);

/// <summary>
/// Checks scenario files without running them: the same parser, binder and value rules as the
/// xUnit gate, against a catalog built from the fixture's type. Used by the <c>brinell-data</c> tool.
/// </summary>
public static partial class UatScenarioCheck
{
    /// <summary>The catalogs a suite's steps bind against, from its fixture type and config.</summary>
    /// <param name="fixtureType">The fixture the config's <c>Fixture</c> names.</param>
    /// <param name="config">The suite's config, for the <c>Apps</c> table and the head.</param>
    /// <remarks>
    /// An <see cref="IUatAppsHost"/> names each head's modules (<see cref="IUatAppsHost.ModulesFor"/>);
    /// they are asked of an uninitialized fixture, so a head whose modules need a started fixture
    /// binds against the suite's catalog.
    /// </remarks>
    public static UatCatalogSet CatalogsFor(Type fixtureType, UatConfig config)
    {
        ArgumentNullException.ThrowIfNull(fixtureType);
        ArgumentNullException.ThrowIfNull(config);

        var runtime = UatReflectionRuntime.ForChecking(fixtureType);
        var catalog = runtime.CreateCommandCatalog();
        if (typeof(IUatSessionHost).IsAssignableFrom(fixtureType))
        {
            UatSessionSteps.Register(catalog, host: null, config);
        }

        var suiteHead = config.Runtime.TryGetValue(UatConfigFields.Head, out var head) && head.Trim() is { Length: > 0 } trimmed ? trimmed : null;
        List<UatReflectionRuntime> runtimes = [runtime];
        Dictionary<string, UatCommandCatalog> byHead = new(StringComparer.OrdinalIgnoreCase);
        if (typeof(IUatAppsHost).IsAssignableFrom(fixtureType))
        {
            var host = (IUatAppsHost)RuntimeHelpers.GetUninitializedObject(fixtureType);
            foreach (var app in config.Apps)
            {
                if ((app.Head ?? suiteHead) is not { } appHead || byHead.ContainsKey(appHead) || TryModulesFor(host, app) is not { } modules)
                {
                    continue;
                }

                var headRuntime = UatReflectionRuntime.ForChecking(fixtureType, TestCompositionCatalog.Discover(modules));
                var headCatalog = headRuntime.CreateCommandCatalog();
                UatSessionSteps.Register(headCatalog, host: null, config);
                byHead[appHead] = headCatalog;
                runtimes.Add(headRuntime);
            }
        }

        var reserved = runtimes.SelectMany(r => r.Pages).SelectMany(page => page.Controls.Select(control => control.Name).Append(page.Name));
        return new UatCatalogSet(catalog, config.Apps, suiteHead, byHead, reserved);
    }

    private static TestCompositionDiscoveryOptions? TryModulesFor(IUatAppsHost host, UatAppConfig app)
    {
        try
        {
            return host.ModulesFor(new TestAppDefinition(app.Name, app.User, app.Head, app.Values));
        }
        catch (Exception exception) when (exception is NotSupportedException or NullReferenceException or InvalidOperationException)
        {
            // A head whose modules need a started fixture: its steps bind against the suite's catalog.
            return null;
        }
    }

    /// <summary>Every problem in a scenario file: parse (<c>UAT0xx</c>), bind (<c>UATB0xx</c>) and values (<c>UATA0xx</c>).</summary>
    public static IReadOnlyList<UatDiagnostic> CheckFile(string filePath, UatCatalogSet catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        var file = UatScenarioFiles.Load(filePath);
        if (file.Document is null)
        {
            return file.Diagnostics;
        }

        // The file's own broken callables are warnings at their headings; a call to one is the error.
        var own = new UatCallableSet(file.Document.Callables).Prepare(catalogs).All;
        return [.. file.Diagnostics, .. file.Bind(catalogs).Diagnostics, .. BrokenCallables(own)];
    }

    /// <summary>
    /// A phrase file's problems: parse errors, and each broken callable as a warning at its heading.
    /// A call to a broken callable is the error, in the scenario that makes it.
    /// </summary>
    public static IReadOnlyList<UatDiagnostic> CheckPhraseFile(string filePath, UatCatalogSet catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        var parse = UatScenarioFiles.ParsePhraseFile(filePath);
        if (parse.Document is null)
        {
            return parse.Diagnostics;
        }

        return [.. parse.Diagnostics, .. BrokenCallables(new UatCallableSet(parse.Document.Callables).Prepare(catalogs).All)];
    }

    /// <summary>The callable phrases as the phrase list shows them: types, where each is declared, and problems.</summary>
    public static IReadOnlyList<string> DescribeCallables(IEnumerable<UatPreparedCallable> callables)
    {
        ArgumentNullException.ThrowIfNull(callables);
        return callables
            .OrderBy(callable => callable.Callable.Phrase, StringComparer.Ordinal)
            .Select(callable => $"Callable {callable}  <- {callable.Callable.Source}"
                + (callable.Problem is { } problem ? $"  warning {problem.Code}: {problem.Message}" : string.Empty))
            .ToList();
    }

    private static IEnumerable<UatDiagnostic> BrokenCallables(IEnumerable<UatPreparedCallable> callables)
        => callables
            .Where(callable => callable.Problem is not null)
            .Select(callable => new UatDiagnostic(
                UatDiagnosticSeverity.Warning,
                callable.Problem!.Code,
                $"The phrase '{callable.Callable.Phrase}' {callable.Problem.Message} Steps that call it fail.",
                callable.Callable.Source));

    /// <summary>The text of a file with every diagnostic's <see cref="UatDiagnostic.Fix"/> applied on its line.</summary>
    /// <returns>The new text, and how many values it rewrote.</returns>
    public static (string Text, int Fixed) ApplyFixes(string text, IEnumerable<UatDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = text.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        var count = 0;
        foreach (var diagnostic in diagnostics.Where(d => d.Fix is not null))
        {
            var index = diagnostic.Location.LineNumber - 1;
            var fix = diagnostic.Fix!;
            if (index < 0 || index >= lines.Length || lines[index].IndexOf(fix.Original, StringComparison.Ordinal) is var at && at < 0)
            {
                continue;
            }

            lines[index] = lines[index][..at] + fix.Replacement + lines[index][(at + fix.Original.Length)..];
            count++;
        }

        return (string.Join(newline, lines), count);
    }

    /// <summary>The phrases of a catalog as the phrase list shows them, with types and problems.</summary>
    public static IReadOnlyList<string> DescribePhrases(UatCommandCatalog catalog, UatEffectiveStepKeyword? keyword = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var lines = catalog.Patterns
            .Where(pattern => keyword is null || pattern.Keyword == keyword)
            .OrderBy(pattern => pattern.Keyword)
            .ThenBy(pattern => pattern.Phrase, StringComparer.Ordinal)
            .Select(Describe)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        lines.AddRange(catalog.Unmatchable
            .Where(declaration => keyword is null || declaration.Keyword == keyword)
            .Select(declaration => $"{declaration.Keyword} (no phrase)  <- {declaration.Problem.Declaration}  warning {declaration.Problem.Code}: {declaration.Problem.Message}"));
        return lines;
    }

    /// <summary>A method for a step that binds to nothing: its phrase in the method name, the values as parameters.</summary>
    public static string Snippet(UatStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        List<string> words = [];
        List<string> phrase = [];
        List<string> parameters = [];
        var encodable = true;
        foreach (Match token in TokenRegex().Matches(step.Text))
        {
            if (token.Groups["word"].Success)
            {
                var word = token.Value;
                encodable &= word.All(char.IsLetterOrDigit);
                words.Add(word);
                phrase.Add(word);
                continue;
            }

            var name = $"p{parameters.Count + 1}";
            parameters.Add($"{TypeOf(token)} {name}");
            words.Add(name.ToUpperInvariant());
            phrase.Add($"{{{name}}}");
        }

        if (step.Table is not null)
        {
            parameters.Add("UatTable table");
        }
        else if (step.DocString is not null)
        {
            parameters.Add("string text");
        }

        var attribute = $"Uat{step.EffectiveKeyword}";
        var signature = string.Join(", ", parameters);
        var builder = new StringBuilder();
        if (encodable)
        {
            builder.AppendLine($"[{attribute}]");
            builder.AppendLine($"public void {step.EffectiveKeyword}_{string.Join('_', words)}({signature})");
        }
        else
        {
            builder.AppendLine($"[{attribute}(\"{string.Join(' ', phrase).Replace("\"", "\\\"", StringComparison.Ordinal)}\")]");
            builder.AppendLine($"public void {string.Concat(words.Where(word => !word.StartsWith('P') || !word.Skip(1).All(char.IsDigit)).Select(Pascal))}({signature})");
        }

        builder.AppendLine("{");
        builder.Append('}');
        return builder.ToString();
    }

    private static string Describe(UatCommandPattern pattern)
    {
        var text = pattern.Phrase;
        foreach (var parameter in pattern.Parameters.Where(p => p.Source == UatParameterSource.Phrase))
        {
            text = text.Replace($"{{{parameter.Name}}}", parameter.ToString(), StringComparison.Ordinal);
        }

        var below = pattern.Parameters.FirstOrDefault(p => p.Source != UatParameterSource.Phrase) is { } block
            ? block.Source == UatParameterSource.DocString ? $"  + doc string '{block.Name}'" : $"  + table '{block.Name}': {UatValues.Describe(block.Type)}"
            : string.Empty;
        var problem = pattern.Problem is { } broken ? $"  warning {broken.Code}: {broken.Message}" : string.Empty;
        return $"{pattern.Keyword} {text}{below}  <- {pattern.CommandId}{problem}";
    }

    private static string TypeOf(Match token)
    {
        var text = token.Value;
        return token.Groups["quoted"].Success ? "string"
            : token.Groups["date"].Success ? "DateOnly"
            : token.Groups["number"].Success ? text.Contains('.', StringComparison.Ordinal) ? "decimal" : "int"
            : "string";
    }

    private static string Pascal(string word)
        => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + new string(word[1..].Where(char.IsLetterOrDigit).ToArray());

    [GeneratedRegex(@"(?<quoted>""(?:[^""\\]|\\.)*"")|(?<date>\b\d{1,2}-[A-Za-z]{3,}-\d{4}\b|\b\d{4}-\d{2}-\d{2}\b)|(?<number>(?<![\w-])-?\d[\d,]*(?:\.\d+)?\b)|(?<word>[^\s""]+)")]
    private static partial Regex TokenRegex();
}
