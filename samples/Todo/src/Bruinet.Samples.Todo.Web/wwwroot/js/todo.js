// The page's busy signal for Bruinet: <body data-busy="true|false"> (html plan, design 7.2).
// ViewModelPage calls this after a render in which its view model's IsBusy changed.
window.bruinetTodo = {
    setBusy: function (busy) {
        document.body.dataset.busy = busy ? "true" : "false";
    }
};
