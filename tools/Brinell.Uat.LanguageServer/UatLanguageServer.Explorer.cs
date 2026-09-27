using Brinell.Uat.Preview;
using Brinell.Uat.Workspace;
using StreamJsonRpc;

namespace Brinell.Uat.LanguageServer;

// The UAT Explorer (phase 4): a suite's steps and hooks, its pages, and its last results.
public sealed partial class UatLanguageServer
{
    /// <summary><c>brinell/explorerHost</c>: the page the explorer's webview loads once; <c>brinell/explorer</c> fills it.</summary>
    [JsonRpcMethod("brinell/explorerHost")]
    public PreviewHostResult ExplorerHost() => new(UatExplorerWriter.Page);

    /// <summary><c>brinell/explorer</c>: one view of a suite, the one picked or the first.</summary>
    [JsonRpcMethod("brinell/explorer")]
    public ExplorerResult Explorer(ExplorerParams request)
    {
        IReadOnlyList<UatSuite> suites;
        lock (_gate)
        {
            suites = _workspace?.Suites.OrderBy(suite => suite.Name, StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        }

        var view = request.View is UatExplorerWriter.Pages or UatExplorerWriter.Results ? request.View : UatExplorerWriter.Steps;
        var suite = suites.FirstOrDefault(candidate => string.Equals(candidate.Name, request.Suite, StringComparison.OrdinalIgnoreCase)) ?? suites.FirstOrDefault();
        var names = suites.Select(candidate => candidate.Name).ToList();
        if (suite is null)
        {
            return new ExplorerResult(view, "<p class=\"uat-banner\">No suite in this solution: no project has a uat.config.md or bdd.config.md.</p>", names, null);
        }

        if (view == UatExplorerWriter.Results)
        {
            return new ExplorerResult(view, UatExplorerWriter.ResultsView(suite, RunsOfSuite(suite)), names, suite.Name);
        }

        if (suite.TryGetCatalogs(out var reason) is not { } catalogs)
        {
            return new ExplorerResult(view, $"<p class=\"uat-banner\" data-mode=\"parse-only\">{System.Net.WebUtility.HtmlEncode(reason ?? "Not built.")}</p>", names, suite.Name);
        }

        var html = view == UatExplorerWriter.Pages
            ? UatExplorerWriter.PagesView(suite, catalogs)
            : UatExplorerWriter.StepsView(suite, catalogs, [.. suite.StepFiles.SelectMany(path => (UatScenarioFiles.ParsePhraseFile(path).Document?.Callables ?? []).Select(callable => callable with { Source = callable.Source with { FilePath = callable.Source.FilePath ?? path } }))]);
        return new ExplorerResult(view, html, names, suite.Name);
    }

    // The newest run of each scenario of the suite's feature files, with the scenario's heading line.
    private List<(string File, UatScenarioRun Run, int Line)> RunsOfSuite(UatSuite suite)
    {
        List<(string, UatScenarioRun, int)> runs = [];
        if (_results is not { } results)
        {
            return runs;
        }

        foreach (var path in suite.FeatureFiles.Concat(suite.LinkedFiles).Where(path => path.EndsWith(UatFileFormat.FeatureExtension, StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            IReadOnlyDictionary<string, UatScenarioRun> found;
            lock (results)
            {
                found = results.For(suite.Name, path);
            }

            if (found.Count == 0)
            {
                continue;
            }

            var scenarios = UatMarkdownParser.Parse(File.ReadAllText(path), path).Document?.Scenarios ?? [];
            foreach (var run in found.Values)
            {
                var line = scenarios.FirstOrDefault(scenario => scenario.Name == run.Scenario)?.Source.LineNumber ?? 1;
                runs.Add((Path.GetFullPath(path), run, line));
            }
        }

        return runs;
    }
}
