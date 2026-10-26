using System.Globalization;
using System.Text;
using Bruinet.Core.Artifacts;

namespace Bruinet.Core.Calls;

/// <summary>
/// The run's near-misses in one place: every call that passed only after trouble, kept for the
/// process and written as <c>near-misses.md</c> in the suite's artifact folder, worst first, each
/// time one is added. On without a switch: a run with none writes nothing.
/// </summary>
public static class NearMisses
{
    /// <summary>The summary's file name in the suite's folder.</summary>
    public const string FileName = "near-misses.md";

    private static readonly object Gate = new();
    private static readonly List<NearMiss> Seen = [];
    private static string? _path;

    /// <summary>One call that passed after trouble.</summary>
    /// <param name="Test">The test context's name.</param>
    /// <param name="Page">The scope the call stood in.</param>
    /// <param name="Control">The control.</param>
    /// <param name="Call">The public member.</param>
    /// <param name="ElapsedMs">How long the call took.</param>
    /// <param name="BudgetMs">Its budget.</param>
    /// <param name="Detail">What its attempts saw.</param>
    public sealed record NearMiss(string Test, string Page, string Control, string Call, int ElapsedMs, int BudgetMs, string Detail)
    {
        /// <summary>How much of its budget the call used.</summary>
        public double Share => BudgetMs > 0 ? (double)ElapsedMs / BudgetMs : 0;
    }

    /// <summary>The near-misses this process has seen, worst first.</summary>
    public static IReadOnlyList<NearMiss> All
    {
        get
        {
            lock (Gate)
            {
                return [.. Seen.OrderByDescending(miss => miss.Share)];
            }
        }
    }

    /// <summary>Where the summary goes; the suite's artifact folder unless set (tests).</summary>
    public static string Path
    {
        get => _path ??= System.IO.Path.Combine(DefaultTestArtifactPathProvider.Create().SuiteDirectory, FileName);
        set => _path = value;
    }

    /// <summary>Adds <paramref name="miss"/> and writes the summary again. Never fails the call.</summary>
    public static void Add(NearMiss miss)
    {
        ArgumentNullException.ThrowIfNull(miss);
        lock (Gate)
        {
            Seen.Add(miss);
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                File.WriteAllText(Path, Render(Seen.OrderByDescending(m => m.Share)));
            }
            catch (IOException)
            {
                // A summary that cannot be written is no reason to fail a test.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>Forgets what was seen (tests).</summary>
    internal static void Clear()
    {
        lock (Gate)
        {
            Seen.Clear();
        }
    }

    private static string Render(IEnumerable<NearMiss> misses)
    {
        var text = new StringBuilder()
            .AppendLine("# Near-misses")
            .AppendLine()
            .AppendLine("Calls that passed after using more than their budget's share or after several replacements, worst first.")
            .AppendLine()
            .AppendLine("| Share | Call | Page | Test | Took | What its attempts saw |")
            .AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var miss in misses)
        {
            text.Append(CultureInfo.InvariantCulture,
                $"| {miss.Share:P0} | `{Cell(miss.Control)}.{Cell(miss.Call)}` | {Cell(miss.Page)} | {Cell(miss.Test)} | {miss.ElapsedMs} of {miss.BudgetMs} ms | {Cell(miss.Detail)} |")
                .AppendLine();
        }

        return text.ToString();
    }

    private static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal).Replace('\n', ' ');
}
