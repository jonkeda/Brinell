# Test 4.3 — ControlGroupDetector Tests

**Covers:** Step 4.5 — `ControlGroupDetector` (auto-detect forms, tables, lists, nav, fieldsets, role-based containers)

**File:** `Brinell.Scraper.Tests/Services/ControlGroupDetectorTests.cs`

## Test Inventory (8 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `Detect_FindsForms` | DOM tree containing a `<form>` element returns a `FormContainer` suggestion with the form's child inputs |
| 2 | `Detect_FindsTables` | DOM tree containing `<table>` with `<thead>` and `<tbody>` returns a `TableContainer` suggestion |
| 3 | `Detect_FindsLists` | DOM tree containing `<ul>` with 2+ `<li>` children returns a `ListContainer` suggestion |
| 4 | `Detect_FindsNavElements` | DOM tree containing `<nav>` returns a `NavigationContainer` suggestion |
| 5 | `Detect_FindsFieldsets` | DOM tree containing `<fieldset>` with a `<legend>` child returns a named container suggestion using the legend text |
| 6 | `Detect_FindsRoleBasedDivs` | DOM tree containing `<div role="dialog">` returns a `RoleContainer` suggestion |
| 7 | `Detect_IgnoresSingleItemList` | DOM tree containing `<ul>` with only 1 `<li>` child is skipped — no `ListContainer` suggestion |
| 8 | `Detect_ReturnsEmpty_ForPlainDiv` | DOM tree containing only plain `<div>` elements (no forms, tables, lists, nav, fieldsets, or role attributes) returns an empty suggestion list |

## Notes

- `ControlGroupDetector` operates on the in-memory `DomElement` tree — no WebView2 or WPF dependencies. Pure logic, safe to test on any thread.
- Build test DOM trees programmatically using `DomElement` constructors:
  ```csharp
  var form = new DomElement
  {
      Tag = "form",
      Id = "loginForm",
      Children = [
          new DomElement { Tag = "input", Type = "text", Name = "username" },
          new DomElement { Tag = "input", Type = "password", Name = "password" },
          new DomElement { Tag = "button", Type = "submit", TextContent = "Login" }
      ]
  };
  ```
- Test 2 (`<table>`) must include both `<thead>` and `<tbody>` children — a `<table>` without headers may or may not be detected depending on implementation.
- Test 5 (`<fieldset>`) should verify the container name is derived from `<legend>` text content.
- Test 6 should also cover `role="form"` and `role="tablist"` variants.
- The detector returns a list of `ControlGroupSuggestion` (or similar) with container type and the matched `DomElement` reference.
- Consider testing that multiple patterns in one DOM tree all appear in the results (e.g., a page with both a form and a nav).
