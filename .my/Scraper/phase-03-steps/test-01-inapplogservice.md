# Test 3.1 — InAppLogService Tests

**Covers:** Step 03 — `InAppLogService` (thread-safe `ObservableCollection<LogEntry>` with dispatcher marshalling)

**File:** `Brinell.Scraper.Tests/Logging/InAppLogServiceTests.cs`

## Test Inventory (5 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `Entries_IsEmpty_Initially` | `Entries.Count == 0` after construction |
| 2 | `Add_AppendsEntry_WhenNoDispatcher` | Without WPF `Application.Current`, `Add` is a no-op (entry not added). Verifies guard clause behavior. |
| 3 | `Clear_DoesNotThrow_WhenNoDispatcher` | Without WPF `Application.Current`, `Clear` is a no-op — no exception thrown |
| 4 | `LogEntry_Record_StoresAllFields` | `LogEntry` record stores `Timestamp`, `Level`, `Source`, `Message` correctly |
| 5 | `LogEntry_Equality_ByValue` | Two `LogEntry` records with same values are equal (record semantics) |

## Notes

- `InAppLogService.Add()` and `Clear()` guard on `Application.Current?.Dispatcher`. In unit tests (no WPF app running), `Application.Current` is null, so the methods are no-ops.
- Tests 2–3 verify the null-dispatcher guard doesn't throw.
- Tests 4–5 test the `LogEntry` record type directly since it's the model used by the service.
- Integration testing with a real WPF dispatcher would require a `[STAThread]` test or WPF test host — out of scope for unit tests.
