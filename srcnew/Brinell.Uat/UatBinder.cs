using System.Text.RegularExpressions;

namespace Brinell.Uat;

public static class UatBinder
{
    private const string UserParameter = "user";

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
        ArgumentNullException.ThrowIfNull(catalogs);

        List<UatDiagnostic> diagnostics = [.. catalogs.CheckNames(document.Source)];
        List<UatBoundScenario> scenarios = [];

        foreach (var scenario in document.Scenarios)
        {
            List<UatStepInvocation> invocations = [];
            var apps = new AppTracker(catalogs);

            foreach (var step in document.Background)
            {
                BindStep(catalogs, apps, scenario, step, fromBackground: true, invocations, diagnostics);
            }

            foreach (var step in scenario.Steps)
            {
                BindStep(catalogs, apps, scenario, step, fromBackground: false, invocations, diagnostics);
            }

            scenarios.Add(new UatBoundScenario(scenario, invocations));
        }

        var boundDocument = diagnostics.Any(x => x.Severity == UatDiagnosticSeverity.Error)
            ? null
            : new UatBoundDocument(document, scenarios);

        return new UatBindResult(boundDocument, diagnostics);
    }

    private static void BindStep(
        UatCatalogSet catalogs,
        AppTracker apps,
        UatScenario scenario,
        UatStep step,
        bool fromBackground,
        ICollection<UatStepInvocation> invocations,
        ICollection<UatDiagnostic> diagnostics)
    {
        var app = apps.Current;
        UatCommandMatch? match = null;
        IReadOnlyList<string> tried = [];

        if (catalogs.TryFindSubject(step.Text, out var subject, out var rest))
        {
            if (!apps.TryResolve(subject, out var subjectApp, out var error))
            {
                diagnostics.Add(new UatDiagnostic(UatDiagnosticSeverity.Error, "UATB006", error, step.Source));
                return;
            }

            var catalog = catalogs.CatalogFor(subjectApp);

            // As written, with the subject filling the phrase's first parameter: {app} uses the app.
            var asWritten = catalog.Match(step)
                .Where(candidate => FirstArgument(candidate) == subject)
                .ToArray();
            if (asWritten.Length == 1)
            {
                match = asWritten[0];
                app = subjectApp;
            }
            else
            {
                // In the first person: "Daan taps OK" is "I tap OK" on Daan's app.
                tried = UatSubjects.FirstPersonCandidates(rest);
                var firstPerson = tried
                    .SelectMany(text => catalog.Match(step with { Text = text }))
                    .Where(candidate => candidate.Pattern.Phrase.StartsWith("I ", StringComparison.Ordinal))
                    .DistinctBy(candidate => candidate.Pattern)
                    .ToArray();
                if (firstPerson.Length > 1)
                {
                    diagnostics.Add(new UatDiagnostic(
                        UatDiagnosticSeverity.Error,
                        "UATB002",
                        $"Step '{step.Text}' is ambiguous: as {subject}, it matches {string.Join(", ", firstPerson.Select(candidate => $"'{candidate.Pattern.Phrase}'"))}.",
                        step.Source));
                    return;
                }

                if (firstPerson.Length == 1)
                {
                    match = firstPerson[0];
                    app = subjectApp;
                }
            }
        }

        if (match is null)
        {
            var matches = catalogs.CatalogFor(app).Match(step);
            if (matches.Count == 0)
            {
                diagnostics.Add(new UatDiagnostic(
                    UatDiagnosticSeverity.Error,
                    "UATB001",
                    tried.Count == 0
                        ? $"No command binding matches step '{step.Text}'."
                        : $"No command binding matches step '{step.Text}'. As {subject}, tried {string.Join(", ", tried.Select(text => $"'{text}'"))}.",
                    step.Source));
                return;
            }

            if (matches.Count > 1)
            {
                diagnostics.Add(new UatDiagnostic(
                    UatDiagnosticSeverity.Error,
                    "UATB002",
                    $"Step '{step.Text}' is ambiguous and matches {matches.Count} command bindings.",
                    step.Source));
                return;
            }

            match = matches[0];
        }

        if (match.Pattern.RequiresTable && step.Table is null)
        {
            diagnostics.Add(new UatDiagnostic(
                UatDiagnosticSeverity.Error,
                "UATB003",
                $"Step '{step.Text}' requires a table.",
                step.Source));
            return;
        }

        if (!match.Pattern.AllowsTable && step.Table is not null)
        {
            diagnostics.Add(new UatDiagnostic(
                UatDiagnosticSeverity.Error,
                "UATB004",
                $"Step '{step.Text}' does not accept a table.",
                step.Table.Source));
            return;
        }

        var arguments = match.Arguments;
        if (catalogs.Apps.Count > 0 && arguments.TryGetValue(UserParameter, out var user))
        {
            if (!TryBindUser(catalogs, apps, match, user, ref app, out var boundUser, out var error))
            {
                diagnostics.Add(new UatDiagnostic(UatDiagnosticSeverity.Error, "UATB007", $"Step '{step.Text}': {error}", step.Source));
                return;
            }

            arguments = new Dictionary<string, string>(arguments, StringComparer.Ordinal) { [UserParameter] = boundUser };
        }

        apps.Use(app);
        invocations.Add(new UatStepInvocation(
            scenario.Name,
            step,
            match.Pattern,
            arguments,
            step.Table,
            fromBackground)
        {
            App = app?.Name,
        });
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

    private static string? FirstArgument(UatCommandMatch match)
        => FirstParameterName(match.Pattern) is { } name && match.Arguments.TryGetValue(name, out var value) ? value : null;

    private static string? FirstParameterName(UatCommandPattern pattern)
        => Regex.Match(pattern.Phrase, @"^\{([A-Za-z][A-Za-z0-9_-]*)\}") is { Success: true } found ? found.Groups[1].Value : null;

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
}
