using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Bruinet.Narrator;

/// <summary>
/// The page an agent hands over at the end of its work: what it changed, and each recorded video
/// with its captions, a chapter list that seeks the video, and every step's result. One HTML file
/// beside the videos, nothing loaded from elsewhere.
/// </summary>
public static partial class BddReviewPage
{
    /// <summary>Writes the page, and a <c>.vtt</c> beside each video that has an <c>.srt</c> (a browser plays WebVTT).</summary>
    /// <param name="path">The page to write.</param>
    /// <param name="title">What the work was.</param>
    /// <param name="summary">A paragraph or two on what was done; null for none.</param>
    /// <param name="changes">What changed, a line each: files, behaviour.</param>
    /// <param name="videos">The recorded videos (<c>.mp4</c>), each with its <c>.timeline.json</c> and <c>.srt</c> beside it.</param>
    public static void Write(string path, string title, string? summary, IReadOnlyList<string> changes, IReadOnlyList<string> videos)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(videos);
        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var html = new StringBuilder();
        html.Append(CultureInfo.InvariantCulture, $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="color-scheme" content="light dark">
            <title>{Encode(title)}</title>
            <style>{Style}</style>
            </head>
            <body>
            <main>
            <h1>{Encode(title)}</h1>
            <p class="when">Review, {DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}</p>

            """);
        if (summary is { Length: > 0 })
        {
            foreach (var paragraph in summary.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries))
            {
                html.Append("<p>").Append(Encode(paragraph.Trim())).Append("</p>\n");
            }
        }

        if (changes.Count > 0)
        {
            html.Append("<h2>What changed</h2>\n<ul class=\"changes\">\n");
            foreach (var change in changes)
            {
                html.Append("<li>").Append(Code(Encode(change))).Append("</li>\n");
            }

            html.Append("</ul>\n");
        }

        var number = 0;
        foreach (var video in videos)
        {
            number++;
            WriteVideo(html, folder, Path.GetFullPath(video), number);
        }

        html.Append("</main>\n<script>").Append(Script).Append("</script>\n</body>\n</html>\n");
        File.WriteAllText(path, html.ToString());
    }

    /// <summary>WebVTT from SubRip: the same cues, a header and dots in the times.</summary>
    /// <param name="srt">The <c>.srt</c> text.</param>
    public static string ToWebVtt(string srt)
    {
        ArgumentNullException.ThrowIfNull(srt);
        return "WEBVTT\n\n" + SrtTime().Replace(srt.Replace("\r\n", "\n", StringComparison.Ordinal), "$1.$2");
    }

    private static void WriteVideo(StringBuilder html, string folder, string video, int number)
    {
        var timelinePath = Path.ChangeExtension(video, ".timeline.json");
        var timeline = File.Exists(timelinePath) ? BddVideoTimeline.Load(timelinePath) : null;
        var failed = timeline?.Scenes.Any(scene => scene.Status == BddStepResultStatus.Failed) == true;
        var id = $"video-{number}";
        html.Append(CultureInfo.InvariantCulture, $"<section class=\"video\">\n<h2>{Encode(timeline?.Title ?? Path.GetFileNameWithoutExtension(video))} <span class=\"badge\" data-status=\"{(failed ? "failed" : "passed")}\">{(failed ? "failed" : "passed")}</span></h2>\n");
        html.Append(CultureInfo.InvariantCulture, $"<video id=\"{id}\" controls preload=\"metadata\" src=\"{Encode(Link(folder, video))}\">");
        var srt = Path.ChangeExtension(video, ".srt");
        if (File.Exists(srt))
        {
            var vtt = Path.ChangeExtension(video, ".vtt");
            File.WriteAllText(vtt, ToWebVtt(File.ReadAllText(srt)));
            html.Append(CultureInfo.InvariantCulture, $"<track kind=\"captions\" srclang=\"en\" label=\"Captions\" default src=\"{Encode(Link(folder, vtt))}\">");
        }

        html.Append("</video>\n");
        if (timeline is null)
        {
            html.Append("<p class=\"when\">No timeline beside the video: no chapters or step results.</p>\n</section>\n");
            return;
        }

        foreach (var scene in timeline.Scenes)
        {
            var start = scene.Steps.Count > 0 ? scene.Steps.Min(step => step.Start) : scene.Clips.Count > 0 ? scene.Clips.Min(clip => clip.Start) : 0;
            html.Append(CultureInfo.InvariantCulture, $"<details class=\"scene\" open><summary><a href=\"#\" data-seek=\"{id}\" data-at=\"{start.ToString("0.###", CultureInfo.InvariantCulture)}\">{Time(start)}</a> {Encode(scene.Name)} <span class=\"badge\" data-status=\"{Status(scene.Status)}\">{Status(scene.Status)}</span></summary>\n");
            if (scene.Scenario is { } scenario)
            {
                html.Append("<p class=\"when\">").Append(Encode(scenario)).Append("</p>\n");
            }

            html.Append("<ol class=\"steps\">\n");
            foreach (var step in scene.Steps)
            {
                html.Append(CultureInfo.InvariantCulture, $"<li data-status=\"{Status(step.Status)}\"><a href=\"#\" data-seek=\"{id}\" data-at=\"{step.Start.ToString("0.###", CultureInfo.InvariantCulture)}\">{Time(step.Start)}</a> {Encode(step.Text)}");
                if (step.Message is { } message)
                {
                    html.Append("<div class=\"message\">").Append(Encode(message)).Append("</div>");
                }

                html.Append("</li>\n");
            }

            html.Append("</ol>\n</details>\n");
        }

        html.Append("</section>\n");
    }

    // A video beside the page by its name; elsewhere by its file URL.
    private static string Link(string folder, string file)
        => string.Equals(Path.GetDirectoryName(file), folder, StringComparison.OrdinalIgnoreCase)
            ? Uri.EscapeDataString(Path.GetFileName(file))
            : new Uri(file).AbsoluteUri;

    private static string Status(BddStepResultStatus status) => status.ToString().ToLowerInvariant();

    private static string Time(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture) : time.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    // `code` in a change line, as an agent writes a file or a member.
    private static string Code(string encoded) => BackTicks().Replace(encoded, "<code>$1</code>");

    [GeneratedRegex(@"(\d\d:\d\d:\d\d),(\d\d\d)")]
    private static partial Regex SrtTime();

    [GeneratedRegex("`([^`]+)`")]
    private static partial Regex BackTicks();

    private const string Style = """
        :root { --text: #1f2328; --muted: #656d76; --bg: #ffffff; --panel: #f6f8fa; --border: #d0d7de; --accent: #0969da; --passed: #1a7f37; --failed: #cf222e; }
        @media (prefers-color-scheme: dark) { :root { --text: #e6edf3; --muted: #8d96a0; --bg: #0d1117; --panel: #161b22; --border: #30363d; --accent: #4493f8; --passed: #3fb950; --failed: #f85149; } }
        body { margin: 0; background: var(--bg); color: var(--text); font: 15px/1.5 system-ui, -apple-system, "Segoe UI", sans-serif; }
        main { max-width: 960px; margin: 0 auto; padding: 24px 16px 48px; }
        h1 { margin: 0 0 4px; font-size: 1.6em; }
        h2 { font-size: 1.2em; margin: 28px 0 10px; }
        .when { color: var(--muted); font-size: 0.9em; margin: 0 0 12px; }
        code { font-family: ui-monospace, Consolas, monospace; background: var(--panel); padding: 0 4px; border-radius: 4px; }
        .changes li { margin: 2px 0; }
        video { width: 100%; max-height: 70vh; background: #000; border-radius: 6px; }
        .scene { border: 1px solid var(--border); border-radius: 6px; padding: 6px 12px; margin: 10px 0; background: var(--panel); }
        .scene summary { cursor: pointer; font-weight: 600; }
        .steps { margin: 6px 0; padding-left: 1.4em; }
        .steps li { margin: 2px 0; }
        .steps li[data-status="failed"] { color: var(--failed); }
        .steps a, .scene summary a { font-family: ui-monospace, Consolas, monospace; font-size: 0.85em; color: var(--accent); text-decoration: none; margin-right: 6px; }
        .message { font-family: ui-monospace, Consolas, monospace; font-size: 0.85em; white-space: pre-wrap; }
        .badge { font-size: 0.7em; font-weight: 600; text-transform: uppercase; border-radius: 10px; padding: 1px 8px; vertical-align: middle; border: 1px solid currentColor; }
        .badge[data-status="passed"] { color: var(--passed); }
        .badge[data-status="failed"] { color: var(--failed); }
        .badge[data-status="skipped"], .badge[data-status="canceled"] { color: var(--muted); }
        """;

    private const string Script = """
        document.addEventListener("click", (event) => {
          const seek = event.target.closest("[data-seek]");
          if (!seek) { return; }
          event.preventDefault();
          const video = document.getElementById(seek.getAttribute("data-seek"));
          video.currentTime = Number(seek.getAttribute("data-at"));
          video.play();
        });
        """;
}
