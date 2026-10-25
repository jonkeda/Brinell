using System.Text.RegularExpressions;
using Microsoft.Playwright;

// Claude's part of .my/ScraperEx/manual-test-plan.md: the view through the stand-in host (bridge.py),
// the recording browser driven over DevTools as a person would. Args: <repo root> <scratch solution>
var repo = args[0];
var scratch = args[1];
const string SampleBridge = "http://127.0.0.1:8791/";   // solution: the Bruinell root, debug port 9333
const string ShopBridge = "http://127.0.0.1:8792/";     // solution: the scratch one, debug port 9334
const string EmptyBridge = "http://127.0.0.1:8793/";    // solution: an empty folder
const string Site = "http://localhost:8766";

var results = new List<(string Id, bool Pass, string Note)>();
async Task Check(string id, Func<Task<string>> check)
{
    try
    {
        var note = await check();
        results.Add((id, true, note));
        Console.WriteLine($"PASS {id} {note}");
    }
    catch (Exception e)
    {
        results.Add((id, false, e.Message.Split('\n')[0]));
        Console.WriteLine($"FAIL {id} {e.Message.Split('\n')[0]}");
    }
}
void Expect(bool condition, string what) { if (!condition) throw new Exception(what); }

using var pw = await Playwright.CreateAsync();
await using var viewer = await pw.Chromium.LaunchAsync();
var view = await viewer.NewPageAsync(new() { ViewportSize = new() { Width = 1400, Height = 1000 } });

async Task<string> Status() => await view.InnerTextAsync("#sc-status");
async Task<string> Problem() => await view.InnerTextAsync("#sc-problem");
async Task<string> Left() => await view.InnerTextAsync("#sc-left");
async Task<string> Right() => await view.InnerTextAsync("#sc-right");
async Task WaitFor(Func<Task<bool>> condition, string what, int seconds = 30)
{
    var until = DateTime.UtcNow.AddSeconds(seconds);
    while (!await condition())
    {
        if (DateTime.UtcNow > until) throw new Exception($"timed out waiting for {what}; status '{await Status()}', problem '{await Problem()}'");
        await Task.Delay(250);
    }
}
async Task<int> Captures() { var m = Regex.Match(await Status(), @": (\d+) page"); return m.Success ? int.Parse(m.Groups[1].Value) : -1; }
async Task Record(string session, string url = "")
{
    await view.FillAsync("#sc-session", session);
    await view.FillAsync("#sc-url", url);
    await view.ClickAsync("#sc-start");
    await WaitFor(async () => await Captures() >= 1, "the first capture");
}
async Task Stop() { await view.ClickAsync("#sc-stop"); await WaitFor(async () => (await Status()).Length == 0, "the recording to stop"); }
async Task<IPage> Browser(int port)
{
    var cdp = await pw.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{port}");
    return cdp.Contexts[0].Pages.First(p => !p.Url.StartsWith("devtools", StringComparison.Ordinal));
}
async Task OpenPage(string key, int captureIndex = 0)
{
    await view.ClickAsync("[data-tab=pages]");
    await view.WaitForTimeoutAsync(800);
    var item = view.Locator("#sc-left .sc-item:not(.sc-sub)").Filter(new() { HasTextRegex = new Regex("^" + Regex.Escape(key) + " ") });
    await item.First.ClickAsync();
    await view.WaitForSelectorAsync("pre.sc-outline span");
}

var mode = args.Length > 2 ? args[2] : "all";
if (mode == "g4") { await view.GotoAsync(SampleBridge); await WaitFor(async () => (await Left()).Contains("/userform"), "the pages list"); }
if (mode == "all")
{
// ---- V-4: a solution without a scraper.config.md
await Check("V-4", async () =>
{
    await view.GotoAsync(EmptyBridge);
    await WaitFor(async () => (await Problem()).Contains("scraper.config.md"), "the no-project line");
    return $"'{await Problem()}'";
});

// ---- The Blazor sample, in the repo's project
await view.GotoAsync(SampleBridge);
await WaitFor(async () => (await Left()).Contains("/userform"), "the pages list");

await Check("V-3", async () =>
{
    var project = await view.InputValueAsync("#sc-project");
    var pages = await view.Locator("#sc-left .sc-item:not(.sc-sub)").CountAsync();
    var userform = await view.Locator("#sc-left .sc-item:not(.sc-sub)").Filter(new() { HasTextRegex = new Regex("^/userform ") }).InnerTextAsync();
    Expect(project == "Bruinell.Blazor.UITests", $"project {project}");
    Expect(pages == 13, $"{pages} pages");
    Expect(userform.Contains("UserFormPage"), $"'{userform}'");
    return $"{project}, {pages} pages, '{userform.Trim()}'";
});

IPage? live = null;
await Check("R-1", async () =>
{
    await Record("manual");
    var status = await Status();
    Expect(!await view.IsDisabledAsync("#sc-capture") && !await view.IsDisabledAsync("#sc-pick") && !await view.IsDisabledAsync("#sc-stop") && await view.IsDisabledAsync("#sc-start"), "buttons");
    live = await Browser(9333);
    Expect(live.Url.StartsWith("http://localhost:5180/", StringComparison.Ordinal), live.Url);
    return $"'{status}', browser at {live.Url}";
});

await Check("R-2", async () =>
{
    await live!.ClickAsync("[data-automation-id=NavCounter]");
    await WaitFor(async () => await Captures() == 2, "the Counter capture");
    await live.ClickAsync("[data-automation-id=NavLogin]");
    await WaitFor(async () => await Captures() == 3, "the Login capture");
    var left = await Left();
    Expect(left.Contains("/counter") && left.Contains("/login"), "pages list");
    return $"'{await Status()}'";
});

await Check("R-3", async () =>
{
    await live!.ClickAsync("[data-automation-id=NavCounter]");
    await live.WaitForURLAsync("**/counter");
    await Task.Delay(3000); // longer than a capture takes to settle (500 ms quiet): nothing may come
    Expect(await Captures() == 3, $"count {await Captures()}");
    return "still 3";
});

await Check("R-4", async () =>
{
    await live!.ClickAsync("[data-testid=increment-btn]");
    await view.ClickAsync("#sc-capture");
    await WaitFor(async () => await Captures() == 4, "the new capture");
    return $"'{await Status()}'";
});

await Check("P-3", async () =>
{
    await OpenPage("/counter");
    var line = view.Locator("pre.sc-outline span").Filter(new() { HasText = "increment-btn" });
    await line.ClickAsync();
    await WaitFor(async () => await live!.EvaluateAsync<bool>("() => !!document.getElementById('__bruinell_highlight')"), "the outline in the browser", 10);
    var box = await live!.EvaluateAsync<string>("() => { const b = document.getElementById('__bruinell_highlight').getBoundingClientRect(); const e = document.querySelector('[data-testid=increment-btn]').getBoundingClientRect(); return `${Math.round(b.x)},${Math.round(b.y)} vs ${Math.round(e.x)},${Math.round(e.y)}`; }");
    return $"outline at {box}";
});

await Check("G-1", async () =>
{
    await WaitFor(async () => (await view.InnerTextAsync("#sc-node")).Contains("Control"), "the node's details", 15);
    var details = await view.InnerTextAsync("#sc-node");
    Expect(details.Contains("ButtonControl") && details.Contains("increment-btn"), details);
    return details.Replace('\n', ' ').Trim();
});

await Check("R-8", async () =>
{
    var session = (await Status()).Split(' ')[1].TrimEnd(':');
    await Stop();
    var folder = Path.Combine(repo, "testsnew", "Bruinell.Blazor.UITests", "Recordings", session);
    var files = Directory.GetFiles(folder).Select(Path.GetFileName).ToArray();
    Expect(files.Contains("recording.json") && files.Count(f => f!.EndsWith(".nodes.jsonl")) == 4, string.Join(", ", files));
    Expect(!await view.IsDisabledAsync("#sc-start"), "Record enabled");
    return $"{session}: {string.Join(", ", files)}";
});

await Check("R-9", async () =>
{
    await Record("cookie-1");
    var first = await Browser(9333);
    await first.EvaluateAsync("() => { document.cookie = 'bruinell_check=kept; max-age=3600; path=/'; }");
    await Stop();
    await Record("cookie-2");
    var second = await Browser(9333);
    var cookie = await second.EvaluateAsync<string>("() => document.cookie");
    await Stop();
    Expect(cookie.Contains("bruinell_check=kept"), $"cookies '{cookie}'");
    return $"'{cookie}'";
});

if (mode is "all" or "g4") await Check("G-4", async () =>
{
    await view.ClickAsync("[data-tab=patterns]");
    await WaitFor(async () => (await Left()).Contains("page(s)"), "patterns");
    var count = await view.Locator("#sc-left .sc-item").CountAsync();
    await view.Locator("#sc-left .sc-item").First.ClickAsync();
    await WaitFor(async () => await view.Locator("#sc-right .sc-item").CountAsync() > 0, "the pattern's examples");
    var right = await Right();
    var example = await view.Locator("#sc-right .sc-item").First.InnerTextAsync();
    await view.Locator("#sc-right .sc-item").First.ClickAsync();
    await WaitFor(async () => await view.Locator("pre.sc-outline span[aria-current=true]").CountAsync() > 0, $"the example '{example}' selected; right: '{(await Right())[..Math.Min(200, (await Right()).Length)]}'", 20);
    var selected = await view.Locator("pre.sc-outline span[aria-current=true]").First.InnerTextAsync();
    return $"{count} patterns; first: '{right.Split((char)10)[0]}'; its example '{example}' opened at '{selected.Trim()}'";
});

await Check("G-5", async () =>
{
    await view.ClickAsync("[data-tab=code]");
    await WaitFor(async () => (await Left()).Contains("UserFormPage"), "the code list");
    var names = await Left();
    await view.Locator("#sc-left .sc-item").Filter(new() { HasText = "UserFormPage" }).ClickAsync();
    var asked = await view.EvaluateAsync<string>("() => JSON.stringify((window.__toIde || []).slice(-1))");
    Expect(asked.Contains("UserFormPage.cs") && asked.Contains("\"open\""), asked);
    return $"'{names.Replace('\n', ';')}'; asked the IDE to open UserFormPage.cs";
});

// ---- The test site, in the scratch project (a copy, so it can be changed)
await view.GotoAsync(ShopBridge);
await WaitFor(async () => (await view.InputValueAsync("#sc-project")) == "Shop.UITests", "the scratch project");

await Check("R-5", async () =>
{
    await Record("spa", $"{Site}/pages/spa.html");
    var shop = await Browser(9334);
    await shop.ClickAsync("[data-testid=nav-order]");
    await WaitFor(async () => (await Left()).Contains("/pages/spa.html#/orders/{id}"), "the hash route capture");
    await shop.ClickAsync("[data-testid=nav-settings]");
    await WaitFor(async () => (await Left()).Contains("/pages/settings"), "the pushState capture");
    live = shop;
    return $"'{await Status()}'";
});

await Check("R-6", async () =>
{
    await live!.GotoAsync($"{Site}/pages/spa.html#/orders/42");
    await live.ClickAsync("[data-testid=open-dialog]");
    var before = await Captures();
    await view.ClickAsync("#sc-capture");
    await WaitFor(async () => await Captures() > before, "the dialog capture");
    await OpenPage("/pages/spa.html#/orders/{id}");
    var outline = await view.InnerTextAsync("pre.sc-outline");
    Expect(outline.Contains("role=dialog"), "no dialog in the capture");
    return "the capture has role=dialog";
});

await Check("R-7", async () =>
{
    var before = await Captures();
    await live!.GotoAsync("http://127.0.0.1:8766/index.html");
    await Task.Delay(3000);
    Expect(await Captures() == before, $"count went from {before} to {await Captures()}");
    Expect(!(await Left()).Contains("127.0.0.1"), "an other-host page listed");
    return $"still {before}";
});

await Check("M-2", async () =>
{
    await live!.GotoAsync($"{Site}/pages/spa.html");
    await live.FillAsync("shadow-search input", "typed-secret-4711");
    await view.ClickAsync("#sc-capture");
    await Task.Delay(2500);
    var files = Directory.GetFiles(Path.Combine(scratch, "Shop.UITests", "Recordings"), "*", SearchOption.AllDirectories);
    Expect(!files.Any(f => File.ReadAllText(f).Contains("typed-secret-4711")), "typed text found");
    return $"not in any of {files.Length} files";
});

await Check("P-4", async () =>
{
    await live!.GotoAsync($"{Site}/pages/checkout.html");
    await WaitFor(async () => (await Left()).Contains("/pages/checkout.html"), "the checkout capture");
    await OpenPage("/pages/checkout.html");
    await view.Locator("pre.sc-outline span").Filter(new() { HasText = "[data-testid=card-number]" }).First.ClickAsync();
    var frame = live.Frames.First(f => f.Url.EndsWith("payment-frame.html", StringComparison.Ordinal));
    await WaitFor(async () => await frame.EvaluateAsync<bool>("() => !!document.getElementById('__bruinell_highlight')"), "the outline in the frame", 10);
    return "outlined inside payment-frame.html";
});

await Check("M-1", async () =>
{
    await live!.GotoAsync($"{Site}/pages/catalog.html");
    await WaitFor(async () => (await Left()).Contains("/pages/catalog.html"), "the catalog capture");
    await Stop();
    var files = Directory.GetFiles(Path.Combine(scratch, "Shop.UITests", "Recordings"), "*catalog*.nodes.jsonl", SearchOption.AllDirectories);
    var text = string.Concat(files.Select(File.ReadAllText));
    Expect(!text.Contains("P-100") && text.Contains("SKU-MASKED"), "mask not applied");
    return $"P-100 masked as SKU-MASKED in {files.Length} file(s)";
});

}

if (mode == "refresh")
{
    // F-1: the catalog has a page object (A-3); the site changes and the page is recorded again.
    var catalog = Path.Combine(scratch, "site", "pages", "catalog.html");
    File.WriteAllText(catalog, File.ReadAllText(catalog).Replace("<h2>Filter</h2>", "<h2>Filter</h2><button type=\"button\" data-testid=\"clear-filters\">Clear filters</button>"));
    await view.GotoAsync(ShopBridge);
    await WaitFor(async () => (await Left()).Contains("/pages/catalog.html"), "the pages list");
    await Check("F-1", async () =>
    {
        var before = await view.Locator("#sc-left .sc-item:not(.sc-sub)").Filter(new() { HasTextRegex = new Regex("^/pages/catalog.html ") }).InnerTextAsync();
        Expect(before.Contains("CatalogPage") && !before.Contains("changed"), $"before: '{before}'");
        await Record("changed", $"{Site}/pages/catalog.html");
        await Stop();
        var after = await view.Locator("#sc-left .sc-item:not(.sc-sub)").Filter(new() { HasTextRegex = new Regex("^/pages/catalog.html ") }).InnerTextAsync();
        Expect(after.Contains("CatalogPage: changed"), $"after: '{after}'");
        await OpenPage("/pages/catalog.html");
        await view.Locator("button").Filter(new() { HasText = "Changes" }).ClickAsync();
        await WaitFor(async () => (await view.InnerTextAsync("#sc-node")).Contains("added"), "the changes");
        var changes = await view.InnerTextAsync("#sc-node");
        Expect(changes.Contains("clear-filters"), changes);
        return $"'{after.Trim()}'; {changes.Split((char)10)[0]}";
    });
}

File.WriteAllLines(Path.Combine(scratch, $"results-{mode}.txt"), results.Select(r => $"{r.Id}\t{(r.Pass ? "Pass" : "Fail")}\t{r.Note}"));
await view.ScreenshotAsync(new() { Path = Path.Combine(scratch, "view-after-run.png") });
Console.WriteLine($"{results.Count(r => r.Pass)} of {results.Count} passed");
