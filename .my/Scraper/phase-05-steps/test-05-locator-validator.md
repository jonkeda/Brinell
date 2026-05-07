# Test 5.5 — Locator Validator Tests

**Covers:** Step 5.10 — `CodeValidator` locator validation (method names, arguments, ByCss warning)

**File:** `Brinell.Scraper.Tests/Services/LocatorValidatorTests.cs`

## Test Inventory (5 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `ValidateLocators_AllValidMethods_NoWarnings` | Code using `Locator.ByText("x")`, `Locator.ByDataTestId("y")`, `Locator.ByAriaLabel("z")`, `Locator.ById("w")` — no warnings |
| 2 | `ValidateLocators_ByCss_WarnsUser` | Code using `Locator.ByCss(".my-class")` produces a warning: "ByCss is a last-resort locator" |
| 3 | `ValidateLocators_UnknownMethod_WarnsUser` | Code using `Locator.ByXPath("//div")` produces a warning: "Unknown locator method: Locator.ByXPath()" |
| 4 | `ValidateLocators_EmptyArgument_WarnsUser` | Code using `Locator.ByText("")` produces a warning about non-empty string literal |
| 5 | `ValidateLocators_PreferenceOrder_MultipleByCss_WarnsAll` | Code with 3 `Locator.ByCss(...)` calls produces 3 separate warnings (one per occurrence) |

## Notes

- Valid locator methods: `ByText`, `ByLinkText`, `ByPartialLinkText`, `ByDataTestId`, `ByAriaLabel`, `ById`, `ByCss`.
- `ByCss` is valid but produces a warning (last-resort locator).
- Warnings include line and column numbers for each occurrence.
- Test with complete, syntactically valid code blocks (Roslyn must parse without syntax errors for locator validation to run).
- Example test code:
  ```csharp
  public sealed class TestPage : HtmlPageObjectBase<TestPage>
  {
      public TextInputControl<TestPage> Email =>
          Control<TextInputControl<TestPage>>(Locator.ByText("Email"));
  
      public ButtonControl<TestPage> Submit =>
          Control<ButtonControl<TestPage>>(Locator.ByCss(".submit-btn"));
  }
  ```
- Locator validation runs as part of `Validate()` after syntax checks pass — warnings are in the `Warnings` list.
