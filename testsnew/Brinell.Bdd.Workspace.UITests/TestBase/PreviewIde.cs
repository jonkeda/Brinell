using System.Text.Json;
using Brinell.Bdd.Preview;

namespace Brinell.Bdd.Workspace.UITests.TestBase;

/// <summary>
/// The IDE's side of the Scenario Preview, played in Chromium: it loads the page Visual Studio and
/// VS Code load (<see cref="BddPreview.HostPage"/>), sends it their messages, and keeps what the page
/// sends back. Tests drive the page through <see cref="PageObjects.ScenarioPreviewPage"/>.
/// </summary>
// The app host of these tests, as BlazorSample is for the Html UI tests: the one place that touches the browser page.
public sealed class PreviewIde : IAsyncDisposable
{
    // VS Code's webview API, as the page looks for it: what the page posts is kept for the test.
    private const string VsCodeApi = """
        window.__brinellSent = [];
        window.acquireVsCodeApi = () => ({ postMessage: (message) => window.__brinellSent.push(message) });
        """;

    private readonly PlaywrightTestContext _context;
    private readonly string _folder;

    private PreviewIde(PlaywrightTestContext context, string folder)
    {
        _context = context;
        _folder = folder;
    }

    /// <summary>The browser's test context, for page objects.</summary>
    public IHtmlTestContext Context => _context;

    /// <summary>Opens the preview page in a new browser, with the IDE's API in place.</summary>
    public static async Task<PreviewIde> StartAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Brinell", "PreviewIde", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var page = Path.Combine(folder, "preview.html");
        await File.WriteAllTextAsync(page, BddPreview.HostPage);

        var context = await PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions
        {
            Headless = !string.Equals(Environment.GetEnvironmentVariable("HEADLESS"), "false", StringComparison.OrdinalIgnoreCase),
            BrowserType = "chromium",
        });
        await context.InternalPage.AddInitScriptAsync(VsCodeApi);
        await context.NavigateToAsync(new Uri(page).AbsoluteUri);
        return new PreviewIde(context, folder);
    }

    /// <summary>Shows a feature file, rendered as the language server renders it before a build.</summary>
    /// <param name="featureText">The file's text.</param>
    public void Show(string featureText)
    {
        var path = Path.Combine(_folder, "Features", "Shelf.feature.md");
        var html = BddPreview.Render(featureText, path).Html;
        Send(new { type = "update", html, suites = Array.Empty<string>(), bindState = "parse-only" });
    }

    /// <summary>Sends the page a message, as the IDE does.</summary>
    /// <param name="message">The message; serialized as JSON.</param>
    public void Send(object message)
        => _context.InternalPage.EvaluateAsync("message => window.postMessage(message, '*')", message).GetAwaiter().GetResult();

    /// <summary>What the page has sent the IDE so far, oldest first.</summary>
    public IReadOnlyList<JsonElement> Sent()
        => _context.InternalPage.EvaluateAsync<JsonElement>("() => window.__brinellSent").GetAwaiter().GetResult().EnumerateArray().ToList();

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        Directory.Delete(_folder, recursive: true);
    }
}
