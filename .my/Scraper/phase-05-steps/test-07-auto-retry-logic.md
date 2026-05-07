# Test 5.7 — Auto-Retry Logic Tests

**Covers:** Step 5.10 — `RetryService.ValidateWithRetryAsync()` (re-prompt LLM on validation failure)

**File:** `Brinell.Scraper.Tests/Services/RetryServiceTests.cs`

## Test Inventory (3 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `ValidateWithRetry_ValidCode_NoRetry` | Valid code passes on first attempt; `ICopilotService.GenerateAsync()` is never called; returns `IsValid = true` |
| 2 | `ValidateWithRetry_FirstAttemptFails_SecondSucceeds` | First code has a syntax error; mock `ICopilotService.GenerateAsync()` returns corrected code on retry; returns `IsValid = true`; `GenerateAsync` called exactly once |
| 3 | `ValidateWithRetry_AllAttemptsFail_ReturnsErrors` | Code with persistent syntax error; mock returns bad code on both retries; returns `IsValid = false` after 2 retry attempts; `GenerateAsync` called exactly 2 times (max retries) |

## Notes

- Use NSubstitute to mock `ICopilotService`:
  ```csharp
  var copilotService = Substitute.For<ICopilotService>();
  copilotService.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns("```csharp\n<corrected code>\n```");
  ```
- Use `NullLogger<RetryService>.Instance` for the logger.
- Use a mock `IControlRegistry` that returns an empty control list (or use `NSubstitute`).
- Valid code sample: complete `sealed class` with `using` statements.
- Invalid code sample: missing closing brace `}`.
- Corrected code sample: same class with the brace fixed.
- Persistent invalid code: code that still has errors after "correction" (e.g., different syntax error).
- Verify exact call count on the mock to confirm retry behavior.
- The retry prompt should include the original code and error messages — verify by capturing the argument passed to `GenerateAsync`.
