# Review: what can move to Core now

Date: 2026-09-26, after the Html plan finished (`../html/plan.md`). Checked against the source
on `move`.

**Status: L1 done (2026-09-26).** Every "Yes" row below is in Core, merged as in section 2,
with no copies left in MAUI or Html. Namespaces: `Brinell.Core.Calls`, `Brinell.Core.Exceptions`,
`Brinell.Core.Scopes` (no `Core` prefix). No backward-compatibility shims. AD-010, R9 and the
CHANGELOG are updated. Verified: solution and every test/sample project build; Core, MAUI,
Html, Blazor and Generator unit tests; Html, Blazor and Todo web UI suites; MAUI UI tier 3
(351 passed, only the opt-in stress and probe skips). L2 (busy signal) and the rows marked
"No" still wait. The question: can classes such as the calls layer or the exceptions move from
`Brinell.Maui` and `Brinell.Html` into `Brinell.Core`?

## Short answer

**Yes, for a clear "leaf" set, and now is the right time.** There are now two proven copies (MAUI
and Html), and they differ by a few lines each. The exceptions, scope readiness, the whole calls
layer and a few small types can move without any of the new `ICore*` interfaces. Nothing in them
needs an element, a driver or a scope type. The control bases, containers, pages and item keys
cannot move yet: they are typed on each stack's element. They wait for this plan's interface
steps.

Doing it is the move down that AD-010 describes, not a shortcut past it. It needs:

- AD-010 amended;
- this plan re-sequenced (section 5).

| Group | Move now? | Why |
| --- | --- | --- |
| Exceptions (`StaleElementException`, `ElementNotReadyException` + `NotReadyReason`, `ScopeNotReadyException`, `AppUnavailableException`, `RouteUnavailableException`) | **Yes** | Identical code on both stacks. They differ only in the wording of two messages, and depend only on `ScopeReadiness` |
| `ScopeReadiness`, `ScopeReadinessState` | **Yes** | Identical except that MAUI has a `RootReacquired` flag; take MAUI's |
| Calls: `Deadline`, `AttemptContext`, `Observation`, `ObservationLog`, `Poller`, `Confirmer`, `Confirmation<T>`, `ControlCall`, `ScopeGate` | **Yes** | 22-41 differing lines per file, most of them comments. The code differences are features one copy has and the other lacks; the merged version is the union (section 2) |
| `NearMissSettings`, `CallBudget` | **Yes** | `NearMissSettings` is identical. `CallBudget` generalises MAUI's `AttemptContext.RemainingOr` |
| `BusySignal` (Html) against `BusySignalPolicy` (Core) | Not yet | The two differ in meaning; reconcile them as one decision (section 3) |
| `ItemKey` | No | Same idea, different kinds: MAUI keys by logical index, automation id or position; Html by `data-item-key` or position. A shared type would be a union nobody needs |
| `ViewBase` / `ControlBase`, `ContainerObjectBase` / `ContainerBase`, pages, collections, `AppRoot` | No, not in this step | Typed on `IMauiElement` or `IHtmlElement`. They need `ICoreElement` and `ICoreScope` (this plan's steps 1-4) |
| Driver error mapping (`PlaywrightErrors`, FlaUI and Appium errors) | No | Platform code by nature |
| `WebAppHost` | Maybe later | Not web-specific: it starts any process on a free port with explicit settings. It could serve the several-apps work (a MAUI test hosting the Todo API). Move it when a second user needs it |

## 1. What the two copies look like

Each file was compared with its line endings ignored, and the stack names (`Maui`/`Html`)
replaced by one name:

| File | MAUI lines | Html lines | Differing lines | What differs in code (not comments) |
| --- | --- | --- | --- | --- |
| `Deadline` | 32 | 31 | 5 | MAUI has `IsPassed` |
| `AttemptContext` | 75 | 52 | 31 | MAUI has `MaySweep` (scroll-to-find) and `RemainingOr` |
| `Observation` | 78 | 78 | 14 | MAUI has `ObservationKind.ItemChanged` (a row now holds another item) |
| `ObservationLog` | 127 | 122 | 41 | MAUI maps `ItemChanged` to a stale error; Html returns an assertion's `Mismatch` error as it is |
| `Poller` | 63 | 58 | 25 | MAUI has the `stop` parameter (end of a list); Html has the cut-off rule (a `TimeoutException` after the deadline is not an observation) |
| `Confirmer` | 46 | 64 | 22 | Html runs the confirmation under its own `AttemptContext` |
| `ControlCall` | 98 | 98 | 28 | Html logs the context's `Name`; MAUI logs a constant, "Test" |
| `ScopeGate` | 49 | 45 | 18 | Comments only; each takes its own stack's scope interface |
| Exceptions | 1 file | 1 file | messages only | "no longer in the UI tree" against "no longer in the page" |
| `ScopeReadiness` | | | | MAUI has `RootReacquired`; Html adds its `BusySignal` enum to the same file |

Every class in `Calls/` is `internal` on both stacks. `Confirmation<T>` and Html's `CallBudget`
are public.

## 2. The merged version

The Core copy is the union, with nothing dropped:

- **`Deadline`**, with `IsPassed`.
- **`AttemptContext`**, MAUI's: `MaySweep`, `MayScrollIntoView`, `RemainingOr`. Html uses only
  part of it, and that is fine.
- **`Observation`**, with `ItemChanged`.
- **`ObservationLog`**, with both mappings: `ItemChanged` to a stale error, and `Mismatch` to its
  own error. Check MAUI's asserts after the switch: MAUI's `RunAssert` builds its failure from
  the `Mismatch` itself, so the Html branch should not change a MAUI message, but the MAUI
  suites decide.
- **`Poller`**, with both the `stop` parameter and the cut-off rule. The cut-off rule is inert on
  MAUI today, because nothing below a MAUI call throws `TimeoutException` when the budget runs
  out. It becomes needed on MAUI when its drivers take the call's budget.
- **`Confirmer`**, Html's (its own context). Harmless on MAUI, and needed there under the same
  condition.
- **`ControlCall`**, Html's (the context's name). MAUI gains the name that the several-apps work
  needs (`../html/todo-blazor.md` section 4, constraint 2).
- **`CallBudget`**, Html's. MAUI's `CallRemainingMs` calls it; the at-least-1-ms floor is
  harmless on MAUI.

### 2.1 Cutting the three ties to a stack

The calls layer touches stack types in only three places, and none needs an `ICore*` interface:

1. **`ControlCall` takes the test context** for its logger, its name and its near-miss
   settings. Pass the three values instead: `ControlCall(ILogger? logger, string? name,
   NearMissSettings nearMiss, string scopeName, string controlId)`. `ILogger` is already in
   `Brinell.Core.Logging`. The stack's base class reads them from its own context.
2. **`ScopeGate.Check(IXElementScope scope)`** calls only `scope.ProbeReadiness()`. Core keeps
   the `Check(ScopeReadiness)` overload. The stack calls `ScopeGate.Check(scope.ProbeReadiness())`
   in its one call site, or keeps a one-line helper.
3. **The exceptions** need only `Locator` (Core) and `ScopeReadiness` (moving with them).

### 2.2 The messages

Two messages name the platform:

- `StaleElementException`: "no longer in the UI tree" (MAUI) against "no longer in the page"
  (Html);
- `ElementNotReadyException`'s default text.

Use one neutral wording, "is no longer there: it was removed or replaced". No test asserts
either text (checked with grep over `testsnew/` and `samples/`).

## 3. What waits

- **`BusySignal` against `BusySignalPolicy`.** Core's policy has `Disabled` and `Required`, where
  a missing sentinel is a configuration error. Html's has `Ignore`, `Optional` and `Required`,
  where a missing attribute means "not loaded yet", because a Blazor app sets it when it becomes
  interactive. Both meanings are right for their platform. The Core type needs a third value,
  or a flag for what "missing" means. Decide that with the pages, not with the calls.
- **The control and scope bases.** `ViewBase<TScope>` (MAUI) and `ControlBase<TScope>` (Html)
  are the same design at about 800 lines each, and hold most of the duplicated logic that is
  left. They are typed on the stack's element, so they need `ICoreElement` (this plan's steps
  1-4). After the leaf move, that duplication is the main reason to do those steps.
- **The generator's helper names.** Before the bases move, close the R8 gap in Html's
  `ControlBase` (`../html/review-html-vs-maui.md` section 2): a shared base must offer the
  signatures the generator emits.

## 4. What it changes for users

- **Namespaces.** `Brinell.Maui.Exceptions.StaleElementException` becomes a Core type. Few
  files name the exception namespaces outside the stacks' own code: two MAUI UI-test files,
  one Html UI-test file, one Todo UI-test container, and the Html unit tests. MAUI's global
  usings hide the rest. A `using` change each; no behaviour change.
- **One type across stacks.** A test that catches `StaleElementException` or
  `AppUnavailableException` catches it from any stack. That matters for the several-apps
  scenarios: with two copies loaded, the same condition would be two unrelated types.
- **Other stacks are not affected.** The move only adds to Core. WPF, WinForms, NativeAndroid
  and Stride keep `PageLoadException` and their own waits until they adopt the model.

## 5. How it fits this plan

This plan (draft, 2026-09-20) lands the new layer in three steps:

- steps 1-2: `ICoreElement`, `ICoreScope`, `ICoreDriver`, `ICoreTestContext`, and with them the
  exceptions and readiness;
- step 3: the calls layer, copied from MAUI;
- step 4: MAUI, then Html, inherit `ICore*`.

Two things have changed since it was drafted:

1. **Html did not wait for `ICore*`.** Step 5 assumed Html would build on `ICore*` directly.
   Instead Html built its own set, as MAUI had (AD-010). So there are two proven copies to
   merge, not one to copy.
2. **The leaf layer does not need the interfaces.** Section 2.1 shows that the calls,
   exceptions and readiness move without them.

Suggested re-sequencing:

| New step | What | Done when |
| --- | --- | --- |
| **L1. Leaf layer to Core** | Exceptions, `ScopeReadiness`, the calls, `NearMissSettings` and `CallBudget` into Core, merged as in section 2. MAUI and Html delete their copies in the same change. AD-010 amended: the leaf layer is in Core; the interfaces and bases are still per stack | The solution builds with no `Brinell.Maui.Calls` or `Brinell.Html.Calls` type left. `Brinell.Maui.Tests`, `Brinell.Html.Tests`, both Html UI suites and the MAUI UI suite (tier 3) are green and identical to the baseline |
| L2. Busy-signal decision | One Core busy-signal type with Html's three meanings | `BusySignalPolicy` and `BusySignal` are one type |
| Then steps 1, 2, 4, 5 as drafted | `ICore*` interfaces, then the bases, for MAUI and Html; step 3 is already done by L1 | As drafted |

**Naming.** The draft prefixes the new types with `Core` (`CoreStaleElementException`,
`CoreScopeReadiness`) so that they can coexist with old names. None of the leaf types collides
with an existing Core type: Core has no `StaleElementException` and no `ScopeReadiness`. So the
prefix buys nothing here. Keep the names MAUI and Html already use, under a new namespace. The
prefix still earns its place on the interfaces, where `IElement<T>` and `ICoreElement` must
coexist. This is ?C-level: decide it in step 0.

**Visibility.** The calls are `internal`, and should stay so. Core declares `InternalsVisibleTo`
for `Brinell.Maui`, `Brinell.Html`, their test projects and `Brinell.Maui.Extensions` (which
already has MAUI's internals). Public: the exceptions, `ScopeReadiness`, `Confirmation<T>`,
`NearMissSettings` and `CallBudget` (drivers call it).

## 6. Risks

| Risk | Mitigation |
| --- | --- |
| The merge changes MAUI behaviour (the cut-off rule, the `Confirmer` context, `Mismatch` mapping) | Each is inert on MAUI today (section 2). Prove it: the MAUI tier-3 suite identical to its baseline, the near-miss count unchanged |
| Core gains `InternalsVisibleTo` for named stacks | A short, explicit list; the alternative (public calls) exposes a mechanism nobody should call |
| A breaking namespace change for consumers | `CHANGELOG.md` "Changed" entry; a `using` change in about five files |
| AD-010 says "Core is not changed ahead of the deliberate move-down" | L1 *is* the deliberate move down, for the leaf layer. Amend AD-010 in the same change |
