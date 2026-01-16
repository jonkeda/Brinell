# Fix 020: Add Example UI Tests for Button and Entry Controls

| Field | Value |
|-------|-------|
| Status | Resolved |
| Date Created | January 15, 2026 |
| Date Resolved | January 15, 2026 |
| Affected Version | 0.1.0 |
| Fixed Version | 0.1.0 |

## Summary

Add example UI tests to `Brinell.Maui.UITests` project that demonstrate testing Button and Entry controls against the `Brinell.Samples.Maui.App` sample application.

## Symptoms

1. No example UI tests exist to demonstrate framework usage
2. Users lack reference implementations for writing their own tests
3. Framework validation against real app is not automated

## Evidence

The `testsnew/Brinell.Maui.UITests` project exists but contains no test files.

## Root Cause

Initial framework development focused on core functionality; example tests were deferred.

## Proposed Solution

Create example UI tests covering:

### Button Control Tests
- Click button and verify effect
- Increment/Decrement counter buttons
- Reset button functionality
- Button state verification (exists, visible, enabled, clickable)

### Entry Control Tests  
- Enter text and verify value
- Clear text
- Placeholder verification
- Text assertion with fluent chaining
- SetText operation

### Test Structure
- Page object pattern with `MainPage` class
- Control factory methods (Button, Entry)
- Fluent assertion chaining

### Affected Files

| File | Expected Change |
|------|-----------------|
| `testsnew/Brinell.Maui.UITests/Pages/MainPage.cs` | Create page object for MainPage |
| `testsnew/Brinell.Maui.UITests/Tests/ButtonControlTests.cs` | Button control test examples |
| `testsnew/Brinell.Maui.UITests/Tests/EntryControlTests.cs` | Entry control test examples |

## Files Modified

| File | Change |
|------|--------|
| `testsnew/Brinell.Maui.UITests/GlobalUsings.cs` | Updated global usings for pages and interfaces |
| `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` | Created test fixture for Appium driver lifecycle |
| `testsnew/Brinell.Maui.UITests/Pages/MainPage.cs` | Created page object for sample app MainPage |
| `testsnew/Brinell.Maui.UITests/Tests/ButtonControlTests.cs` | Created button control test examples (12 tests) |
| `testsnew/Brinell.Maui.UITests/Tests/EntryControlTests.cs` | Created entry control test examples (15 tests) |

## Verification

- [x] Tests compile successfully
- [x] Page object pattern correctly implemented
- [x] Button tests demonstrate all common scenarios
- [x] Entry tests demonstrate text input scenarios
- [x] Fluent chaining works correctly

## Notes

- Tests will require Appium server running to execute
- Sample app must be deployed to device/emulator
- Tests are marked with appropriate traits for integration testing
