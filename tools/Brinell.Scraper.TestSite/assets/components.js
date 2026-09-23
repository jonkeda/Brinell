class DemoPill extends HTMLElement {
  connectedCallback() {
    const label = this.getAttribute("label") || "Pill";
    const tone = this.getAttribute("tone") || "info";
    const toneStyle = tone === "warn"
      ? "background:#fef3c7;color:#92400e"
      : tone === "ok"
        ? "background:#dcfce7;color:#166534"
        : "background:#e0f2fe;color:#075985";

    this.innerHTML = `<span style="display:inline-block;padding:4px 9px;border-radius:999px;font-weight:700;${toneStyle}">${label}</span>`;
  }
}

class DemoCounter extends HTMLElement {
  connectedCallback() {
    const start = Number(this.getAttribute("start") || "0");
    this.count = start;
    this.render();
  }

  render() {
    this.innerHTML = `
      <div style="display:flex;gap:8px;align-items:center">
        <button type="button" data-action="minus" style="width:auto;padding:6px 10px;background:#334155;color:#fff;border:none;border-radius:6px">-</button>
        <strong data-role="value" style="min-width:24px;text-align:center">${this.count}</strong>
        <button type="button" data-action="plus" style="width:auto;padding:6px 10px;background:#0f766e;color:#fff;border:none;border-radius:6px">+</button>
      </div>`;

    this.querySelector('[data-action="minus"]').addEventListener("click", () => {
      this.count -= 1;
      this.querySelector('[data-role="value"]').textContent = String(this.count);
    });

    this.querySelector('[data-action="plus"]').addEventListener("click", () => {
      this.count += 1;
      this.querySelector('[data-role="value"]').textContent = String(this.count);
    });
  }
}

class DemoCollapse extends HTMLElement {
  connectedCallback() {
    const title = this.getAttribute("title") || "Details";
    this.innerHTML = `
      <details>
        <summary style="cursor:pointer;font-weight:700">${title}</summary>
        <div style="padding-top:8px"><slot></slot></div>
      </details>`;
  }
}

customElements.define("demo-pill", DemoPill);
customElements.define("demo-counter", DemoCounter);
customElements.define("demo-collapse", DemoCollapse);
