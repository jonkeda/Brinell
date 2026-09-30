// The preview's page script, the same in every host: Visual Studio's WebView2 or VS Code's webview.
// In:  update {html, suites, suite, bindState, reason}, scrollTo {line}, theme {vars, kind}, running {line?, file?}, breaks {lines},
//      paused {text, line, here, isCall, depth, calls} (or {} when the run goes on)
// Out: reveal {line}, pickSuite {suite}, openScreenshot {path}, run {line, debug}, runFile {debug}, cancel, toggleBreak {line},
//      debug {move: continue | over | into | out | stop}, deleteResults {scope: scenario | file, line?}
(function () {
  "use strict";

  const host = (function () {
    if (window.chrome && window.chrome.webview) {
      return {
        post: (message) => window.chrome.webview.postMessage(message),
        listen: (handler) => window.chrome.webview.addEventListener("message", (event) => handler(event.data)),
      };
    }
    if (typeof acquireVsCodeApi === "function") {
      const api = acquireVsCodeApi();
      return {
        post: (message) => api.postMessage(message),
        listen: (handler) => window.addEventListener("message", (event) => handler(event.data)),
      };
    }
    // No host (a page opened on its own): messages go to the console, and can be sent with postMessage.
    return {
      post: (message) => console.log("preview ->", JSON.stringify(message)),
      listen: (handler) => window.addEventListener("message", (event) => handler(event.data)),
    };
  })();

  const root = document.getElementById("uat-root");
  const picker = document.getElementById("uat-suite");
  const state = document.getElementById("uat-state");
  const cancel = document.getElementById("uat-cancel");
  const deleteFile = document.getElementById("uat-delete-file");
  const pausedBar = document.getElementById("uat-paused");
  const pausedAt = document.getElementById("uat-paused-at");
  let current = null;
  let running = null;
  let breaks = [];
  let paused = null;

  // The block for a line: the last element whose data-line is at or before it.
  function blockAt(line) {
    let best = null;
    for (const element of root.querySelectorAll("[data-line]")) {
      const at = Number(element.getAttribute("data-line"));
      if (at <= line && (best === null || at >= Number(best.getAttribute("data-line")))) {
        best = element;
      }
    }
    return best;
  }

  function markCurrent(element) {
    if (current) {
      current.removeAttribute("data-current");
    }
    current = element && element.closest(".uat-step") ? element.closest(".uat-step") : element;
    if (current) {
      current.setAttribute("data-current", "");
    }
  }

  function update(message) {
    // Replacing the content keeps the scroll position; open callables and rows stay open by line.
    const open = Array.from(root.querySelectorAll("details[open]")).map((details) => details.closest("[data-line]")?.getAttribute("data-line") + "|" + details.className);
    const top = window.scrollY;
    root.innerHTML = message.html;
    for (const details of root.querySelectorAll("details")) {
      if (open.includes(details.closest("[data-line]")?.getAttribute("data-line") + "|" + details.className)) {
        details.open = true;
      }
    }
    window.scrollTo(0, top);
    showRunning();
    showBreaks();
    showPaused();
    // The file's results can be deleted when there are any, and a host to do it.
    deleteFile.hidden = !root.querySelector(".uat-delete");

    const suites = message.suites || [];
    picker.hidden = suites.length < 2;
    picker.replaceChildren(...suites.map((suite) => new Option(suite, suite, false, suite === message.suite)));
    state.textContent = message.bindState === "bound" ? "Bound in " + message.suite : "Parsed only";
    state.title = message.reason || "";
    state.setAttribute("data-state", message.bindState || "parse-only");
  }

  function scrollTo(line) {
    const element = blockAt(line);
    if (element) {
      markCurrent(element);
      element.scrollIntoView({ block: "nearest" });
    }
  }

  // While a run goes, the Run buttons hide, the running scenario says so, and the bar offers Cancel.
  function showRunning() {
    document.body.toggleAttribute("data-running", running !== null);
    cancel.hidden = running === null;
    for (const heading of root.querySelectorAll("[data-running-here]")) {
      heading.removeAttribute("data-running-here");
    }
    if (running) {
      const target = running.file ? root.querySelector(".uat-title") : root.querySelector(`.uat-scenario[data-line="${running.line}"]`);
      target?.setAttribute("data-running-here", "");
    }
  }

  function showBreaks() {
    for (const step of root.querySelectorAll(".uat-step[data-line]")) {
      step.toggleAttribute("data-break", breaks.includes(Number(step.getAttribute("data-line"))));
    }
  }

  // A debugged run paused at a step: the bar says where and offers the moves; the step is marked
  // when it is in this file. Step into only at a call; Step out only inside one.
  function showPaused() {
    pausedBar.hidden = paused === null;
    for (const element of root.querySelectorAll("[data-paused]")) {
      element.removeAttribute("data-paused");
    }
    if (paused === null) {
      return;
    }
    const inCall = paused.calls && paused.calls.length ? " · in " + paused.calls[paused.calls.length - 1] : "";
    pausedAt.textContent = "⏸ Paused " + (paused.isCall ? "at the call: " : "before: ") + paused.text + " (line " + paused.line + inCall + ")";
    pausedBar.querySelector('[data-move="into"]').disabled = !paused.isCall;
    pausedBar.querySelector('[data-move="out"]').disabled = !(paused.depth > 0);
    if (paused.here) {
      const step = root.querySelector(`.uat-step[data-line="${paused.line}"]`) || blockAt(paused.line);
      if (step) {
        step.setAttribute("data-paused", "");
        // Centred: the sticky bars would hide a step scrolled only to the edge.
        step.scrollIntoView({ block: "center" });
      }
    }
  }

  function theme(message) {
    for (const [name, value] of Object.entries(message.vars || {})) {
      document.documentElement.style.setProperty(name, value);
    }
    if (message.kind) {
      document.documentElement.setAttribute("data-theme", message.kind);
    }
  }

  host.listen((message) => {
    switch (message && message.type) {
      case "update": update(message); break;
      case "scrollTo": scrollTo(message.line); break;
      case "theme": theme(message); break;
      case "running": running = message.line || message.file ? { line: message.line, file: message.file } : null; showRunning(); break;
      case "breaks": breaks = message.lines || []; showBreaks(); break;
      case "paused": paused = message.text ? message : null; showPaused(); break;
    }
  });

  pausedBar.addEventListener("click", (event) => {
    const move = event.target.closest("[data-move]");
    if (move && !move.disabled) {
      host.post({ type: "debug", move: move.getAttribute("data-move") });
    }
  });

  root.addEventListener("click", (event) => {
    const remove = event.target.closest("[data-delete]");
    if (remove) {
      event.stopPropagation();
      host.post({ type: "deleteResults", scope: "scenario", line: Number(remove.getAttribute("data-delete")) });
      return;
    }
    const record = event.target.closest("[data-record], [data-record-file]");
    if (record) {
      event.stopPropagation();
      if (!record.disabled) {
        host.post(record.hasAttribute("data-record-file") ? { type: "record" } : { type: "record", line: Number(record.getAttribute("data-record")) });
      }
      return;
    }
    const watch = event.target.closest("[data-watch]");
    if (watch) {
      event.preventDefault();
      host.post({ type: "watch", path: watch.getAttribute("data-watch") });
      return;
    }
    const run = event.target.closest(".uat-run");
    if (run) {
      event.stopPropagation();
      if (!run.disabled) {
        const debug = run.hasAttribute("data-debug");
        host.post(run.hasAttribute("data-run-file") ? { type: "runFile", debug } : { type: "run", line: Number(run.getAttribute("data-run")), debug });
      }
      return;
    }
    const gutter = event.target.closest(".uat-step[data-line] > .uat-kw");
    if (gutter) {
      host.post({ type: "toggleBreak", line: Number(gutter.parentElement.getAttribute("data-line")) });
      return;
    }
    const shot = event.target.closest("[data-open]");
    if (shot) {
      event.preventDefault();
      host.post({ type: "openScreenshot", path: shot.getAttribute("data-open") });
      return;
    }
    if (event.target.closest("summary, a, select")) {
      return;
    }
    const block = event.target.closest("[data-line]");
    if (block) {
      markCurrent(block);
      host.post({ type: "reveal", line: Number(block.getAttribute("data-line")) });
    }
  });

  picker.addEventListener("change", () => host.post({ type: "pickSuite", suite: picker.value }));
  cancel.addEventListener("click", () => host.post({ type: "cancel" }));
  deleteFile.addEventListener("click", () => host.post({ type: "deleteResults", scope: "file" }));

  host.post({ type: "ready" });
})();
