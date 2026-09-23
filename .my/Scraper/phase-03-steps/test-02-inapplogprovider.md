# Test 3.2 — InAppLogProvider Tests

**Covers:** Step 03 — `InAppLogProvider` + inner `InAppLogger` (`ILoggerProvider` feeding log entries to `InAppLogService`)

**File:** `Brinell.Scraper.Tests/Logging/InAppLogProviderTests.cs`

## Test Inventory (4 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `CreateLogger_ReturnsNonNull` | `CreateLogger("Some.Category")` returns a non-null `ILogger` |
| 2 | `Logger_ExtractsShortSource_FromCategory` | Category `"Brinell.Scraper.ViewModels.BrowserViewModel"` → source is `"BrowserViewModel"` |
| 3 | `Logger_IsEnabled_ReturnsTrueForDebugAndAbove` | `IsEnabled(LogLevel.Debug)` → true, `IsEnabled(LogLevel.Trace)` → false |
| 4 | `Logger_BeginScope_ReturnsNull` | `BeginScope(state)` returns null (scopes not supported) |

## Notes

- The `InAppLogger` is a private nested class — test indirectly via `InAppLogProvider.CreateLogger()`.
- Tests 2–4 exercise the `ILogger` methods on the returned logger.
- Cannot test `Log<TState>` adding entries because `InAppLogService.Add()` requires WPF dispatcher — covered by integration tests.
- Source extraction logic: `categoryName.LastIndexOf('.')` then substring. Test with dotted and non-dotted category names.
- `Dispose()` on the provider is a no-op — no need to test.
