using System.Text.RegularExpressions;

namespace Brinell.Uat;

public static class UatBinder
{
    private const string UserParameter = "user";
    private const string PageParameter = "page";

    // A callable that calls a callable that calls a callable is already hard to follow in a
    // failure report; a fourth level is refused.
    private const int MaxCallDepth = 3;

    public static UatBindResult Bind(UatDocument document, UatCommandCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return Bind(document, new UatCatalogSet(catalog));
    }

    /// <summary>
    /// Binds every step against the catalog of the app it runs on. The current app follows the
    /// steps: a step that starts with an app or user (<c>Daan taps Save</c>) makes that app
    /// current, and so does <c>{app} uses the app</c>. See AD-015.
    /// </summary>
    /// <param name="document">The parsed document.</param>
    /// <param name="catalogs">The catalogs and the apps.</param>
    public static UatBindResult Bind(UatDocument document, UatCatalogSet catalogs)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Bind(document, catalogs, new UatCallableSet(document.Callables));
    }

    /// <summary>
    /// Binds every step, and expands every call to a callable phrase into its steps, bound where
    /// the call is. A problem inside a call is reported at the call.
    /// </summary>
    /// <param name="document">The parsed document.</param>
    /// <param name="catalogs">The catalogs and the apps.</param>
    /// <param name="callables">The callable phrases the document can see (<see cref="UatScenarioFiles.Load"/>).</param>
    public static UatBindResult Bind(UatDocument document, UatCatalogSet catalogs, UatCallableSet callables)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentNullException.ThrowIfNull(callables);

        List<UatDiagnostic> diagnostics = [.. catalogs.CheckNames(document.Source)];
        List<UatBoundScenario> scenarios = [];
        var prepared = callables.Prepare(catalogs);

        foreach (var scenario in document.Scenarios)
        {
            List<UatStepInvocation> invocations = [];
            var walk = new Walk(catalogs, prepared, scenario.Name, invocations, inference: null);

            foreach (var step in document.Background)
            {
                walk.Step(step, fromBackground: true, frame: null, diagnostics);
            }

            foreach (var step in scenario.Steps)
            {
                walk.Step(step, fromBackground: false, frame: null, diagnostics);
            }

            scenarios.Add(new UatBoundScenario(scenario, invocations));
        }

        var boundDocument = diagnostics.Any(x => x.Severity == UatDiagnosticSeverity.Error)
            ? null
            : new UatBoundDocument(document, scenarios);

        return new UatBindResult(boundDocument, diagnostics);
    }

    // Binds a callable's body with its placeholders in, against the suite's own catalog, to learn
    // the types of its values. Only a problem that no call could avoid makes it broken.
    internal static void Infer(UatCallable callable, UatCatalogSet catalogs, UatPreparedCallables callables, UatInference inference)
    {
        var walk = new Walk(catalogs, callables, callable.Phrase, [], inference);
        List<UatDiagnostic> ignored = [];
        foreach (var step in callable.Steps)
        {
            walk.Step(step, fromBackground: false, frame: null, ignored);
        }
    }

    // {user} is the acting user: "I" is the current app's user, and a name is checked against the
    // Apps table. In first place it also makes that user's app current, as a subject does.
    private static bool TryBindUser(
        UatCatalogSet catalogs,
        AppTracker apps,
        UatCommandMatch match,
        string user,
        ref UatAppConfig? app,
        out string boundUser,
        out string error)
    {
        boundUser = user;
        if (user == "I")
        {
            if (app is null)
            {
                error = $"it says 'I' for {{{UserParameter}}}, but no app is current yet, so there is no user. Start with a step that names one: {catalogs.DescribeApps()}.";
                return false;
            }

            boundUser = app.User;
            error = string.Empty;
            return true;
        }

        if (!apps.TryResolve(user, out var userApp, out error))
        {
            return false;
        }

        boundUser = userApp.User;
        if (FirstParameterName(match.Pattern) == UserParameter)
        {
            app = userApp;
        }

        return true;
    }

    private static IReadOnlyList<string> NamedPhrases(UatCommandCatalog catalog, UatEffectiveStepKeyword keyword)
        => catalog.Patterns
            .Where(pattern => pattern.Keyword == keyword && FirstParameterName(pattern) == UserParameter)
            .Select(pattern => pattern.Phrase)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string? FirstArgument(UatCommandMatch match)
        => FirstParameterName(match.Pattern) is { } name && match.Arguments.TryGetValue(name, out var value) ? value : null;

    private static string? FirstParameterName(UatCommandPattern pattern)
        => Regex.Match(pattern.Phrase, @"^\{([A-Za-z][A-Za-z0-9_-]*)\}") is { Success: true } found ? found.Groups[1].Value : null;

    private static UatDiagnostic Error(string code, string message, UatSourceLocation location)
        => new(UatDiagnosticSeverity.Error, code, message, location);

    // One scenario's steps in order, with the current app. Inference walks a callable's body
    // instead: it learns types and drops what only a call can decide.
    private sealed class Walk(
        UatCatalogSet catalogs,
        UatPreparedCallables callables,
        string scenarioName,
        List<UatStepInvocation> invocations,
        UatInference? inference)
    {
        private readonly AppTracker _apps = new(catalogs);
        private readonly Dictionary<string, PageState> _pages = new(StringComparer.OrdinalIgnoreCase);

        public void Step(UatStep step, bool fromBackground, UatCallFrame? frame, List<UatDiagnostic> diagnostics)
        {
            var app = _apps.Current;
            UatCommandMatch? match = null;
            IReadOnlyList<string> named = [];
            List<UatDiagnostic> pageProblems = [];

            if (catalogs.TryFindSubject(step.Text, out var subject, out _))
            {
                if (!_apps.TryResolve(subject, out var subjectApp, out var error))
                {
                    diagnostics.Add(Error("UATB006", error, step.Source));
                    return;
                }

                // Only a phrase declared with the subject in its first parameter binds it: {user} taps
                // {control}, {app} uses the app. The engine infers no verb forms (AD-015).
                var catalog = catalogs.CatalogFor(subjectApp);
                var declared = Match(catalog, step, subjectApp, pageProblems)
                    .Where(candidate => FirstArgument(candidate) == subject)
                    .ToArray();
                if (declared.Length > 1)
                {
                    diagnostics.Add(Error(
                        "UATB002",
                        $"Step '{step.Text}' is ambiguous: as {subject}, it matches {string.Join(", ", declared.Select(candidate => $"'{candidate.Pattern.Phrase}'"))}.",
                        step.Source));
                    return;
                }

                if (declared.Length == 1)
                {
                    match = declared[0];
                    app = subjectApp;
                }
                else if (pageProblems.Count > 0)
                {
                    diagnostics.AddRange(pageProblems);
                    return;
                }
                else
                {
                    named = NamedPhrases(catalog, step.EffectiveKeyword);
                }
            }

            if (match is null)
            {
                pageProblems.Clear();
                var matches = Match(catalogs.CatalogFor(app), step, app, pageProblems);
                if (matches.Count == 0 && pageProblems.Count > 0)
                {
                    diagnostics.AddRange(pageProblems);
                    return;
                }

                if (matches.Count == 0)
                {
                    diagnostics.Add(Error(
                        "UATB001",
                        subject.Length == 0
                            ? $"No command binding matches step '{step.Text}'."
                            : $"No command binding matches step '{step.Text}'. A step that names {subject} binds only to a phrase declared with {{{UserParameter}}} first; "
                              + (named.Count == 0
                                  ? $"no {step.EffectiveKeyword} phrase is."
                                  : $"the {step.EffectiveKeyword} ones are {string.Join(", ", named.Select(phrase => $"'{phrase}'"))}."),
                        step.Source));
                    return;
                }

                if (matches.Count > 1)
                {
                    diagnostics.Add(Error(
                        "UATB002",
                        $"Step '{step.Text}' is ambiguous and matches {matches.Count} command bindings: {string.Join(", ", matches.Select(Describe))}.",
                        step.Source));
                    return;
                }

                match = matches[0];
            }

            // A body step that passes the call's table has none yet: only a call can check it.
            if (match.Pattern.Callable is null && !(inference is not null && step.Passes != UatPassedBlock.None))
            {
                if (match.Pattern.RequiresTable && step.Table is null)
                {
                    diagnostics.Add(Error("UATB003", $"Step '{step.Text}' requires a table.", step.Source));
                    return;
                }

                if (!match.Pattern.AllowsTable && step.Table is not null)
                {
                    diagnostics.Add(Error("UATB004", $"Step '{step.Text}' does not accept a table.", step.Table.Source));
                    return;
                }
            }

            var arguments = match.Arguments;
            var rawArguments = match.RawArguments;
            if (arguments.TryGetValue(UserParameter, out var user) && inference is not null && UatOutline.WholePlaceholder(user) is { } userPlaceholder)
            {
                inference.Record(userPlaceholder, typeof(UatUserRole));
            }
            else if (catalogs.Apps.Count == 0 && user is not null && user != "I")
            {
                diagnostics.Add(Error(
                    "UATB007",
                    $"Step '{step.Text}' names the user '{user}', but the config has no Apps table to say whose app that is.",
                    step.Source));
                return;
            }
            else if (catalogs.Apps.Count > 0 && user is not null)
            {
                if (!TryBindUser(catalogs, _apps, match, user, ref app, out var boundUser, out var error))
                {
                    diagnostics.Add(Error("UATB007", $"Step '{step.Text}': {error}", step.Source));
                    return;
                }

                arguments = new Dictionary<string, string>(arguments, StringComparer.Ordinal) { [UserParameter] = boundUser };
                rawArguments = new Dictionary<string, string>(rawArguments, StringComparer.Ordinal) { [UserParameter] = boundUser };
            }

            if (match.Pattern.Callable is { } callable)
            {
                if (inference is not null)
                {
                    InferThroughCall(step, callable, rawArguments);
                }
                else
                {
                    Call(step, callables.Get(callable), rawArguments, app, fromBackground, frame, diagnostics);
                }

                return;
            }

            // A broken phrase and a value that does not fit fail here, before any app starts (AD-016).
            var argumentProblems = UatArguments.Check(
                match.Pattern,
                step,
                rawArguments,
                new UatValueContext(DateTimeOffset.UtcNow, catalogs.Apps, catalogs.CatalogFor(app).ArgumentTypes)
                {
                    Placeholders = inference is null ? null : inference.Record,
                });
            if (inference is not null)
            {
                // What the body writes itself cannot be fixed by a call: the callable is broken.
                var avoidable = step.Passes == UatPassedBlock.None ? [] : new[] { "UATA007" };
                if (argumentProblems.FirstOrDefault(problem => !avoidable.Contains(problem.Code)) is { } problem)
                {
                    inference.Fail("UATC008", $"has a step that cannot run: {problem.Code} {problem.Message}");
                }

                if (step.Passes == UatPassedBlock.Table)
                {
                    inference.BlockTypes.Add(match.Pattern.Parameters.FirstOrDefault(p => p.Source == UatParameterSource.Table)?.Type ?? typeof(UatTable));
                }

                _apps.Use(app);
                return;
            }

            if (argumentProblems.Count > 0)
            {
                diagnostics.AddRange(argumentProblems);
                return;
            }

            if (!Advance(step, match, arguments, app, diagnostics))
            {
                return;
            }

            _apps.Use(app);
            var pageAfter = match.Pattern.IsPageScoped && PageOf(app) is { LostAt: null, Page: { } known } ? known : null;
            invocations.Add(new UatStepInvocation(
                scenarioName,
                step,
                match.Pattern,
                arguments,
                step.Table,
                fromBackground)
            {
                App = app?.Name,
                RawArguments = rawArguments,
                Call = frame,
                PageAfter = pageAfter,
            });
        }

        // The phrases a step can bind to where it stands (D5): a control verb or page phrase only
        // on the current page, and never one of those together with a phrase of wider scope. A
        // catalog without pages, and a callable's body read on its own, bind without page scope.
        private IReadOnlyList<UatCommandMatch> Match(UatCommandCatalog catalog, UatStep step, UatAppConfig? app, List<UatDiagnostic> problems)
        {
            IReadOnlyList<UatCommandMatch> candidates = [.. catalog.Candidates(step), .. callables.Candidates(step)];
            if (catalog.Pages.Count == 0 || inference is not null)
            {
                return UatCommandCatalog.Select(candidates);
            }

            var wide = candidates.Where(candidate => !candidate.Pattern.IsPageScoped).ToList();
            var scoped = candidates.Where(candidate => candidate.Pattern.IsPageScoped).ToList();
            if (scoped.Count == 0)
            {
                return UatCommandCatalog.Select(wide);
            }

            var state = PageOf(app);
            if (state.Page is null || !catalog.Pages.TryGetValue(state.Page, out var page))
            {
                if (wide.Count > 0)
                {
                    return UatCommandCatalog.Select(wide);
                }

                problems.Add(Error(
                    "UATB008",
                    $"Step '{step.Text}' needs to know the page, but no step has said it yet. Start with 'Given I am on the ... page'.",
                    step.Source));
                return [];
            }

            List<string> reasons = [];
            var fitting = scoped.Where(candidate => Fits(page, candidate, reasons)).ToList();
            if (fitting.Count == 0 && wide.Count == 0 && state.LostAt is { } lost)
            {
                problems.Add(Error(
                    "UATB008",
                    $"Step '{step.Text}' does not fit the {page.Name} page, and the page may have changed at '{lost.Text}' ({lost.Source}). Say where that step ends: 'Then I should be on the ... page'.",
                    step.Source));
                return [];
            }

            if (fitting.Count > 0 && wide.Count > 0)
            {
                problems.Add(Error(
                    "UATB002",
                    $"Step '{step.Text}' is ambiguous: on the {page.Name} page it matches {string.Join(", ", fitting.Select(Describe))}, and also {string.Join(", ", wide.Select(Describe))}. Rename one of them.",
                    step.Source));
                return [];
            }

            if (fitting.Count == 0 && wide.Count == 0)
            {
                problems.Add(Error("UATB009", $"Step '{step.Text}' does not fit the {page.Name} page: {string.Join(" ", reasons.Distinct())}", step.Source));
                return [];
            }

            return UatCommandCatalog.Select(fitting.Count > 0 ? fitting : wide);
        }

        private static bool Fits(UatPageModel page, UatCommandMatch candidate, List<string> reasons)
        {
            if (candidate.Pattern.Page is { } owner)
            {
                if (string.Equals(owner, page.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                reasons.Add($"'{candidate.Pattern.Phrase}' is a phrase of the {owner} page.");
                return false;
            }

            if (UatPageScope.ControlFor(page, candidate, out var problem) is not null)
            {
                return true;
            }

            reasons.Add(char.ToUpperInvariant(problem![0]) + problem[1..]);
            return false;
        }

        private PageState PageOf(UatAppConfig? app)
            => _pages.TryGetValue(app?.Name ?? string.Empty, out var state) ? state : default;

        // The page after the step (D7): the page a step names; a page phrase's or control verb's
        // returned page. After a verb that can leave the page and does not say where, the page is
        // only a guess until a step fits it. Other phrases keep it.
        private bool Advance(UatStep step, UatCommandMatch match, IReadOnlyDictionary<string, string> arguments, UatAppConfig? app, List<UatDiagnostic> diagnostics)
        {
            var catalog = catalogs.CatalogFor(app);
            if (catalog.Pages.Count == 0)
            {
                return true;
            }

            var key = app?.Name ?? string.Empty;
            var pattern = match.Pattern;
            if (pattern.NamesPage && arguments.TryGetValue(PageParameter, out var named))
            {
                if (!catalog.Pages.TryGetValue(named, out var namedPage))
                {
                    diagnostics.Add(Error(
                        "UATB009",
                        $"Step '{step.Text}' names the page '{named}', which does not exist. The pages are {string.Join(", ", catalog.Pages.Keys.Order(StringComparer.OrdinalIgnoreCase))}.",
                        step.Source));
                    return false;
                }

                _pages[key] = new PageState(namedPage.Name, null);
                return true;
            }

            var state = PageOf(app);
            if (state.Page is null || !catalog.Pages.TryGetValue(state.Page, out var page))
            {
                return true;
            }

            if (pattern.Page is not null)
            {
                _pages[key] = new PageState(pattern.PageAfter ?? page.Name, null);
                return true;
            }

            if (pattern.ControlVerb is { } verb && UatPageScope.ControlFor(page, match, out _) is { } control)
            {
                var after = UatPageScope.PageAfter(page, control, verb, catalog.Pages);
                _pages[key] = new PageState(after ?? page.Name, after is null ? step : null);
            }

            return true;
        }

        private static string Describe(UatCommandMatch match)
            => match.Pattern.Callable is { } callable ? $"'{callable.Phrase}' ({callable.Source})" : $"'{match.Pattern.Phrase}'";

        // A body step that calls another callable: its placeholders take that callable's types.
        private void InferThroughCall(UatStep step, UatCallable callable, IReadOnlyDictionary<string, string> rawArguments)
        {
            var called = callables.Get(callable);
            if (called.Problem is { } problem)
            {
                inference!.Fail(problem.Code == "UATC004" ? "UATC004" : "UATC008", $"calls '{callable.Phrase}' ({callable.Source}), which {problem.Message.TrimEnd('.')}.");
                return;
            }

            foreach (var (name, value) in rawArguments)
            {
                if (UatOutline.WholePlaceholder(value) is { } placeholder && called.Types.TryGetValue(name, out var type))
                {
                    inference!.Record(placeholder, type);
                }
            }

            if (step.Passes == UatPassedBlock.Table && called.Passes == UatPassedBlock.Table && called.BlockType is { } blockType)
            {
                inference!.BlockTypes.Add(blockType);
            }

            if (callable.Kind == UatCallableKind.EachRow && step.Table is { } table)
            {
                foreach (var column in table.Columns)
                {
                    var parameter = called.Columns.FirstOrDefault(name => UatTableBinding.Key(name) == UatTableBinding.Key(column));
                    foreach (var row in table.Rows)
                    {
                        if (parameter is not null && UatOutline.WholePlaceholder(row.Cells[column]) is { } placeholder)
                        {
                            inference!.Record(placeholder, called.Types[parameter]);
                        }
                    }
                }
            }
        }

        private void Call(
            UatStep step,
            UatPreparedCallable prepared,
            IReadOnlyDictionary<string, string> rawArguments,
            UatAppConfig? app,
            bool fromBackground,
            UatCallFrame? frame,
            List<UatDiagnostic> diagnostics)
        {
            var callable = prepared.Callable;
            if (prepared.Problem is { } broken)
            {
                diagnostics.Add(Error(broken.Code, $"Step '{step.Text}': the phrase '{callable.Phrase}' from {callable.Source} {broken.Message}", step.Source));
                return;
            }

            if (frame?.Chain.FirstOrDefault(outer => outer.Callable == callable) is not null)
            {
                var chain = frame.Chain.Select(outer => $"'{outer.Callable.Phrase}'").Append($"'{callable.Phrase}'");
                diagnostics.Add(Error("UATC004", $"Step '{step.Text}' calls a phrase that is already running: {string.Join(" -> ", chain)}.", step.Source));
                return;
            }

            if ((frame?.Depth ?? 0) >= MaxCallDepth)
            {
                diagnostics.Add(Error("UATC005", $"Step '{step.Text}' calls '{callable.Phrase}' at depth {frame!.Depth + 1}; callable phrases nest at most {MaxCallDepth} deep. Call it from the scenario, or make one of them a C# phrase.", step.Source));
                return;
            }

            if (!CheckBlocks(step, prepared, diagnostics) || !CheckValues(step, prepared, rawArguments, app, diagnostics, out var rows))
            {
                return;
            }

            _apps.Use(app);
            foreach (var (row, rowValues) in rows)
            {
                var values = new Dictionary<string, string>(rawArguments, StringComparer.Ordinal);
                foreach (var (name, value) in rowValues)
                {
                    values[name] = value;
                }

                var callFrame = new UatCallFrame(callable, step, row, values, frame);
                var first = invocations.Count;
                List<UatDiagnostic> inner = [];
                foreach (var body in callable.Steps)
                {
                    var expanded = UatOutline.Substitute(body, values, quoteAware: true) with { Passes = UatPassedBlock.None };
                    expanded = body.Passes switch
                    {
                        UatPassedBlock.Table => expanded with { Table = step.Table },
                        UatPassedBlock.DocString => expanded with { DocString = step.DocString },
                        _ => expanded,
                    };
                    Step(expanded, fromBackground, callFrame, inner);
                }

                diagnostics.AddRange(inner.Select(diagnostic => Relocate(diagnostic, step, callFrame)));
                if (inner.Count == 0 && PageProblem(callable, invocations.Skip(first).ToList()) is { } pageProblem)
                {
                    diagnostics.Add(Error("UATC006", $"Step '{step.Text}': the phrase '{callable.Phrase}' from {callable.Source} {pageProblem}", callFrame.Location));
                }
            }
        }

        private static bool CheckBlocks(UatStep step, UatPreparedCallable prepared, List<UatDiagnostic> diagnostics)
        {
            var phrase = prepared.Callable.Phrase;
            string? problem = prepared.Callable.Kind == UatCallableKind.EachRow
                ? step.Table is null || step.Table.Rows.Count == 0 ? "UATA007" : step.DocString is not null ? "UATB004" : null
                : prepared.Passes switch
                {
                    UatPassedBlock.Table when step.Table is null => "UATA007",
                    UatPassedBlock.DocString when step.DocString is null => "UATA007",
                    not UatPassedBlock.Table when step.Table is not null => "UATB004",
                    not UatPassedBlock.DocString when step.DocString is not null => "UATB004",
                    _ => null,
                };

            if (problem is null)
            {
                return true;
            }

            var message = (problem, prepared.Callable.Kind, prepared.Passes) switch
            {
                ("UATA007", UatCallableKind.EachRow, _) => $"Step '{step.Text}' needs a table under it: '{phrase}' runs once per row, with the columns {string.Join(", ", prepared.Columns)}.",
                ("UATA007", _, UatPassedBlock.Table) => $"Step '{step.Text}' needs a table under it, which '{phrase}' passes on.",
                ("UATA007", _, _) => $"Step '{step.Text}' needs a doc string (\"\"\") under it, which '{phrase}' passes on.",
                _ => $"Step '{step.Text}' does not accept a {(step.Table is not null ? "table" : "doc string")}: '{phrase}' does not pass one on.",
            };
            diagnostics.Add(Error(problem, message, problem == "UATB004" && step.Table is not null ? step.Table.Source : step.Source));
            return false;
        }

        // Every value of the call is read as its type now, so a wrong one is reported once, at
        // the call's line or its row, and nothing is expanded.
        private bool CheckValues(
            UatStep step,
            UatPreparedCallable prepared,
            IReadOnlyDictionary<string, string> rawArguments,
            UatAppConfig? app,
            List<UatDiagnostic> diagnostics,
            out IReadOnlyList<(int? Row, IReadOnlyDictionary<string, string> Values)> rows)
        {
            var callable = prepared.Callable;
            var context = new UatValueContext(DateTimeOffset.UtcNow, catalogs.Apps, catalogs.CatalogFor(app).ArgumentTypes);
            var count = diagnostics.Count;
            void Check(string name, string text, UatSourceLocation location)
            {
                var type = prepared.Types[name];
                if (!UatValues.TryRead(text, type, context, out _, out var problem, allowNull: true))
                {
                    diagnostics.Add(Error(problem!.Code, $"Step '{step.Text}': '{name}' of '{callable.Phrase}' is a {UatValues.Describe(type)}; {problem.Message}", location) with
                    {
                        Fix = problem is { Fix: { } fix, Original: { } original } ? new UatTextFix(original, fix) : null,
                    });
                }
            }

            foreach (var name in callable.HeadingParameters)
            {
                if (rawArguments.TryGetValue(name, out var text))
                {
                    Check(name, text, step.Source);
                }
            }

            rows = [(null, new Dictionary<string, string>())];
            if (callable.Kind == UatCallableKind.EachRow && step.Table is { } table)
            {
                Dictionary<string, string> byColumn = new(StringComparer.Ordinal);
                foreach (var column in table.Columns)
                {
                    if (prepared.Columns.FirstOrDefault(name => UatTableBinding.Key(name) == UatTableBinding.Key(column)) is { } name)
                    {
                        byColumn[column] = name;
                    }
                    else
                    {
                        diagnostics.Add(Error("UATA006", $"Step '{step.Text}': '{column}' is not a column of '{callable.Phrase}'. Its columns are {string.Join(", ", prepared.Columns)}.", table.Source));
                    }
                }

                foreach (var name in prepared.Columns.Where(name => !byColumn.ContainsValue(name)))
                {
                    diagnostics.Add(Error("UATA006", $"Step '{step.Text}': the table needs a column '{name}' for '{callable.Phrase}'.", table.Source));
                }

                if (diagnostics.Count > count)
                {
                    return false;
                }

                List<(int?, IReadOnlyDictionary<string, string>)> perRow = [];
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    Dictionary<string, string> values = new(StringComparer.Ordinal);
                    foreach (var (column, name) in byColumn)
                    {
                        Check(name, row.Cells[column], row.Source);
                        values[name] = row.Cells[column];
                    }

                    perRow.Add((i + 1, values));
                }

                rows = perRow;
            }

            if (prepared.Passes == UatPassedBlock.Table && step.Table is { } passed && prepared.BlockType is { } blockType && blockType != typeof(UatTable))
            {
                diagnostics.AddRange(UatTableBinding.Check(passed, blockType, context).Select(found =>
                    Error(found.Problem.Code, $"Step '{step.Text}': the table '{callable.Phrase}' passes on: {found.Problem.Message}", found.Location) with
                    {
                        Fix = found.Problem is { Fix: { } fix, Original: { } original } ? new UatTextFix(original, fix) : null,
                    }));
            }

            return diagnostics.Count == count;
        }

        // A problem inside the call is reported at the call (or its row), and the steps it came
        // through are kept. A cell of the table the call passed on already has the call's line.
        private static UatDiagnostic Relocate(UatDiagnostic diagnostic, UatStep call, UatCallFrame frame)
        {
            if (call.Table is { } table && (diagnostic.Location == table.Source || table.Rows.Any(row => row.Source == diagnostic.Location)))
            {
                return diagnostic;
            }

            return diagnostic with
            {
                Location = frame.Location,
                Within = [diagnostic.Location, .. diagnostic.Within],
                Message = $"{diagnostic.Message} (in '{frame.Callable.Phrase}' at {diagnostic.Location})",
            };
        }

        // Without page tracking, only what the steps say is checked: a callable that names a page
        // ends on one, and a row ends on the page it started on.
        private static string? PageProblem(UatCallable callable, IReadOnlyList<UatStepInvocation> bound)
        {
            var pages = bound.Where(invocation => invocation.Command.NamesPage).ToList();
            if (pages.Count == 0 || bound.Count == 0)
            {
                return null;
            }

            if (!bound[^1].Command.NamesPage)
            {
                return $"names the {Page(pages[^1])} page but does not end on one. End it with a step that says where it ends: 'Then I should be on the ... page'.";
            }

            return callable.Kind == UatCallableKind.EachRow && !string.Equals(Page(pages[0]), Page(bound[^1]), StringComparison.OrdinalIgnoreCase)
                ? $"runs once per row, so each row ends on the page it started on; this one starts on the {Page(pages[0])} page and ends on the {Page(bound[^1])} page."
                : null;
        }

        private static string Page(UatStepInvocation invocation)
            => invocation.Arguments.TryGetValue(PageParameter, out var page) ? page : "?";
    }

    // The page an app is on as binding walks a scenario; null before any step has said it. With
    // LostAt, only a guess: the step there may have left it.
    private readonly record struct PageState(string? Page, UatStep? LostAt);

    // The current app as binding walks a scenario, and which app of each user was used last.
    private sealed class AppTracker(UatCatalogSet catalogs)
    {
        private readonly Dictionary<string, UatAppConfig> _lastUsed = new(StringComparer.OrdinalIgnoreCase);

        public UatAppConfig? Current { get; private set; }

        public void Use(UatAppConfig? app)
        {
            Current = app;
            if (app is not null)
            {
                _lastUsed[app.User] = app;
            }
        }

        // An app name; a user with one app; a user with several: the one used last (AD-015).
        public bool TryResolve(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out UatAppConfig? app, out string error)
        {
            error = string.Empty;
            app = catalogs.FindApp(name);
            if (app is not null)
            {
                return true;
            }

            var owned = catalogs.AppsOf(name);
            if (owned.Count == 1)
            {
                app = owned[0];
                return true;
            }

            if (owned.Count > 1 && _lastUsed.TryGetValue(name, out app))
            {
                return true;
            }

            error = owned.Count > 1
                ? $"{name} has several apps ({string.Join(", ", owned.Select(candidate => candidate.Name))}) and none has been used yet in this scenario. Name the app, or start with '{owned[0].Name} uses the app'."
                : $"no app or user '{name}' in the Apps table. It has: {catalogs.DescribeApps()}.";
            return false;
        }
    }
}

public sealed class UatBindResult
{
    public UatBindResult(UatBoundDocument? document, IReadOnlyList<UatDiagnostic> diagnostics)
    {
        Document = document;
        Diagnostics = diagnostics;
    }

    public UatBoundDocument? Document { get; }

    public IReadOnlyList<UatDiagnostic> Diagnostics { get; }

    public bool Success => Document is not null && Diagnostics.All(x => x.Severity != UatDiagnosticSeverity.Error);
}

public sealed record UatBoundDocument(
    UatDocument Source,
    IReadOnlyList<UatBoundScenario> Scenarios);

public sealed record UatBoundScenario(
    UatScenario Source,
    IReadOnlyList<UatStepInvocation> Invocations);

public sealed record UatStepInvocation(
    string ScenarioName,
    UatStep Step,
    UatCommandPattern Command,
    IReadOnlyDictionary<string, string> Arguments,
    UatTable? Table,
    bool FromBackground)
{
    public string MatchedPattern => Command.Phrase;

    public string CommandId => Command.CommandId;

    /// <summary>The app the step runs on, from the <c>Apps</c> table; null for the fixture's own.</summary>
    public string? App { get; init; }

    /// <summary>The arguments as written, quotes kept, which typed parameters are read from.</summary>
    public IReadOnlyDictionary<string, string> RawArguments { get; init; } = Arguments;

    /// <summary>The call that put this step here, for a step of a callable phrase; null for a step written in the scenario.</summary>
    public UatCallFrame? Call { get; init; }

    /// <summary>
    /// The page the binder knows is current after this control verb or page phrase; null when the
    /// step is not page-scoped or may have left the page. The runtime follows it.
    /// </summary>
    public string? PageAfter { get; init; }
}
