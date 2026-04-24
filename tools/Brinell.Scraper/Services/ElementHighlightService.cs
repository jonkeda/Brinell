using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace Brinell.Scraper.Services;

public sealed class ElementHighlightService
{
    private readonly ILogger<ElementHighlightService> _logger;
    private readonly List<CoreWebView2Frame> _trackedFrames = [];
    private bool _isActive;
    private bool _isTrackingFrames;

    public ElementHighlightService(ILogger<ElementHighlightService> logger)
    {
        _logger = logger;
    }

    public bool IsActive => _isActive;

    /// <summary>
    /// Start tracking iframe frames so overlay can be injected into them.
    /// Call once after WebView2 is initialized.
    /// </summary>
    public void TrackFrames(CoreWebView2 webView)
    {
        if (_isTrackingFrames) return;
        _isTrackingFrames = true;

        webView.FrameCreated += (_, args) =>
        {
            var frame = args.Frame;
            _trackedFrames.Add(frame);
            frame.Destroyed += (_, _) => _trackedFrames.Remove(frame);

            // If overlay is already active, inject into the new frame
            if (_isActive)
            {
                try { frame.ExecuteScriptAsync(IFrameOverlayScript).ConfigureAwait(false); }
                catch { /* frame may not support script execution */ }
            }
        };
    }

    public async Task EnableAsync(CoreWebView2 webView, bool force = false)
    {
        if (_isActive && !force) return;

        if (force)
            _trackedFrames.Clear();

        _isActive = true;
        await webView.ExecuteScriptAsync(OverlayScript);

        // Inject into all tracked iframes
        foreach (var frame in _trackedFrames.ToArray())
        {
            try { await frame.ExecuteScriptAsync(IFrameOverlayScript); }
            catch { /* frame may have been destroyed */ }
        }

        _logger.LogDebug("Element highlight overlay enabled (top + {FrameCount} frames)", _trackedFrames.Count);
    }

    public async Task DisableAsync(CoreWebView2 webView)
    {
        if (!_isActive) return;
        _isActive = false;
        await webView.ExecuteScriptAsync(RemoveOverlayScript);

        // Remove from all tracked iframes
        foreach (var frame in _trackedFrames.ToArray())
        {
            try { await frame.ExecuteScriptAsync(RemoveOverlayScript); }
            catch { /* frame may have been destroyed */ }
        }

        _logger.LogDebug("Element highlight overlay disabled");
    }

    public async Task ToggleAsync(CoreWebView2 webView)
    {
        if (_isActive)
            await DisableAsync(webView);
        else
            await EnableAsync(webView);
    }

    private const string OverlayScript = """
        (function() {
            if (window.__brinellOverlay) return;

            const overlay = document.createElement('div');
            overlay.id = '__brinell-overlay';
            overlay.style.cssText = 'position:fixed;pointer-events:none;z-index:2147483647;border:2px solid #4285f4;background:rgba(66,133,244,0.1);display:none;transition:all 0.05s ease;';
            document.body.appendChild(overlay);

            const tooltip = document.createElement('div');
            tooltip.id = '__brinell-tooltip';
            tooltip.style.cssText = 'position:fixed;pointer-events:none;z-index:2147483647;background:#333;color:#fff;padding:6px 10px;border-radius:4px;font:12px/1.4 monospace;max-width:400px;white-space:pre-wrap;display:none;';
            document.body.appendChild(tooltip);

            function isDynamicId(id) {
                return /[0-9a-f]{8,}|_\d+$|\d{4,}|guid|uuid/i.test(id);
            }

            function findAssociatedLabel(el) {
                if (el.id) {
                    const lbl = document.querySelector('label[for="' + CSS.escape(el.id) + '"]');
                    if (lbl) return lbl;
                }
                const parent = el.closest('label');
                if (parent) return parent;
                const prev = el.previousElementSibling;
                if (prev && prev.tagName === 'LABEL') return prev;
                return null;
            }

            function generateMinimalSelector(el) {
                if (el.id && !isDynamicId(el.id)) return '#' + CSS.escape(el.id);
                const parts = [];
                let current = el;
                while (current && current !== document.body) {
                    let sel = current.tagName.toLowerCase();
                    if (current.id && !isDynamicId(current.id)) {
                        parts.unshift('#' + CSS.escape(current.id) + ' > ' + sel);
                        break;
                    }
                    const parent = current.parentElement;
                    if (parent) {
                        const siblings = Array.from(parent.children).filter(c => c.tagName === current.tagName);
                        if (siblings.length > 1) sel += ':nth-of-type(' + (siblings.indexOf(current) + 1) + ')';
                    }
                    parts.unshift(sel);
                    current = current.parentElement;
                }
                return parts.join(' > ');
            }

            function suggestLocator(el) {
                const testId = el.getAttribute('data-testid');
                if (testId) return 'Locator.ByDataTestId("' + testId + '")';
                if (el.id && !isDynamicId(el.id)) return 'Locator.ById("' + el.id + '")';
                const label = findAssociatedLabel(el);
                if (label) return 'Locator.ByText("' + label.textContent.trim().substring(0, 50) + '")';
                const aria = el.getAttribute('aria-label');
                if (aria) return 'Locator.ByAriaLabel("' + aria + '")';
                return 'Locator.ByCss("' + generateMinimalSelector(el) + '")';
            }

            function formatInfo(el) {
                let info = el.tagName.toLowerCase();
                if (el.id) info += '#' + el.id;
                const type = el.getAttribute('type');
                if (type) info += '  type="' + type + '"';
                const aria = el.getAttribute('aria-label');
                if (aria) info += '  aria-label="' + aria + '"';
                info += '\nSuggested: ' + suggestLocator(el);
                return info;
            }

            let lastEl = null;

            document.addEventListener('mousemove', function(e) {
                const el = document.elementFromPoint(e.clientX, e.clientY);
                if (!el || el === overlay || el === tooltip || el === lastEl) return;
                lastEl = el;

                const rect = el.getBoundingClientRect();
                overlay.style.left = rect.left + 'px';
                overlay.style.top = rect.top + 'px';
                overlay.style.width = rect.width + 'px';
                overlay.style.height = rect.height + 'px';
                overlay.style.display = 'block';

                tooltip.textContent = formatInfo(el);
                tooltip.style.left = rect.left + 'px';
                tooltip.style.top = (rect.bottom + 4) + 'px';
                tooltip.style.display = 'block';

                // Keep tooltip on screen
                const tr = tooltip.getBoundingClientRect();
                if (tr.bottom > window.innerHeight) tooltip.style.top = (rect.top - tr.height - 4) + 'px';
                if (tr.right > window.innerWidth) tooltip.style.left = (window.innerWidth - tr.width - 4) + 'px';
            }, true);

            document.addEventListener('click', function(e) {
                if (!e.ctrlKey) return;
                e.preventDefault();
                e.stopPropagation();

                const el = document.elementFromPoint(e.clientX, e.clientY);
                if (!el || el === overlay || el === tooltip) return;

                const existing = el.getAttribute('data-brinell-selected');
                if (existing) {
                    el.removeAttribute('data-brinell-selected');
                    el.style.outline = '';
                    el.style.outlineOffset = '';
                } else {
                    el.setAttribute('data-brinell-selected', 'true');
                    el.style.outline = '2px solid #34a853';
                    el.style.outlineOffset = '-2px';
                }

                const rect = el.getBoundingClientRect();
                window.chrome.webview.postMessage(JSON.stringify({
                    type: 'elementSelected',
                    tag: el.tagName.toLowerCase(),
                    id: el.id || null,
                    dataTestId: el.getAttribute('data-testid'),
                    ariaLabel: el.getAttribute('aria-label'),
                    selected: !existing,
                    boundingBox: { x: rect.x, y: rect.y, width: rect.width, height: rect.height }
                }));
            }, true);

            window.__brinellOverlay = true;
        })();
        """;

    /// <summary>
    /// Overlay script variant for iframes. The overlay/tooltip are rendered inside the iframe
    /// but selection messages include the iframe's offset so the parent can compute page-level coordinates.
    /// </summary>
    private const string IFrameOverlayScript = """
        (function() {
            if (window.__brinellOverlay) return;

            const overlay = document.createElement('div');
            overlay.id = '__brinell-overlay';
            overlay.style.cssText = 'position:fixed;pointer-events:none;z-index:2147483647;border:2px solid #4285f4;background:rgba(66,133,244,0.1);display:none;transition:all 0.05s ease;';
            document.body.appendChild(overlay);

            const tooltip = document.createElement('div');
            tooltip.id = '__brinell-tooltip';
            tooltip.style.cssText = 'position:fixed;pointer-events:none;z-index:2147483647;background:#333;color:#fff;padding:6px 10px;border-radius:4px;font:12px/1.4 monospace;max-width:400px;white-space:pre-wrap;display:none;';
            document.body.appendChild(tooltip);

            function formatInfo(el) {
                let info = '[iframe] ' + el.tagName.toLowerCase();
                if (el.id) info += '#' + el.id;
                const type = el.getAttribute('type');
                if (type) info += '  type="' + type + '"';
                return info;
            }

            let lastEl = null;

            document.addEventListener('mousemove', function(e) {
                const el = document.elementFromPoint(e.clientX, e.clientY);
                if (!el || el === overlay || el === tooltip || el === lastEl) return;
                lastEl = el;

                const rect = el.getBoundingClientRect();
                overlay.style.left = rect.left + 'px';
                overlay.style.top = rect.top + 'px';
                overlay.style.width = rect.width + 'px';
                overlay.style.height = rect.height + 'px';
                overlay.style.display = 'block';

                tooltip.textContent = formatInfo(el);
                tooltip.style.left = rect.left + 'px';
                tooltip.style.top = (rect.bottom + 4) + 'px';
                tooltip.style.display = 'block';
            }, true);

            document.addEventListener('click', function(e) {
                if (!e.ctrlKey) return;
                e.preventDefault();
                e.stopPropagation();

                const el = document.elementFromPoint(e.clientX, e.clientY);
                if (!el || el === overlay || el === tooltip) return;

                const existing = el.getAttribute('data-brinell-selected');
                if (existing) {
                    el.removeAttribute('data-brinell-selected');
                    el.style.outline = '';
                    el.style.outlineOffset = '';
                } else {
                    el.setAttribute('data-brinell-selected', 'true');
                    el.style.outline = '2px solid #34a853';
                    el.style.outlineOffset = '-2px';
                }

                // Compute page-level coordinates by adding iframe offset
                const rect = el.getBoundingClientRect();
                let offsetX = 0, offsetY = 0;
                try {
                    const frameRect = window.frameElement?.getBoundingClientRect();
                    if (frameRect) { offsetX = frameRect.x; offsetY = frameRect.y; }
                } catch(ex) { /* cross-origin, can't read frameElement */ }

                window.chrome.webview.postMessage(JSON.stringify({
                    type: 'elementSelected',
                    tag: el.tagName.toLowerCase(),
                    id: el.id || null,
                    dataTestId: el.getAttribute('data-testid'),
                    ariaLabel: el.getAttribute('aria-label'),
                    selected: !existing,
                    inIframe: true,
                    boundingBox: { x: rect.x + offsetX, y: rect.y + offsetY, width: rect.width, height: rect.height }
                }));
            }, true);

            window.__brinellOverlay = true;
        })();
        """;

    private const string RemoveOverlayScript = """
        (function() {
            const overlay = document.getElementById('__brinell-overlay');
            if (overlay) overlay.remove();
            const tooltip = document.getElementById('__brinell-tooltip');
            if (tooltip) tooltip.remove();
            document.querySelectorAll('[data-brinell-selected]').forEach(el => {
                el.removeAttribute('data-brinell-selected');
                el.style.outline = '';
                el.style.outlineOffset = '';
            });
            window.__brinellOverlay = false;
        })();
        """;
}
