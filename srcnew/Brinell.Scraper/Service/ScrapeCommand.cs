using System.Globalization;
using System.Text.Json;
using Brinell.Core.Scraping;
using Brinell.Scraper.Explore;

namespace Brinell.Scraper.Service;

/// <summary>
/// The Scraper from a command line (the language server's <c>scrape</c> mode), for a person, the loop and CI.
/// Exit code 0 when it ran within its guards, 1 when a guard stopped it, 2 for an error or when not started.
/// </summary>
public static class ScrapeCommand
{
    /// <summary>How to call it.</summary>
    public const string Usage =
        "usage: scrape explore --project <name> [--solution <folder>] [--url <start>] [--dry-run] [--max-pages N] [--max-depth N] [--max-minutes N] [--delay-ms N] [--headless] [--yes] [--json]";

    /// <summary>Runs one command; <paramref name="input"/> answers the question before a run, and a sign-in when the browser shows.</summary>
    public static async Task<int> RunAsync(string[] args, TextReader input, TextWriter output, TextWriter errors, Func<string, IScrapeSource>? sourceFor = null, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0 || args[0] != "explore")
        {
            await errors.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }
        try
        {
            return await ExploreAsync(args[1..], input, output, errors, sourceFor, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is ScraperProblem or FormatException or FileNotFoundException or ArgumentException or InvalidOperationException)
        {
            await errors.WriteLineAsync(e.Message).ConfigureAwait(false);
            return 2;
        }
    }

    private static async Task<int> ExploreAsync(string[] args, TextReader input, TextWriter output, TextWriter errors, Func<string, IScrapeSource>? sourceFor, CancellationToken cancellationToken)
    {
        var yes = args.Contains("--yes");
        var headless = args.Contains("--headless");
        var options = new ExploreOptions
        {
            DryRun = args.Contains("--dry-run"),
            Url = Option(args, "--url"),
            MaxPages = Number(args, "--max-pages"),
            MaxDepth = Number(args, "--max-depth"),
            MaxMinutes = Number(args, "--max-minutes"),
            DelayMs = Number(args, "--delay-ms"),
            Visible = !headless,
            // Unattended, or with no window to sign in in, a sign-in page ends the run.
            StopAtSignIn = yes || headless,
            WaitForSignIn = yes || headless ? null : async ct =>
            {
                await errors.WriteLineAsync("A sign-in page: sign in in the browser window, then press Enter here.").ConfigureAwait(false);
                await input.ReadLineAsync(ct).ConfigureAwait(false);
            },
            Progress = new Lines(errors),
        };

        await using var service = new ScraperService(Option(args, "--solution") ?? Environment.CurrentDirectory, sourceFor);
        var project = Option(args, "--project");
        await errors.WriteLineAsync(service.ExploreSummary(project, options)).ConfigureAwait(false);
        if (!yes)
        {
            await errors.WriteAsync("Go on? [y/N] ").ConfigureAwait(false);
            var answer = (await input.ReadLineAsync(cancellationToken).ConfigureAwait(false))?.Trim();
            if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) && !string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase))
            {
                await errors.WriteLineAsync("Not started.").ConfigureAwait(false);
                return 2;
            }
        }

        var result = await service.ExploreAsync(project, options, cancellationToken).ConfigureAwait(false);
        if (args.Contains("--json"))
        {
            await output.WriteLineAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true })).ConfigureAwait(false);
        }
        else
        {
            await output.WriteLineAsync($"{result.Stop}: {result.StopDetail}").ConfigureAwait(false);
            await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"{result.Captures.Count} captures, {result.Clicks} clicks, {result.Skipped.Count} skipped, {result.Blocked.Count} blocked in {result.Took.TotalSeconds:0.0} s")).ConfigureAwait(false);
            if (result.Report is not null) await output.WriteLineAsync($"Report: {result.Report}").ConfigureAwait(false);
        }
        return result.WithinGuards ? 0 : 1;
    }

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static int? Number(string[] args, string name) =>
        Option(args, name) is not { } value
            ? null
            : int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0 ? n : throw new ArgumentException($"{name} takes a whole number above 0, not '{value}'.");

    // Progress as a line each, as it happens.
    private sealed class Lines(TextWriter errors) : IProgress<ExploreProgress>
    {
        public void Report(ExploreProgress p) =>
            errors.WriteLine(string.Create(CultureInfo.InvariantCulture, $"[{p.Captures} captured, {p.Clicks} clicks, {p.Skipped} skipped, {p.Blocked} blocked] {p.Doing}"));
    }
}
