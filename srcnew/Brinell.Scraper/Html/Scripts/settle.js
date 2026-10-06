// Resolves when the document has had no change for `quiet` ms: true, or false when `max` ms passed first
// (an animation that never stops). A framework renders after "load" and after its data arrives.
({ quiet, max }) => new Promise(resolve => {
    let timer;
    const done = settled => {
        observer.disconnect();
        clearTimeout(timer);
        clearTimeout(limit);
        resolve(settled);
    };
    const observer = new MutationObserver(() => {
        clearTimeout(timer);
        timer = setTimeout(() => done(true), quiet);
    });
    observer.observe(document, { subtree: true, childList: true, attributes: true, characterData: true });
    timer = setTimeout(() => done(true), quiet);
    const limit = setTimeout(() => done(false), max);
})
