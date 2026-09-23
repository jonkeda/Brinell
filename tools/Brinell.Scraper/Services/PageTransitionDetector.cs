using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace Brinell.Scraper.Services;

public sealed class PageTransitionDetector
{
    private readonly ILogger<PageTransitionDetector> _logger;
    private bool _isActive;

    public PageTransitionDetector(ILogger<PageTransitionDetector> logger)
    {
        _logger = logger;
    }

    public bool IsActive => _isActive;

    /// <summary>Fired when a page transition is detected. The string parameter is the new URL.</summary>
    public event Action<string>? PageTransitionDetected;

    public async Task StartAsync(CoreWebView2 webView)
    {
        if (_isActive) return;
        _isActive = true;

        webView.WebMessageReceived += OnWebMessageReceived;
        await webView.ExecuteScriptAsync(TransitionDetectorScript);

        _logger.LogDebug("Page transition detector started");
    }

    public async Task StopAsync(CoreWebView2 webView)
    {
        if (!_isActive) return;
        _isActive = false;

        webView.WebMessageReceived -= OnWebMessageReceived;
        await webView.ExecuteScriptAsync(RemoveDetectorScript);

        _logger.LogDebug("Page transition detector stopped");
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            // Simple parse — look for type: "pageTransition"
            if (json.Contains("\"pageTransition\"", StringComparison.Ordinal))
            {
                // Extract URL from the message
                var urlStart = json.IndexOf("\"url\":", StringComparison.Ordinal);
                if (urlStart >= 0)
                {
                    var valueStart = json.IndexOf('"', urlStart + 6) + 1;
                    var valueEnd = json.IndexOf('"', valueStart);
                    var url = json[valueStart..valueEnd];

                    _logger.LogInformation("Page transition detected: {Url}", url);
                    PageTransitionDetected?.Invoke(url);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse web message");
        }
    }

    private const string TransitionDetectorScript = """
        (function() {
            if (window.__brinellTransitionDetector) return;

            let lastUrl = location.href;
            let mutationTimeout = null;
            const SETTLE_MS = 500;
            const THRESHOLD = 10;

            // MutationObserver for large DOM changes
            const observer = new MutationObserver((mutations) => {
                const totalChanged = mutations.reduce(
                    (sum, m) => sum + m.addedNodes.length + m.removedNodes.length, 0);

                if (totalChanged >= THRESHOLD) {
                    // Wait for DOM to settle
                    if (mutationTimeout) clearTimeout(mutationTimeout);
                    mutationTimeout = setTimeout(() => {
                        const currentUrl = location.href;
                        window.chrome.webview.postMessage(JSON.stringify({
                            type: 'pageTransition',
                            url: currentUrl,
                            trigger: 'mutation',
                            changedNodes: totalChanged
                        }));
                    }, SETTLE_MS);
                }
            });
            observer.observe(document.body, { childList: true, subtree: true });

            // hashchange event
            window.addEventListener('hashchange', () => {
                window.chrome.webview.postMessage(JSON.stringify({
                    type: 'pageTransition',
                    url: location.href,
                    trigger: 'hashchange'
                }));
            });

            // popstate event (back/forward with pushState)
            window.addEventListener('popstate', () => {
                window.chrome.webview.postMessage(JSON.stringify({
                    type: 'pageTransition',
                    url: location.href,
                    trigger: 'popstate'
                }));
            });

            // URL polling for pushState changes
            setInterval(() => {
                if (location.href !== lastUrl) {
                    lastUrl = location.href;
                    window.chrome.webview.postMessage(JSON.stringify({
                        type: 'pageTransition',
                        url: location.href,
                        trigger: 'urlPoll'
                    }));
                }
            }, 300);

            window.__brinellTransitionDetector = observer;
        })();
        """;

    private const string RemoveDetectorScript = """
        (function() {
            if (window.__brinellTransitionDetector) {
                window.__brinellTransitionDetector.disconnect();
                window.__brinellTransitionDetector = null;
            }
        })();
        """;
}
