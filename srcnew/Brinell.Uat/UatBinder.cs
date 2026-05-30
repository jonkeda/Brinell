namespace Brinell.Uat;

public static class UatBinder
{
    public static UatBindResult Bind(UatDocument document, UatCommandCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(catalog);

        List<UatDiagnostic> diagnostics = [];
        List<UatBoundScenario> scenarios = [];

        foreach (var scenario in document.Scenarios)
        {
            List<UatStepInvocation> invocations = [];

            foreach (var step in document.Background)
            {
                BindStep(catalog, scenario, step, fromBackground: true, invocations, diagnostics);
            }

            foreach (var step in scenario.Steps)
            {
                BindStep(catalog, scenario, step, fromBackground: false, invocations, diagnostics);
            }

            scenarios.Add(new UatBoundScenario(scenario, invocations));
        }

        var boundDocument = diagnostics.Any(x => x.Severity == UatDiagnosticSeverity.Error)
            ? null
            : new UatBoundDocument(document, scenarios);

        return new UatBindResult(boundDocument, diagnostics);
    }

    private static void BindStep(
        UatCommandCatalog catalog,
        UatScenario scenario,
        UatStep step,
        bool fromBackground,
        ICollection<UatStepInvocation> invocations,
        ICollection<UatDiagnostic> diagnostics)
    {
        var matches = catalog.Match(step);
        if (matches.Count == 0)
        {
            diagnostics.Add(new UatDiagnostic(
                UatDiagnosticSeverity.Error,
                "UATB001",
                $"No command binding matches step '{step.Text}'.",
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

        var match = matches[0];
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

        invocations.Add(new UatStepInvocation(
            scenario.Name,
            step,
            match.Pattern,
            match.Arguments,
            step.Table,
            fromBackground));
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
}
