# 130_012 Accessibility Testing

## quality AccessibilityTesting

- **attribute**: Compliance
- **requirement**: Framework supports accessibility verification
- **priority**: high

---

## Description

This requirement ensures the framework can verify accessibility properties and support accessibility testing workflows, helping users create accessible applications.

---

## Sub-Requirements

### NFR-COMP-001.1: Accessibility Testing

- Framework SHOULD support accessibility property verification
- Framework SHOULD integrate with accessibility testing tools
- Framework SHOULD verify WCAG compliance where applicable

---

## Accessibility Properties

Controls should expose and verify accessibility properties:

| Property | Description | Example Assertion |
|----------|-------------|-------------------|
| Name | Accessible name | `AssertAccessibleName("Submit")` |
| Role | ARIA role | `AssertRole("button")` |
| Description | Additional context | `AssertDescription("...")` |
| State | Current state | `AssertState("expanded")` |

---

## WCAG Verification

### Supported Checks

- Text alternatives for images
- Keyboard accessibility
- Focus indicators
- Color contrast (via external tools)
- Heading structure

### Example Usage

```csharp
// Verify accessible name
submitButton.AssertAccessibleNameExists();

// Verify keyboard accessibility
submitButton.AssertKeyboardFocusable();

// Verify ARIA attributes (Blazor)
dialog.AssertAttribute("aria-modal", "true");
```

---

## Integration Points

### axe-core Integration (Blazor)

```csharp
// Run accessibility audit
var results = page.RunAccessibilityAudit();
results.AssertNoViolations();

// Specific rule check
results.AssertRule("button-name", passed: true);
```

### Platform-Specific

| Platform | Accessibility API |
|----------|-------------------|
| MAUI | UI Automation |
| Blazor | ARIA + axe-core |
| WPF | UI Automation |

---

## Related

- [FR-004 State Verification](../120_functional/120_004_StateVerification.spx.md)
- [NFR-COMP-002 Standards Compliance](130_013_StandardsCompliance.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-COMP-001
