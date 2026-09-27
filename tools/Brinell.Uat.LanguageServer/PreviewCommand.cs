using Brinell.Uat.Preview;

namespace Brinell.Uat.LanguageServer;

// --preview: one file through the same server methods an IDE uses, written as a whole page.
internal static class PreviewCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter errors)
    {
        if (args.Length == 0 || !File.Exists(args[0]))
        {
            errors.WriteLine("usage: --preview <file> [--solution <folder>] [--suite <name>] [--out <page.html>] [--host]");
            return 2;
        }

        var file = Path.GetFullPath(args[0]);
        var solution = Option(args, "--solution") ?? Path.GetDirectoryName(file)!;
        using var server = new UatLanguageServer { FileImageUrls = true };
        server.Initialize(new InitializeParams(null, null, new InitializationOptions(Path.GetFullPath(solution))));
        var preview = server.Preview(new PreviewParams(new Uri(file).AbsoluteUri, Option(args, "--suite")));
        // --host: the IDE's host page with the preview posted into it, as a webview shows it.
        var page = args.Contains("--host")
            ? UatPreview.HostPage.Replace("</body>", $"<script>window.postMessage({System.Text.Json.JsonSerializer.Serialize(new { type = "update", html = preview.Html, suites = preview.Suites, suite = preview.Suite, bindState = preview.BindState, reason = preview.Reason })});</script>\n</body>", StringComparison.Ordinal)
            : UatPreview.Page(new UatPreviewResult(preview.Html, []), Path.GetFileName(file));
        if (Option(args, "--out") is { } path)
        {
            File.WriteAllText(path, page);
            errors.WriteLine($"{preview.BindState} in {preview.Suite ?? "no suite"}{(preview.Reason is { } reason ? $": {reason}" : string.Empty)}; wrote {path}");
        }
        else
        {
            output.Write(page);
        }

        return 0;
    }

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
