# Plan — give `Brinell.Presenter` the Brinell MAUI gesture bridge

Status: Proposed — not started
Date: 2026-09-21
Area: `srcnew/Brinell.Presenter/`, `testsnew/Brinell.Presenter.Uat.Tests/`
Related:

- [AD-008 — gestures and semantic actions go through UI Automation](../../../.docs/decisions/ad-008-gestures-and-semantic-actions-go-through-ui-automation.md)
- [AD-005 — physical input is opt-in](../../../.docs/decisions/ad-005-physical-input-is-opt-in.md)
- [Bridge verb contract](../../../.docs/contracts/bridge.md)
- [Presenter UI refresh — WP3 §3.4 records this as "Route B"](../fix/00-presenter-ui-refresh.md)
- `samples/Brinell.Samples.Maui.App/MauiProgram.cs` — the reference wiring
- `samples/Brinell.Samples.Maui.App/Automation/ToolkitVerbSink.cs` — the reference sink

---

## 0. What this is

The Presenter is the one MAUI app in `srcnew/` that is also an **app under test** —
`Brinell.Presenter.Uat.Tests` launches the real `.exe` and drives it through UI
Automation. The sample app carries the gesture bridge; the Presenter does not. This
plan adds it, following the same two-line wiring the sample app uses.

Deliberately small. The bridge is a remotely invocable command channel into app logic
(AD-008), so this plan turns it **on** and publishes **almost nothing** through it.
Verbs are added later, one at a time, each past AD-008's three admission tests.

### `Brinell.Maui.CommunityToolkit` is not the other half of this

Easy to conflate, so state it plainly — these are two halves of one wire, in two
processes:

| | Lives in | Runs in | Role |
| --- | --- | --- | --- |
| `Brinell.Maui.CommunityToolkit` | `srcnew/` | the **test** process | the **caller**. `Expander<T>` is already written. |
| `Brinell.Maui.AppSupport` | `samples/` | the **app under test** | the **answerer**. The Presenter does not have it. |

`Expander<T>` is not waiting to be written, and this plan does not write a second one.
Its two operations are already nothing but bridge calls —
`element.ReadState("IsExpanded")` and `element.PerformGesture(MauiGesture.Tap)`
(`Expander.tpl.cs:52`, `:70`) — and its own first doc line says **"Windows needs the
app's sink."** Against the Presenter today those calls have nothing to talk to: the
toolkit `Expander` publishes no UI Automation peer, and `FlaUIDeclaredElement`, the
driver's stand-in for controls the tree does not show, exists **only** for elements the
app declared on the bridge.

So the control object is necessary and not sufficient. WP3 below is the missing half,
and it is about eight lines.

## 1. Ground truth (verified against the running Presenter, not assumed)

| Fact | How it was checked |
| --- | --- |
| The Presenter has no bridge and no `AddBrinellAutomationHandlers()` | `MauiProgram.CreateMauiApp` is 12 lines: `UseMauiApp<App>()` and a Debug logger |
| `Brinell.Maui.AppSupport` lives under `samples/`, and **no `srcnew/` project references anything in `samples/`** today | `grep samples srcnew/**/*.csproj` → no hits |
| The runtime gate is **already open** — `FlaUIMauiDriver` sets `BRINELL_UIA_BRIDGE=1` on every app it launches | `FlaUIMauiDriver.cs:79` |
| The compile gate comes free in Debug via `Brinell.Uia.Bridge.props` | `BrinellUiaBridge` defaults to `true` when `Configuration == Debug` |
| **Layout `AutomationId`s already resolve in the Presenter without the automation handlers** | probed the live `.exe`: `PresenterRoot` and all 10 `WorkspaceNode_*` grids appear as `Pane` |
| Verb classification is central, so a sink reusing existing verbs needs no `ContractTests` change | `BrinellVerb.KindOf` |

The fifth row matters: it **removes** the usual first reason to take
`Brinell.Maui.AppSupport`. `AddBrinellAutomationHandlers()` is not needed here — the
Presenter's containers are already addressable. The only thing this plan buys is the
**gesture bridge**.

## 2. The one decision: how `srcnew/` reaches `samples/`

The fix plan's WP3 made "no `srcnew → samples` project reference" a gate. That gate was
written to stop the Presenter growing a dependency *just* for toolkit `Expander` header
taps. If the bridge is now wanted on its own merits, the dependency is the point rather
than a side effect — but it is still a real inversion, so decide it explicitly.

| Route | Cost |
| --- | --- |
| **A — `ProjectReference` to `samples/Brinell.Maui.AppSupport` (recommended)** | One line. This is route 1 of AppSupport's own README, and the sample app does exactly this. Inverts `srcnew → samples` for the first time in the repo. |
| B — link AppSupport's sources with `<Compile Include="..\..\samples\...\**\*.cs" Link="..." />` | Keeps the layering rule and avoids duplication, but the bridge gate's *file-removal list* in `Brinell.Maui.AppSupport.csproj` would have to be copied too — the "two copies of this rule would eventually disagree" hazard `Brinell.Uia.Bridge.props` explicitly warns about. |
| C — copy the ~20 source files in | Drift. Not for an app inside this repo. |

**Take A**, and record the layering exception in the csproj next to the reference. If
the inversion is judged unacceptable, the honest fix is not B — it is moving
`Brinell.Maui.AppSupport` to `srcnew/`, which keeps both of its documented routes
working and is mechanical. That is a separate plan; note it and move on.

---

## WP1 — Wiring, no verbs

1. `Brinell.Presenter.csproj`: `<ProjectReference Include="..\..\samples\Brinell.Maui.AppSupport\Brinell.Maui.AppSupport.csproj" />`, with a comment saying why a `srcnew` project points at `samples`.
2. `MauiProgram.cs`: `using Brinell.Maui.AppSupport;` and `builder.UseBrinellGestureBridge();` — unconditional, before `Build()`, exactly as the sample app does it and for the same reason (the two gates decide, not the call site).
3. Do **not** add `AddBrinellAutomationHandlers()`. §1 shows the Presenter does not need it, and registering handlers it does not need is how the `SwipeView` tree-collapse got found.

### Done when

- Debug build of the Presenter defines `BRINELL_UIA_BRIDGE`; a `-p:BrinellUiaBridge=false` build does not, and still builds and runs.
- Launched by the UAT fixture, the bridge reports itself enabled in `BRINELL_UIA_LOG`.
- `Brinell.Presenter.Uat.Tests` passes unchanged — **no test edit should be needed for WP1.**

## WP2 — The first verb: `GetState` on what the user can see

Add `Automation/PresenterVerbSink.cs`, modelled on `ToolkitVerbSink`: a singleton
`IBrinellGestureSink` declaring `GetState` only, attached per element in
`PresenterPage.xaml` with `uia:GestureAutomation.Verbs="GetState"` and
`uia:GestureAutomation.Sink="{x:Static ...PresenterVerbSink.Instance}"`.

Candidates, all of them state the user can see on screen:

| Element | State | Today's only route |
| --- | --- | --- |
| recent-folders panel | `IsVisible` | the `OpenRecentButton` caption (`^` / `v`) |
| selection panel | `IsVisible` | the `SelectionExpander` caption |
| each tab panel | `IsVisible` | the `[Brackets]` in the tab caption |

Those three captions are also what the fix plan's WP2 retires when the tabs become
icons. Landing this first gives that change somewhere to land.

### Not a target: the hidden text mirrors

`WorkspaceTreeText`, `AllWorkspaceTreeText`, `StepListText` and friends stay exactly as
they are. They are 1px labels holding composed view-model strings, and reading them
through the bridge instead would be reading the view model — which fails AD-008's third
admission test ("if an assertion needs the view model, the coverage belongs in
`Brinell.Maui.Tests`"). The bridge is not a shortcut around K2.

### Done when

- Each instrumented element answers `GetState` and every other verb returns `UIA_E_NOTSUPPORTED`.
- A test reads one panel's visibility through the bridge and agrees with the caption it replaces.
- The accessibility audit reports no newly instrumented element that only a pointer can reach.

## WP3 — `Tap` on the toolkit `Expander`

**This work is already recorded**, as Route B in
[the UI refresh plan's WP3 §3.4](../fix/00-presenter-ui-refresh.md), which defers it
with: *"Record Route B here as the route to take if and when someone wants
`Expander<T>` coverage of the Presenter itself."* This WP is that "if and when" — it
does not re-decide anything §3.4 settled. Read §3.4 first; what follows is only the
delta.

The work itself: add `BrinellVerb.Tap` to the sink for `Expander`, flipping
`IsExpanded` — the same three lines as `ToolkitVerbSink.ToggleExpander`, plus
`GetState("IsExpanded")` from WP2. Admission test 1 passes: the toolkit `Expander`
publishes no ExpandCollapse and no Invoke pattern on Windows, so there is no UI
Automation route to take instead.

### Two corrections to §3.4, both measured

1. **"adds three gates to get right" — it is one.** The runtime gate is already set by
   `FlaUIMauiDriver` for every launched app, and the compile gate comes free in Debug
   from `Brinell.Uia.Bridge.props`. `PresenterFixture` needs no change at all. Route B's
   cost is the `srcnew → samples` reference (§2) and nothing else.
2. **Nothing tests either expander today.** `SelectionExpander` does not appear in
   `PageObjects/PresenterPage.cs`; `OpenRecentButton` is declared there as
   `Button<PresenterPage>` but **no test or `.uat.md` scenario calls it**. §3.4 already
   notes that no test taps the header chrome — it is stronger than that.

### The ordering that follows

§3.4 takes Route A (a real `Button` inside `Expander.Header`) because Route B was not
available. If **WP1 of this plan lands first**, fix-WP3 gets a free choice:

- **Route A still works** and stays the lower-risk option. `OpenRecentButton` keeps
  answering `Invoke` as a real button.
- **Route B becomes viable** — put the ids on the `Expander` itself and expose
  `Expander<T>` in the page object. This *does* mean a page-object edit, because a
  bridge-declared element has no `Invoke` (`FlaUIDeclaredElement.Click()` throws,
  naming `PerformGesture`). Since neither control has a test today, that edit breaks
  nothing — it is additive, not a migration.

**Recommendation: do not let this plan block fix-WP3.** Route A is a fine landing
place, and Route B can replace it later without touching the ids. But if WP1 here is
already done when fix-WP3 starts, prefer Route B — it is the thing that actually gets
`Expander<T>` exercised against a real app, which is the point of owning the control
object.

---

## Sequence and risk

WP1 → WP2 → WP3, in that order; each is independently shippable. WP1 is the only one
with an architectural decision in it, so get §2 agreed before writing code.

The one thing to verify early: that a `-p:BrinellUiaBridge=false` Presenter still
builds. The Presenter is Windows-only and `Brinell.Maui.AppSupport` is multi-targeted,
so the reference resolves to AppSupport's Windows TFM — that should be uneventful, but
it is the kind of thing that decides whether WP1 is an hour or a morning.

## Out of scope

- `AddBrinellAutomationHandlers()` — §1 shows the Presenter does not need it.
- Moving `Brinell.Maui.AppSupport` into `srcnew/` (§2).
- Any change to `PresenterFixture` — the driver already sets `BRINELL_UIA_BRIDGE=1`.
- Replacing the hidden text mirrors (WP2, "Not a target").
- Verbs for anything that already answers `Invoke`, `Value` or `SelectionItem` — which
  is every button, entry and list row the Presenter has.
