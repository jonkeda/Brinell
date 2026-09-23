function showToast(message) {
  const host = document.getElementById("toastHost");
  if (!host) return;

  const el = document.createElement("div");
  el.textContent = message;
  el.style.cssText = "margin-top:8px;padding:8px 10px;border-radius:8px;background:#111827;color:#fff;font-size:12px";
  host.appendChild(el);

  setTimeout(() => {
    el.remove();
  }, 2000);
}

document.addEventListener("click", (ev) => {
  const trigger = ev.target.closest("[data-toast]");
  if (!trigger) return;
  showToast(trigger.getAttribute("data-toast"));
});
