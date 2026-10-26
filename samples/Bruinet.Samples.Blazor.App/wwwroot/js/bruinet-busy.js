// The page-level busy signal for Bruinet (.my/html/sample-app.md item 3): <body data-busy>.
// The layout calls set(false) on its first interactive render, so the attribute's presence also
// says "the app is interactive": a prerendered page has none, and Blazor pages wait for it.
window.bruinetBusy = {
    set: function (busy) {
        document.body.dataset.busy = busy ? "true" : "false";
    }
};
