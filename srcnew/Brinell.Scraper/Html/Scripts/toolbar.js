// The test recording's toolbar, in the top frame only: Assert, Stop and a count of the actions recorded.
// Its buttons go to the host through window.__brinellCommand; the host asks it which check to make with
// window.__brinellChooseCheck(facts). It lives in a shadow root under #__brinell_toolbar, which a capture
// skips and the recorder ignores.
() => {
    if (window.top !== window || window.__brinellToolbar) return true;
    window.__brinellToolbar = true;
    const install = () => {
        if (document.getElementById('__brinell_toolbar')) return;
        const host = document.createElement('div');
        host.id = '__brinell_toolbar';
        host.style.cssText = 'position:fixed;right:16px;bottom:16px;z-index:2147483647';
        const root = host.attachShadow({ mode: 'open' });
        root.innerHTML = `
            <style>
              .bar { display:flex; gap:6px; align-items:center; font:13px system-ui,sans-serif; background:#1f2937; color:#f9fafb;
                     padding:6px 8px; border-radius:8px; box-shadow:0 2px 8px rgba(0,0,0,.3) }
              button { font:inherit; border:0; border-radius:6px; padding:4px 10px; cursor:pointer; background:#374151; color:#f9fafb }
              button:hover { background:#4b5563 }
              .stop { background:#b91c1c } .stop:hover { background:#dc2626 }
              .count { min-width:5ch; text-align:right; opacity:.8 }
              .checks { display:none; flex-direction:column; gap:4px; margin-bottom:6px; background:#1f2937; padding:6px; border-radius:8px }
              .checks.open { display:flex }
              .checks button { text-align:left; max-width:360px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap }
            </style>
            <div class="checks" part="checks"></div>
            <div class="bar">
              <button data-command="assert" title="Click an element, then choose what to check">Assert</button>
              <button data-command="stop" class="stop">Stop</button>
              <span class="count" data-count>0</span>
            </div>`;
        root.querySelectorAll('[data-command]').forEach(b => b.addEventListener('click', ev => {
            ev.preventDefault();
            ev.stopPropagation();
            if (window.__brinellCommand) window.__brinellCommand(b.dataset.command);
        }));
        document.documentElement.appendChild(host);
    };
    if (document.documentElement) install(); else document.addEventListener('DOMContentLoaded', install);

    const root = () => document.getElementById('__brinell_toolbar')?.shadowRoot;
    window.__brinellToolbarCount = n => {
        const count = root()?.querySelector('[data-count]');
        if (count) count.textContent = String(n);
        return true;
    };
    // Shows the checks the element allows, and resolves with the one chosen, or null on Cancel or Escape.
    window.__brinellChooseCheck = facts => new Promise(resolve => {
        install();
        const panel = root().querySelector('.checks');
        const short = t => t.length > 60 ? t.substring(0, 57) + '...' : t;
        const options = [['visible', 'Is visible']];
        if (facts.text) options.push(['text', `Has the text "${short(facts.text)}"`]);
        if (facts.value !== null && facts.value !== undefined) options.push(['value', `Has the value "${short(facts.value)}"`]);
        if (facts.checked !== null && facts.checked !== undefined) options.push(['checked', facts.checked ? 'Is checked' : 'Is not checked']);
        options.push(['page', 'Is on this page'], ['cancel', 'Cancel']);
        panel.replaceChildren(...options.map(([check, label]) => {
            const b = document.createElement('button');
            b.dataset.check = check;
            b.textContent = label;
            b.addEventListener('click', ev => { ev.preventDefault(); ev.stopPropagation(); done(check === 'cancel' ? null : check); });
            return b;
        }));
        panel.classList.add('open');
        const key = ev => { if (ev.key === 'Escape') { ev.preventDefault(); ev.stopPropagation(); done(null); } };
        document.addEventListener('keydown', key, true);
        window.__brinellChooseCancel = () => done(null);
        function done(check) {
            document.removeEventListener('keydown', key, true);
            panel.classList.remove('open');
            panel.replaceChildren();
            window.__brinellChooseCancel = undefined;
            resolve(check);
        }
    });
    return true;
}
