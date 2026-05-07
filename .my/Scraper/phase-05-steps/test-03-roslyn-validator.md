# Test 5.3 — Roslyn Syntax Validator Tests

**Covers:** Step 5.10 — `CodeValidator.Validate()` (Roslyn syntax parsing)

**File:** `Brinell.Scraper.Tests/Services/CodeValidatorTests.cs`

## Test Inventory (5 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `Validate_ValidCode_ReturnsNoErrors` | A complete, valid PageObject class (with `using`, `namespace`, `sealed class`, properties) returns `IsValid = true` and empty `Errors` list |
| 2 | `Validate_SyntaxError_MissingBrace` | Code with a missing closing `}` returns `IsValid = false` with at least one error; error has correct line number |
| 3 | `Validate_SyntaxError_InvalidExpression` | Code with `public TextInputControl<LoginPage> UserName => ;` (missing expression) returns `IsValid = false` with error at the correct line/column |
| 4 | `Validate_EmptyInput_ReturnsError` | `""` and `null` return `IsValid = false` with an "Empty code" error |
| 5 | `Validate_MultipleErrors_ReportsAll` | Code with 3 syntax errors returns all 3 in the `Errors` list with distinct line numbers |

## Notes

- Test with realistic Brinell PageObject code structures.
- Valid code sample:
  ```csharp
  using Brinell.Core.Locators;
  using Brinell.Html.Controls;

  namespace ExactOnline.Pages;

  public sealed class LoginPage : HtmlPageObjectBase<LoginPage>
  {
      public LoginPage(IHtmlTestContext context) : base(context) { }

      public TextInputControl<LoginPage> UserName =>
          Control<TextInputControl<LoginPage>>(Locator.ByText("User name"));
  }
  ```
- Roslyn parses syntax only — type resolution (`HtmlPageObjectBase` etc.) is not checked at this level.
- Verify that `Line` and `Column` in `CodeError` are 1-based (human-readable).
- No WPF dependencies — no STA thread required.
