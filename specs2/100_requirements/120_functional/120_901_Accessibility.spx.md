# functional Accessibility
- **id**: FR-901
- **title**: Accessibility Support
- **priority**: low
- **status**: draft
- **category**: Compliance

The framework must provide access to accessibility information for accessibility testing scenarios.

## capabilities

### AccessibilityProperties
- **id**: FR-901.1
- **title**: Access to accessibility properties

Controls must provide access to accessibility properties:

| Property | Description |
|----------|-------------|
| AccessibleName | Announced name for screen readers |
| AccessibleRole | Control role (button, textbox, etc.) |
| AccessibleDescription | Extended description |
| AccessibleState | Current accessibility state |

Properties available where platform supports.

### AccessibilityTreeTraversal
- **id**: FR-901.2
- **title**: Accessibility tree traversal

Framework should support accessibility tree navigation:
- Get accessible children
- Find by accessible name
- Navigate parent/child/sibling
- Inspect accessibility hierarchy

Useful for verifying proper accessibility structure.

### AriaAttributes
- **id**: FR-901.3
- **title**: ARIA attribute access (web)

Web platform must support ARIA attribute access:
- aria-label
- aria-describedby
- aria-expanded
- aria-selected
- aria-checked
- role

Via standard GetAttribute method.

### AccessibilityIdLocation
- **id**: FR-901.4
- **title**: Accessibility ID as preferred locator

AccessibilityId/AutomationId is preferred locator strategy:
- Most stable (not affected by styling)
- Matches accessibility implementation
- Consistent across platforms
- Recommended in element location guidance

See [FR-200 Element Location](120_200_ElementLocation.spx.md).

### AccessibilityAuditing
- **id**: FR-901.5
- **title**: Accessibility audit integration (optional)

Framework may integrate with accessibility audit tools:
- axe-core for web accessibility
- Platform-native accessibility checkers
- WCAG compliance validation

Integration optional, via extension mechanism.

### PlatformSupport
- **id**: FR-901.6
- **title**: Platform accessibility support

Accessibility support varies by platform:

| Platform | Accessibility Support |
|----------|----------------------|
| Windows (WPF) | Full (UI Automation) |
| Windows (WinForms) | Full (UI Automation) |
| MAUI Windows | Full (UI Automation) |
| Web | Full (DOM/ARIA) |
| Android | Partial (Accessibility Service) |
| iOS | Partial (Accessibility API) |
| Stride | Limited |

---

## relationships

- Uses [FR-200 Element Location](120_200_ElementLocation.spx.md) for accessibility IDs
- Extension via [FR-800 Extensibility](120_800_Extensibility.spx.md)

---

## constraints

- Accessibility features must not impact non-accessibility tests
- Missing accessibility support must not cause failures
- Accessibility properties may be null where not applicable
