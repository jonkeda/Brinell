# Step 4.3 — DOM Tree View Panel

## Objective

Build a tree view in the inspector panel from the captured DOM snapshot, with hover-highlight and click-to-scroll.

## Dependencies

- Step 4.1 (DomSnapshot / DomElement models)
- Step 4.2 (element highlight overlay for hover sync)

## Implementation

### WPF TreeView

- `TreeView` with `HierarchicalDataTemplate` bound to `DomElement.Children`.
- Each node shows: `<tag id="..." class="...">` with attribute details on expand.
- Filter text box at top to search by tag, id, class, or text content.

### Interactions

- **Hover tree node** → highlight the corresponding element in the browser (via JS overlay).
- **Click tree node** → scroll the browser to that element using `el.scrollIntoView()`.
- **Expand/collapse** via ▼/▶ toggles.

### Panel layout

DOM Inspector appears as a panel to the right of the browser when Inspect mode is active:

```
│ DOM Inspector          │
│ 🔎 Filter: [________] │
│                        │
│ ▼ <html>              │
│   ▼ <body>            │
│     ▼ <form id="..."> │
│       ☑ <input>       │
│         ├ type: text   │
│         └ name: email  │
│       ☐ <div>         │
│       ☑ <button>      │
│         └ text: "Save" │
```

## Checklist

- [ ] TreeView renders full DOM hierarchy with parent-child relationships
- [ ] Each node displays tag name with key attributes (id, class)
- [ ] Expanding a node shows attribute details
- [ ] Filter text box filters nodes by tag, id, class, or text content
- [ ] Hovering a tree node highlights the element in the browser
- [ ] Clicking a tree node scrolls the browser to that element
