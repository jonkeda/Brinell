# AD-009: App Bugs Stay Failures

The framework waits for the app to *reach* a state. It never waits out, retries
through or swallows a *defect*. A MAUI UI test found the Todo sample's lost
`CanExecuteChanged` because it failed; a framework that had absorbed that failure
would have shipped the bug.

- **An action that may have run is never repeated.** Its effect is confirmed
  (`Confirm`); an effect that never shows, or an element replaced before it could be
  read, fails the call.
- **An action that did not happen may be asked again, within the budget.** A Core
  method throws `ElementNotReadyException` only before it acts: its guards come first,
  and a driver raises it when nothing was performed (no bridge target answered, the
  item is disabled). A command that never re-enables still fails, as "disabled", when
  the budget runs out.
- **Some failures end the call at once:** the app process exits or the driver session
  is lost (`AppUnavailableException`); a page's busy signal is missing or unreadable
  (`ScopeNotReadyException`, a configuration error); or there is no route for what was
  asked (`RouteUnavailableException` - this element, driver or platform cannot do it,
  and waiting will not change that). A bridge verb the app did not answer is *not* a
  missing route: an app republishes its bridge between pages, so it is retried.
- **A budget the caller set is never extended**: a scroll below the call gets what is
  left of it. It bounds the wait for the control, not the whole call - an action that
  confirms its own effect waits again afterwards, on a budget of its own. No other
  element is used in place of the one asked for - every attempt finds the element
  again, and a row whose element now shows another item is found again by its key -
  and an error is never treated as absence: an exception every attempt raised is
  reported with its type and count.
- **Rows are protected by what their elements can say.** A row is checked against an
  `ItemKey`, which reads the platform's logical index or the row's automation id. A
  collection publishing neither can only key rows by position, and nothing on such an
  element can reveal that it was recycled: give the item template an id.
- **Success after trouble is reported.** A call that passed after its element was
  replaced three or more times, or after using more than half its budget, logs a
  near-miss warning (thresholds: `MauiTestContextOptions.NearMiss`). Set `BRINELL_CALL_LOG` to a folder to write every context's calls
  to CSV and list them.

Proven on 2026-09-19 (`.my/stale-readiness/plan.md`, step 8): with the bug put back,
the Todo journey TOD.04.4 failed 3 times out of 3, naming the dialog that never
appeared.

See [call-model](../contracts/call-model.md) for the R0-R9 rules this rests on.
