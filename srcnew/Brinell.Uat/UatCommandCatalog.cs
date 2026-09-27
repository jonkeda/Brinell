using System.Text.RegularExpressions;

namespace Brinell.Uat;

public sealed class UatCommandCatalog
{
    private readonly List<UatCommandPattern> _patterns = [];
    private readonly List<UatUnmatchableDeclaration> _unmatchable = [];

    public IReadOnlyList<UatCommandPattern> Patterns => _patterns;

    /// <summary>Declarations that are broken and have no phrase text to match (AD-016).</summary>
    public IReadOnlyList<UatUnmatchableDeclaration> Unmatchable => _unmatchable;

    /// <summary>The app's own argument types, which step values are read with.</summary>
    public UatArgumentTypes ArgumentTypes { get; set; } = UatArgumentTypes.Empty;

    public UatCommandPattern Register(
        UatEffectiveStepKeyword keyword,
        string phrase,
        string? commandId = null,
        bool requiresTable = false,
        bool allowsTable = true,
        UatCommandHandler? handler = null,
        IReadOnlyList<UatPhraseParameter>? parameters = null,
        UatPhraseProblem? problem = null)
    {
        var pattern = new UatCommandPattern(keyword, phrase, commandId, requiresTable, allowsTable, handler, parameters, problem);
        _patterns.Add(pattern);
        return pattern;
    }

    /// <summary>Records a broken declaration that has no phrase to match.</summary>
    public void AddUnmatchable(UatUnmatchableDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        _unmatchable.Add(declaration);
    }

    public IReadOnlyList<UatCommandMatch> Match(UatStep step)
    {
        var matches = _patterns
            .Where(x => x.Keyword == step.EffectiveKeyword)
            .Select(x => x.TryMatch(step))
            .OfType<UatCommandMatch>()
            .ToArray();

        var exactMatches = matches.Where(x => x.Pattern.IsExact).ToArray();
        if (exactMatches.Length > 0)
        {
            return exactMatches;
        }

        if (matches.Length <= 1)
        {
            return matches;
        }

        var highestSpecificity = matches.Max(x => x.Pattern.Specificity);
        return matches.Where(x => x.Pattern.Specificity == highestSpecificity).ToArray();
    }
}

public sealed class UatCommandPattern
{
    private readonly Regex _regex;
    private readonly IReadOnlyList<string> _parameterNames;

    public UatCommandPattern(
        UatEffectiveStepKeyword keyword,
        string phrase,
        string? commandId = null,
        bool requiresTable = false,
        bool allowsTable = true,
        UatCommandHandler? handler = null,
        IReadOnlyList<UatPhraseParameter>? parameters = null,
        UatPhraseProblem? problem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phrase);

        Keyword = keyword;
        Phrase = phrase.Trim();
        CommandId = string.IsNullOrWhiteSpace(commandId) ? Phrase : commandId.Trim();
        RequiresTable = requiresTable;
        AllowsTable = allowsTable;
        Handler = handler;
        Parameters = parameters ?? [];
        Problem = problem;

        (_regex, _parameterNames) = Compile(Phrase);
        IsExact = _parameterNames.Count == 0;
        Specificity = CalculateSpecificity(Phrase);
    }

    public UatEffectiveStepKeyword Keyword { get; }

    public string Phrase { get; }

    public string CommandId { get; }

    public bool RequiresTable { get; }

    public bool AllowsTable { get; }

    public UatCommandHandler? Handler { get; }

    /// <summary>What the step supplies, with types; empty for a pattern whose arguments stay text.</summary>
    public IReadOnlyList<UatPhraseParameter> Parameters { get; }

    /// <summary>Why the phrase cannot run, reported when a step binds to it; null when it is fine.</summary>
    public UatPhraseProblem? Problem { get; }

    public bool IsExact { get; }

    public int Specificity { get; }

    public UatCommandMatch? TryMatch(UatStep step)
    {
        if (step.EffectiveKeyword != Keyword)
        {
            return null;
        }

        var match = _regex.Match(step.Text);
        if (!match.Success)
        {
            return null;
        }

        Dictionary<string, string> arguments = new(StringComparer.Ordinal);
        Dictionary<string, string> raw = new(StringComparer.Ordinal);
        for (var i = 0; i < _parameterNames.Count; i++)
        {
            raw[_parameterNames[i]] = match.Groups[$"p{i}"].Value.Trim();
            arguments[_parameterNames[i]] = TrimArgument(match.Groups[$"p{i}"].Value);
        }

        return new UatCommandMatch(this, arguments) { RawArguments = raw };
    }

    private static (Regex Regex, IReadOnlyList<string> Parameters) Compile(string phrase)
    {
        var matches = Regex.Matches(phrase, @"\{([A-Za-z][A-Za-z0-9_-]*)\}");
        List<string> parameterNames = [];
        var pattern = "^";
        var lastIndex = 0;

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            pattern += EscapeLiteral(phrase[lastIndex..match.Index]);
            pattern += $"(?<p{i}>.+?)";
            parameterNames.Add(match.Groups[1].Value);
            lastIndex = match.Index + match.Length;
        }

        pattern += EscapeLiteral(phrase[lastIndex..]);
        pattern += "$";

        return (new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant), parameterNames);
    }

    private static int CalculateSpecificity(string phrase)
    {
        var withoutParameters = Regex.Replace(phrase, @"\{[A-Za-z][A-Za-z0-9_-]*\}", string.Empty);
        return withoutParameters.Count(x => !char.IsWhiteSpace(x));
    }

    private static string EscapeLiteral(string literal)
    {
        var escaped = string.Empty;
        var inWhitespace = false;

        foreach (var character in literal)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!inWhitespace)
                {
                    escaped += @"\s+";
                    inWhitespace = true;
                }

                continue;
            }

            escaped += Regex.Escape(character.ToString());
            inWhitespace = false;
        }

        return escaped;
    }

    private static string TrimArgument(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"'
            ? trimmed[1..^1]
            : trimmed;
    }
}

public sealed record UatCommandMatch(
    UatCommandPattern Pattern,
    IReadOnlyDictionary<string, string> Arguments)
{
    /// <summary>The arguments as written, quotes kept, which typed parameters are read from.</summary>
    public IReadOnlyDictionary<string, string> RawArguments { get; init; } = Arguments;
}

public delegate Task<UatStepResult> UatCommandHandler(
    UatExecutionContext context,
    UatStepInvocation invocation,
    CancellationToken cancellationToken);
