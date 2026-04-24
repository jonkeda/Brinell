# Test 1.3 — AsyncRelayCommand Tests

## Covers

Step 02 (`02-mvvm-foundation.md`) — `AsyncRelayCommand`, `AsyncRelayCommand<T>`

## Tests (7)

| Test | Description |
|------|-------------|
| `Execute_CallsAsyncAction` | Async action invoked |
| `IsRunning_TrueWhileExecuting` | Property tracks execution state |
| `IsRunning_FalseAfterCompletion` | Resets after task completes |
| `CanExecute_ReturnsFalse_WhileRunning` | Prevents re-entry |
| `Execute_HandlesException` | Exception doesn't crash |
| `CancellationToken_Propagated` | Token passed to async action |
| `AsyncRelayCommandT_PassesParameter` | Generic version passes typed parameter |

## Implementation Notes

- Use `TaskCompletionSource` to control async timing in tests
- Verify `IsRunning` transitions: false → true → false
- For exception test, verify `IsRunning` resets to false even on failure
- `CancellationToken` test: cancel during execution, verify token was cancelled
