namespace Brinell.Bdd.Preview;

// What the binder made of the file, by the line each step is written on: for the scenario (or
// Background) being drawn, each written step's invocations, the page it runs on, and a call's body.
internal sealed class BddPreviewBinding
{
    private const string PageParameter = "page";

    private readonly Dictionary<int, List<BddBoundScenario>> _scenarios = [];
    private BddCatalogSet? _catalogs;
    private Dictionary<int, BddPreviewStep> _steps = [];
    private int? _scenarioLine;

    public void Add(IReadOnlyList<BddBoundScenario> scenarios, BddCatalogSet catalogs)
    {
        _catalogs = catalogs;
        foreach (var scenario in scenarios)
        {
            var line = scenario.Source.Source.LineNumber;
            if (!_scenarios.TryGetValue(line, out var rows))
            {
                _scenarios[line] = rows = [];
            }

            rows.Add(scenario);
        }
    }

    // A scenario heading: its steps are the first row's (an outline's first Examples row).
    public void Enter(int scenarioLine)
    {
        _scenarioLine = scenarioLine;
        _steps = _scenarios.TryGetValue(scenarioLine, out var rows) && rows.Count > 0
            ? Steps(rows[0].Invocations.Where(invocation => !invocation.FromBackground))
            : [];
    }

    // The Background: its steps as the first scenario that runs them bound them.
    public void EnterBackground()
    {
        _scenarioLine = null;
        var first = _scenarios.Values.SelectMany(rows => rows).FirstOrDefault(scenario => scenario.Invocations.Any(invocation => invocation.FromBackground));
        _steps = first is null ? [] : Steps(first.Invocations.Where(invocation => invocation.FromBackground));
    }

    public void Leave()
    {
        _scenarioLine = null;
        _steps = [];
    }

    public BddPreviewStep? At(int line) => _steps.GetValueOrDefault(line);

    public IReadOnlyList<BddPreviewRow> RowsOf(int fenceLine)
    {
        if (_scenarioLine is not { } line || !_scenarios.TryGetValue(line, out var rows) || rows.Count < 2 || rows[0].Source.ExampleIndex is null)
        {
            return [];
        }

        return rows.Select(row => new BddPreviewRow(
                row.Source.ExampleIndex ?? 0,
                row.Source.Name,
                row.Invocations.Where(invocation => !invocation.FromBackground).Select(invocation => Flat(invocation)).ToList()))
            .ToList();
    }

    // A scenario for another head is not bound (AD-014): say why, as the runner would skip it.
    public string? WhyNotOnHead(int scenarioLine)
    {
        if (_catalogs?.SuiteHead is not { } head || !_scenarios.TryGetValue(scenarioLine, out var rows) || rows.Count == 0 || rows[0].Invocations.Count > 0)
        {
            return null;
        }

        var tags = rows[0].Source.Tags.Select(BddTagConventions.NormalizeTag).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return BddTagConventions.WhyNotOnHead(tags, head);
    }

    private static Dictionary<int, BddPreviewStep> Steps(IEnumerable<BddStepInvocation> invocations)
    {
        Dictionary<int, BddPreviewStep> steps = [];
        Dictionary<string, string?> current = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string?> shown = new(StringComparer.OrdinalIgnoreCase);
        foreach (var group in invocations.GroupBy(WrittenLine))
        {
            var list = group.ToList();
            var first = list[0];
            var app = first.App ?? string.Empty;
            var named = first.Command.NamesPage ? first.Arguments.GetValueOrDefault(PageParameter) : null;
            var runsOn = named ?? first.Command.Page ?? (first.Command.ControlVerb is not null ? current.GetValueOrDefault(app) : null);

            // A page is compared and shown by its name; the binder may qualify it by namespace (Users/User List).
            string? pageShown = null;
            if (named is not null)
            {
                shown[app] = Name(named);
            }
            else if (runsOn is not null && !string.Equals(Name(runsOn), shown.GetValueOrDefault(app), StringComparison.Ordinal))
            {
                pageShown = first.App is { } name ? $"{name} · {Name(runsOn)}" : Name(runsOn);
                shown[app] = Name(runsOn);
            }

            foreach (var invocation in list)
            {
                var key = invocation.App ?? string.Empty;
                var namedHere = invocation.Command.NamesPage ? invocation.Arguments.GetValueOrDefault(PageParameter) : null;
                current[key] = invocation.PageAfter
                    ?? namedHere
                    ?? (invocation.Command.ControlVerb?.Navigates == true ? null : current.GetValueOrDefault(key));
            }

            steps[group.Key] = new BddPreviewStep(Describe(list, runsOn), pageShown, first.Call is null ? null : Call(list));
        }

        return steps;
    }

    // The line a step is written on in this file: its own, or the outermost call's for a callable's step.
    private static int WrittenLine(BddStepInvocation invocation)
        => invocation.Call is { } frame ? Outermost(frame).Call.Source.LineNumber : invocation.Step.Source.LineNumber;

    private static BddCallFrame Outermost(BddCallFrame frame)
    {
        while (frame.Parent is { } parent)
        {
            frame = parent;
        }

        return frame;
    }

    private static string Describe(IReadOnlyList<BddStepInvocation> invocations, string? runsOn)
    {
        var first = invocations[0];
        var on = first.App is { } app ? $" · on {app}" : string.Empty;
        return first.Call is { } frame
            ? $"calls {Outermost(frame).Callable}{on}"
            : Describe(first, first.Command.ControlVerb is not null ? runsOn : null);
    }

    // The method (or built-in) and, for a step on a page, the page.
    private static string Describe(BddStepInvocation invocation, string? page = null)
        => $"binds to {invocation.CommandId}"
           + (invocation.Scope.Length > 0 ? $" · {invocation.Scope}" : page is not null ? $" · page {Name(page)}" : string.Empty)
           + (invocation.App is { } app ? $" · on {app}" : string.Empty);

    private static string Name(string page) => page[(page.LastIndexOf('/') + 1)..];

    private static BddPreviewCall Call(IReadOnlyList<BddStepInvocation> invocations)
        => new(Outermost(invocations[0].Call!).Callable.ToString(), invocations.Select(invocation => Flat(invocation)).ToList());

    private static BddPreviewFlatStep Flat(BddStepInvocation invocation)
        => new(
            invocation.Step.Keyword.ToString(),
            invocation.Step.Text,
            BddPreviewWriter.Where(invocation.Step.Source),
            Describe(invocation),
            invocation.Call?.Depth ?? 0,
            invocation.Call is { } frame ? Outermost(frame).Row : null);
}

// A written step: what it binds to, the page shown when it changes, and a call's body.
internal sealed record BddPreviewStep(string Binds, string? PageShown, BddPreviewCall? Call)
{
    public string Describe() => Binds;
}

internal sealed record BddPreviewCall(string Heading, IReadOnlyList<BddPreviewFlatStep> Steps);

// A step as it runs: the text with values filled in, where it is written, and what it binds to.
internal sealed record BddPreviewFlatStep(string Keyword, string Text, string Source, string Binds, int Depth, int? Row);

internal sealed record BddPreviewRow(int Index, string Name, IReadOnlyList<BddPreviewFlatStep> Steps);
