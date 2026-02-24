# Plan: Fix 5 Failing HTML UI Tests

**Created:** February 24, 2026
**Status:** COMPLETE
**Context:** After the async migration (Phases 0–7), 31 of 36 HTML UI tests pass. The 5 failures trace to 3 distinct root causes unrelated to the async work itself.

---

## Test Results Summary

| # | Test | Status | Root Cause Group |
|---|------|--------|------------------|
| 1 | `Button_Click_IncrementsCounter` (sync) | PASS | A — Blazor circuit not ready |
| 2 | `Login_InvalidCredentials_ShowsErrorMessage` (sync) | PASS | B — Password too short |
| 3 | `Login_InvalidCredentials_ShowsErrorMessage_Async` | PASS | B — Password too short |
| 4 | `Select_SelectByValue_UpdatesSelectedValue` (sync) | PASS | A — Blazor circuit not ready |
| 5 | `Select_SelectByText_UpdatesSelectedValue_Async` | PASS | C — SelectByText uses value match |

---

## Group A — Blazor Circuit Not Ready After Navigation

**Affected tests:** #1, #4

### Problem

`NavigateToPage()` calls `Context.NavigateTo()` which waits for the browser `load` event but does not wait for the Blazor Interactive Server circuit to establish via SignalR. The app uses `@rendermode="RenderMode.InteractiveServer"` so all event handlers are server-side — clicks fired before the circuit connects are silently lost.

Evidence:

- Counter page starts at `_currentCount = 0`. The static SSR HTML renders "Current count: 0" immediately.
  `AssertText("Current count: 0")` passes on static HTML, then `Click()` fires before the circuit is live. The click is lost.
- FormControls page starts with `_selectedCountry = ""`. `SelectByValue("de")` fires before the circuit connects — the select reverts to default after the circuit establishes.
- The Blazor.UITests test base already handles this with `_context!.WaitForBlazorReady()` which polls `typeof window._blazor !== 'undefined'`.
- Async counterparts of these tests intermittently pass because task scheduling overhead gives the circuit extra time.

### Fix

Add a Blazor-readiness wait to `BlazorSampleTestBase` after every navigation. Both sync and async paths need coverage.

**File:** `testsnew/Brinell.Html.UITests/TestBase/BlazorSampleTestBase.cs`

Steps:

- [x] **A.1** Add a `WaitForBlazorCircuit()` helper that polls `window.Blazor` via `IHtmlElement.Evaluate<bool>()`. Uses `Thread.Yield()` polling (no `Thread.Sleep`). Timeout at 10 seconds.
- [x] **A.2** Call `WaitForBlazorCircuit()` at the end of `NavigateToPage()` (sync path).
- [x] **A.3** Add `WaitForBlazorCircuitAsync()` (true async with `Task.Delay(50)`) and call it at the end of `NavigateToPageAsync()`.
- [x] **A.4** Tests #1 and #4 now pass.

### Design Notes

The helper needs access to `PlaywrightTestContext.InternalPage`. Since `BlazorSampleTestBase` stores `_context` as a private `PlaywrightTestContext?`, the helper can cast and access it directly. Avoid adding Blazor-specific wait logic to the framework itself — this belongs in the test base since it's sample-app-specific behavior.

For the async version, use `await page.EvaluateAsync<bool>(...)` with `ConfigureAwait(false)` and `Task.Delay` polling instead of `Thread.Sleep`.

---

## Group B — Password Validation Prevents Form Submission

**Affected tests:** #2, #3

### Problem

Both tests enter password `"wrong"` (5 characters). The `LoginModel.Password` property has a `[MinLength(6)]` validation attribute. The form uses `OnValidSubmit`, so when model validation fails:

1. `HandleValidSubmit` never executes.
2. `_errorMessage` stays `null`.
3. The `[data-testid='error-message']` div never renders.
4. `WaitVisible(true)` times out and returns `false` (return value unchecked).
5. `AssertTextContaining("Invalid email or password")` finds no element — fails with `Actual: ''`.

The valid-credentials tests pass because `"password123"` (11 chars) satisfies the constraint.

### Fix

Change the test password to meet the 6-character minimum so the form actually submits.

**File:** `testsnew/Brinell.Html.UITests/Tests/Pages/LoginPageTests.cs`

Steps:

- [x] **B.1** In `Login_InvalidCredentials_ShowsErrorMessage`, changed `"wrong"` to `"wrongpw"`.
- [x] **B.2** In `Login_InvalidCredentials_ShowsErrorMessage_Async`, changed `"wrong"` to `"wrongpw"`.
- [x] **B.3** Tests #2 and #3 now pass.

---

## Group C — Async SelectByText Matches Option Value Instead of Label

**Affected tests:** #5

### Problem

The sync `SelectByText` in `SelectControl` manually iterates `<option>` elements, matches the visible text case-insensitively, extracts the `value` attribute, then calls `SelectOption(value)`. This works correctly.

The async `SelectByText` in `SelectorControlBase` takes a shortcut:

```csharp
async Task<TScope> IHtmlAsyncSelector<TScope>.SelectByText(string text)
    => await RunWithElementAsync(async e =>
        await e.SelectOption(new[] { text }).ConfigureAwait(false)).ConfigureAwait(false);
```

`SelectOption(string[])` calls `PlaywrightHtmlElement.SelectOption(string[])` which creates `SelectOptionValue { Value = text }`. For text `"German"`, Playwright searches for `<option value="German">` — but the actual markup is `<option value="de">German</option>`. No match → 30-second timeout.

### Fix

Use Playwright's `Label` property on `SelectOptionValue` to match visible text rather than value.

Steps:

- [x] **C.1** Added `SelectOptionByLabel(string label)` to `IHtmlElement`.
- [x] **C.2** Added `Task SelectOptionByLabel(string label)` to `IAsyncHtmlElement`.
- [x] **C.3** Implemented both in `PlaywrightHtmlElement` with `SelectOptionValue { Label = label }`.
- [x] **C.4** Updated async `SelectByText` in `SelectorControlBase` to call `e.SelectOptionByLabel(text)`.
- [x] **C.5** Build passes: 0 errors, 0 warnings.
- [x] **C.6** Test #5 now passes.

### Design Notes

Adding `SelectOptionByLabel` as a new element-level method is cleaner than trying to inline the Playwright-specific `SelectOptionValue` into the framework. The sync `SelectByText` in `SelectControl` can remain as-is since it already works — this is about fixing the async path only.

---

## Execution Order

Groups A, B, and C are independent and can be executed in any order. Recommended sequence:

1. **Group B** (trivial test data fix — builds confidence)
2. **Group A** (test infrastructure — unblocks other sync tests)
3. **Group C** (framework fix — requires interface + implementation changes)

---

## Validation

After all 3 groups:

- [x] `dotnet build srcnew/Brinell.Html.Playwright/` — 0 errors, 0 warnings
- [x] `dotnet test testsnew/Brinell.Html.UITests/` — **36 passed, 0 failed**
