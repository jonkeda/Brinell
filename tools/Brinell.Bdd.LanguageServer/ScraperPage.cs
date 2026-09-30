using Brinell.Bdd.Preview;

namespace Brinell.Bdd.LanguageServer;

// The Scraper view: one page both IDEs show (a VS Code webview, a Visual Studio tool window). All its
// logic is here; an IDE only relays. The page sends {type: "call", id, method, params} for a
// brinell/scraper/* request and gets {type: "result", id, value}; the IDE forwards
// brinell/scraper/changed as {type: "changed", ...}, and does "open" (a file), "diff" (two files) and
// "chat" (a prompt for the agent) itself.
internal static class ScraperPage
{
    public static string Html { get; } = $$"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <meta name="color-scheme" content="light dark">
        <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'">
        <title>Scraper</title>
        <style>
        {{BddPreview.Stylesheet}}
        body { padding: 0 0.6em; }
        .sc-bar { display: flex; flex-wrap: wrap; gap: 0.4em; align-items: center; padding: 0.5em 0; position: sticky; top: 0; background: var(--uat-bg); border-bottom: 1px solid var(--uat-border); z-index: 1; }
        .sc-bar input { width: 11em; }
        .sc-bar button, .sc-tabs button, .sc-small { background: var(--uat-surface); color: var(--uat-fg); border: 1px solid var(--uat-border); border-radius: 3px; padding: 0.15em 0.6em; cursor: pointer; font: inherit; }
        .sc-bar button:disabled { opacity: 0.5; cursor: default; }
        .sc-status { color: var(--uat-muted); margin-left: auto; }
        .sc-status[data-recording="true"] { color: var(--uat-failed); }
        .sc-problem { color: var(--uat-failed); padding: 0.3em 0; }
        .sc-tabs { display: flex; gap: 0.3em; padding: 0.4em 0; }
        .sc-tabs button[aria-selected="true"] { border-color: var(--uat-accent); color: var(--uat-accent); }
        .sc-split { display: grid; grid-template-columns: minmax(14em, 1fr) 2fr; gap: 0.8em; }
        .sc-list .sc-item { padding: 0.15em 0.4em; border-radius: 3px; cursor: pointer; }
        .sc-list .sc-item:hover, .sc-item[aria-current="true"] { background: var(--uat-surface); }
        .sc-sub { padding-left: 1.2em; color: var(--uat-muted); font-size: 0.9em; }
        .sc-muted { color: var(--uat-muted); }
        .sc-kind { display: inline-block; min-width: 6em; color: var(--uat-muted); }
        pre.sc-outline { font-family: var(--uat-mono); font-size: 0.85em; margin: 0; white-space: pre; overflow-x: auto; }
        pre.sc-outline span { display: block; cursor: pointer; border-radius: 2px; }
        pre.sc-outline span:hover { background: var(--uat-surface); }
        pre.sc-outline span[aria-current="true"] { outline: 1px solid var(--uat-accent); }
        .sc-actions { display: flex; flex-wrap: wrap; gap: 0.4em; margin: 0.4em 0; }
        .sc-node { border: 1px solid var(--uat-border); border-radius: 4px; padding: 0.4em; margin: 0.4em 0; }
        </style>
        </head>
        <body>
        <div class="sc-bar">
          <select id="sc-project" aria-label="Project"></select>
          <input id="sc-session" placeholder="Session name" aria-label="Session name">
          <input id="sc-url" placeholder="Start URL (config's)" aria-label="Start URL">
          <button id="sc-start" type="button">Record</button>
          <button id="sc-capture" type="button" disabled>Capture</button>
          <button id="sc-pick" type="button" disabled>Pick</button>
          <button id="sc-stop" type="button" disabled>Stop</button>
          <span id="sc-status" class="sc-status"></span>
        </div>
        <div id="sc-problem" class="sc-problem" hidden></div>
        <nav class="sc-tabs" role="tablist">
          <button type="button" role="tab" data-tab="pages" aria-selected="true">Pages</button>
          <button type="button" role="tab" data-tab="patterns" aria-selected="false">Patterns</button>
          <button type="button" role="tab" data-tab="code" aria-selected="false">Code</button>
          <button type="button" class="sc-small" id="sc-generate" title="Ask the agent to make page objects from the recorded pages">Generate…</button>
        </nav>
        <main id="sc-main" class="sc-split">
          <div id="sc-left" class="sc-list"></div>
          <div id="sc-right"></div>
        </main>
        <script>
        (function () {
          "use strict";
          const host = (function () {
            if (window.chrome && window.chrome.webview) {
              return { post: m => window.chrome.webview.postMessage(m), on: f => window.chrome.webview.addEventListener("message", e => f(e.data)) };
            }
            const api = typeof acquireVsCodeApi === "function" ? acquireVsCodeApi() : { postMessage: m => window.parent.postMessage(m, "*") };
            return { post: m => api.postMessage(m), on: f => window.addEventListener("message", e => f(e.data)) };
          })();
          const $ = id => document.getElementById(id);
          const el = (tag, props, ...children) => {
            const e = document.createElement(tag);
            for (const [k, v] of Object.entries(props || {})) {
              if (k === "class") e.className = v; else if (k.startsWith("on")) e.addEventListener(k.slice(2), v); else if (v !== undefined && v !== null) e.setAttribute(k, v);
            }
            for (const c of children) if (c !== undefined && c !== null) e.append(c);
            return e;
          };

          // Requests go through the IDE to the server; each answer comes back by its id.
          let nextId = 1;
          const waiting = new Map();
          const call = (method, params) => new Promise(resolve => {
            const id = nextId++;
            waiting.set(id, resolve);
            host.post({ type: "call", id, method: "brinell/scraper/" + method, params: params || {} });
          });
          // A ScraperResult: the value, or the problem shown in the bar.
          const answer = async (method, params) => {
            const result = await call(method, params);
            if (result && typeof result === "object" && "problem" in result) {
              showProblem(result.problem);
              return result.problem ? undefined : result.value;
            }
            return result;
          };
          const showProblem = text => { $("sc-problem").hidden = !text; $("sc-problem").textContent = text || ""; };

          const state = { project: null, tab: "pages", recording: null, pages: [], page: null, capture: null, live: null, node: null };

          host.on(message => {
            if (message.type === "result") { const done = waiting.get(message.id); waiting.delete(message.id); if (done) done(message.value); return; }
            if (message.type === "theme") {
              for (const [name, value] of Object.entries(message.vars || {})) document.documentElement.style.setProperty(name, value);
              if (message.kind) document.documentElement.setAttribute("data-theme", message.kind);
              return;
            }
            if (message.type === "changed") { onChanged(message); }
          });

          async function onChanged(change) {
            state.recording = change.kind === "Stopped" ? null : change.recording;
            if (change.kind === "Failed") showProblem(change.message);
            if (change.kind === "Captured" && change.captured && change.recording) {
              // The newest capture of the live session is what Highlight and Pick number by.
              state.live = { session: change.recording.session, number: change.captured.number };
            }
            showRecording();
            if (change.kind !== "Started") await loadPages();
          }

          function showRecording() {
            const r = state.recording;
            $("sc-capture").disabled = $("sc-pick").disabled = $("sc-stop").disabled = !r;
            $("sc-start").disabled = !!r;
            $("sc-status").textContent = r ? `Recording ${r.session}: ${r.captures} page(s)` : "";
            $("sc-status").setAttribute("data-recording", String(!!r));
          }

          async function loadProjects() {
            const projects = await call("projects") || [];
            const picker = $("sc-project");
            picker.replaceChildren(...projects.map(p => new Option(p.name, p.name, false, p.name === state.project)));
            if (projects.length === 0) {
              showProblem("No project here has a scraper.config.md. Add one to the UI test project the pages go into (the scrape-pages skill says what it holds).");
              return;
            }
            state.project = picker.value;
            state.recording = await call("state");
            showRecording();
            await show();
          }

          async function show() {
            for (const tab of document.querySelectorAll("[data-tab]")) tab.setAttribute("aria-selected", String(tab.getAttribute("data-tab") === state.tab));
            $("sc-left").replaceChildren();
            $("sc-right").replaceChildren();
            if (state.tab === "pages") await loadPages();
            else if (state.tab === "patterns") await loadPatterns();
            else await loadCode();
          }

          async function loadPages() {
            if (state.tab !== "pages") return;
            state.pages = await answer("pages", { project: state.project }) || [];
            const left = $("sc-left");
            left.replaceChildren(...(state.pages.length ? [] : [el("p", { class: "sc-muted" }, "Nothing recorded yet: Record opens a browser; each page you go to is captured.")]));
            for (const page of state.pages) {
              left.append(el("div", { class: "sc-item", title: page.location, "aria-current": String(page.key === state.page), onclick: () => openCapture(page.key, page.captures[0]) },
                page.key, el("span", { class: "sc-muted" }, ` ${page.name}`)));
              for (const c of page.captures) {
                left.append(el("div", { class: "sc-item sc-sub", onclick: () => openCapture(page.key, c) }, `${c.session} #${c.number}, ${c.nodes} nodes, ${new Date(c.at).toLocaleString()}`));
              }
            }
            if (state.page && state.capture) {
              const page = state.pages.find(p => p.key === state.page);
              const fresh = page && page.captures.find(c => c.session === state.capture.session && c.number === state.capture.number);
              if (fresh) await openCapture(page.key, fresh, state.node);
            }
          }

          async function openCapture(key, capture, node) {
            state.page = key;
            state.capture = capture;
            state.node = node || null;
            const text = await answer("outline", { project: state.project, page: key, session: capture.session, number: capture.number, maxLines: 2000 });
            if (text === undefined) return;
            const page = state.pages.find(p => p.key === key);
            const older = page ? page.captures.filter(c => c !== capture) : [];
            const live = isLive(capture);
            const right = $("sc-right");
            const actions = el("div", { class: "sc-actions" },
              el("button", { type: "button", class: "sc-small", onclick: () => host.post({ type: "open", path: capture.file }) }, "Open file"),
              older.length ? el("button", { type: "button", class: "sc-small", title: "Compare with the capture before it", onclick: () => host.post({ type: "diff", left: older[0].file, right: capture.file, title: `${key}: ${older[0].session} #${older[0].number} ↔ ${capture.session} #${capture.number}` }) }, "Compare") : null,
              el("button", { type: "button", class: "sc-small", onclick: () => chat(`Make or update the page object for the recorded page ${key} of ${state.project}.`) }, "Generate page object"),
              el("span", { class: "sc-muted" }, live ? "Live: click a line to show it in the browser." : ""));
            const pre = el("pre", { class: "sc-outline" });
            for (const line of text.split("\n")) {
              if (!line) continue;
              const match = /#(\d+) /.exec(line);
              const number = match ? Number(match[1]) : null;
              pre.append(el("span", { "data-node": number, "aria-current": String(number === state.node), onclick: () => selectNode(number) }, line));
            }
            const details = el("div", { id: "sc-node" });
            right.replaceChildren(el("h3", {}, key), actions, details, pre);
            if (state.node) await selectNode(state.node, true);
          }

          const isLive = capture => !!(state.recording && state.live && capture && capture.session === state.live.session && capture.number === state.live.number);

          async function selectNode(number, scroll) {
            if (!number) return;
            state.node = number;
            for (const line of document.querySelectorAll("pre.sc-outline span")) line.setAttribute("aria-current", String(Number(line.getAttribute("data-node")) === number));
            const current = document.querySelector(`pre.sc-outline span[data-node="${number}"]`);
            if (scroll && current) current.scrollIntoView({ block: "center" });
            if (isLive(state.capture)) await answer("highlight", { node: number });
            const similar = await answer("similar", { project: state.project, page: state.page, node: number }) || [];
            const header = await answer("header", { project: state.project, page: state.page, node: number });
            $("sc-node").replaceChildren(el("div", { class: "sc-node" },
              el("div", {}, current ? current.textContent.trim() : `#${number}`),
              el("div", { class: "sc-muted" }, `The same kind of node: ${similar.length} on ${new Set(similar.map(s => s.page)).size} page(s).`),
              el("div", { class: "sc-actions" },
                el("button", { type: "button", class: "sc-small", onclick: () => chat(`Make a control for the node #${number} of the recorded page ${state.page} of ${state.project} (it occurs ${similar.length} times). Its header: ${header || ""}`) }, "Make a control"))));
          }

          async function loadPatterns() {
            const patterns = await answer("patterns", { project: state.project }) || [];
            const left = $("sc-left");
            left.replaceChildren(...(patterns.length ? [] : [el("p", { class: "sc-muted" }, "No patterns: record some pages first.")]));
            for (const p of patterns) {
              left.append(el("div", { class: "sc-item", onclick: () => showPattern(p) },
                el("span", { class: "sc-kind" }, p.kind), `${p.pages} page(s), ${p.occurrences}×`, p.control ? el("span", { class: "sc-muted" }, ` = ${p.control}`) : null,
                el("div", { class: "sc-sub" }, p.examples[0].line.replace(/^#\d+ /, "").slice(0, 80))));
            }
          }

          function showPattern(p) {
            const right = $("sc-right");
            right.replaceChildren(el("h3", {}, `${p.kind}: ${p.pages} page(s), ${p.occurrences}×`),
              el("div", { class: "sc-muted" }, p.signature),
              p.control ? el("p", {}, `Stood for by ${p.control}.`) : el("div", { class: "sc-actions" },
                el("button", { type: "button", class: "sc-small", onclick: () => chat(`Make a control for the ${p.kind} pattern '${p.signature}' of ${state.project}, seen on ${p.pages} page(s) ${p.occurrences} times, e.g. node #${p.examples[0].node} of ${p.examples[0].page}.`) }, "Make a control")),
              ...p.examples.map(x => el("div", { class: "sc-item", onclick: async () => { state.tab = "pages"; await show(); const page = state.pages.find(pg => pg.key === x.page); if (page) await openCapture(page.key, page.captures[0], x.node); } }, `${x.page}  ${x.line}`)));
          }

          async function loadCode() {
            const classes = await answer("controls", { project: state.project }) || [];
            const left = $("sc-left");
            left.replaceChildren(...(classes.length ? [] : [el("p", { class: "sc-muted" }, "No page objects or controls yet.")]));
            for (const c of classes) {
              left.append(el("div", { class: "sc-item", title: c.file, onclick: () => host.post({ type: "open", path: c.file }) },
                el("span", { class: "sc-kind" }, c.kind), c.name, el("span", { class: "sc-muted" }, c.header ? (c.header.page ? `  ${c.header.page}` : "  scraped") : "")));
            }
          }

          function chat(prompt) { host.post({ type: "chat", prompt: `/brinell-scrape-pages ${prompt}` }); }

          $("sc-project").addEventListener("change", async () => { state.project = $("sc-project").value; state.page = state.capture = null; await show(); });
          for (const tab of document.querySelectorAll("[data-tab]")) tab.addEventListener("click", async () => { state.tab = tab.getAttribute("data-tab"); await show(); });
          $("sc-start").addEventListener("click", async () => {
            $("sc-start").disabled = true;
            const started = await answer("start", { project: state.project, session: $("sc-session").value || "recording", url: $("sc-url").value || null });
            if (!started) $("sc-start").disabled = false;
          });
          $("sc-capture").addEventListener("click", () => answer("capture", {}));
          $("sc-stop").addEventListener("click", () => answer("stop"));
          $("sc-pick").addEventListener("click", async () => {
            $("sc-status").textContent = "Click an element in the browser (Escape cancels)…";
            const picked = await answer("pick");
            showRecording();
            if (!picked || !picked.capture || !picked.node) return;
            state.tab = "pages";
            await show();
            const page = state.pages.find(p => p.captures.some(c => c.session === state.recording.session && c.number === picked.capture.number));
            const capture = page && page.captures.find(c => c.session === state.recording.session && c.number === picked.capture.number);
            if (capture) { state.live = { session: capture.session, number: capture.number }; await openCapture(page.key, capture, picked.node); }
          });
          $("sc-generate").addEventListener("click", () => chat(`Make page objects and controls for the recorded pages of ${state.project}.`));
          host.post({ type: "ready" });
          loadProjects();
        })();
        </script>
        </body>
        </html>
        """;
}
