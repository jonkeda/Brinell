# Contract: The call model

The MAUI and Html stacks run every public control/page/container/collection/row member
as one **call**. This is the shape all MAUI and Html code must keep. Sources of record:
`.my/stale-readiness/design.md` section 2 (MAUI) and `.my/html/design.md` (Html, which
adopts the same rules). Enforces
[AD-004](../decisions/ad-004-wait-for-state.md) and
[AD-009](../decisions/ad-009-app-bugs-stay-failures.md).

## The rules (R0-R9)

| Rule | What must stay true |
| --- | --- |
| **R0** | App bugs stay failures. The framework waits for *state*; it never waits out, retries through, or swallows a *defect*. Whatever a call absorbs on the way to success is still reported (near-miss). |
| **R1** | One unit per public call: one log entry/exit pair, one failure, around the whole call - the action included. |
| **R2** | Only the call's poll waits. Readiness, lookup, visibility, scrolling and settling are single attempts inside it. Nothing below the poll loops or sleeps. |
| **R3** | Each phase has the caller's budget: resolve with `timeoutMs`, then confirm with `timeoutMs`. A confirmation is a second phase with its own budget, not a second poll. |
| **R4** | A call waits for its scope chain: each scope reports itself and asks its parent. |
| **R5** | A stale element is a signal (`StaleElementException`) from both drivers. A poll re-resolves on it; a confirmation reports it and does not act again. No catch-alls. |
| **R6** | One check that a cached root is still alive, used by every scope that caches a root (`InstanceKey` read through the driver's `Live` helper). |
| **R7** | Non-`Try` members wait; `Try*` members answer about now. |
| **R8** | The generator does not change: same `Run*` names and signatures on both hierarchies. |
| **R9** | MAUI and Html own the interfaces and bases they change. The calls layer, its exceptions and `ScopeReadiness` are one copy, in Core (`Brinell.Core.Calls`, `.Exceptions`, `.Scopes`). See [AD-010](../decisions/ad-010-maui-ahead-of-core.md). |

## What a call may absorb, and what it must report (R0)

- A **stale element** (R5) is re-resolved by the poll and never acted on twice.
- A **not-ready scope** (R4) is waited for, within the budget.
- A **not-ready element** before an action (`ElementNotReadyException`) may be asked
  again, within the budget.
- Below the poll there are **no catch-alls** (R5): a failure is either an answer (an
  absent element, an unknown candidate) or a signal.
- These **end the call at once**: `AppUnavailableException` (process/session lost);
  configuration errors (`ScopeNotReadyException` - missing/invalid busy signal);
  `RouteUnavailableException` (no route on this element/driver/platform).
- A **budget** the caller set is never extended; a scroll below the call gets what is
  left of it. A driver call below a call that takes a timeout is given what is left of the
  call's budget (`CallBudget`), never the driver's own default. A read or action cut off by the
  budget ends the poll; the call is reported by the last attempt that completed.
- A **confirmation is a phase with its own budget**, down to the driver: the reads that confirm
  an action get the confirmation's time, not what the resolve phase left (R3). `Confirmer` runs
  under its own attempt context.
- A **read is about now**. A driver whose reads wait for their element (Playwright's locators
  do) caps that wait short and reports a node that is gone as stale (R5), for the poll to find
  again: one read never waits out the call (R2).

## Broken when

- A wait loop or sleep appears below a call's poll (violates R2).
- A catch-all swallows an exception below the poll (violates R5, R0).
- An action is repeated instead of confirmed (violates R0; see
  [control-object](control-object.md)).
- A budget is extended past the caller's `timeoutMs`.

## Html specifics

The rules above hold unchanged. Where Playwright differs from UI Automation and Appium:

- **Playwright calls take a timeout**, so the Playwright driver is the one that reads
  `CallBudget` today. A read waits for its element at most `PlaywrightHtmlElement.ReadAttachMs`.
- **A page's busy signal is `data-busy` on `<body>`.** A Blazor page requires it: a page
  without it is not loaded yet (a prerendered page is not acted on), not misconfigured. A
  value other than `true`/`false` is a configuration error and ends the call at once.
- **A row stands for its item.** It finds its element again by `data-item-key` on every
  lookup, with a selector on the key, never by position: a Playwright element found by position
  is found by position again on every read. When the item is gone it reports `ItemChanged` and
  never acts on the row now in its place.
- **A scope shown over a busy page** (a dialog) does not ask its page (`AsksParent => false`).
