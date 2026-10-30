// Lets the user click an element in this frame: outlines what the mouse is over, and resolves with the
// number, in this frame's last capture, of the element clicked (or of its nearest captured ancestor), or
// 0 on Escape or when window.__bruinetPickCancel() is called. The click does not reach the page.
() => new Promise(resolve => {
    const nodes = window.__bruinetScrapeNodes || [];
    const numberOf = el => {
        for (let e = el; e; e = e.parentElement || (e.getRootNode && e.getRootNode().host)) {
            const i = nodes.indexOf(e);
            if (i >= 0) return i + 1;
        }
        return 0;
    };
    let box = document.createElement('div');
    box.id = '__bruinet_highlight';
    box.style.cssText = 'position:fixed;pointer-events:none;z-index:2147483647;border:2px dashed #2563eb;background:rgba(37,99,235,.10)';
    document.documentElement.appendChild(box);
    const over = ev => {
        const el = ev.composedPath()[0];
        if (!el || !el.getBoundingClientRect) return;
        const r = el.getBoundingClientRect();
        Object.assign(box.style, { left: r.x + 'px', top: r.y + 'px', width: r.width + 'px', height: r.height + 'px' });
    };
    const stop = ev => { ev.preventDefault(); ev.stopImmediatePropagation(); };
    const finish = n => {
        document.removeEventListener('mousemove', over, true);
        for (const type of ['click', 'mousedown', 'mouseup', 'pointerdown', 'pointerup']) document.removeEventListener(type, handlers[type], true);
        document.removeEventListener('keydown', key, true);
        box.remove();
        window.__bruinetPickCancel = undefined;
        resolve(n);
    };
    const handlers = {
        click: ev => { stop(ev); finish(numberOf(ev.composedPath()[0])); },
        mousedown: stop, mouseup: stop, pointerdown: stop, pointerup: stop,
    };
    const key = ev => { if (ev.key === 'Escape') { stop(ev); finish(0); } };
    document.addEventListener('mousemove', over, true);
    for (const type of Object.keys(handlers)) document.addEventListener(type, handlers[type], true);
    document.addEventListener('keydown', key, true);
    window.__bruinetPickCancel = () => finish(0);
})
