using System.Diagnostics;
using System.Text.Json;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace Brinell.Scraper.Services;

public sealed class DomCaptureService
{
    private readonly ILogger<DomCaptureService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DomCaptureService(ILogger<DomCaptureService> logger)
    {
        _logger = logger;
    }

    public async Task<DomSnapshot> CaptureAsync(CoreWebView2 webView)
    {
        var sw = Stopwatch.StartNew();

        var json = await webView.ExecuteScriptAsync(CaptureScript);

        // WebView2 returns the result wrapped in quotes as a JSON string literal;
        // we need to unescape it first.
        var unescaped = JsonSerializer.Deserialize<string>(json)!;
        var root = JsonSerializer.Deserialize<DomElement>(unescaped, JsonOptions)!;

        sw.Stop();

        var snapshot = new DomSnapshot
        {
            PageUrl = webView.Source,
            PageTitle = webView.DocumentTitle,
            CapturedAt = DateTimeOffset.UtcNow,
            RootElement = root
        };

        var elementCount = CountElements(root);
        _logger.LogInformation(
            "DOM capture — URL: {Url}, Elements: {ElementCount}, Size: {SnapshotSizeBytes} bytes, Elapsed: {ElapsedMs} ms",
            snapshot.PageUrl, elementCount, unescaped.Length, sw.ElapsedMilliseconds);

        return snapshot;
    }

    /// <summary>
    /// Parses a raw JSON string (as produced by the capture script) into a DomSnapshot.
    /// Exposed for unit testing without WebView2.
    /// </summary>
    internal static DomSnapshot ParseSnapshot(string json, string? pageUrl = null, string? pageTitle = null)
    {
        var root = JsonSerializer.Deserialize<DomElement>(json, JsonOptions)!;
        return new DomSnapshot
        {
            PageUrl = pageUrl ?? "",
            PageTitle = pageTitle ?? "",
            CapturedAt = DateTimeOffset.UtcNow,
            RootElement = root
        };
    }

    internal static int CountElements(DomElement element)
    {
        var count = 1;
        foreach (var child in element.Children)
            count += CountElements(child);
        return count;
    }

    private const string CaptureScript = """
        (function() {
            function captureElement(el) {
                const rect = el.getBoundingClientRect();
                let children = Array.from(el.children).map(captureElement);
                let frameSource = null;

                // Traverse into same-origin iframes
                if (el.tagName === 'IFRAME') {
                    frameSource = el.src || null;
                    try {
                        const iframeDoc = el.contentDocument;
                        if (iframeDoc && iframeDoc.documentElement) {
                            children = [captureElement(iframeDoc.documentElement)];
                        }
                    } catch (e) {
                        // Cross-origin iframe — cannot access contentDocument
                    }
                }

                return {
                    tag: el.tagName.toLowerCase(),
                    id: el.id || null,
                    className: (typeof el.className === 'string' ? el.className : null) || null,
                    name: el.getAttribute('name'),
                    type: el.getAttribute('type'),
                    dataTestId: el.getAttribute('data-testid'),
                    role: el.getAttribute('role'),
                    ariaLabel: el.getAttribute('aria-label'),
                    placeholder: el.getAttribute('placeholder'),
                    textContent: el.childNodes.length === 1 && el.childNodes[0].nodeType === 3
                        ? el.textContent.trim().substring(0, 200) : null,
                    frameSource: frameSource,
                    boundingBox: { x: rect.x, y: rect.y, width: rect.width, height: rect.height },
                    children: children
                };
            }
            return JSON.stringify(captureElement(document.documentElement));
        })();
        """;
}
