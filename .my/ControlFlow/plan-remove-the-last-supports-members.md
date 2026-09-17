# Plan: remove the last `Supports*` members from `IMauiElement`

**Where this starts.** After [design-every-call-through-the-element.md](design-every-call-through-the-element.md),
controls reach the app only through `IMauiElement`. Six `Supports*` members are still on the
interface:

- `SupportsInvoke` and `SupportsSelect`
- `SupportsDropdown`
- `SupportsGesture(gesture)`
- `SupportsStateReads`
- `SupportsScrollToIndex`

**The goal.** Remove all six. The design document kept these because they are *real* route
choices: on Windows the false branch is a working route, not a throw. So removing them is not
"push the fallback into Appium" as it was last time. Each one needs its own answer:

| Member | Replaced by | Kind of change |
|---|---|---|
| `SupportsGesture` | nothing: no control asks it | delete, make it private in FlaUI |
| `SupportsDropdown` | `bool? IsDropdownOpen`, a `CloseDropdown` that tolerates no dropdown, `ReadDropdownItemTexts()` | the question becomes a nullable read |
| `SupportsInvoke`, `SupportsSelect` | `Activate()` | the choice moves into the element |
| `SupportsStateReads` | `string? ReadState(property)`: null when the app does not answer | the question and the read become one call |
| `SupportsScrollToIndex` | `ScrollStep ScrollTowards(int index)` | the choice moves into the element; the riskiest step |

Measured by reading the code on 2026-09-15. Nothing has been run for this plan.

---

## 0. Why each one can go

The rule in the design document allowed a `Supports*` question when both answers occur on the
same backend and both branches work there. All six pass that rule. Three things still make them
worth removing:

- **A question plus a command costs two round trips.** On FlaUI, `SupportsStateReads` walks the
  bridge and `ReadState` walks it again; `SupportsScrollToIndex` does the same.
- **Appium answers most of them with a constant.** `SupportsInvoke` and `SupportsSelect` are
  always true, and `SupportsDropdown`, `SupportsStateReads` and `SupportsScrollToIndex` are
  always false. Every control that asks one of these has a branch that never runs on mobile.
- **Some are already answered better as a result.** The last design turned `IsFlyoutOpen` and
  `ReadItemTexts` into nullable reads, where null means "this platform does not say", and that
  read well. The same convention already covers `Checked`, `RangeValue` and `Value`.

**The rule after this plan:** `IMauiElement` asks no questions. Every member performs, reads, or
reads nullably. The driver's `SupportsGesture(id, gesture)` is out of scope (see 7).

---

## 1. `SupportsGesture` — delete

**Used by:**

| Caller | Use |
|---|---|
| `FlaUIMauiElement.Click` and `Focus` | internally |
| `FlaUIDeclaredElement` | implements it |
| `AppiumMauiElement` | implements it, and its own `PerformGesture` documentation refers to it |
| Control objects | **none** |
| UI tests | **none**. The gesture tests ask the driver's by-id form. |

**Change:**

- Remove it from `IMauiElement`.
- In `FlaUIMauiElement`, rename it to a private `DeclaresGesture`.
- In `FlaUIDeclaredElement`, delete it.
- In `AppiumMauiElement`, make it private. Its `PerformGesture` already throws for the gestures
  it cannot perform.
- Fix the crefs in `IMauiDriver.SupportsGesture` and in `TryFindDeclared`.

**Risk:** none. No caller changes behaviour.

---

## 2. `SupportsDropdown` — the question becomes a nullable read

**Used by:**

| Caller | What false does |
|---|---|
| `Picker.OpenFlyoutCore` | throws a `BrinellException`: "cannot be expanded" |
| `Picker.CloseFlyoutCore` | nothing |
| `Picker.IsFlyoutOpenCore` | false |
| `Picker.GetDropdownItemTextsCore` | null |
| `FlaUIMauiElement` | internally, in selection, `ReadItemTexts` and `SelectedItemText` |

**New dropdown members on `IMauiElement`:**

```csharp
bool? IsDropdownOpen => null;                          // null: this element has no dropdown
void OpenDropdown();                                   // performs or throws, as today
void CloseDropdown() { }                               // no-op when there is none, or it is shut
IReadOnlyList<string>? ReadDropdownItemTexts() => null; // open, read, restore; null: no dropdown
string? SelectedItemText => null;                      // unchanged
```

**Removed:** `SupportsDropdown`, and `ReadDropdownItems()`. The latter returns elements that go
stale when the dropdown closes, so only a caller that holds the dropdown open can use it. That
caller is now FlaUI's own selection route, so it becomes private there. `WithDropdownOpen` leaves
`SelectorControlBase` for the same reason.

**What `Picker` becomes:**

```csharp
protected virtual void OpenFlyoutCore(IMauiElement element, int? timeoutMs = null) => element.OpenDropdown();
protected virtual void CloseFlyoutCore(IMauiElement element, int? timeoutMs = null) => element.CloseDropdown();
protected virtual bool? IsFlyoutOpenCore(IMauiElement? element) => element?.IsDropdownOpen ?? false;
protected virtual IReadOnlyList<string>? GetDropdownItemTextsCore(IMauiElement? element) => element?.ReadDropdownItemTexts();
```

**`CloseDropdown` becomes lenient.** Today the interface says it throws when there is no
dropdown. Closing something that cannot be open already has the state that was asked for, which
is the same reasoning `CloseFlyout` uses for "already shut is success".

**One behaviour change: the exception type.** `Picker.OpenFlyout` on an element with no dropdown
threw `BrinellException`; it will throw FlaUI's `NotSupportedException`, which names the missing
ExpandCollapse pattern. Check no test asserts the old type:
`grep "cannot be expanded" testsnew`.

**FlaUI:** `ReadItemTexts` delegates to `ReadDropdownItemTexts`. The two are the same read on
Windows today; they stay two members because they mean different things ("every item" versus
"what the popup shows"), as documented on `Picker`. **Appium:** keeps the defaults.

---

## 3. `SupportsInvoke` and `SupportsSelect` — `Activate()`

**Used by:** three copies of `TryActivate` in the Extensions project, plus unit-test mocks and a
diagnostic message in `StepperTests`:

| File | Candidates it walks |
|---|---|
| `SelectionList.tpl.cs` | the containing `ListItem` rows, then the matched element |
| `GenericBrowser.tpl.cs` | item button, row, label, close button |
| `EditableField.cs` | native button, icon, root, OK button |

Each copy asks `SupportsSelect`, then `SupportsInvoke`, then falls back to `Click()`. That is the
one place a control genuinely does not know what an element is: it walks candidates of mixed
kinds.

**New member:**

```csharp
/// Performs whatever activating this element means on this platform. Performs or throws.
/// For callers walking candidates of unknown kind; a control that knows its operation names it.
void Activate() => throw new NotSupportedException(...);
```

| FlaUI | Appium |
|---|---|
| SelectionItem pattern, then `Select`; otherwise Invoke pattern, then `Invoke`; otherwise the declared `Tap` verb; otherwise throw naming all three. Chosen by which pattern is **present**, never by trying one and falling back when it fails. That is the same choice `TryActivate` makes today. | `Click()` |

The three `TryActivate` methods become `try { element.Activate(); return true; } catch { return false; }`,
and the bounds guards stay where they are.

**This is not the activation ladder coming back**
(`.my/fix/design-controls-know-how-to-click.md`). The ladder *tried* a rung and fell to the next
when it failed, and two rungs were measured reporting success falsely. `Activate` chooses once by
presence and runs one route. The `ToolbarItem` lie cannot reach it, because `ToolbarButton` calls
`InvokeToolbarItem`, and no LegacyIAccessible rung is added.

**Remove:** `SupportsInvoke` and `SupportsSelect` from the interface. FlaUI keeps private
`HasInvokePattern` and `HasSelectionItemPattern` for `Perform`.

**Tests:**

- `SemanticControlTestsBase.CreateInvokableElement` and `CreateSelectableElement` also set up
  `Activate()` with the same callback. Moq does not run default interface members, so without
  that setup `TryActivate` would report success and the callback would never fire.
- Remove the dead `SupportsInvoke` setups in `TabMenuTests` and `ClickActivationTests`.
- `StepperTests` prints `Enabled` only.

**Coverage gap:** the Extensions controls have no Windows UI tests. Their mock tests in
`Brinell.Maui.Tests` are the check, plus `Brinell.Presenter.Uat.Tests` where it uses them.

---

## 4. `SupportsStateReads` — `ReadState` returns null when the app does not answer

**Used by:**

| Caller | Reads | When the app does not answer |
|---|---|---|
| `Picker` ×4 | `SelectedItem`, `SelectedIndex`, `Items`, `ItemCount` | the base selector reads |
| `Image` | `Source`, `IsLoading` | size for `IsLoaded`, null for `Source` |
| `ProgressBar` | `Progress` | the RangeValue pattern |
| `Stepper` | `Value`, `Minimum`, `Maximum`, `Increment` (through `TryFindDeclared`) | the base range reads |
| `CollectionObjectBase.GetLogicalItemCount` | `ItemCount` | null |
| `SelectionVerbTests.Pickers_OfferTheSemanticRoute` | asserts the flag | — |

**New signature**, replacing both the flag and today's `string ReadState`:

```csharp
/// A named piece of app state, or null when the app does not answer state reads on this element.
/// Throws when it does answer and refuses this name: an unknown name is a defect, not an absence.
string? ReadState(string property) => null;
```

**Why a nullable read and not typed members.** Typed members, such as `ImageSource` and
`Progress`, would put MAUI control vocabulary into the element interface: one member per control
property, and the element deciding that a progress bar runs from 0 to 1. The property names are
already the app provider's contract, and the controls already own parsing them. Null for "the app
does not say" is the convention `Checked`, `RangeValue` and `IsFlyoutOpen` use.

**FlaUI: one bridge walk instead of two.** `BridgeVerbRunner.TryResolve` does not check that the
verb is declared, and a provider answers `UIA_E_NOTSUPPORTED` both for an undeclared verb and
possibly for a name it does not handle. Telling "not declared" apart from "refused" therefore
needs the declaration. Add `BridgeVerbRunner.SendIfDeclared(root, automation, id, verb, argument)`:

- resolve the target once;
- return a "not declared" result when `SupportedVerbs()` lacks the verb;
- otherwise exchange on the pattern it already holds.

`ReadState` maps "not declared" to null and a refusal to today's `NotSupportedException`.
`FlaUIDeclaredElement.ReadState` does the same check on the target it already holds.

**What the callers become:**

```csharp
// Picker
protected override string? GetSelectedTextCore(IMauiElement? element)
    => element?.ReadState("SelectedItem") is { } selected
        ? (selected.Length == 0 ? null : selected)
        : base.GetSelectedTextCore(element);

// Stepper
private double? ReadNumericState(string property)
    => StateSource()?.ReadState(property) is { } value ? Parse(value, property) : null;
// used as: ReadNumericState("Value") ?? base.GetValueCore(element)
```

`Image.IsLoadedCore` reads `Source` first. If that is null it takes the size route, and it reads
`IsLoading` only when `Source` answered.

**One subtlety to keep:** `Picker` has to call `ReadState` **once** per read and branch on the
result. Calling it twice, once to decide and once to use, would bring back the double walk under
a different name.

**Tests:** `SelectionVerbTests.Pickers_OfferTheSemanticRoute` asserts
`Assert.NotNull(picker.ReadState("ItemCount"))`. `SelectIndex_WhereMauiCannotHold…` already reads
`ReadState("ItemCount")` and keeps compiling. `FlaUIMauiElement.ScrollToIndex`'s item-count message
uses `ReadState(...) ?? "unknown"` instead of the driver's pair.

---

## 5. `SupportsScrollToIndex` — `ScrollTowards(index)`

**This is the riskiest step, and the one to abandon if the collection tier says so (see 5.3).**

**Used by:** `CollectionObjectBase`, twice.

| Caller | True | False |
|---|---|---|
| `ScrollToItem(index)` | check the logical count, `ScrollToIndex(index)`, poll until the row resolves | loop `TryMaterializeMore` until the row resolves or reach stops growing |
| `TryMaterializeMore(next)` | `ScrollToIndex(next)`, then wait for progress and for the realized indexes to settle (the CollectionView recycle-race fix) | `ScrollIntoView` the last realized row; if that realized nothing new, `ScrollHelper.StepForward` |

Only `GridCollectionDemoView` declares `ScrollToIndex`. `ProductCollection` takes the false
branch, so the Windows suite covers both today.

### 5.1 New member

```csharp
/// Moves the content towards the item at an index. Performs or throws.
/// Where the platform can jump to an index it does, and reports Jumped; otherwise it scrolls one
/// step, as ScrollContent does. Past the end: ArgumentOutOfRangeException, as ScrollToIndex today.
ScrollStep ScrollTowards(int index);
```

This adds `ScrollStep.Jumped` (5.3), which only `ScrollTowards` returns.

| FlaUI | Appium |
|---|---|
| `ScrollToIndex` verb when declared, including its settle, returning `Jumped`. Otherwise `ScrollContent(1)`. | `ScrollContent(1)` |

`ScrollToIndex` itself stays on the interface. `SupportsScrollToIndex` goes.

### 5.2 What the collection becomes

**`ScrollToItem`** becomes one loop:

- check the logical count first, whether or not the platform can jump. `GetLogicalItemCount`
  already uses `SizeOfSet` or the nullable `ReadState("ItemCount")`;
- `ScrollTowards(index)`;
- poll until the row resolves; stop on `NotMoved` or when reach stops growing.

With the verb, the first call jumps and the row resolves, which is today's path. Without it, each
call is one step, which is today's materialize loop.

**`TryMaterializeMore`**, in the shape 5.3 recommends:

```csharp
var before = FurthestReachableIndex();
var step = target.ScrollTowards(nextIndex);    // ArgumentOutOfRange → return false, as today

return step switch
{
    ScrollStep.Jumped      => WaitForProgressThenSettle(before),              // today's verb path
    ScrollStep.NotMoved    => false,
    _ /* Moved, Unconfirmed */ => HasMoreThan(countBefore),
};
```

For a platform that cannot jump, the element's `ScrollContent` step replaces
`ScrollHelper.StepForward`. The `ScrollIntoView(last row)` rung stays in the collection, which
remembers the last outcome for its lifetime:

- **The last call jumped:** go straight to `ScrollTowards`.
- **Otherwise:** try the rung first, as today, and call `ScrollTowards` only if the rung realized
  nothing new.

The very first call on a collection has no outcome to remember, so it calls `ScrollTowards`
without trying the rung first. That first call is the one place the order differs from today.

### 5.3 The risk, stated plainly

Today, a Windows collection **without** the verb calls `ScrollIntoView(last row)` *before*
stepping. After this change it steps first, and its `Moved` goes through the verb's progress wait.
That wait returns false when a Scroll-pattern step realizes no row beyond `before`, which today's
path would have followed with another step.

That could shorten `ProductCollection` walks, or make them flaky; the recycle race that
`CollectionIndexProbeTests` guards against is exactly this sort of thing. Two ways out if it
regresses:

1. **Keep the old order inside the collection for a non-verb `Moved`:** call
   `ScrollIntoView(last)` first. This needs the collection to know whether the move was a jump.
   Return a distinct `ScrollStep.Jumped` rather than `Moved`, so a caller can tell a jump from a
   pattern step. That is still a result, not a question.
2. **Keep `SupportsScrollToIndex`** and record why: it is the one question whose answer changes a
   multi-step algorithm that the element cannot own, because the element does not know the
   collection's item strategy.

**Recommendation:** implement with `ScrollStep.Jumped` from the start (option 1), together with the
remembered outcome described in 5.2. That costs one enum value and one field, and keeps both of
today's paths:

- `Jumped`: the verb wait;
- `Moved` or `Unconfirmed`: the count check, with the `ScrollIntoView(last)` rung tried first on
  later calls;
- `NotMoved`: stop.

`ScrollContent` never returns `Jumped`. What remains different from today is the first non-jump
call on a collection, which steps before trying the rung. If three collection runs show that
matters, fall back to option 2.

---

## 6. Order of work and verification

Each step builds the solution and the UI, Mobile and UAT test projects, and runs the Maui unit
tests. After that, run the smallest UI tier that can prove the step wrong, one UI process at a
time.

| # | Step | UI tier |
|---|---|---|
| 1 | `SupportsGesture` (1) | `FullyQualifiedName~Tests.Gestures` |
| 2 | Dropdown (2) | `FullyQualifiedName~Tests.Selection\|FullyQualifiedName~SelectionVerbTests` |
| 3 | `Activate` (3) | unit tests; `Brinell.Presenter.Uat.Tests` if it runs locally |
| 4 | Nullable `ReadState` (4) | `FullyQualifiedName~Tests.Display\|FullyQualifiedName~Tests.Range\|FullyQualifiedName~Tests.Selection\|FullyQualifiedName~Tests.Background` |
| 5 | `ScrollTowards` with `Jumped` (5) | `FullyQualifiedName~Tests.Collection\|FullyQualifiedName~Tests.Container\|FullyQualifiedName~Tests.Scroll`, **three runs**: the recycle race showed up only across runs |
| 6 | Docs and the full suite | full suite: expect 296 tests, 295 passed, 1 skipped |

**Docs to update in step 6:**

- `docs/platform-guides/maui.md`, which names `SupportsInvoke`;
- `.my/bridge/supports-properties.md`: add a pointer that the rest are gone;
- section 8 of the design document;
- class remarks that say "a `Supports*` question": `ScrollHelper`, `SelectorControlBase` and
  `Picker`.

**Regenerate** the `.gen.cs` files for `Picker`, `Image`, `ProgressBar`, `Stepper` and
`SelectorControlBase` with `Brinell.Generator.Cli`. Expect doc-only differences. A signature
difference means a Core method changed shape by mistake.

---

## 7. Out of scope, and why

- **The driver's `SupportsGesture(id, gesture)`.** The gesture tests use it to assert what the app
  declared, which is its whole purpose, and no control calls it. It could move to
  `AppElement.TryFindDeclared(id)` plus a nullable gesture read, but that is a test-API change
  with no control benefit.
- **`HasUsableBounds` in Extensions.** It is a geometry guard, not a platform question.
- **Android and iOS.** None of this can be run on a device from here. The Appium side of every
  step is a default or a one-line delegation, except `Activate` (`Click`) and `ScrollTowards`
  (`ScrollContent(1)`), both of which reuse routes that already exist.

---

## 8. The interface afterwards

| Region | Members |
|---|---|
| Activation | `Invoke`, `Toggle`, `Select`, `Activate`, `InvokeToolbarItem` |
| Focus | `Focus`, `ClearFocus` |
| Checked | `Checked`, `SetChecked` |
| Text | `Value`, `IsReadOnly`, `AppendText` |
| Range | `RangeValue`, `RangeMinimum`, `RangeMaximum`, `RangeSmallChange`, `SetRangeValue` |
| Dropdown | `IsDropdownOpen` (nullable), `OpenDropdown`, `CloseDropdown`, `ReadDropdownItemTexts`, `SelectedItemText`, `ReadItemTexts` |
| Gestures | `PerformGesture` |
| App state | `ReadState` (nullable) |
| Scrolling | `ScrollContent`, `ScrollTowards`, `ScrollTo`, `ScrollToIndex`, `ReadScrollPosition`, `TryFindByScrolling` |
| Selection | `SelectIndex`, `SelectByText` |
| Dates | `SetDate`, `SetTime` |
| The app | `OpenFlyout`, `CloseFlyout`, `IsFlyoutOpen`, `ReadAlert`, `TryFindActiveDialog`, `TryFindDeclared` |

That is **no `Supports*` member**: six removed, three added (`Activate`, `ReadDropdownItemTexts`,
`ScrollTowards`), one removed without replacement (`ReadDropdownItems`), and two signatures
changed (`IsDropdownOpen` and `ReadState` become nullable).
