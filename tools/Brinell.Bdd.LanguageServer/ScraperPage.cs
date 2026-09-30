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
        .sc-form { display: grid; grid-template-columns: max-content 1fr; gap: 0.3em 0.6em; align-items: center; margin: 0.4em 0; }
        .sc-form input[type=number] { width: 6em; }
        .sc-row { display: flex; gap: 0.4em; align-items: center; padding: 0.1em 0; }
        .sc-row .sc-grow { flex: 1; overflow-wrap: anywhere; }
        pre.sc-summary { font-family: var(--uat-mono); font-size: 0.85em; white-space: pre-wrap; border: 1px solid var(--uat-border); border-radius: 4px; padding: 0.4em; }
        .sc-refused { color: var(--uat-failed); }
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
          <button id="sc-continue" type="button" hidden title="Signed in in the browser: go on">Continue</button>
          <select id="sc-here" aria-label="Add this page to the config" hidden>
            <option value="">This page…</option>
            <option value="host">Add its host to Explore hosts</option>
            <option value="alias">Add it as an alias</option>
            <option value="start">Use it as start URL</option>
            <option value="never">Never visit it</option>
          </select>
          <span id="sc-status" class="sc-status"></span>
        </div>
        <div id="sc-problem" class="sc-problem" hidden></div>
        <nav class="sc-tabs" role="tablist">
          <button type="button" role="tab" data-tab="pages" aria-selected="true">Pages</button>
          <button type="button" role="tab" data-tab="patterns" aria-selected="false">Patterns</button>
          <button type="button" role="tab" data-tab="code" aria-selected="false">Code</button>
          <button type="button" role="tab" data-tab="explore" aria-selected="false">Explore</button>
          <button type="button" role="tab" data-tab="settings" aria-selected="false">Explore settings</button>
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

          const state = { project: null, tab: "pages", recording: null, pages: [], page: null, capture: null, live: null, node: null, exploring: null, explored: null, sessions: [] };

          host.on(message => {
            if (message.type === "result") { const done = waiting.get(message.id); waiting.delete(message.id); if (done) done(message.value); return; }
            if (message.type === "theme") {
              for (const [name, value] of Object.entries(message.vars || {})) document.documentElement.style.setProperty(name, value);
              if (message.kind) document.documentElement.setAttribute("data-theme", message.kind);
              return;
            }
            if (message.type === "changed") { onChanged(message); }
          });

          // A recording's state only moves forward: the server's notices can arrive out of order (a
          // capture's before the start's), and the start's answer can come after both.
          const stopped = new Set();
          function setRecording(recording) {
            if (recording && stopped.has(recording.session)) return;
            const current = state.recording;
            if (recording && current && recording.session === current.session && recording.captures < current.captures) return;
            state.recording = recording;
          }

          async function onChanged(change) {
            if (change.kind === "Exploring" || change.kind === "SignIn" || change.kind === "Explored") {
              onExplore(change);
              if (change.kind === "Explored") await loadPages();
              return;
            }
            if (change.kind === "Stopped") { if (change.recording) stopped.add(change.recording.session); state.recording = null; } else setRecording(change.recording);
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
            $("sc-status").textContent = state.exploring ? state.exploring : r ? `Recording ${r.session}: ${r.captures} page(s)` : "";
            $("sc-status").setAttribute("data-recording", String(!!r));
            $("sc-here").hidden = !r;
            if (state.exploring) { $("sc-capture").disabled = $("sc-pick").disabled = true; $("sc-stop").disabled = false; }
          }

          async function loadProjects() {
            const projects = await call("projects") || [];
            const picker = $("sc-project");
            picker.replaceChildren(...projects.map(p => new Option(p.name, p.name, false, p.name === state.project)));
            if (projects.length === 0) {
              showProblem("No project here has a scraper.config.md yet.");
              await showCreateConfig();
              return;
            }
            state.project = picker.value;
            state.sessions = (projects.find(p => p.name === state.project) || {}).sessions || [];
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
            else if (state.tab === "explore") await loadExplore();
            else if (state.tab === "settings") await loadSettings();
            else await loadCode();
          }

          async function loadPages() {
            if (state.tab !== "pages") return;
            state.pages = await answer("pages", { project: state.project }) || [];
            const left = $("sc-left");
            left.replaceChildren(...(state.pages.length ? [] : [el("p", { class: "sc-muted" }, "Nothing recorded yet: Record opens a browser; each page you go to is captured. Explore goes through the app by itself.")]));
            if (state.pages.length) left.append(el("div", { class: "sc-actions" },
              el("button", { type: "button", class: "sc-small", title: "A page object per recorded page, from the suggestions: Name.scraped.cs, built, or put back", onclick: () => draft(null) }, "Draft page objects"),
              el("button", { type: "button", class: "sc-small", title: "Look up every page object member in the running app", onclick: verify }, "Verify"),
              el("button", { type: "button", class: "sc-small", title: "Ask the agent to make controls and better names for the drafts", onclick: improve }, "Improve with the agent")));
            for (const page of state.pages) {
              const made = page.pageObject
                ? el("span", { class: "sc-muted", title: page.changedSinceGenerated ? "The page changed since its page object was made: Changes shows what" : "Its page object" }, page.changedSinceGenerated ? `  ${page.pageObject}: changed` : `  ${page.pageObject}`)
                : null;
              left.append(el("div", { class: "sc-item", title: page.location, onclick: () => openCapture(page.key, page.captures[0]) },
                page.key, el("span", { class: "sc-muted" }, ` ${page.name}`), made));
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
            const text = await answer("outline", { project: state.project, page: key, session: capture.session, number: capture.number, maxLines: 2000, focus: node || 0 });
            if (text === undefined) return;
            const page = state.pages.find(p => p.key === key);
            const older = page ? page.captures.filter(c => c !== capture) : [];
            const live = isLive(capture);
            const right = $("sc-right");
            const actions = el("div", { class: "sc-actions" },
              el("button", { type: "button", class: "sc-small", onclick: () => host.post({ type: "open", path: capture.file }) }, "Open file"),
              older.length ? el("button", { type: "button", class: "sc-small", title: "Compare with the capture before it", onclick: () => host.post({ type: "diff", left: older[0].file, right: capture.file, title: `${key}: ${older[0].session} #${older[0].number} ↔ ${capture.session} #${capture.number}` }) }, "Compare") : null,
              el("button", { type: "button", class: "sc-small", onclick: () => chat(`Make or update the page object for the recorded page ${key} of ${state.project}.`) }, "Generate page object"),
              el("button", { type: "button", class: "sc-small", title: "Draft this page's page object from the suggestions, no agent", onclick: () => draft(key) }, "Draft"),
              page && (page.pageObject || older.length) ? el("button", { type: "button", class: "sc-small", title: "What changed: since the page object was made, or since the capture before", onclick: () => showDiff(key) }, "Changes") : null,
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
            const suggested = (await answer("suggest", { project: state.project, page: state.page, node: number }) || []).find(s => s.node === number);
            $("sc-node").replaceChildren(el("div", { class: "sc-node" },
              el("div", {}, current ? current.textContent.trim() : `#${number}`),
              el("div", { class: "sc-muted" }, `The same kind of node: ${similar.length} on ${new Set(similar.map(s => s.page)).size} page(s).`),
              suggested ? el("div", {}, el("code", {}, `${suggested.control}<…> ${suggested.name} => new(${suggested.scope || "this"}, ${suggested.locator});`), el("span", { class: "sc-muted" }, `  by ${suggested.why}`)) : null,
              el("div", { class: "sc-actions" },
                el("button", { type: "button", class: "sc-small", onclick: () => chat(`Make a control for the node #${number} of the recorded page ${state.page} of ${state.project} (it occurs ${similar.length} times). Its header: ${header || ""}`) }, "Make a control"),
                el("button", { type: "button", class: "sc-small", title: "Explore never clicks it", onclick: () => nodeMatch(number, "Never click") }, "Never click this"),
                el("button", { type: "button", class: "sc-small", title: "Explore clicks it whatever its words say", onclick: () => nodeMatch(number, "Always click") }, "Always click this"))));
          }

          async function showDiff(key) {
            const diff = await answer("diff", { project: state.project, page: key });
            if (!diff) return;
            const list = (title, lines) => lines.length ? [el("h4", {}, title), el("pre", { class: "sc-outline" }, lines.join("\n"))] : [];
            $("sc-node").replaceChildren(el("div", { class: "sc-node" },
              el("div", {}, `${diff.addedCount} added, ${diff.removedCount} removed, ${diff.changedCount} changed`),
              ...list("Added", diff.added),
              ...list("Removed", diff.removed),
              ...list("Changed", diff.changed.map(c => `${c.before}\n  → ${c.after}`)),
              el("div", { class: "sc-actions" }, el("button", { type: "button", class: "sc-small", onclick: () => chat(`The recorded page ${key} of ${state.project} changed since its page object was made: update the page object (diff_page).`) }, "Update page object"))));
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
              ...p.examples.map(x => el("div", { class: "sc-item", onclick: async () => { state.tab = "pages"; state.capture = null; await show(); const page = state.pages.find(pg => pg.key === x.page); if (page) await openCapture(page.key, page.captures[0], x.node); } }, `${x.page}  ${x.line}`)));
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

          function improve() {
            chat(`Improve the drafted page objects (*.scraped.cs) of ${state.project}: make controls for the repeated patterns (detect_patterns), give members better names, drop members a test would not use; check_code until it passes.`);
          }

          // One change to the config; the sentence the server gives says what it wrote.
          async function edit(action, section, values) {
            const said = await answer("configEdit", { project: state.project, action, section, values });
            if (said === undefined) return false;
            $("sc-status").textContent = said;
            if (state.tab === "settings") await loadSettings();
            return true;
          }

          async function nodeMatch(number, section) {
            const match = await answer("elementMatch", { project: state.project, page: state.page, node: number });
            if (match) await edit("add", section, [match]);
          }

          async function showCreateConfig() {
            const folders = await call("configCandidates") || [];
            const url = el("input", { placeholder: "https://localhost:5001/", "aria-label": "Start URL" });
            const picker = el("select", { "aria-label": "Project" }, ...folders.map(f => new Option(f, f)));
            const create = async () => {
              if (await answer("createConfig", { folder: picker.value, startUrl: url.value })) { showProblem(null); await loadProjects(); }
            };
            $("sc-right").replaceChildren(el("h3", {}, "Create config"),
              folders.length
                ? el("div", {},
                    el("p", {}, "Write a scraper.config.md into a UI test project: the Scraper records into it and drafts its page objects there."),
                    el("div", { class: "sc-form" }, el("span", {}, "Project"), picker, el("span", {}, "Start URL"), url),
                    el("button", { type: "button", class: "sc-small", onclick: create }, "Create config"))
                : el("p", { class: "sc-muted" }, "No UI test project here (a *Tests.csproj) to write one into."));
          }

          // Explore: the summary of host, guards and budget first; Start only once it is shown.
          function exploreParams() {
            const n = id => { const v = $(id) && $(id).value; return v ? Number(v) : null; };
            return { project: state.project, dryRun: $("sc-dry") ? $("sc-dry").checked : true, maxPages: n("sc-max-pages"), maxDepth: n("sc-max-depth"), maxMinutes: n("sc-max-minutes"), url: $("sc-url").value || null };
          }

          async function loadExplore() {
            const config = await answer("config", { project: state.project });
            if (!config) return;
            // A project's first run is a dry run unless the person says otherwise.
            const first = !state.sessions.some(s => /-explore(-\d+)?$/.test(s));
            const number = (id, value, label) => [el("label", { for: id }, label), el("input", { id, type: "number", min: "1", placeholder: String(value) })];
            const summary = el("pre", { class: "sc-summary", id: "sc-summary" });
            const risky = el("label", { hidden: "" }, el("input", { type: "checkbox", id: "sc-risky" }), " This is a throwaway environment: ClickPolicy all clicks everything, submits too.");
            const start = el("button", { type: "button", id: "sc-explore-start", onclick: startExplore }, "Start Explore");
            const form = el("div", { class: "sc-form" },
              el("label", { for: "sc-dry" }, "Dry run"), el("span", {}, el("input", { id: "sc-dry", type: "checkbox" }), el("span", { class: "sc-muted" }, " links only, nothing clicked")),
              ...number("sc-max-pages", config.maxPages, "Max pages"), ...number("sc-max-depth", config.maxDepth, "Max depth"), ...number("sc-max-minutes", config.maxMinutes, "Max minutes"));
            $("sc-left").replaceChildren(el("h3", {}, "Explore"),
              el("p", { class: "sc-muted" }, "Goes through the app by itself and captures each page and state, within the guards below (Explore settings). The browser shows it; a sign-in is yours: it stops and waits for Continue."),
              form, summary, risky, el("div", { class: "sc-actions" }, start));
            $("sc-dry").checked = first;
            // ClickPolicy all asks a second time: the box must be ticked too.
            const refresh = async () => {
              const text = await answer("exploreSummary", exploreParams()) || "";
              summary.textContent = text;
              summary.classList.toggle("sc-refused", text.includes("REFUSED"));
              risky.hidden = !text.includes("ClickPolicy all");
              start.disabled = !!state.exploring || text.includes("REFUSED") || (!risky.hidden && !$("sc-risky").checked);
            };
            for (const input of form.querySelectorAll("input")) input.addEventListener("change", refresh);
            $("sc-risky").addEventListener("change", refresh);
            await refresh();
            showExplored();
          }

          async function startExplore() {
            const started = await answer("explore", exploreParams());
            if (!started) return;
            state.exploring = "Explore starting…";
            $("sc-explore-start").disabled = true;
            showRecording();
          }

          function onExplore(change) {
            if (change.kind === "Exploring") {
              const p = change.progress || {};
              state.exploring = `Exploring: ${p.captures} captured, ${p.clicks} clicks, ${p.skipped} skipped, ${p.blocked} blocked. ${p.doing || ""}`;
              if (change.recording) setRecording(change.recording);
            } else if (change.kind === "SignIn") {
              state.exploring = "Explore waits: sign in in the browser, then Continue.";
              $("sc-continue").hidden = false;
            } else {
              state.exploring = null;
              state.explored = change.explored;
              $("sc-continue").hidden = true;
              if (change.explored && change.explored.folder) state.sessions.push(change.explored.folder.split(/[\\/]/).pop());
              if (state.tab === "explore") loadExplore();
            }
            showRecording();
            if (change.kind === "Explored") $("sc-status").textContent = change.message || "";
          }

          // The last run: what it captured, skipped and blocked, with the config's actions on each line.
          function showExplored() {
            const r = state.explored;
            const right = $("sc-right");
            if (!r) { right.replaceChildren(el("p", { class: "sc-muted" }, "No run yet in this view. A run's report is explore.md in its session.")); return; }
            const section = (title, rows) => rows.length ? [el("h4", {}, `${title} (${rows.length})`), ...rows] : [];
            right.replaceChildren(
              el("h3", {}, `${r.stop}${r.dryRun ? " (dry run)" : ""}`),
              el("p", {}, r.stopDetail),
              el("div", { class: "sc-muted" }, `${r.captures.length} captures, ${r.clicks} clicks, ${r.skipped.length} skipped, ${r.blocked.length} blocked`),
              el("div", { class: "sc-actions" },
                r.report ? el("button", { type: "button", class: "sc-small", onclick: () => host.post({ type: "open", path: r.report }) }, "Open explore.md") : null,
                r.captures.length ? el("button", { type: "button", class: "sc-small", onclick: () => draft(null) }, "Draft page objects") : null),
              ...section("Captures", r.captures.map(c => el("div", { class: "sc-row" }, el("span", { class: "sc-grow" }, `${c.page}  `, el("span", { class: "sc-muted" }, c.via))))),
              ...section("Would click", (r.wouldClick || []).map(w => el("div", { class: "sc-row" }, el("span", { class: "sc-grow" }, `${w.page}: ${w.label} `, el("span", { class: "sc-muted" }, w.reason))))),
              ...section("Skipped", r.skipped.map(k => el("div", { class: "sc-row" },
                el("span", { class: "sc-grow" }, `${k.page}: ${k.label} `, el("span", { class: "sc-muted" }, `${String(k.verdict).toLowerCase()}, ${k.reason}`)),
                el("button", { type: "button", class: "sc-small", title: "Explore clicks it whatever its words say", onclick: () => edit("add", "Always click", [`text: ${k.label}`]) }, "Always click this"),
                el("button", { type: "button", class: "sc-small", title: "Explore never clicks it", onclick: () => edit("add", "Never click", [`text: ${k.label}`]) }, "Never click this")))),
              ...section("Blocked requests", r.blocked.map(b => el("div", { class: "sc-row" },
                el("span", { class: "sc-grow" }, `${b.method} ${b.url} `, el("span", { class: "sc-muted" }, b.reason)),
                el("button", { type: "button", class: "sc-small", title: "Let this request through while exploring", onclick: () => edit("add", "Allow requests", [b.method, new URL(b.url).pathname]) }, "Allow this request")))),
              ...section("Notes", (r.notes || []).map(n => el("div", {}, n))));
          }

          async function draft(page) {
            $("sc-status").textContent = page ? `Drafting ${page}…` : "Drafting page objects and building…";
            const result = await answer("draft", { project: state.project, page });
            $("sc-status").textContent = "";
            if (!result) return;
            state.tab = "code";
            await show();
            const build = result.build;
            $("sc-right").replaceChildren(el("h3", {}, "Drafts"),
              el("p", {}, build ? (build.succeeded ? "The project builds with them." : "The project does not build: see the drafts put back.") : "Nothing new to write."),
              ...result.pages.map(p => el("div", { class: "sc-node" },
                el("div", {}, el("span", { class: "sc-kind" }, p.outcome), `${p.className} `, el("span", { class: "sc-muted" }, `${p.key}, ${p.members} members, ${p.unstable.length} left out`)),
                p.reason ? el("div", { class: "sc-muted" }, p.reason) : null,
                ...p.removed.map(m => el("div", {}, `Removed ${m.name}, last drafted from ${m.lastSeenIn || "?"}`)),
                ...p.problems.slice(0, 5).map(x => el("div", { class: "sc-refused" }, `line ${x.line}: ${x.code} ${x.message}`)))),
              result.makeControls.length
                ? el("div", {}, el("h4", {}, "Make controls of these (the agent, or you)"), ...result.makeControls.map(m => el("div", { class: "sc-muted" }, `${m.kind}: ${m.signature} (${m.occurrences}× on ${m.pages} pages)`)))
                : null,
              el("div", { class: "sc-actions" }, el("button", { type: "button", class: "sc-small", onclick: improve }, "Improve with the agent")));
          }

          async function verify() {
            $("sc-status").textContent = "Verifying the page objects in the running app…";
            const result = await answer("verify", { project: state.project });
            $("sc-status").textContent = "";
            if (!result) return;
            const passed = result.pages.every(p => !p.problem && p.members.every(m => m.verdict === "Once" || m.verdict === "Unreadable"));
            $("sc-right").replaceChildren(el("h3", {}, passed ? "Every member found once" : "Not every member found once"),
              ...result.pages.map(p => el("div", { class: "sc-node" },
                el("div", {}, `${p.className} `, el("span", { class: "sc-muted" }, p.key)),
                p.problem ? el("div", { class: "sc-refused" }, p.problem) : null,
                ...p.members.filter(m => m.verdict !== "Once").map(m => el("div", { class: m.verdict === "Unreadable" ? "sc-muted" : "sc-refused" },
                  `${m.name}: ${m.locator}, ${m.verdict === "Several" ? `found ${m.count} times` : m.verdict === "NotFound" ? "not found" : "not read"}`)),
                el("div", { class: "sc-muted" }, `${p.members.filter(m => m.verdict === "Once").length} of ${p.members.length} found once`))));
          }

          // The Explore settings: each change is one edit of scraper.config.md.
          async function loadSettings() {
            const c = await answer("config", { project: state.project });
            if (!c) return;
            const field = (name, value, type) => {
              const input = el("input", { type: type || "text", value: String(value), "aria-label": name });
              input.addEventListener("change", () => edit("set", "Explore", [name, input.value]));
              return [el("label", {}, name), input];
            };
            const choice = (name, value, options) => {
              const select = el("select", { "aria-label": name }, ...options.map(o => new Option(o, o, false, o === value)));
              select.addEventListener("change", () => edit("set", "Explore", [name, select.value]));
              return [el("label", {}, name), select];
            };
            const methods = ["POST", "PUT", "PATCH", "DELETE"].map(m => {
              const box = el("input", { type: "checkbox", "data-method": m });
              box.checked = c.blockMethods.includes(m);
              box.addEventListener("change", () => edit("set", "Explore", ["BlockMethods", [...document.querySelectorAll("[data-method]")].filter(b => b.checked).map(b => b.getAttribute("data-method")).join(", ")]));
              return el("label", {}, box, ` ${m} `);
            });
            const words = Object.entries(c.dangerWords).map(([category, on]) => {
              const box = el("input", { type: "checkbox" });
              box.checked = on;
              box.addEventListener("change", () => edit("set", "Danger words", [category, String(box.checked)]));
              return el("label", { title: on ? "Its words keep Explore from clicking" : "Its words are let through" }, box, ` ${category} `);
            });
            $("sc-left").replaceChildren(el("h3", {}, "Explore settings"),
              el("div", { class: "sc-actions" }, el("button", { type: "button", class: "sc-small", onclick: () => host.post({ type: "open", path: c.path }) }, "Open scraper.config.md")),
              el("h4", {}, "Budget"),
              el("div", { class: "sc-form" }, ...field("MaxPages", c.maxPages, "number"), ...field("MaxDepth", c.maxDepth, "number"), ...field("MaxMinutes", c.maxMinutes, "number"), ...field("DelayMs", c.delayMs, "number"), ...field("AgentMinutes", c.agentMinutes, "number"), ...field("LoginUrl", c.loginUrl)),
              el("h4", {}, "Guards"),
              el("div", { class: "sc-form" },
                ...choice("ClickPolicy", c.clickPolicy, ["links", "safe", "not-unsafe", "all"]),
                el("label", {}, "BlockMethods"), el("span", {}, ...methods),
                ...choice("WebsocketApps", c.websocketApps, ["links", "warn", "ignore"])),
              el("h4", {}, "Danger words"), el("div", {}, ...words));
            const list = (title, rows, hint) => {
              const input = el("input", { placeholder: hint, "aria-label": `Add to ${title}` });
              const add = () => {
                const value = input.value.trim();
                if (!value) return;
                // Allow requests takes a method and a URL: "POST /graphql".
                const pair = /^(\w+)\s+(.+)$/.exec(value);
                edit("add", title, title === "Allow requests" ? (pair ? [pair[1].toUpperCase(), pair[2]] : ["", value]) : [value]);
              };
              return [el("h4", {}, title),
                ...rows.map(row => el("div", { class: "sc-row" }, el("span", { class: "sc-grow" }, row),
                  el("button", { type: "button", class: "sc-small", title: "Remove", onclick: () => edit("remove", title, [title === "Allow requests" ? row.split(" ")[0] : row]) }, "×"))),
                el("div", { class: "sc-row" }, input, el("button", { type: "button", class: "sc-small", onclick: add }, "Add"))];
            };
            $("sc-right").replaceChildren(
              ...list("Explore hosts", c.hosts, "test.example.com"),
              ...list("Never click", c.neverClick, "text: Empty cache, or selector: [data-testid=x]"),
              ...list("Always click", c.alwaysClick, "text: Filters, or selector: ..."),
              ...list("Never visit", c.neverVisit, "/admin"),
              ...list("Allow requests", c.allowRequests, "POST /graphql"),
              ...list("Aliases", c.aliases, "https://login.example.com/"));
          }

          $("sc-project").addEventListener("change", async () => { state.project = $("sc-project").value; state.page = state.capture = null; await show(); });
          for (const tab of document.querySelectorAll("[data-tab]")) tab.addEventListener("click", async () => { state.tab = tab.getAttribute("data-tab"); await show(); });
          $("sc-start").addEventListener("click", async () => {
            $("sc-start").disabled = true;
            const started = await answer("start", { project: state.project, session: $("sc-session").value || "recording", url: $("sc-url").value || null });
            if (!started) { $("sc-start").disabled = false; return; }
            setRecording(started);
            showRecording();
          });
          $("sc-capture").addEventListener("click", () => answer("capture", {}));
          $("sc-stop").addEventListener("click", () => state.exploring ? call("exploreStop") : answer("stop"));
          $("sc-continue").addEventListener("click", async () => { $("sc-continue").hidden = true; await call("exploreContinue"); });
          $("sc-here").addEventListener("change", async () => {
            const what = $("sc-here").value;
            $("sc-here").value = "";
            const location = await call("location");
            if (!what || !location) return;
            const url = new URL(location);
            if (what === "host") await edit("add", "Explore hosts", [url.hostname]);
            else if (what === "alias") await edit("add", "Aliases", [url.origin + "/"]);
            else if (what === "start") await edit("set", "App", ["StartUrl", location]);
            else await edit("add", "Never visit", [url.pathname]);
          });
          $("sc-pick").addEventListener("click", async () => {
            $("sc-status").textContent = "Click an element in the browser (Escape cancels)…";
            const picked = await answer("pick");
            showRecording();
            if (!picked || !picked.capture || !picked.node) return;
            // Not the capture open before: loadPages would open that one again, after this one.
            state.tab = "pages";
            state.capture = null;
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
