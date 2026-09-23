# Test 4.6 — Export/Import Snapshot Tests

**Covers:** Step 4.11 — DOM snapshot JSON serialization and deserialization

**File:** `Brinell.Scraper.Tests/Services/SnapshotExportImportTests.cs`

## Test Inventory (6 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `Export_ProducesValidJson` | Serializing a `DomSnapshot` produces a string that `JsonDocument.Parse()` can read without error |
| 2 | `Export_UsesCamelCase` | Exported JSON property names are camelCase (`"pageName"`, `"pageUrl"`, `"rootElement"`, `"capturedAt"`) — not PascalCase |
| 3 | `Export_Indented` | Exported JSON string contains newlines and indentation (not minified) |
| 4 | `Import_DeserializesCorrectly` | A valid JSON string deserializes to a `DomSnapshot` with correct `SiteName`, `PageName`, `PageUrl`, `PageTitle`, `CapturedAt`, `RootElement.Tag`, and nested children |
| 5 | `Import_InvalidJson_ThrowsOrReturnsError` | An invalid JSON string (e.g., `"{ not valid"`) throws `JsonException` during deserialization |
| 6 | `RoundTrip_ExportImport_Preserves` | Export a `DomSnapshot` to JSON, then import the JSON back — resulting snapshot has identical field values including nested `Children`, `SelectedElements`, and `BoundingBox` |

## Notes

- Export uses `System.Text.Json.JsonSerializer.Serialize()` with options:
  ```csharp
  new JsonSerializerOptions
  {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true
  };
  ```
- Import uses `JsonSerializer.Deserialize<DomSnapshot>()` with the same options.
- Test 2: parse the JSON string and check that root-level property names are lowercase-initial (e.g., `rootElement.TryGetProperty("pageName", ...)`).
- Test 3: assert the JSON string contains `\n` or `\r\n` and leading whitespace.
- Test 6 (round-trip) should include:
  - A `DomSnapshot` with `SelectedElements` containing 2+ elements.
  - Nested `DomElement.Children` at 3 levels deep.
  - `BoundingBox` values on at least one element.
  - `CapturedAt` as a `DateTimeOffset` with timezone offset.
- Pure logic — no WPF or WebView2 dependencies.
- No temp files needed — serialize/deserialize from strings.
