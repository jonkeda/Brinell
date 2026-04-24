# Test 4.2 — DomCaptureService Tests

**Covers:** Step 4.1 — `DomCaptureService` (JS injection → JSON → `DomSnapshot` deserialization)

**File:** `Brinell.Scraper.Tests/Services/DomCaptureServiceTests.cs`

## Test Inventory (6 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `CaptureAsync_DeserializesJsonToSnapshot` | Given a mock `CoreWebView2` that returns valid JSON from `ExecuteScriptAsync`, the result is a non-null `DomSnapshot` with populated `RootElement` |
| 2 | `CaptureAsync_SetsPageUrl` | `DomSnapshot.PageUrl` is populated from the captured data |
| 3 | `CaptureAsync_SetsPageTitle` | `DomSnapshot.PageTitle` is populated from the captured data |
| 4 | `CaptureAsync_HandlesNestedChildren` | JSON with 3 levels of nested elements deserializes correctly — `RootElement.Children[0].Children[0]` exists with correct `Tag` |
| 5 | `CaptureAsync_HandlesMissingAttributes` | JSON where optional attributes (`id`, `className`, `dataTestId`, etc.) are null deserializes without error — properties are null |
| 6 | `CaptureAsync_SetsCapturedAt` | `DomSnapshot.CapturedAt` is set to approximately the current time (within 5-second tolerance) |

## Notes

- `DomCaptureService.CaptureAsync` takes a `CoreWebView2` parameter — this is a sealed WPF/WebView2 class that cannot be easily mocked with NSubstitute. Two test strategies:
  1. **Extract the deserialization logic** into a testable `ParseSnapshot(string json)` method and test that directly.
  2. **Test the JSON → model mapping** by calling the deserialization path with known JSON strings, bypassing the WebView2 call.
- Recommend strategy 2: create a helper or make the JSON parsing method `internal` with `[InternalsVisibleTo]`.
- Test JSON should match the structure produced by the injected JS capture script (camelCase property names).
- `CapturedAt` is set by the service after capture, not by the JS — test that it's approximately `DateTimeOffset.UtcNow`.
- `ILogger<DomCaptureService>` dependency can use `NullLogger<DomCaptureService>.Instance` in tests.
- Sample test JSON:
  ```json
  {
    "tag": "html",
    "id": null,
    "className": null,
    "children": [
      {
        "tag": "body",
        "id": null,
        "children": [
          { "tag": "input", "id": "email", "type": "text", "dataTestId": "email-input", "children": [] }
        ]
      }
    ]
  }
  ```
