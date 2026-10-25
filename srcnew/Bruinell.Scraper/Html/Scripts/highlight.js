// Outlines node `n` of this frame's last capture, or removes the outline when n is 0. The outline is
// skipped by the capture, and ignores the mouse.
(n) => {
    let box = document.getElementById('__bruinell_highlight');
    const target = n > 0 && window.__bruinellScrapeNodes ? window.__bruinellScrapeNodes[n - 1] : null;
    const el = target && target.host ? target.host : target; // a shadow root has no box; its host does
    if (!el || !el.getBoundingClientRect) {
        if (box) box.remove();
        return false;
    }
    if (!box) {
        box = document.createElement('div');
        box.id = '__bruinell_highlight';
        box.style.cssText = 'position:fixed;pointer-events:none;z-index:2147483647;border:2px solid #e11d48;background:rgba(225,29,72,.12);border-radius:2px';
        document.documentElement.appendChild(box);
    }
    el.scrollIntoView({ block: 'nearest', inline: 'nearest' });
    const r = el.getBoundingClientRect();
    Object.assign(box.style, { left: r.x + 'px', top: r.y + 'px', width: r.width + 'px', height: r.height + 'px' });
    return true;
}
