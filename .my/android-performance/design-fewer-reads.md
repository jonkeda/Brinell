# Design: fewer reads per call (after P1, P2)

Date: 2026-10-31. Status: built and measured; results at the end. Follows [plan.md](plan.md) P2. Measured on Kofflard's `LaneDiagnosticTests` on the emulator, one test at a time.

## The question

After P1 and P2 the lane test makes about 1,550 Appium requests. About 350 are the test's own work:

| Purpose | Requests |
| --- | --- |
| the reads the test asked for (`text`) | 84 |
| finding rows and the label in each (`element`, `elements`) | ~250 |
| swipes, taps, typing, session | ~20 |

The rest is Bruinet making sure. **Which of those can go without weakening what a call guarantees?**

## What the evidence says

Three findings, two of which corrected first guesses:

1. **A name read is 12 requests.** From Appium's log, for one card:

   ```
   rect(page) · rect(page) · enabled(list) · rect(row) · rect(row) · resource-id(row) ×3 · find(label) · displayed(label) · text · class(label)
   ```

2. **The repeated page and row checks are separate attempts, not one.** A temporary trace (each `Rect` with its call stack) showed the same page-root object read through `AttemptMemo` many times. The reads came from different attempts of polling calls ("suggestions shown", "text equals…"), which re-check every 100 ms. Each attempt re-checking its scope is the rule (R2, R6), not waste.

   My first reading, "two polls per call", was wrong: a read is one poll.
3. **The row-id reads are a uniqueness check.** `CollectionObjectBase.KeyOf` keys a row by its AutomationId only if no other realized row has the same one (`IsUniqueAmongRealized`). That reads the id of every realized row, for every row keyed, on every attempt. Kofflard's rows all share the id `Suggestion`, so the check runs and always fails, then falls back to the position.

Two facts the design relies on:
- **A native element's id and class never change.** UiAutomator2's element id stands for one native view. A RecyclerView that rebinds a view gives it new content, but not a new `resource-id` or widget class, because both come from the item template.
- **On Android, found means shown.** UiAutomator leaves out of the tree what the user cannot see. Bruinet already relies on this for bounds checks (`IShownWhenFound`).

## Constraints

- **Call model R2, R4, R5, R6:** a call's poll is the only wait, each attempt checks its scope chain afresh, and a stale element is a signal, never swallowed.
- **No answer kept across an action.** The shared memo tried before showed why: a UAT step long-pressed a card and then read answers from before the press. Anything kept must be either immutable or scoped to one attempt.
- **Windows and headless stay as they are**, unless a change is free and correct there too.

## The changes

| # | Change | Where | Kept for | Expected | Risk |
| --- | --- | --- | --- | --- | --- |
| D1 | **Immutable facts cached by element id:** `resource-id` (AutomationId) and `class` (TagName), on the Appium driver, keyed by the WebDriver element id, for the session. | `AppiumMauiElement.AutomationId`, `.TagName`; a dictionary on `AppiumMauiDriver` | the session | −170 `resource-id`, −80 `class` | none known: both come from the item template. A view replaced by a new one gets a new element id, so a new entry. |
| D2 | **Row-id uniqueness once per attempt:** the realized rows' ids are read once per attempt, not once per row keyed. | `CollectionObjectBase.IsUniqueAmongRealized`, via `AttemptMemo` | one attempt | with D1 already cached; on Windows it removes the O(n²) reads per attempt | none: the same answer, asked once |
| D3 | **No visibility read before a read on Android:** the check a call makes before acting or reading (`IControlHost.IsVisible`) answers true for an element whose driver only finds what is shown. Explicit visibility questions (`IsVisible()`, `AssertVisible`, "should not be visible") still read. | `ViewBase`'s `IControlHost.IsVisible`, `ContainerObjectBase`'s | — | −100 `displayed` | an element in the tree with `displayed=false` (zero size, fully transparent) would be read rather than refused. The read then returns what UiAutomator has, as now. |
| D4 | **Alive check folded into the bounds check:** when a scope root's bounds were read in this attempt, that read already proved it alive, so the separate `InstanceKey` read is skipped. | `ContainerObjectBase.IsCachedRootValid` | one attempt | −30 to −60 `enabled` (only where both are asked) | none: a stale element fails the bounds read the same way |

**Expected total: about 1,550 → 1,100–1,150 requests (−25 to −30 %).** My earlier estimate of −800 assumed the page and row checks repeated within one attempt; the trace shows they don't.

### What remains, and is not in this design

- **The page root, checked once per attempt (~400 `rect`).** It is the rule, and the cost per attempt is one request. To cut it, poll less often where a request is expensive: a polling interval suited to the head (100 ms on Windows, perhaps 250 ms on Android). That is a decision of its own, since it trades reaction time for requests.
- **Swipes (11 s):** P4 and P5 in the plan.
- **One tree read per attempt** (`GET source`) answering every read: the biggest cut, but it changes how staleness works (R5, R6). It needs an AD first.

## How it is checked

- **Pins in `ListAndPageRootPinTests`:**
  - keying ten rows reads the realized ids once per attempt (D2);
  - an element whose driver finds only shown elements is not asked `Visible` before a read, but is asked for `AssertVisible(false)` (D3);
  - a root whose bounds were read in the attempt is not asked its `InstanceKey` (D4).
- **D1 has no mock to pin it**, because the cache is in the Appium element. It is checked by request counts on the emulator.
- **The lane test on the emulator**, before and after each change, with `measure.sh` and the Appium log: requests by command, and wall time. Recorded in [plan.md](plan.md).
- **Regression:** Bruinet's MAUI UI suite on Windows and headless, Kofflard's UI and UAT on both, and the Html, Blazor and Uat unit suites. Run once at the end, not per change.

## Results (built)

The lane test on the emulator, twice:

| Request | Before (P1, P2) | After D1–D4 | By |
| --- | --- | --- | --- |
| **all** | ~1,550 | **1,205–1,207** (−22 %) | |
| `attribute/resource-id` | 188 | 12 | D1, D2 |
| `attribute/class` | 84 | 12 | D1 |
| `displayed` | 111 | 1 | D3 |
| `enabled` | 181 | 184–185 | D4: no effect |
| `rect` | 610 | 614–616 | unchanged, by design (one page check per attempt) |
| wall time | 45.7–47.5 s | 44.3–45.5 s | |

- **D4 saved nothing here.** The alive check (`enabled`) and the bounds check (`rect`) are asked of different elements: the list's alive check and the page's bounds. D4 is kept because it is correct and free, but it is not where the remaining `enabled` reads go.
- **D2 has no pin.** No mock scenario reaches its path meaningfully (a row keyed outside a listing, more than once in one attempt). It returns the same answer as before, asked once.
- **The wall time moved little.** The requests removed were the cheap ones, about 8 ms each. The time is now in the page check per attempt (~600 `rect`) and the swipes (11 s, P4 and P5).

Pins: `FoundMeansShown_TheReadSkipsTheVisibilityCheck_AnAssertionStillAsks` (D3, fails without it), with `ARead_AsksThePageRootForItsBoundsOnce` from P2.

