using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright;

// Spike 0: headed Chromium through Playwright, the Scraper's capture script, frames (same- and
// cross-origin, nested) and open shadow roots, one snapshot per page written to a project folder.
var site = args.Length > 0 ? args[0] : throw new ArgumentException("site folder");
var outDir = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "spike0");
var headed = !args.Contains("--headless");
Directory.CreateDirectory(outDir);

var port = FreePort();
using var server = Serve(site, port);
Console.WriteLine($"serving {site} on {port}");

using var pw = await Playwright.CreateAsync();
await using var browser = await pw.Chromium.LaunchAsync(new() { Headless = !headed });
var page = await browser.NewPageAsync();

var transitions = new List<string>();
page.FrameNavigated += (_, f) => { if (f == page.MainFrame) transitions.Add(f.Url); };

var urls = args.Where(a => a.StartsWith("http")).ToArray();
foreach (var url in urls)
{
    try
    {
        await page.GotoAsync(url, new() { Timeout = 60000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
    }
    catch (TimeoutException) { Console.WriteLine($"  {url}: not idle in time, captured anyway"); }
    var stable = Stopwatch.StartNew();
    // Stable: the element count unchanged over 1 s (4 polls), at most 20 s.
    int last = -1, same = 0;
    while (same < 4 && stable.Elapsed < TimeSpan.FromSeconds(20))
    {
        var c = await page.EvaluateAsync<int>("() => document.querySelectorAll('*').length");
        same = c == last ? same + 1 : 0; last = c;
        await Task.Delay(250);
    }
    Console.WriteLine($"  stable after {stable.ElapsedMilliseconds} ms at {last} elements");
    var framework = await page.EvaluateAsync<string>(FrameworkScript);
    var sw0 = Stopwatch.StartNew();
    var snap = await CaptureAsync(page);
    sw0.Stop();
    var text = snap.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    var (n0, f0, s0, fill0) = Count(snap["root"]!);
    await File.WriteAllTextAsync(Path.Combine(outDir, new Uri(url).Host + ".snapshot.json"), text);
    var lines = new List<string>();
    Flatten(snap["root"]!.AsObject(), 0, lines);
    var flat = string.Join('\n', lines);
    await File.WriteAllTextAsync(Path.Combine(outDir, new Uri(url).Host + ".flat.jsonl"), flat);
    Console.WriteLine($"  one node per line, no nulls: {flat.Length / 1024} KB");
    Console.WriteLine($"{url}\n  framework: {framework}; {n0} nodes, {f0} iframes ({fill0} with content), {s0} shadow roots, {text.Length / 1024} KB, {sw0.ElapsedMilliseconds} ms");
}
if (urls.Length > 0) return;

foreach (var path in new[] { "index.html", "pages/catalog.html", "pages/checkout.html", "pages/support.html" })
{
    await page.GotoAsync($"http://localhost:{port}/{path}");
    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    if (path == "pages/checkout.html")
    {
        // Cross-origin: 127.0.0.1 is another origin than localhost.
        await page.EvaluateAsync($$"""() => { const f = document.createElement('iframe'); f.src = 'http://127.0.0.1:{{port}}/frames/profile-widget.html'; f.title='Cross origin'; document.body.appendChild(f); return new Promise(r => f.onload = r); }""");
        // An open shadow root, as a web component library makes.
        await page.EvaluateAsync("""() => { const h = document.createElement('fluent-like'); document.body.appendChild(h); const r = h.attachShadow({mode:'open'}); r.innerHTML = '<button aria-label="Shadow button">Go</button><input placeholder="Shadow input">'; }""");
    }
    var sw = Stopwatch.StartNew();
    var snapshot = await CaptureAsync(page);
    sw.Stop();
    var json = snapshot.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    var file = Path.Combine(outDir, Path.GetFileNameWithoutExtension(path) + ".snapshot.json");
    await File.WriteAllTextAsync(file, json);
    var (nodes, frames, shadows, filled) = Count(snapshot["root"]!);
    Console.WriteLine($"{path}: {nodes} nodes, {frames} iframes ({filled} with content), {shadows} shadow roots, {json.Length / 1024} KB, {sw.ElapsedMilliseconds} ms");
}
Console.WriteLine($"main-frame navigations seen: {transitions.Count}");
return;

static async Task<JsonObject> CaptureAsync(IPage page)
{
    var roots = new Dictionary<IFrame, JsonObject>();
    foreach (var frame in page.Frames)
    {
        try { roots[frame] = JsonNode.Parse(await frame.EvaluateAsync<string>(CaptureScript))!.AsObject(); }
        catch (PlaywrightException e) { Console.WriteLine($"  frame {frame.Url}: {e.Message.Split('\n')[0]}"); }
    }
    // Hang each child frame's tree under its <iframe> node: the parent frame's script gave every
    // iframe an id in a WeakMap, and the frame element handle is evaluated in the parent.
    foreach (var frame in page.Frames.Where(f => f.ParentFrame is not null))
    {
        if (!roots.TryGetValue(frame, out var child) || !roots.TryGetValue(frame.ParentFrame!, out var parent)) continue;
        var element = await frame.FrameElementAsync();
        var id = await element.EvaluateAsync<int>("el => window.__brinellScrape.get(el)");
        var host = Find(parent, id);
        if (host is null) { Console.WriteLine($"  no host for {frame.Url}"); continue; }
        host["frame"] = new JsonObject { ["url"] = frame.Url, ["name"] = frame.Name };
        host["children"] = new JsonArray(child);
    }
    return new JsonObject
    {
        ["url"] = page.Url,
        ["title"] = await page.TitleAsync(),
        ["platform"] = "html",
        ["root"] = roots[page.MainFrame],
    };
}

static void Flatten(JsonObject node, int parent, List<string> lines)
{
    var id = lines.Count + 1;
    var o = new JsonObject { ["n"] = id, ["p"] = parent };
    foreach (var (k, v) in node)
        if (k != "children" && v is not null) o[k] = v.DeepClone();
    lines.Add(o.ToJsonString());
    foreach (var c in node["children"]?.AsArray() ?? [])
        Flatten(c!.AsObject(), id, lines);
}

static JsonObject? Find(JsonObject node, int frameId)
{
    if (node["frameId"]?.GetValue<int>() == frameId) return node;
    foreach (var c in node["children"]?.AsArray() ?? [])
        if (Find(c!.AsObject(), frameId) is { } found) return found;
    return null;
}

static (int nodes, int frames, int shadows, int filled) Count(JsonNode node)
{
    var o = node.AsObject();
    int n = 1, f = 0, s = 0, filled = 0;
    if (o["tag"]?.GetValue<string>() == "iframe") { f++; if (o["children"]?.AsArray().Count > 0) filled++; }
    if (o["kind"]?.GetValue<string>() == "shadow-root") s++;
    foreach (var c in o["children"]?.AsArray() ?? [])
    {
        var (a, b, d, e) = Count(c!);
        n += a; f += b; s += d; filled += e;
    }
    return (n, f, s, filled);
}

static int FreePort()
{
    var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
    l.Start();
    var p = ((IPEndPoint)l.LocalEndpoint).Port;
    l.Stop();
    return p;
}

static HttpListener Serve(string root, int port)
{
    var listener = new HttpListener();
    listener.Prefixes.Add($"http://localhost:{port}/");
    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
    listener.Start();
    _ = Task.Run(async () =>
    {
        while (listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await listener.GetContextAsync(); } catch (HttpListenerException) { return; } catch (ObjectDisposedException) { return; }
            var file = Path.Combine(root, ctx.Request.Url!.AbsolutePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(file))
            {
                ctx.Response.ContentType = Path.GetExtension(file) switch { ".html" => "text/html", ".css" => "text/css", ".js" => "text/javascript", _ => "application/octet-stream" };
                var bytes = await File.ReadAllBytesAsync(file);
                await ctx.Response.OutputStream.WriteAsync(bytes);
            }
            else ctx.Response.StatusCode = 404;
            ctx.Response.Close();
        }
    });
    return listener;
}

public partial class Program
{
    const string FrameworkScript = """
        () => {
            const found = [];
            const all = Array.from(document.querySelectorAll('*'));
            if (all.some(e => e.__vue__)) found.push('Vue 2 (' + (window.Vue && window.Vue.version || all.find(e => e.__vue__).__vue__.$root.constructor.version || '?') + ')');
            if (window.__VUE__ || all.some(e => e.__vue_app__)) found.push('Vue 3');
            if (all.some(e => Object.keys(e).some(k => k.startsWith('__reactFiber') || k.startsWith('__reactContainer') || k.startsWith('_reactRootContainer')))) found.push('React');
            if (window.Blazor) found.push('Blazor' + (window.DotNet ? '' : '') + (document.querySelector('script[src*="blazor.webassembly"]') ? ' WebAssembly' : document.querySelector('script[src*="blazor.web"]') ? ' Web' : document.querySelector('script[src*="blazor.server"]') ? ' Server' : ''));
            if (window.angular || document.querySelector('[ng-version]')) found.push('Angular');
            if (window.jQuery) found.push('jQuery ' + window.jQuery.fn.jquery);
            const shadows = all.filter(e => e.shadowRoot).length;
            return (found.join(', ') || 'none detected') + '; open shadow hosts: ' + shadows + '; hash route: ' + (location.hash.startsWith('#/') ? 'yes' : 'no');
        }
        """;

    // The Scraper's capture script, changed for Playwright: every iframe gets an id in a WeakMap
    // (no DOM change), frames are not entered here (each frame is captured on its own), and open
    // shadow roots are captured as a node of their own kind.
    const string CaptureScript = """
        () => {
            const ids = window.__brinellScrape = new WeakMap();
            let next = 0;
            function capture(el) {
                const rect = el.getBoundingClientRect();
                const node = {
                    tag: el.tagName.toLowerCase(),
                    id: el.id || null,
                    className: (typeof el.className === 'string' ? el.className : null) || null,
                    name: el.getAttribute('name'),
                    type: el.getAttribute('type'),
                    dataTestId: el.getAttribute('data-testid') || el.getAttribute('data-test'),
                    role: el.getAttribute('role'),
                    ariaLabel: el.getAttribute('aria-label'),
                    placeholder: el.getAttribute('placeholder'),
                    text: el.childNodes.length === 1 && el.childNodes[0].nodeType === 3 ? el.textContent.trim().substring(0, 200) : null,
                    bounds: [Math.round(rect.x), Math.round(rect.y), Math.round(rect.width), Math.round(rect.height)],
                    children: Array.from(el.children).map(capture)
                };
                if (el.tagName === 'IFRAME' || el.tagName === 'FRAME') { node.frameId = ++next; ids.set(el, next); }
                if (el.shadowRoot) node.children.unshift({ kind: 'shadow-root', children: Array.from(el.shadowRoot.children).map(capture) });
                return node;
            }
            return JSON.stringify(capture(document.documentElement));
        }
        """;
}
