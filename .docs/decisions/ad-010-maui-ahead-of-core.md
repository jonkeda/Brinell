# AD-010: MAUI And Html Ahead Of Core, On Purpose

`Brinell.Maui` owns the interfaces its call model needs (`IMauiElement`,
`IMauiDriver`, `IMauiElementScope`, `IMauiPage`, `IMauiTestContext`) and no longer
implements Core's element, driver, scope and page interfaces. Since 2026-09-26
`Brinell.Html` does the same, with its own set (`IHtmlElement`, `IHtmlDriver`,
`IHtmlElementScope`, `IHtmlPage`, `IHtmlTestContext`). The stacks where the model was
designed and proven keep their own shapes; the other stacks keep Core's until the proven
shape moves down to Core, one layer at a time, as its own step
([move-down plan](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/stale-readiness/move-down.md)).

**The leaf layer has moved down** (2026-09-26, [review at 7162ac7](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/core/review-what-can-move-now.md)).
It names no element, driver or scope type, so it needed none of the interfaces:

- `Brinell.Core.Calls`: `Deadline`, `AttemptContext`, `Observation`, `ObservationLog`,
  `Poller`, `Confirmer`, `ControlCall`, `ScopeGate` (internal, visible to `Brinell.Maui`,
  `Brinell.Html` and their test projects), and `Confirmation<T>`, `CallBudget`,
  `NearMissSettings` (public);
- `Brinell.Core.Exceptions`: `StaleElementException`, `ElementNotReadyException` with
  `NotReadyReason`, `ScopeNotReadyException`, `AppUnavailableException`,
  `RouteUnavailableException`;
- `Brinell.Core.Scopes`: `ScopeReadiness`, `ScopeReadinessState`.

MAUI and Html have no copies of these. What is still per stack: the interfaces, the
control and scope bases, pages, collections and item keys, and Html's `BusySignal`.
Expect those to differ from their Core counterparts, and from each other where the
platform differs. Do not add adapters between them and Core: a bridge now would have to
be undone in the move down.

**Broken when:** an adapter is added between the MAUI or Html interfaces and Core's;
Core is changed to match either stack's interfaces or bases ahead of their deliberate
move-down; or a stack grows its own copy of a leaf-layer type again.
