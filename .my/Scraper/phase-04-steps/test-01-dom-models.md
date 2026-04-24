# Test 4.1 — DomElement & DomSnapshot Model Tests

**Covers:** Step 4.1 — `DomElement`, `DomSnapshot`, `BoundingBox` models

**File:** `Brinell.Scraper.Tests/Models/DomElementTests.cs`

## Test Inventory (5 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `DomElement_DefaultValues_AreNull` | Optional properties (`Id`, `ClassName`, `Name`, `Type`, `DataTestId`, `Role`, `AriaLabel`, `Placeholder`, `TextContent`, `BoundingBox`) are all null by default |
| 2 | `DomElement_Children_DefaultEmpty` | `Children` is an empty `List<DomElement>` after construction (not null) |
| 3 | `DomSnapshot_RoundTrip_Json` | Serialize a `DomSnapshot` with nested elements to JSON via `System.Text.Json` (camelCase, indented), then deserialize — all fields preserved including `CapturedAt`, `PageUrl`, `PageTitle`, `SelectedElements`, and nested `Children` |
| 4 | `DomSnapshot_SelectedElements_DefaultEmpty` | `SelectedElements` is an empty list after construction (not null) |
| 5 | `BoundingBox_RecordEquality` | Two `BoundingBox` records with same `X`, `Y`, `Width`, `Height` values are equal; different values → not equal (record semantics) |

## Notes

- `DomElement` is a class with `init` properties — test via object initializers.
- `DomSnapshot` is also a class with mutable `SiteName` / `PageName` and `init` for the rest.
- JSON round-trip test should use `JsonSerializerOptions` with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` to match the capture script output format.
- Nested children (2–3 levels deep) should be tested in the round-trip to verify recursive deserialization.
- `BoundingBox` is a sealed record — test equality and inequality via the record-generated `Equals`.
- `Tag` is the only required-like property (defaults to `""`) — verify it serializes and deserializes correctly.
