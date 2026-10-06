// The page's busy signal for Brinell: <body data-busy="true|false"> (html plan, design 7.2).
// ViewModelPage calls this after a render in which its view model's IsBusy changed.
window.brinellTodo = {
    setBusy: function (busy) {
        document.body.dataset.busy = busy ? "true" : "false";
    }
};
