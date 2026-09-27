// The preview's page script, the same in every host: Visual Studio's WebView2 or VS Code's webview.
// In:  update {html, suites, suite, bindState, reason}, scrollTo {line}, theme {vars, kind}
// Out: reveal {line}, pickSuite {suite}, openScreenshot {path}
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
  let current = null;

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
    }
  });

  root.addEventListener("click", (event) => {
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

  host.post({ type: "ready" });
})();
