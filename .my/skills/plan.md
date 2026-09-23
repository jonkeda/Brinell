# Agent skills for MAUI controls and UI tests - plan

Goal: give agents (Claude Code, and Copilot through the same files) two skills that let them
work without copying a neighbour blindly:

1. **`maui-control`** - create a new MAUI ControlObject of any of the four kinds: simple
   control, container, component, collection. The skill states the rules; examples illustrate
   them but are not the source of truth.
2. **`maui-ui-test`** - write tests through page objects, containers and controls, never
   through the driver, `IMauiElement` or `Find*` calls.

Plus the framework changes that make rule 2 enforceable rather than advisory (section 6).

Status (2026-09-18): steps 1-5 of section 7 done. The skills are in `.github/skills/`
(`maui-control`, `maui-ui-test`, `convert-control`) with stubs in `.claude/skills/`;
`AGENTS.md`, `.github/copilot-instructions.md`, `docs/guides/test-writing.md` and
`control-source-builder.md` point at them. Step 7 (P1 - P4) is not started. No code under
`srcnew/` changed in the main tree.

Step 6, first round (2026-09-18), two fresh agents in worktrees, one after the other:
- **Control eval: CommunityToolkit `DockLayout`.** Chose container, a plain `.cs` (R5); no
  members the platform cannot answer, with a comment saying so; probed Windows; ran
  `Control=DockLayout` 3/3 and `Tests.CommunityToolkit` 42/42 unprompted. No never-list hits.
- **Test eval: `CarouselView` / `IndicatorView` tests.** Added members to the controls rather
  than working around them, extended the bridge for existing verbs, and ran `Control=...` 8/8,
  tier 2b 58/58 and `Brinell.Maui.Tests` 114/114 unprompted. One never-list hit: the row
  constructor's `IMauiElement itemRoot`, which the collection pattern requires.
- Wording fixed after each round: `partial` only for `.tpl.cs`; no-generation path for a plain
  `.cs`; probing without a repo tool; sample-app and Shell-app build commands; the `.gen.cs`
  line-ending restore command (tested); ids vs locators in tests; the row-constructor exception;
  visibility on Windows; looping carousels; a pattern with no app-side effect is not a route;
  bridge state counts as "published"; extending the bridge needs AD-008 answers too.

Both results were brought into the main tree (2026-09-18). `DockLayout` lost `partial`. One
carousel test failed there and not in the agent's worktree: it demanded `Item(2)` of a
virtualized carousel to check that card 2 was off screen. Fixed with `TryItem(i)?`, and added
to both skills. After that: toolkit + collection + tier 2b 123/124 before the fix, the three
controls 11/11 three times after it, `Brinell.Maui.Tests` 114/114.

A second round with new tasks has not been run.

## 1. Format: what "best these days" means here

| Layer | Holds | Why |
| --- | --- | --- |
| `.github/skills/<name>/SKILL.md` | **The source.** The workflow and the rules for one task, with `name`/`description` frontmatter. | One copy for every tool. Recent Copilot versions read skills from `.github/skills`; skills load on demand, so they can be long without costing context in unrelated sessions. |
| `.github/skills/<name>/references/*.md` | Detail that only some runs need: one file per control kind, the generator contract, the forbidden-API list. | Progressive disclosure: `SKILL.md` stays short and routes to the one reference that applies. |
| `.claude/skills/<name>/SKILL.md` | **A stub.** The same `name` and `description` frontmatter, and one line: "Read `.github/skills/<name>/SKILL.md` and follow it." | Claude Code discovers skills only under `.claude/skills`, and it triggers on the stub's `description`. A stub beats a symlink: git on Windows checks symlinks out as text files unless Developer Mode and `core.symlinks` are on. The one cost: keep the two `description`s identical. |
| `AGENTS.md` (always loaded) | Three or four lines: the never-rules (no driver or `IMauiElement` in tests) and a pointer to both skills in `.github/skills`. | The never-rules must hold even when no skill triggers, for example during a quick fix to a test. |
| `.github/copilot-instructions.md` | One pointer paragraph to `.github/skills`. | For Copilot surfaces that do not read skills yet. No duplicated rules. |
| Roslyn analyzer (section 6, P1) | The forbidden-API rule, as a build diagnostic. | Instructions are advisory; an analyzer holds for every author, human or agent. |

The existing `.claude/skills/convert-control` moves to `.github/skills/convert-control` and
leaves a stub behind, like the new skills. `.claude/agents/control-source-builder.md` stays
where it is (agents are Claude-only) but points at `.github/skills`. The generator contract
moves out of `convert-control` into a shared reference that all three use (step 1), so the
rules live in one place.

Not chosen: a subagent per control kind (the kinds share 80% of their rules, and a subagent
starts cold), or putting everything in `AGENTS.md` (always-on context for rules that apply
to one task in ten).

## 2. Deliverables

```
.github/skills/                   the source
  maui-control/
    SKILL.md                      workflow, kind decision table, rules common to all kinds
    references/
      generator-contract.md       shared with convert-control and control-source-builder
      routes.md                   pattern -> bridge verb -> AD-008; platform evidence
      simple-control.md
      container.md
      component.md
      collection.md
  maui-ui-test/
    SKILL.md                      workflow, test shape, the never-list
    references/
      page-objects.md             pages, AppRoot, app-specific containers and rows
      forbidden-apis.md           the list, why, and what to do instead
      unit-tests.md               Brinell.Maui.Tests: mocked elements behind the public API
  convert-control/SKILL.md        moved from .claude; slimmed; stale facts fixed
.claude/skills/                   stubs: frontmatter + "read .github/skills/<name>/SKILL.md"
  maui-control/SKILL.md
  maui-ui-test/SKILL.md
  convert-control/SKILL.md
AGENTS.md                         + never-rules and pointers to .github/skills
.github/copilot-instructions.md   + pointer to .github/skills
docs/guides/test-writing.md       + forbidden-API section (docs are for humans too)
```

Stub shape:

```markdown
---
name: maui-control
description: <identical to .github/skills/maui-control/SKILL.md>
---

Read `.github/skills/maui-control/SKILL.md` and follow it. Its `references/` paths are
relative to that folder.
```

## 3. Rules to write into `maui-control`

These are drafted from the code (`ViewBase.tpl.cs`, `ClickableControlBase.tpl.cs`,
`ToggleControlBase.tpl.cs`, `ContainerObjectBase.cs`, `ComponentObjectBase.cs`,
`CollectionObjectBase.cs`, `ItemContainerBase.cs`, `ItemStrategy.cs`, `Stepper.tpl.cs`,
`Expander.tpl.cs`, `ScrollView.tpl.cs`, `RefreshView.tpl.cs`, `Popup.tpl.cs`,
`StateContainer.tpl.cs`, `MediaElement.tpl.cs`, `AvatarView.tpl.cs` and their parts), not
copied from any one control. The skill
states them as rules; a short example follows each, and the example never replaces the rule.

### 3.1 Choosing the kind (the first thing `SKILL.md` asks)

| The thing on screen... | Kind | Base |
| --- | --- | --- |
| is one element to the test, even when the platform draws parts (Stepper's +/-), and its parts are implementation detail | **simple control** | `ViewBase<TScope>` or a capability base |
| is a region whose content the *app* chooses, which scopes other controls and may read or act on its own root (Border, ScrollView, Popup, StateContainer) | **container** | `ContainerObjectBase<TParent, TSelf>` |
| is one control to the user but made of fixed parts the *control* knows and a test wants to address by name (MediaElement's transport buttons, slider, time labels; AvatarView's initials and image) | **component** | `ComponentObjectBase<TScope, Foo<TScope>>` |
| repeats rows whose children are controls (a row has a label, a checkbox, a delete button) | **collection** + item | `CollectionObjectBase<TParent, TSelf, TItem>` + `ItemContainerBase<TCollection, TSelf>` |

Tie-breakers the skill spells out:

- Options that are only text (Picker) make a **simple** selector (`SelectorControlBase`), not
  a collection.
- If you would expose parts as public properties, it is a component. If you would only use
  them inside Core methods, it is a simple control with private part resolution (Stepper).
- Behaviour on the root alone does not make a container a component. A container declares
  Core methods on its own root (`Popup.IsOpenCore`, `StateContainer.IsShowingCore`,
  `ScrollView.ScrollForwardCore`) and implements capability interfaces
  (`IRefreshableControlObject`, `ISwipeableControlObject`). It becomes a component when the
  control itself names parts whose ids or shapes are fixed by the platform or library, not by
  the app.

### 3.2 Rules common to every kind

**Placement and naming**
- Framework controls: `srcnew/Brinell.Maui/Controls/<Family>/Foo.tpl.cs`. Third-party
  sources: `srcnew/Brinell.Maui.<Source>/Controls/<Family>/Foo.tpl.cs`. Namespace follows the
  folder.
- `Foo.gen.cs` is generated by `tools/Scripts/CreateMaui.Bat` (covers both projects). Never
  edit it by hand; never commit a `.tpl.cs` change without regenerating.
- The class is `partial`. Two constructors: `(IMauiScope<TScope> scope, Locator locator)` and
  `(IMauiScope<TScope> scope, string locatorValue)`. Public on concrete controls, protected on
  abstract bases.

**Generator contract** (full text in `generator-contract.md`)
- Behaviour lives in `protected virtual *Core` methods whose first parameter is the element.
  The generator writes the public API: `Is*Core` -> `IsX / WaitX / AssertX`; `Get*Core` ->
  `GetX / WaitX / AssertX`; `Set*Core(element, T? value, int? timeoutMs = null)` -> `SetX`;
  any other `*Core` -> an action returning the scope.
- Declare `int? timeoutMs = null` on every Core method whose public form takes a timeout.
- `[GenerateComparisons(...)]` for Contains/StartsWith/Empty variants;
  `[AbsenceTolerant]` (with a nullable element) when the question is meaningful for a missing
  element; `[SkipGeneration("reason")]` when the public member is hand-written.
- Guards are `Ensure*Core`, called inside the Core body. Readiness that should be *waited
  for* (enabled a moment late) goes in `EnsureReadyForActionCore`.
- After generating, read the generator's near-miss diagnostics and diff `Foo.gen.cs` against
  the API you intended.

**Semantics**
- Null means skip: `SetX(null)` does nothing, `AssertX(null)` passes. Keep it.
- Setters are idempotent: already in the target state -> no action.
- Actions confirm their effect. After toggling or setting, wait for the read-back to change
  and throw naming what did not happen, before-and-after state, and the `Locator`
  (`ToggleCore`, `SetCheckedDirectly`, `Expander.SetExpandedCore`).
- Reads return `null` when the platform does not publish the value. Never fabricate a value.
- An action the platform cannot perform throws `NotSupportedException` naming the route the
  app would have to offer (a verb, a sink, a handler).
- Every error message includes the `Locator`.
- Do not add members the platform cannot answer; say so in a comment
  (`CollectionView`: "No SelectionMode: neither platform publishes it").

**Routes** (full text in `routes.md`)
1. A UI Automation pattern through an `IMauiElement` member (`Invoke`, `Toggle`,
   `SetChecked`, `Value`, `RangeValue`, `Checked`, `ScrollIntoView`).
2. An existing bridge verb (`PerformGesture`, `ReadState`, `TryFindDeclared`).
3. A new verb only after the three AD-008 tests, answered in writing in the plan or PR.

No coordinates. No physical input on MAUI Windows (AD-005). No `Thread.Sleep`.

**One unit of work per public call** (from [controls-migration-plan.md](controls-migration-plan.md)):
- R1. A Core method reads and acts on its own element only; it never calls a public member of
  another control (a part, `Button(id)`, an item).
- R2. A Core method never calls a `Run*` helper. It waits with `Until(read, done, timeoutMs, out
  lastError)`, and a timeout passes `lastError` as the exception's `InnerException`.
- R3. A part with behaviour of its own gets its own control class; a component forwards to it
  through `*Shortcut` methods.
- R4. A member that needs two parts is hand-written as plain calls in sequence, with a remark
  naming the parts.
- R5. A class is a `.tpl.cs` template if it declares Core methods or shortcuts; otherwise it
  stays a plain `.cs`.
- R6. Public members take ids and values, never `Locator`s.
- A read that is meaningful for a missing element carries `[AbsenceTolerant]`: an `Is*` then
  resolves optionally, and a `Get*` reads once and answers null instead of waiting. Note that
  plain `IsVisible` answers null, not false, for a missing element (see the migration plan's
  open issue), so a part that must answer "not shown" declares its own absence-tolerant read.

The generator warns (it does not fail) when a Core method breaks R1 or R2.

**Inside a control, `IMauiElement` and the scope's `Find*` are the implementation.** That is
exactly where they belong: the control exists so nothing above it touches them.

**Documentation**
- `<summary>` on every member. `<remarks>` records the route per platform and the measured
  evidence ("Windows: no ExpandCollapse pattern; the app declares `Tap,GetState`. Android:
  touch; no state read, so IsExpanded is null").
- Name the app-side requirement (handler, `GestureAutomation.Verbs`, sink) in the class
  remarks.

**Proof**
- A sample element in `samples/Brinell.Samples.Maui.App` with an `AutomationId` and a status
  label that shows the visible effect of each action.
- Page object members and tests, written with the `maui-ui-test` skill.
- Build, regenerate, rebuild the sample app, run tier 1 (`--filter "Control=Foo"`), and
  tier 2b if a verb was added.

### 3.3 Simple control (`simple-control.md`)

- `public partial class Foo<TScope> : Base.XBase<TScope> where TScope : IMauiScope<TScope>`.
  Every generated member returns `TScope`, so a chain returns to the page.
- Pick the base by capability, lowest that fits:

  | Base | Adds | Implement |
  | --- | --- | --- |
  | `ViewBase` | exists, visible, enabled, attribute, scroll-into-view | - |
  | `FocusableControlBase` | focus | `IFocusableControlObject` |
  | `ClickableControlBase` | click, double, right, hover, long press; waits for enabled | `IClickableControlObject` |
  | `ToggleControlBase` | toggle, check, set checked, verified | `IToggleControlObject` |
  | `RangeControlBase` | value, min, max, step, increment | `IRangeControlObject` |
  | `SelectorControlBase` | text options, select | `ISelectorControlObject` |

- Override a base Core method when the platform activates differently (Toggle's `ClickCore`
  toggles; RadioButton selects). Keep the generated name and meaning.
- Domain vocabulary is a thin layer: `Switch.IsOnCore => IsCheckedCore`, plus hand-written
  `TurnOn()` aliases. Do not duplicate behaviour.
- Parts the platform draws separately (Stepper on Windows publishes `{id}Minus`/`{id}Plus`)
  are resolved privately by overriding `TryFindElement()`/`FindElement()`; one part acts as
  the proxy element. `FindElement` throws `ElementNotFoundException` naming every locator it
  tried.
- State the tree cannot show comes from `Context.AppElement.TryFindDeclared(id)?.ReadState(p)`,
  parsed with `InvariantCulture`, and a malformed value throws rather than returning null.

### 3.4 Container (`container.md`)

- Framework container: `public partial class Foo<TParent, TSelf> : ContainerObjectBase<TParent,
  TSelf>` in a `.tpl.cs`, plus `public sealed partial class Foo<TParent> : Foo<TParent,
  Foo<TParent>>` for callers that need no subclass (a page declares
  `public Popup<MyPage> TestPopup => new(this, "TestPopup")`). Both with the two public
  constructors `(IMauiScope<TParent> parentScope, Locator locator)` and `(..., string
  locatorValue)`. A container with no Core methods and no shortcuts may stay a plain `.cs` (R5).
- The base already gives `IsExists`, `IsVisible`, `WaitExists`, `WaitVisible`,
  `AssertExists`, `AssertVisible`, `GetAttribute`, `WaitReady`, and typed children:
  `Child<TControl>(id)`, `Label(id)`, `Button(id)`, `Entry(id)`, `CheckBox(id)`. Do not
  re-declare them.
- **Core methods act on the container root.** Same contract as a control: `protected virtual
  *Core(IMauiElement element, ...)`, where `element` is the root. The generator emits the
  public members through the container's `Run*` helpers; actions and asserts return `TSelf`,
  generated setters return the parent (`SetResult`). The caller leaves with `.Parent`.
- A read that must answer for a missing container carries `[AbsenceTolerant]` and takes a
  nullable root (`Popup.IsOpenCore(element) => element != null`,
  `StateContainer.IsShowingCore(element, viewAutomationId)`).
- A capability interface (`IRefreshableControlObject<TSelf>`) is implemented on the
  container itself, with Core methods for whatever the base does not already supply
  (`RefreshView.IsEnabledCore`, `PullToRefreshCore`).
- Inside a Core method, wait with `Until(read, done, timeoutMs, out lastError)`; the container
  base has it too (R2).
- Scoping is strict: children resolve only under the root, never in the parent. This is the
  point of a container; do not add a fallback.
- **Root lookup overrides**, each with a remark giving the reason:
  - `CacheContainerRoot => false` when the app rebuilds or replaces the root during a test
    (Popup opens and closes; StateContainer swaps its children).
  - `FindContainerRootElement()` when the root is not under the parent: Popup searches
    `Context.AppElement`, because the toolkit shows it as a modal page. Throw
    `ElementNotFoundException` naming the locator.
  - A root that lives outside the raising page also answers `Page => null` and overrides
    `IsParentReady`/`WaitParentReady` to `true`: waiting for the page underneath fails exactly
    while the popup covers it.
- Content that loads asynchronously: override `WaitContentReadyCore` and wait on a concrete
  state (spinner gone, count non-zero).
- A container that scrolls answers `ScrollingRoot` with its own root (ScrollView). Otherwise
  inherit the parent's.
- A member that needs a child and the root is hand-written as plain calls in sequence, with a
  remark naming both (`Popup.CloseWith(id)`: `Button(id).Click()`, then `WaitOpen(false)` and
  a `TimeoutException` naming the button). Same rule as R4.
- Windows: layout views with no automation peer (ContentView, Border) are invisible to UI
  Automation until the app registers the Brinell handlers from `samples/Brinell.Maui.AppSupport`.
  Say so in the class remarks and check the sample registers it.
- App-specific containers (in test projects' `Containers/`) derive from `ContainerObjectBase`
  or from a framework container, take `(IMauiScope<TPage> parentScope, string automationId)`,
  expose children as named properties
  (`public Entry<ProductFormContainer> NameEntry => new(this, "ProductNameEntry")`), nest
  containers the same way (`public ProductOptionsContainer Options => new(this, ...)`), and
  compose domain helpers from those children, returning `Self` (`FillProduct`).

### 3.5 Component (`component.md`)

- `public partial class Foo<TScope> : ComponentObjectBase<TScope, Foo<TScope>> where TScope :
  IMauiScope<TScope>`, in a `.tpl.cs`. Two public constructors named `scope`, like a simple
  control, so a page declares it like one:
  `public MediaElement<MyPage> Player => new(this, "Player")`. It is a container underneath, so
  everything in 3.4 applies (strict scoping, typed children, root overrides); it adds
  `ActivityIndicator(id)` to the typed children.
- **Parts** are named, typed properties scoped to `this`, in a `#region Parts` (or a region
  named for the platform's term, `Transport Controls`):
  `public MediaPlayPauseButton<MediaElement<TScope>> PlayPauseButton => new(this, PlayPauseButtonId)`.
  Part ids are `private const string`. A part the platform publishes without an id is located
  by a `private static readonly Locator` (`AvatarView`: `Locator.ByControlType("text")`); that
  locator stays private (R6).
- **Parts own the behaviour; the component forwards** (see
  [component-operations-plan.md](component-operations-plan.md)). A plain control type is used
  where its members are enough (`Button`, `Slider`). A part that needs more gets its own
  control class, derived from the nearest simple control and named `<Component><Part>`, in the
  component's folder: `MediaPlayPauseButton : Button`, `MediaTimeLabel : Label`,
  `AvatarInitials : Label`, `AvatarImage : Image`. Its element-first Core methods read and act
  on its own element only (`MediaPlayPauseButton.SetPlayingCore` presses, then `Until` the
  name changes). The part's class remarks name the component and say what it cannot see
  ("this control cannot see the media's state, so the component waits first").
- **Absence is the part's answer.** When "not shown" is a valid state (no image set, no
  initials while an image shows), the part declares an `[AbsenceTolerant]` read
  (`AvatarImage.IsShownCore(element) => element?.Visible == true`,
  `AvatarInitials.GetInitialsCore`), because plain `IsVisible` answers null for a missing
  element and `IsExists` may scroll the app and find another element. The component then
  forwards to that read.
- **Shortcuts**, in a `#region Shortcuts`, expose part members on the component:
  `protected bool? IsPlayingShortcut() => PlayPauseButton.IsPlaying();`. Rules (checked by
  `ControlObjectAnalyzer`; a malformed shortcut fails generation): name ends in `Shortcut`;
  `protected`, not virtual; expression-bodied; exactly one `Part.Member(...)` call on a
  property or field of the class. The public name drops `Shortcut`:
  - `Is*` -> `IsX / WaitX / AssertX`, forwarding to the part's `Is*` trio.
  - `Get*` -> `GetX / WaitX / AssertX`; `[GenerateComparisons(...)]` on the shortcut forwards
    those variants, including the ordered ones (`GreaterThan`, `AtLeast`, `LessThan`,
    `AtMost`).
  - An action or setter returning the component is made public as is:
    `protected MediaElement<TScope> PauseShortcut(int? timeoutMs = null) => PlayPauseButton.Pause(timeoutMs);`.
    With a separate button per action no part class is needed:
    `PlayShortcut(int? timeoutMs = null) => PlayButton.Click(timeoutMs)`.
  The generated `Assert*` returns the component, which compiles only when the part is scoped
  to `this`, so a mis-scoped part is a compile error.
- A component with only shortcuts and hand-written members has no Core methods; that is the
  target shape (MediaElement, AvatarView). A component Core method never calls another
  control's public member: that nests one unit of work inside another (two readiness checks,
  two polls, timeouts that add up, swallowed errors).
- **Across two parts**, in a `#region Hand-written: across parts`, as plain calls in sequence,
  with no `Run*` wrapper and a remark naming the parts and why no single part answers. In
  order of preference: find one part that answers it (`WaitOpened` is
  `TimeRemainingLabel.WaitSecondsAtLeast(0)`); hand-write a `Get` only
  (`GetDuration` = elapsed + remaining, no generated `Wait`/`Assert` around it); hand-write
  an action that waits on one part, then acts on another, and throws `TimeoutException`
  naming what did not happen (`MediaElement.Play`: wait for the remaining time's clock, then
  press). Null-skip and idempotence still hold (`SetPlaying(null)` returns, `Play` is a no-op
  while playing).
- Root resolution: the default is the parent's `FindElement(Locator)`. When the platform does
  not publish the root, override `FindContainerRootElement` with a documented stand-in (the
  first part that proves the component is there, as Stepper's buttons stand in for Stepper),
  and throw naming every locator tried and the app-side requirement
  (`ShouldShowPlaybackControls`).
- A platform that flattens the parts beside the root: override `TryFindElement`,
  `FindElement` and `FindElements` to delegate to `Parent`, with remarks giving the evidence
  and the consequence (one component per scope; put each in its own container). This is the
  only licence to break strict scoping. P2 changes these overrides from public to protected.
- Class remarks record: the route per platform and that no bridge is used (or which verb is),
  why the component reads what the user sees rather than app state when those disagree
  (MediaElement's `CurrentState`, with the probe reference), and the limits (localized names,
  missing buttons, platforms that publish nothing yet).

### 3.6 Collection (`collection.md`)

- Framework collection: `public abstract partial class FooView<TParent, TSelf, TItem> :
  CollectionObjectBase<TParent, TSelf, TItem>` with protected constructors that take an
  `IItemStrategy` and an item factory `(collection, root, index) => new TItem(...)`. The
  framework does not ship concrete rows; the remarks show the derivation.
- Item strategy, in order of preference:
  - `ItemStrategy.ByAutomationId("Row")` / `ByLocator(...)`: the row id repeats on every row,
    which is normal MAUI templating.
  - `ItemStrategy.Within(host, inner)` when the root is not the element that holds the rows.
  - `ItemStrategy.ByIndexedId(prefix)` only when the app gives each row a unique id.
  - A custom `IItemStrategy` only for platform structure the built-ins cannot express.
- Items derive from `ItemContainerBase<TCollection, TSelf>` with a public
  `(collection, itemRoot, index)` constructor and named child controls. A row never locates
  itself by a unique id; it is handed its root and re-resolves through the collection when
  virtualization kills it.
- Logical index comes from `PositionInSet` where the platform publishes it; `GetItemCount`
  is the number of realized rows, not the data count. Say which one a member means.
- Override `ScrollTarget` when the scrolling element is not the root, `ActivateItemCore` when
  selecting a row is not a tap/invoke of its root, `MatchesKey` when rows are keyed another
  way.
- Collection-level controls (title, empty view, count label) are named properties on the
  collection, like any container.

## 4. Rules to write into `maui-ui-test`

### 4.1 Workflow

1. Find the page object. If it is missing, create it (4.2). If a control, container or member
   is missing, stop writing the test and add it with `maui-control` first.
2. Write the test (4.3) using only page-object members.
3. If the sample app lacks what the test needs, add it to `samples/Brinell.Samples.Maui.App`
   with `AutomationId`s and a status label that shows the result, then rebuild the app.
4. Run the smallest tier that can fail (`--filter "Control=Foo"`), one test process at a
   time, and never build while it runs.

### 4.2 Page objects (`page-objects.md`)

- `public class FooPage : PageObjectBase<FooPage>`, constructor `(IMauiTestContext context)`,
  `Name` = the page root's `AutomationId`.
- Controls are expression-bodied properties that create a fresh object on each access:
  `public Switch<FooPage> TestSwitch => new(this, "TestSwitch");`. Never cache an element or
  a control instance.
- Locators live here and only here. Prefer `AutomationId`; use `Locator.ByAccessibilityId` or
  `ByName` only where the platform gives nothing else, and never XPath.
- A page with a busy sentinel sets `BusySignalPolicy` and, if needed, `BusyAutomationId`.
- Window chrome that lives outside every page (toolbar items, Shell flyout, native alerts,
  menu bar) is scoped to `AppRoot`, not the page.
- Page-level intent methods (`FillProduct`, `Reset`) compose controls, wait for the resulting
  state, and return the page, or the next page for navigation.
- Regions become containers, repeated rows become collections, both in the test project's
  `Containers/` folder.

### 4.3 Test shape

- Class `<Control>Tests` in `Tests/<Area>/`, `[Collection("Maui")]`,
  `[Trait("Category", "UITest")]`, `[Trait("Control", "<Control>")]`; each test
  `[Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]` with `[Trait("Method", "<Member>")]`.
- The constructor takes `MauiFixture` and opens the page (`_fixture.Open(SamplePage.X)` or
  `NavigateToX()`). The fixture is shared, so a test that changes state resets it through the
  UI (a Reset button), not by relaunching.
- Name: `<Control>_<Action>_<ExpectedResult>`.
- Body: a fluent chain of actions and control `Assert*` calls, which wait. Use xUnit `Assert`
  on values read with `Get*`/`Is*` when the chain cannot express it. No FluentAssertions.
- Assert on what the user sees (status label, text, checked state), not on the view model.
- Negative cases use the API: `AssertExists(false)`, `TryItem(i)` returning null,
  `Assert.Throws<ElementNotFoundException>(...)`.
- No `Thread.Sleep`, no `Task.Delay`, no raised timeouts, no try/catch around control calls.

### 4.4 The never-list (`forbidden-apis.md`, and in `AGENTS.md`)

In tests, page objects' public members and test helpers, do not use:

| Forbidden | Instead |
| --- | --- |
| `Context.Driver`, any `IMauiDriver` member | a control or page member; add one if missing |
| a variable, parameter or return of type `IMauiElement`, or its members (`.Text`, `.Invoke()`, `.ReadState()`, `.PerformGesture()`) | the control's `Get*`/`Is*`/action |
| `FindElement`, `FindElements`, `TryFindElement` on a page, container, context or `AppRoot` | a named control property, `Child<T>(id)`, or a collection's `Item`/`Items`/`ItemWhere` |
| `ContainerRoot`, `AppElement`, `TryGetItemRoot`, `ScrollingRoot` | a container, collection or `AppRoot`-scoped control |
| `new Locator(...)` / `Locator.By*` in a test method | a page-object property |

The reason goes in the skill: raw access skips the readiness gate, the polling, the logging,
the failure messages that name the locator, and the platform routes (Windows bridge vs Android
touch). A test written against the driver works on the platform it was written on and nowhere
else, and its failure says "null reference" instead of "Switch 'TestSwitch' did not turn on".

**One exception, named so it is not a precedent:** tests whose subject *is* the driver or
bridge (`Tests/Background`, `Tests/Gestures/*Bridge*`, `Tests/Diagnostics`) exercise these
APIs on purpose. They are framework tests, marked as such (P1 below), and are not a model for
app tests.

### 4.5 Control unit tests (`unit-tests.md`)

`testsnew/Brinell.Maui.Tests` tests control logic without an app: a mocked `IMauiElement`
(`SemanticControlTestsBase.CreateElement`, `CreateInvokableElement`) is the *fixture*, and the
test still calls the control's public API through a test page. Mocks are set up in the base
or arrange section; the act and assert go through the control. Use this for logic the UI
cannot reach cheaply: stale-element recovery, idempotence, error messages, null-skip.

## 5. Existing files to correct while doing this

- `convert-control/SKILL.md` has stale facts: `CreateMaui.Bat` now covers every `Controls`
  folder in both projects, not only `Controls/Base`; the generator now *reports* near-miss
  Core methods (`ControlObjectAnalyzer`), so "silently skipped" is no longer true; the
  `Ensure` prefix rule has landed, so the "revert once the rule lands" note in the worked
  example is done. Move the contract to `generator-contract.md` and link it.
- `control-source-builder.md` step 3 ("read one finished control and match its shape"):
  replace with "read `maui-control` and the reference for the kind".
- `docs/guides/test-writing.md`: add the never-list.

## 6. Framework changes to propose

Evidence, counted today in `testsnew/Brinell.Maui.UITests` (tests and pages):
`Context.Driver` in 13 files, `Find*/TryFind*` in 12, `IMauiElement` in 8, `AppElement` in 3,
`ReadState` in 3, `PerformGesture` in 4. Most are in bridge and diagnostic tests (legitimate),
but `StepperTests`, `AlertReadTests`, `ContainerTestPage`, `NavigationDemoPage`,
`AutomationProbePage` and `ShellSamplePage` are app-level code reaching past the model.

Each proposal below is separate; P1 and P3 are the ones I would do first.

**P1. Analyzer that enforces the never-list** (new `Brinell.Maui.Analyzers`, referenced by
the MAUI test projects).
- `BRN1001` `IMauiDriver` member access; `BRN1002` `IMauiElement` as a local, parameter,
  return type or member access; `BRN1003` `Find*`/`TryFind*` on an `IElementScope`;
  `BRN1004` `ContainerRoot`/`AppElement`/`TryGetItemRoot`; `BRN1005` `Thread.Sleep` or
  `Task.Delay` in a test; `BRN1006` locator construction inside a `[Fact]`/`[Theory]`.
- Not reported inside types that derive from `ViewBase`, `RootedScopeBase` (pages,
  containers, components, collections) in their non-public members, or `IItemStrategy`
  implementations: that is where raw access belongs.
- `BRN1002` also exempts an `IMauiElement` parameter that is only passed on: an
  `ItemContainerBase` row's `(collection, itemRoot, index)` constructor and the item factory
  lambda given to `CollectionObjectBase`. Reading or calling it is still reported.
- Opt-out: `[DriverLevelTest("reason")]` on a class, for the bridge and diagnostic tests.
- Ship as warnings, fix or mark the current hits, then make them errors in the test projects.
- Why this and not only instructions: it holds for every author and every tool, and it shows
  up in the editor at the moment of writing.

**P2. Take element finding off the public surface of scopes** (breaking, done in one step: no
backward compatibility, decided 2026-09-18).
- Today `RootedScopeBase` exposes `FindElement`, `TryFindElement`, `FindElements` and
  `ContainerRoot` publicly because `IElementScope<IMauiElement>` / `IContainerObject` require
  them, and `CollectionObjectBase.TryGetItemRoot` is public for `IItemRootProvider`. That is
  why `page.FindElements(...)` compiles in a test.
- Implement those interface members explicitly and keep `protected` equivalents for
  subclasses. Controls resolve through `IMauiScope<TScope>` already (`ViewBase.MauiScope`), so
  they keep working; `MediaElement` and `ProductCollection` move from overriding/calling public
  members to protected ones. Callers outside the framework are fixed in the same change. No
  `[Obsolete]` phase.
- `Context` on pages stays public (fixtures need it); the analyzer covers `Context.Driver`.

**P3. Fill the semantic gaps that push tests to raw access**, so the rule is followable.
- **`View<TScope>`**: a concrete `ViewBase` for an element with no specific control type
  (exists, visible, enabled, name/text). Replaces the `TryFindByAutomationId` /
  `TryFindByName` helpers in `ContainerTestPage`, `NavigationDemoPage` and
  `AutomationProbePage`.
- **An alert control on `AppRoot`** (title, message, buttons, accept/cancel), if
  `ContentDialog` does not already cover native alerts. Replaces
  `Context.AppElement.ReadAlert()` in `AlertReadTests`, unless those tests are classified as
  bridge tests. Needs a check.
- **Stepper parts**: `StepperTests` looks up `TestStepperMinus`/`Plus` directly. Either the
  test is asserting platform structure (move it to diagnostics), or Stepper should expose
  `IncrementButton`/`DecrementButton`, which makes it a component. Decide per section 3.1.
- **Menu**: `MenuVerbTests` calls `Driver.InvokeMenuItem`. If `MenuItem` routes through the
  same verb, those tests are bridge tests; otherwise add the route to `MenuItem`.
- **Platform-specific tests**: `Brinell.Maui.UITests` has no platform skip attribute today.
  Tests that only make sense on one head need one (a `[PlatformFact(MauiPlatform.Android)]`
  or a trait the runner filters on), so the test skill can tell agents what to do instead of
  leaving them to invent a runtime `if`.

**P4. Make `MauiFixture` navigation typed**: `Open(SamplePage.X)` followed by
`new XPage(_fixture.Context)` lets a test open one page and construct another. An
`Open<TPage>()` that returns the loaded page removes that mismatch and the need for tests to
touch `Context` at all.

## 7. Steps

1. Write `generator-contract.md` and `routes.md` from `convert-control` and AD-005/AD-008;
   slim `convert-control` to point at them and fix its stale facts (section 5).
2. Write `maui-control/SKILL.md` (workflow, 3.1, 3.2) and the four kind references
   (3.3 - 3.6).
3. Write `maui-ui-test/SKILL.md` (4.1, 4.3, 4.4) and its references (4.2, 4.4, 4.5).
4. Write the `.claude/skills` stubs, copying each `description` exactly. Add the never-rules
   and skill pointers to `AGENTS.md`, the pointer to `.github/copilot-instructions.md`, the
   never-list to `docs/guides/test-writing.md`. Check that a Claude session lists all three
   skills.
5. Update `control-source-builder.md` step 3.
6. **Evaluate the skills**, with fresh agents that get only the task and the skill, not this
   conversation:
   - control: "add a `ViewBase` control for X", "add a component for Y" on a scratch branch;
     check the kind chosen, Core contract, routes, docs, and whether it built and passed
     tier 1 without being told.
   - test: "write tests for `RatingView`"; check that nothing in the never-list appears and
     that missing members were added to the control rather than worked around.
   Tighten the wording wherever an agent went wrong, then run again.
7. Propose P1 - P4 separately; each is its own change with its own verification. P1 lands
   with its current hits marked `[DriverLevelTest]` or fixed.

## 8. Questions for you

1. Decided: skills live in `.github/skills`, with stubs in `.claude/skills`.
2. Decided: no backward compatibility unless asked. P2 removes the public surface in one step.
3. Are the bridge, gesture and diagnostic tests the complete list of "driver-level" tests, or
   are some of them app tests that should be migrated?
