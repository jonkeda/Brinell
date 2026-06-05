namespace Brinell.Uat;

public static class UatDiagnosticsFormatter
{
    public static string FormatDiagnostics(IEnumerable<UatDiagnostic> diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.Select(x => $"{x.Location}: {x.Code} {x.Message}"));
    }

    public static string FormatBindFailure(
        IEnumerable<UatDiagnostic> diagnostics,
        string discoveryReport,
        UatCommandCatalog catalog)
    {
        return string.Join(
            Environment.NewLine,
            [
                FormatDiagnostics(diagnostics),
                discoveryReport,
                FormatCatalog(catalog)
            ]);
    }

    public static string FormatResults(
        UatScenarioRunResult result,
        UatExecutionContext context,
        string discoveryReport,
        UatCommandCatalog catalog,
        string? evidencePath = null)
    {
        List<string> lines =
        [
            .. result.Steps.Select(x =>
                $"{x.Status}: {x.Invocation.Step.Source}: {x.Invocation.CommandId}: {x.Invocation.Step.Text} {x.Message}"),
            "Runtime trace:"
        ];

        if (result.Skipped && result.SkipDecision?.Reason is not null)
        {
            lines.Add($"Skipped: {result.SkipDecision.Reason}");
        }

        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            lines.Add($"Evidence: {evidencePath}");
        }

        lines.AddRange(context.Diagnostics);
        lines.Add(discoveryReport);
        lines.Add(FormatCatalog(catalog));
        return string.Join(Environment.NewLine, lines);
    }

    public static string FormatCatalog(UatCommandCatalog catalog)
    {
        return "Command catalog:" + Environment.NewLine + string.Join(
            Environment.NewLine,
            catalog.Patterns
                .OrderBy(pattern => pattern.Keyword)
                .ThenBy(pattern => pattern.Phrase, StringComparer.Ordinal)
                .Select(pattern => $"- {pattern.Keyword}: {pattern.Phrase} -> {pattern.CommandId}"));
    }
}
