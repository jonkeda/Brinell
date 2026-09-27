using System.Text.RegularExpressions;

namespace Brinell.Bdd;

public sealed class UatCommandCatalog
{
    private readonly List<UatCommandPattern> _patterns = [];
    private readonly List<UatUnmatchableDeclaration> _unmatchable = [];
    private readonly List<BddHook> _hooks = [];

    public IReadOnlyList<UatCommandPattern> Patterns => _patterns;

    /// <summary>The hooks of the step-definition classes.</summary>
    public IReadOnlyList<BddHook> Hooks => _hooks;

    /// <summary>Adds a hook.</summary>
    /// <param name="hook">The hook.</param>
    public void AddHook(BddHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _hooks.Add(hook);
    }

    /// <summary>Declarations that are broken and have no phrase text to match (AD-016).</summary>
    public IReadOnlyList<UatUnmatchableDeclaration> Unmatchable => _unmatchable;

    /// <summary>The app's own argument types, which step values are read with.</summary>
    public UatArgumentTypes ArgumentTypes { get; set; } = UatArgumentTypes.Empty;

    /// <summary>
    /// What a head adds to binding beyond namespaces: Brinell.Uat's pages, which a step's page
    /// scope follows. Null for a catalog without one: then steps bind without page scope.
    /// </summary>
    public IStepScopeModel? Scope { get; set; }

    /// <summary>
    /// Whether this is the generic catalog of built-in phrases only (Brinell.Uat's <c>UatSpecCommandCatalog</c>),
    /// which knows no app's phrases: a file's <c>Using</c> is not checked against it.
    /// </summary>
    public bool IsGeneric { get; set; }

    public UatCommandPattern Register(
        UatEffectiveStepKeyword keyword,
        string phrase,
        string? commandId = null,
        bool requiresTable = false,
        bool allowsTable = true,
        UatCommandHandler? handler = null,
        IReadOnlyList<UatPhraseParameter>? parameters = null,
        UatPhraseProblem? problem = null,
        bool namesPage = false,
        string? page = null,
        string? pageAfter = null,
        UatControlVerb? controlVerb = null,
        string? phraseNamespace = null)
    {
        var pattern = new UatCommandPattern(keyword, phrase, commandId, requiresTable, allowsTable, handler, parameters, problem)
        {
            NamesPage = namesPage,
            Page = page,
            PageAfter = pageAfter,
            ControlVerb = controlVerb,
            Namespace = UatNamespaces.Normalize(phraseNamespace),
        };
        _patterns.Add(pattern);
        return pattern;
    }

    /// <summary>Records a broken declaration that has no phrase to match.</summary>
    public void AddUnmatchable(UatUnmatchableDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        _unmatchable.Add(declaration);
    }

    public IReadOnlyList<UatCommandMatch> Match(UatStep step) => Select(Candidates(step));

    /// <summary>Every pattern the step's text fits, before <see cref="Select"/> picks among them.</summary>
    public IReadOnlyList<UatCommandMatch> Candidates(UatStep step)
        => _patterns
            .Where(x => x.Keyword == step.EffectiveKeyword)
            .Select(x => x.TryMatch(step))
            .OfType<UatCommandMatch>()
            .ToArray();

    /// <summary>
    /// The matches a step binds to: exact phrases before parameterised ones, then the most
    /// specific. One rule for C# phrases and callable phrases alike, so neither shadows the other.
    /// </summary>
    public static IReadOnlyList<UatCommandMatch> Select(IReadOnlyList<UatCommandMatch> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);

        var exactMatches = matches.Where(x => x.Pattern.IsExact).ToArray();
        if (exactMatches.Length > 0)
        {
            return exactMatches;
        }

        if (matches.Count <= 1)
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

    /// <summary>The callable phrase this pattern calls; null for a C# phrase.</summary>
    public UatCallable? Callable { get; init; }

    /// <summary>Whether the step says which page the user is on (<c>I am on the {page} page</c>).</summary>
    public bool NamesPage { get; init; }

    /// <summary>The page a page phrase belongs to; null for any other phrase.</summary>
    public string? Page { get; init; }

    /// <summary>The page that is current after the step: a page phrase's returned page; null when the step does not say.</summary>
    public string? PageAfter { get; init; }

    /// <summary>What the control method is, for a control verb; null for any other phrase.</summary>
    public UatControlVerb? ControlVerb { get; init; }

    /// <summary>The namespace of a C# phrase class's phrase (<see cref="UatNamespaces"/>); <c>""</c> when every scenario sees it.</summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>Whether the step binds against the current page: a control verb or a page phrase.</summary>
    public bool IsPageScoped => ControlVerb is not null || Page is not null;

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

    /// <summary>The <c>{x}</c> placeholders of a phrase, in order.</summary>
    public static IReadOnlyList<string> ParameterNames(string phrase)
        => Regex.Matches(phrase, @"\{([A-Za-z][A-Za-z0-9_-]*)\}").Select(match => match.Groups[1].Value).ToArray();

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

/// <summary>The control method a control verb invokes, which the binder checks a page's control against.</summary>
/// <param name="MethodName">The method, invoked by name.</param>
/// <param name="ArgumentCount">How many arguments the step passes: the item, then the value or literal.</param>
/// <param name="NamesControl">Whether the phrase has <c>{control}</c>; without it, the page's one control that declares the verb runs it.</param>
/// <param name="Navigates">Whether the verb can leave the page (<see cref="Brinell.Core.Testing.BddStepAttribute.Navigates"/>).</param>
public sealed record UatControlVerb(string MethodName, int ArgumentCount, bool NamesControl, bool Navigates);

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
