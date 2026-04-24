# Step 4.2 — Element Highlight Overlay

## Objective

Highlight elements in the browser when the user hovers over them in inspect mode, with locator suggestions.

## Dependencies

- Step 4.1 (DOM snapshot / element model)
- Phase 1 (WebView2 + Inspect toolbar button)

## Implementation

### Overlay behavior

- Inject a `MutationObserver`-safe overlay `<div>` that follows the cursor.
- On `mousemove`, find the element under the cursor and position the overlay using `getBoundingClientRect()`.
- Show a tooltip below the element with: tag, id, aria-label, type, and **suggested locator**.
- Toggle on/off via the 🔍 Inspect button in the toolbar.

### Color scheme

| Color | Meaning |
|-------|---------|
| Blue border + light blue bg | Hovered (mouse over) |
| Green border + light green bg | Selected (clicked / checked in tree) |

### Locator suggestion logic (in JS overlay)

```javascript
function suggestLocator(el) {
    if (el.getAttribute('data-testid'))
        return `Locator.ByDataTestId("${el.getAttribute('data-testid')}")`;
    if (el.id && !isDynamicId(el.id))
        return `Locator.ById("${el.id}")`;
    const label = findAssociatedLabel(el);
    if (label)
        return `Locator.ByText("${label.textContent.trim()}")`;
    if (el.getAttribute('aria-label'))
        return `Locator.ByAriaLabel("${el.getAttribute('aria-label')}")`;
    return `Locator.ByCss("${generateMinimalSelector(el)}")`;
}
```

Labels and visible text are preferred as locator anchors — this produces the most resilient locators.

### Tooltip content

```
input#email  aria-label="Email address"
Suggested: Locator.ByText("Email:")
```

## Checklist

- [ ] Overlay div injected into WebView2 page
- [ ] Blue highlight follows cursor on mousemove
- [ ] Tooltip shows tag, id, aria-label, type, locator suggestion
- [ ] Green highlight persists on selected elements
- [ ] Overlay toggled by 🔍 Inspect button
- [ ] Overlay does not interfere with page functionality (MutationObserver-safe)
