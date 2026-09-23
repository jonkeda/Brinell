# Step 4.5 — Auto-Detect Control Groups

## Objective

Automatically identify forms, tables, lists, and nav regions as candidate container groups for code generation.

## Dependencies

- Step 4.1 (DomSnapshot with element hierarchy)
- Step 4.4 (multi-select to accept/reject suggestions)

## Implementation

### Detection heuristics

| Pattern | Detection Rule | Container Suggestion |
|---------|---------------|---------------------|
| `<form>` | Any `<form>` element | `FormContainer` with child inputs |
| `<table>` | Any `<table>` with `<thead>` and `<tbody>` | `TableContainer` with row/cell controls |
| `<ul>` / `<ol>` | List with 2+ `<li>` children | `ListContainer` |
| `<nav>` | Any `<nav>` element | `NavigationContainer` with link controls |
| Fieldset | `<fieldset>` with `<legend>` | Named container from legend text |
| Div with role | `<div role="dialog|form|tablist">` | Role-based container |

### Auto-suggestion UI

- After DOM capture, scan for these patterns and present a list:
  "Found 2 forms, 1 navigation, 1 table — include as containers?"
- User can accept/reject each suggestion individually.
- Accepted groups are pre-selected in the multi-select view.

## Checklist

- [ ] Scanner identifies `<form>`, `<table>`, `<ul>`/`<ol>`, `<nav>`, `<fieldset>`, role-based containers
- [ ] Suggestions presented in a list after DOM capture
- [ ] User can accept/reject each suggestion
- [ ] Accepted groups auto-select their child elements
