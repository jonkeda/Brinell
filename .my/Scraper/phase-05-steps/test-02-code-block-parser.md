# Test 5.2 — Code Block Parser Tests

**Covers:** Step 5.9 — `CodeBlockParser` (ExtractCSharpBlocks, SplitByClassDeclarations)

**File:** `Brinell.Scraper.Tests/Services/CodeBlockParserTests.cs`

## Test Inventory (6 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `ExtractCSharpBlocks_SingleFencedBlock` | Input with one ` ```csharp ` block returns a list with one trimmed code string |
| 2 | `ExtractCSharpBlocks_MultipleFencedBlocks` | Input with 3 ` ```csharp ` blocks (PageObject + 2 containers) returns a list of 3 code strings in order |
| 3 | `ExtractCSharpBlocks_CsFenceMarker` | Input using ` ```cs ` instead of ` ```csharp ` is correctly extracted |
| 4 | `ExtractCSharpBlocks_NoFences_FallbackExtraction` | Input with raw C# code (no markdown fences) — fallback heuristic detects code starting with `using` or `namespace` keywords |
| 5 | `ExtractCSharpBlocks_EmptyInput_ReturnsEmptyList` | `null`, `""`, and `"   "` all return an empty list |
| 6 | `ExtractCSharpBlocks_ProseOnly_ReturnsEmptyList` | Input with only prose text (no C# keywords) returns an empty list |

## Notes

- Test with realistic LLM response text including prose before/after code blocks.
- Verify that whitespace is trimmed from extracted blocks.
- Fallback test should include a response like:
  ```
  Here is the generated code:

  using Brinell.Core.Locators;

  namespace ExactOnline.Pages;

  public sealed class LoginPage : HtmlPageObjectBase<LoginPage>
  {
      ...
  }
  ```
- The `SplitByClassDeclarations` method is tested separately below if needed, but primary focus is `ExtractCSharpBlocks`.
- Regex must not catastrophically backtrack — test with large inputs (>10KB of code).
