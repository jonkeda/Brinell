# Test 3.4 — LogLevelToBrushConverter Tests

**Covers:** Step 03 — `LogLevelToBrushConverter` (color-coded log level display)

**File:** `Brinell.Scraper.Tests/Converters/LogLevelToBrushConverterTests.cs`

## Test Inventory (5 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `Convert_Debug_ReturnsGray` | `Convert(LogLevel.Debug, ...)` returns `SolidColorBrush(Colors.Gray)` |
| 2 | `Convert_Information_ReturnsDarkBlue` | `Convert(LogLevel.Information, ...)` returns `SolidColorBrush(Colors.DarkBlue)` |
| 3 | `Convert_Warning_ReturnsDarkOrange` | `Convert(LogLevel.Warning, ...)` returns `SolidColorBrush(Colors.DarkOrange)` |
| 4 | `Convert_Error_ReturnsRed` | `Convert(LogLevel.Error, ...)` returns `SolidColorBrush(Colors.Red)` |
| 5 | `Convert_NonLogLevel_ReturnsBlack` | `Convert("not a level", ...)` returns `SolidColorBrush(Colors.Black)` (default) |

## Notes

- `Trace` maps to same brush as `Debug` (Gray) — covered implicitly by test 1.
- `Critical` maps to same brush as `Error` (Red) — could add a 6th test if desired.
- Compare by `Color` property: `((SolidColorBrush)result).Color == Colors.Gray`.
- `ConvertBack` throws `NotSupportedException` — no test needed (one-way converter).
- No dispatcher required — converter is pure logic, safe to test on any thread.
