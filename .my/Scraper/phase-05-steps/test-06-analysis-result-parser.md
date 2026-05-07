# Test 5.6 — Analysis Result Parser Tests

**Covers:** Step 5.6 — `AnalysisResultParser.Parse()` (extracting structured analysis from LLM response)

**File:** `Brinell.Scraper.Tests/Services/AnalysisResultParserTests.cs`

## Test Inventory (4 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `Parse_JsonInCodeFence_ReturnsProposals` | LLM response with ` ```json ` fenced block containing 2 proposed controls — returns `AnalysisResult` with 2 `ControlProposal` items, each with correct Name, DomSignature, Frequency, Confidence, ExampleSnippet, SuggestedProperties |
| 2 | `Parse_RawJson_ReturnsProposals` | LLM response with raw JSON object (no markdown fences) — parser finds the `{...}` block and returns correct `AnalysisResult` |
| 3 | `Parse_WithLocatorReport_ReturnsReport` | JSON includes `locatorReport` with `stableAttributes`, `unstableAttributes`, `recommendations` — all fields populated on returned `LocatorReport` |
| 4 | `Parse_MalformedResponse_ReturnsEmptyResult` | LLM response with no JSON (pure prose) — returns `AnalysisResult` with empty `ProposedControls` list and null `LocatorReport` (no exception thrown) |

## Notes

- Test JSON input:
  ```json
  {
    "proposedControls": [
      {
        "name": "DatePickerControl",
        "domSignature": "div.date-picker > input + button.calendar",
        "frequency": 8,
        "confidence": 94,
        "exampleSnippet": "<div class=\"date-picker\"><input type=\"date\" /><button class=\"calendar\">📅</button></div>",
        "suggestedProperties": ["DateInput", "CalendarButton"]
      }
    ],
    "locatorReport": {
      "stableAttributes": ["data-testid", "aria-label"],
      "unstableAttributes": ["id (dynamic on 8/15 pages)"],
      "recommendations": "Prefer ByText() and ByDataTestId(). Avoid ById() on Dashboard, Settings."
    }
  }
  ```
- `PropertyNamingPolicy.CamelCase` must be used for deserialization.
- Parser should handle: JSON in ` ```json ` fences, raw JSON in prose, and no JSON at all.
- `ControlProposal.IsApproved` defaults to `false` after parsing (user must explicitly approve).
- No exceptions should be thrown for malformed input — return empty result.
