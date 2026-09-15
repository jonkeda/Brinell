# Fixing the last skipped tests

**The goal.** The Windows MAUI suite with nothing set runs 296 tests: 282 passed, **14 skipped**, 0
failed (2026-09-14, after [no-physical-input.md](no-physical-input.md) step 6). This plan takes the
skips to **zero that hide a defect**, and settles what to do with the one that is skipped by design.

| Skips | Tests | What it is | Section |
|---:|---|---|---|
| 11 | `Range/StepperTests` | A resolution bug in the `Stepper` control object, plus two defects behind it | [1](#1-stepper-11-tests) |
| 2 | `Collection/ProductCollectionTests` 13 and 14 | Index-based collection API cannot address a virtualized row | [2](#2-collectionview-2-tests) |
| 1 | `Diagnostics/NavigationStressTests` | Opt-in stress reproduction, skipped on purpose | [3](#3-the-stress-test-1-test) |

Order: **Stepper first** (root cause known, smallest change), **CollectionView second** (needs one
measurement before the design is chosen), **stress test last** (a decision and one long run).

**Working rules for every step** (from `AGENTS.md` and the memory notes):

- Run the smallest tier that can falsify the change; the full suite only at the end of a section.
- One UI test process at a time; no builds while a run is live.
- Rebuild the sample app whenever anything under `samples/` changed.
- `OccludedScreenshotTests` fails when other always-on-top windows are open. Rule that out before
  reading a full run as a regression.

---

## 1. Stepper: 11 tests

### 1.1 What is actually wrong

**The skip message is out of date.** It says "`TestStepper` does not resolve on Windows", and the
stage-J notes in `.my/extension/steps.md` step 31 conclude that a peer for `MauiStepper` cannot be
added. Both are true, but they are not why the tests fail. The control object already works around
the missing peer: `Stepper.tpl.cs` has a Windows **button mode** that finds `TestStepperMinus` and
`TestStepperPlus`, which MAUI does publish.

`Stepper_IsExists_ReturnsTrue` and `Stepper_IsVisible_ReturnsTrue` are **not** skipped, and they pass
through that button mode. The eleven skipped tests all perform an action or read a value, and those
go down a different path. Three defects, found by reading the code, not yet confirmed by a run:

1. **Actions never reach button mode.** `Stepper` overrides `TryFindElement()`, which the
   exists/visible checks use. But every action (`RunDoWithElement`, `RunSetWithElement`) and every
   read (`RunGetWithElement`, `RunAssertWithElement`) goes through `ViewBase.FindElement()`, which
   `Stepper` does not override. `FindElement()` looks for `TestStepper` itself, finds nothing, and
   throws `ElementNotFoundException`. **This is the "does not resolve".**
2. **Increment and decrement would now throw.** Button mode calls `_plusButton.Click()` and
   `_minusButton.Click()`. Since step 2 of the physical-input work, `FlaUIMauiElement.Click()` is the
   `Tap` verb or `NotSupportedException`, and the buttons declare no `Tap`. Even with defect 1 fixed,
   every increment would throw.
3. **The value is read by guessing a label name.** `TryFindValueLabel()` tries `TestLabel`,
   `TestStepperValue` and `TestStepperLabel`. The sample's label is `StepperValueLabel`, so no guess
   matches, `GetValueCore` returns null, and `SetValueCore` starts from 0 and clicks the wrong number of
   times. Guessing an app's label naming convention cannot work in general anyway.

There is a smaller fourth problem: `CanIncrement()` and `CanDecrement()` find out by **pressing the
button and undoing it**, which changes the app to answer a question.

### 1.2 The fix

Keep the platform control alone, as step 31 advised: resolve the Stepper through its two buttons, act
through their Invoke pattern, and read its numbers from the app.

**Why these routes pass AD-008's three admission tests.** Increment and decrement are button presses,
and a button has a real UI Automation route (Invoke), so no verb is added for them. The value,
minimum, maximum and step have no route at all, because the Stepper has no element in the tree, so
reading them through `GetState` is the bridge's intended use. `GetState` is a read, so the
accessibility audit is unaffected.

#### Step S0 — Measure before changing anything

- Unskip `Stepper_IsEnabled_ReturnsTrue` and `Stepper_SetValue_UpdatesDisplay` only, run
  `--filter "FullyQualifiedName~StepperTests"`, and record the exception each throws. Expected:
  `ElementNotFoundException` for `TestStepper` from `FindElement()`. If it is anything else, stop and
  update 1.1 before going on.
- Add a short probe (report, not assert, like `NavigationProbeTests`) that resolves `TestStepperPlus`
  and `TestStepperMinus` and prints `SupportsInvoke`, `Enabled`, and the result of one `Invoke()` on
  Plus followed by the text of `StepperValueLabel`. This answers the one real risk in this section: whether
  invoking MAUI's Stepper buttons raises the value change or, like `ToolbarItem`, reports success and
  does nothing.

**Done when** the actual failure and the Invoke result are written into this section.

**Measured 2026-09-14.** Both enabled probes failed with
`ElementNotFoundException` for `AutomationId:TestStepper`: `AssertEnabled` reached
`RunAssertWithElement`, and `SetValue` reached `RunSetWithElement`. Both Plus and Minus were found,
enabled, and reported `SupportsInvoke=True`. Invoking Plus returned and changed
`StepperValueLabel` from `5` to `6`.

#### Step S1 — Resolve the same way on every path

In `Stepper.tpl.cs`:

- Replace the mutable `_usingButtonMode`, `_minusButton`, `_plusButton` and `_valueLabelElement`
  fields with one private method that returns what it found, for example
  `StepperParts? ResolveParts()` returning the native element (Android, iOS) **or** the Minus/Plus pair
  (Windows).
- Override **both** `TryFindElement()` and `FindElement()` to use it. `FindElement()` throws
  `ElementNotFoundException` naming all three ids it tried when neither shape is present.
- The Minus button stays the proxy element for existence, visibility and enabled checks, as today.

**Verify.** `Stepper_IsEnabled_ReturnsTrue` passes; `IsExists` and `IsVisible` still pass.

#### Step S2 — Increment and decrement through Invoke

- `IncrementCore` and `DecrementCore` call `Invoke()` on the Plus or Minus button. On Android and iOS
  `AppiumMauiElement.Invoke()` is a tap, so one call serves every platform.
- Keep `FindChildButton` only for the native-element shape (Android and iOS).
- **If S0 showed Invoke does nothing** on these buttons: do not fall back to a click, because there is
  none on Windows. Instead add an element-action verb declared on the Stepper, for example
  `StepValue` taking +1/-1 on `Invoke`. `ContractTests` then forces its classification. That is the
  `ToolbarItem` situation again and gets the same treatment as `InvokeToolbarItem`.

#### Step S3 — Read the numbers from the app

- **App side** (`samples/Brinell.Maui.AppSupport/Uia/MauiVerbDispatcher.cs`, `ReadState`): add
  `"Value"`, `"Minimum"`, `"Maximum"` and `"Increment"` cases `when element is Stepper`, formatted with
  `CultureInfo.InvariantCulture`.
- **Sample** (`samples/Brinell.Samples.Maui.App/Views/RangeView.xaml`): declare
  `uia:GestureAutomation.Verbs="GetState"` on `TestStepper`.
- **Client:** the Stepper has no element to call `ReadState` on, since the Minus button's id is not the
  Stepper's. Add a by-id read to the driver, next to the existing by-id gesture calls:
  `IMauiDriver.ReadState(string automationId, string property)` and
  `IMauiDriver.SupportsStateReads(string automationId)`, with defaults that throw and return false.
  `FlaUIMauiDriver` implements both through `BridgeVerbRunner.Send` and `Supports`.
- **Control:** `GetValueCore`, `GetMinimumCore`, `GetMaximumCore` and `GetStepCore` ask the driver
  when it supports state reads for the Stepper's id. Otherwise they use the base implementation
  (Android and iOS). Delete `TryFindValueLabel` and `ExtractNumericValue`.

#### Step S4 — SetValue and the Can* questions

- `SetValueCore`: clamp the target to Minimum/Maximum, compute the presses from Value and Increment,
  invoke that many times, then wait until `Value` reads the clamped target. Wait on the read value,
  not on a delay (AD-004).
- `CanIncrement()` is `Value < Maximum`, and `CanDecrement()` is `Value > Minimum`. The
  press-and-undo goes.
- Regenerate `Stepper.gen.cs` if any `*Core` signature changed (`convert-control` skill: the generator
  CLI on the single file).

#### Step S5 — Unskip and close

- Remove all eleven `Skip =` arguments.
- Run `--filter "FullyQualifiedName~Tests.Range"`: expect 22 passed, 0 skipped.
- Mark step 31 **done** in `.my/extension/steps.md`, with a line saying the peer finding still stands
  and the cause was the control object.

**Done when** the Range tier is 22/22 and nothing in `Stepper.tpl.cs` guesses a name or clicks.

**Done 2026-09-14 (S1-S5).** Invoke works on the Stepper's buttons (S0), so no `StepValue` verb was
added.
- `Stepper.tpl.cs` resolves through `ResolveParts()` on both find paths, invokes the buttons, and
  reads Value, Minimum, Maximum and Increment by id through the new
  `IMauiDriver.ReadState(automationId, property)` / `SupportsStateReads(automationId)`.
- `SetValue` clamps the target and waits on the read value. `CanIncrement` and `CanDecrement` are
  comparisons.
- The label guessing, the press-and-undo and the stale click comments are gone, and
  `Stepper.gen.cs` is regenerated.
- The dispatcher answers the four Stepper properties, and `TestStepper` declares `GetState`.
- Range tier: **23 passed, 0 skipped**, which is the 22 tests plus the S0 probe
  `StepperButtons_ReportInvokeBehavior`. Step 31 is marked done in `steps.md`.

**Not covered:** Android. The memory note records Range at 6/16 failing on Android before any change.
This section changes the native-element path only where it must (S2), and does not try to fix Android.

---

## 2. CollectionView: 2 tests

### 2.1 What is actually wrong

`ScrollToItem_MaterializesOffscreenRow` (13) and `ItemWhere_ScrollsToFindOffscreenRow` (14) add 60
bulk rows to the 3 seed rows (`BulkTotal` = 63). Test 13 then asks for row 60; test 14 asks for the row
named "Bulk Product 55". MAUI keeps only about 30 row containers in the tree and recycles them as
the list scrolls.

**The skip message is partly out of date.** It says scrolling works through the UIA Scroll pattern.
Since then the sample declares `ScrollToIndex` on `ProductCollectionView`, and
`CollectionObjectBase.TryMaterializeMore` uses it. Scrolling is not the problem. **Indexes are**:

- **`Item(int)` / `TryItem(int)` are positional over the realized rows.** `TryGetItemRoot(60)` asks the
  item strategy for the 61st row *currently in the tree*. With ~30 realized rows that never exists, at
  any scroll position. Test 13 cannot pass under that definition, however well it scrolls.
- **`TryMaterializeMore(countBefore)` passes a realized count as a logical index.** It calls
  `ScrollToIndex(countBefore)` with `countBefore` ≈ 30, then succeeds only if the realized count grew.
  On a recycling list the count stays ≈ 30 while the window slides, so the walk stops after one step.
- **`FindItem` checks only indexes past the previous count.** Once the window slides, the new rows sit
  at *low* positions, not at `seen..count`, so they are never examined. Test 14 stops without reaching
  row 55.
- **`ScrollToItem` measures progress by count** (`FurthestReachableIndex` is `count - 1`), which
  plateaus at ≈ 29 for the same reason.

**And a bridge defect found on the way.** `MauiVerbDispatcher` answers `ScrollToIndex` past the end
with `S_FALSE`. It reaches the client as `S_OK` (step 43's finding), so a walk cannot hear "end of
list". `FlaUIMauiElement.ScrollToIndex` only throws on a failure code.

### 2.2 The fix

**One decision first:** what an index on a collection object means.

- **Recommended: a logical index, the position in the app's items, wherever the platform can say.**
  This is what both tests and any reader assume: `Products.Item(60)` is the 61st product. The current
  realized-window meaning is an implementation detail that leaked into the API, and it makes
  `Item(i)` return a different product at every scroll position.
- The alternative is to keep positional indexes and add separate logical members (for example
  `ItemAtLogicalIndex`). That is smaller, but leaves a trap in the most obvious API.

The steps below assume the recommendation. The measurement (C0) decides *how* a row's logical index
is obtained.

#### Step C0 — Measure how a row can know its index

Add `CollectionIndexProbeTests` (report, not assert). On the Grid collection page, after
`BulkAdd` has reached a logical count of 63:

1. At the top: realized row count, and for the first and last realized `ProductRow` print the name
   label text, and UIA `PositionInSet` / `SizeOfSet` read from the row root **and from its nearest
   ancestor** (the WinUI item container is likely the one carrying them).
2. Call `ScrollToIndex(60)` on `ProductCollectionView`, wait for the realized rows to change, and
   print the same again.
3. Check whether realized rows appear in the tree in logical order, i.e. whether the name numbers
   increase down the list.

What the answer selects:

| Result | Route for a row's logical index |
|---|---|
| `PositionInSet` is present and logical, i.e. row "Bulk Product 55" reports 58 | **A.** Read it from UI Automation; no bridge change |
| Absent or positional, but tree order is logical | **B.** Anchor on a scroll: the app scrolls item *i* to the **start** of the viewport, so the first on-screen row is *i* |
| Neither | **C.** Stop and re-plan; do not guess |

**Done when** the table row that applies is written here with the printed numbers.

**Measured 2026-09-14: route A.** At the top, 28 rows were realized. `ProductRow` itself reported
`PositionInSet=-1` / `SizeOfSet=-1`, while its immediate `ListViewItem` ancestor reported `1/63`
for Keyboard and `28/63` for Bulk Product 24. After `ScrollToIndex(60)`, 24 rows were realized in
logical order, from Bulk Product 35 through Bulk Product 58; their ancestors reported `39/63`
through `62/63`. Logical indexes therefore come from the nearest ancestor carrying positive UIA
set metadata.

#### Step C1 — Make ScrollToIndex honest

- App side: `ScrollToIndex` past the end returns `BRINELL_E_DECLINED`, not `S_FALSE`, like
  `SelectIndex` and `NavigateBack` after step 43. Update the dispatcher comment that calls `S_FALSE`
  "not a refusal".
- If C0 chose **B**: accept an optional position in `arg2` (0 = MakeVisible, 1 = Start) and scroll with
  `ScrollToPosition.Start` when asked. Append-only, so the verb number and transport stay the same.
- Add `"ItemCount"` to `ReadState` for `ItemsView` (the `ItemsSource` count), next to the existing
  `Picker` case, and declare `GetState` on `ProductCollectionView`. A walk needs the logical end,
  and the count label is a sample convenience rather than something the framework can rely on.
- Client: `FlaUIMauiElement.ScrollToIndex` reports the decline distinctly, for example
  `ArgumentOutOfRangeException` naming the count, so a walk can stop on it.

**Verify.** `ScrollVerbTests` still passes, plus a new test: `ScrollToIndex(ItemCount)` is refused
with the count in the message.

#### Step C2 — Logical indexes in `CollectionObjectBase`

- Add one internal question on the item side, for example `int? LogicalIndexOf(IMauiElement row)`,
  answered by route A or B. Null means the platform cannot say (Android, or an app without the
  bridge), and then the collection keeps today's positional behaviour, documented as such.
- `TryItem(int index)` / `Item(int index)`: when logical indexes are available, find the realized
  row whose logical index is `index`. If none, return null / throw; do **not** scroll inside a `Try`.
- `ScrollToItem(int index)`: `ScrollToIndex(index)` (Start position under route B), wait until
  a realized row reports logical index `index`, and throw if it never does. The count-based
  `FurthestReachableIndex` loop goes.
- `FindItem(predicate)`: check all realized rows; then, while the last realized logical index is
  below `ItemCount - 1`, `ScrollToIndex(lastLogical + 1)` and check **all realized rows again**,
  skipping logical indexes already seen. Stop at the logical end, which is exact after C1.
- `TryMaterializeMore(countBefore)`: pass the last realized **logical** index + 1, not a count.
- Rows built by `_itemFactory` get their logical index, so `row.Index` means the same as the `Item(i)`
  that returned it.

**Verify.** The Collection tier (`--filter "FullyQualifiedName~Tests.Collection"`) with 13 and 14
still skipped: nothing that passes today may break. Existing tests only use indexes near the top, where
positional and logical agree.

#### Step C3 — Unskip and close

- Remove the two `Skip =` arguments. Check that test 13 still expresses the intent under logical
  semantics: `ScrollToItem(60)` then `TryItem(60)` should now be exactly right, without changing the test.
- Run the Collection tier, then the Background tier (it contains `ScrollVerbTests` and
  `SelectionVerbTests`, which share the verbs).
- Update the `CollectionModuleView.xaml` comment that says only ~30 of 63 rows exist, and point it here.

**Done when** the Collection tier has no skips and `CollectionObjectBase` has no count-as-index
arithmetic left.

**Done 2026-09-14 (C1-C3), route A.**
- **App side.** `ScrollToIndex` past the end answers `BRINELL_E_DECLINED`, and `ReadState` answers
  `ItemCount` for any `ItemsView`. `ProductCollectionView` declares `ScrollToIndex,GetState`.
- **Client side.**
  - `FlaUIMauiElement.ScrollToIndex` turns the decline into `ArgumentOutOfRangeException` naming the
    count.
  - `IMauiElement.PositionInSet` / `SizeOfSet` read the nearest ancestor that publishes them (the
    `ListViewItem`).
  - `CollectionObjectBase` uses them for `Item(i)`, `ScrollToItem`, `FindItem` and the
    materialization walk. It keeps positional behaviour when no row reports a position (Android,
    or no bridge).
- **Tests.** New `ScrollVerbTests.ScrollToIndex_AtItemCount_IsRefusedWithTheCount`. The C0 probe
  stays as `CollectionIndexProbeTests`.
- **Results.** Collection tier: **16 passed, 0 skipped**. Background tier: **61/61**.
- **Earlier Background run.** The first one stalled after `ShellCollectionParallelismTests` waited
  its full 60 s for the Hub half. It was stopped, and the rerun passed with the pair at 362 ms. That
  was not reproduced, and it did not recur in the full run.
- The `CollectionModuleView.xaml` comment now points here.
- **Race found later the same day.** `ItemWhere_ScrollsToFindOffscreenRow` failed once in a full
  run, and every time once `LogicalIndexOf` read `PositionInSet` only once.
  - The walk read rows while the list was still recycling after `ScrollToIndex`.
  - It is fixed in `CollectionObjectBase`: it waits for the realized indexes to settle and
    re-checks a row's index after the predicate reads it.
  - See [supports-properties.md](supports-properties.md) section 7.

**Not covered:** Android. `AppiumMauiElement` has no logical index source; under C2 it keeps
positional behaviour, and that is stated in the XML docs rather than silently different.

---

## 3. The stress test: 1 test

`NavigationStressTests.NavigatingWhileTheBridgeIsBusy_NeverRetiresTheWindowElement` is skipped
unless `BRINELL_STRESS=1`. It is a 150-page reproduction of
`.my/fix/rca-app-freeze-was-a-stale-root.md` and takes minutes, so the skip is not hiding a defect.
**Recommendation: keep it opt-in.** The work is to prove it still passes and to stop it reading as a
failure to fix.

#### Step X1 — Run it now

It has not run since the bridge gained `InvokeToolbarItem` and the driver lost its physical fallback,
and both touch navigation. With nothing else running:

```powershell
$env:BRINELL_STRESS = "1"
dotnet test testsnew\Brinell.Maui.UITests --filter "Pattern=Stress" -v:minimal /nr:false
Remove-Item Env:BRINELL_STRESS
```

**Done when** it passes with 0 root re-attachments, or a failure is written up against the RCA.

**Done 2026-09-14.** Passed in 1 m 33 s with all the changes above in place.

#### Step X2 — Decide how it is reported

Two options, and the first is the default:

- **Leave the `StressFact` skip.** `AGENTS.md` already lists it as a deliberate skip. The full-suite
  expectation becomes "0 failed, 1 skipped (the stress test)".
- **Exclude it by filter instead of skipping it.** A `.runsettings` wired through `RunSettingsFilePath`
  in the test project, with `TestCaseFilter` set to `Pattern!=Stress`, and `StressFact` reduced to a
  plain `Fact`. A normal run then reports 0 skipped. **Measure before choosing it**: confirm that a
  command-line `--filter "Pattern=Stress"` *replaces* the runsettings filter instead of being
  combined with it. If they combine, the test becomes unreachable, and this option is out.

---

## 4. End state

| | Now | After 1 | After 2 | After 3 (default) |
|---|---:|---:|---:|---:|
| Total | 296 | ≈297 (+S0 probe) | ≈299 (+C0 probe, +C1 test) | same |
| Skipped | 14 | 3 | 1 | 1, the stress test |
| Failed | 0 | 0 | 0 | 0 |

Totals are estimates; count after each full run instead of trusting them.

**Measured 2026-09-14, full suite with nothing set.**

| Total | Passed | Skipped | Failed | Time |
|---:|---:|---:|---:|---:|
| 299 | 297 | 1 | 1 | 3.9 min |

- **Skipped:** the stress test. X2 takes the default and leaves the `StressFact` skip, which
  `AGENTS.md` already lists as the only deliberate skip.
- **Failed:** `OccludedScreenshotTests` ("The occluding window did not cover the screen"), the known
  environmental failure. Run alone straight afterwards, it passed.

**Afterwards:** update `AGENTS.md`'s expected-skips note (Stepper and CollectionView leave it), and
`docs/platform-guides/maui.md` if C2 changes what `Item(int)` means for users.
