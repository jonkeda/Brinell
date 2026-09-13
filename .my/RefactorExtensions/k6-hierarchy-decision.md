# K6 — The control base hierarchy: evidence and a decision to make

**Status: Option B chosen and applied 2026-09-13** - see § 7. The inventory below was measured on
the code as it stood after stages K1-K5.

---

## 1. What K2-K5 already took out of the bases

| Removed | Where it lived | Step |
|---|---|---|
| Casts to eight `*PatternElement` interfaces, ~25 sites | Toggle, Range, Selector, Focusable bases; Picker, DatePicker, TimePicker, ProgressBar, Slider, Entry | 107 |
| `TrySetStateByPattern` and the set-state/toggle ladder | `ToggleControlBase` | 108 |
| The WinUI calendar and clock flyout walks, ~250 lines | `DatePicker`, `TimePicker` | 107 |
| `FindChildCore`, `FindChildByControlTypeCore`, and the public no-op `FindChild`/`FindChildByControlType` | `ViewBase` | 101a, 101b |
| `TryFindElementAfterScroll` on every scope, `ScrollingOnceResolver` | `IMauiElementScope` and four implementations, `ViewBase` | 100a, 100b |
| The scroll-then-swipe ladder | `ScrollHelper`, `ScrollView`, `CollectionObjectBase` | 105c |

What is left is structure, not routes. So this is now a question only about shape.

## 2. The hierarchy today

```
ControlObjectBase (Core)
└─ ViewBase                773 lines, 7 Core, 11 generated     20 direct controls
   └─ FocusableControlBase  93 lines, 3 Core, 6 generated       DatePicker, TimePicker, Entry(→Editor, SearchBar)
      ├─ ClickableControlBase 181 lines, 9 Core, 10 generated   Button, ImageButton
      │  └─ ToggleControlBase 190 lines, 3 Core, 6 generated    Switch, CheckBox, RadioButton
      ├─ RangeControlBase    234 lines, 8 Core, 16 generated    Slider, Stepper
      └─ SelectorControlBase 324 lines, 8 Core, 20 generated    Picker
RootedScopeBase
├─ ContainerObjectBase                                          Border, ContentView, Grid, ScrollView, ContentDialog
│  └─ CollectionObjectBase                                      CarouselView, CollectionView, ListView, Menu, ShellFlyout, ShellTabs, TabMenu, Toolbar
└─ PageObjectBase
ComponentObjectBase → ItemContainerBase
└─ ClickableItemBase   103 lines, 5 Core, 12 generated          MenuItem, ToolbarItem
   └─ SelectableItemBase 75 lines, 1 Core, 4 generated          ShellFlyoutItem, ShellTab, TabItem
```

25 `protected override *Core` methods exist across all concrete controls.

## 3. The defects the inventory confirms

**1. Toggles inherit a pointer API they must not use.** `ToggleControlBase : ClickableControlBase`
gives `Switch`, `CheckBox` and `RadioButton` the generated `DoubleClick`, `RightClick`, `Hover`,
`LongPress`, `Press` and `IsClickable`/`WaitClickable`/`AssertClickable`. The first four exist
only as physical input on Windows - refused under the quiet default - and none has any meaning
for a switch. `Click` is overridden twice to mean something else: `ToggleControlBase.ClickCore`
toggles, `RadioButton.ClickCore` selects. **`Click` is used**: 13 calls in `SwitchTests` and
`CheckBoxTests`, so any option keeps it.

**2. The `Run*` plumbing is written twice.** `ViewBase` and `ContainerObjectBase` each declare 10
`Run*` helpers with the same names and shapes, so the generator can emit one body for both. A
fix to one - the readiness gate, logging, the last-exception rethrow - has to be copied to the
other by hand. `ItemContainerBase` borrows its own set through `ComponentObjectBase`.

**3. `RefreshView` and `SwipeView` are views; `ScrollView` and `Border` are containers.** All
four host content in MAUI. On Windows the first two are unaddressable, so nothing has noticed yet.

**4. `ScrollHelper` exists because a container cannot also inherit scrolling.** After step 105c it
is 90 lines and three members, used by `ScrollView` and `CollectionObjectBase`.

## 4. The two options, sketched against Toggle

### Option B — keep the chain, fix the links

```csharp
public abstract partial class ToggleControlBase<TScope> : FocusableControlBase<TScope>,
    IToggleControlObject<TScope>
{
    // Click stays: tests use it and it reads naturally. It is declared here as what it is.
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
        => ToggleCore(element, timeoutMs);
    ...
}
```

- `Switch`/`CheckBox`/`RadioButton` lose `DoubleClick`, `RightClick`, `Hover`, `LongPress`,
  `Press` and the `*Clickable` trio. **Breaking**: none is called in this repository, but an app's
  page objects might.
- `RefreshView`/`SwipeView` move under `ContainerObjectBase`. The generator already emits the same
  call shapes for both bases, so their generated members survive.
- The `Run*` duplication is left alone.
- Generator: no change. Cost: small, one stage.

### Option A — one shallow base, capabilities as behaviours

```csharp
public partial class Switch<TScope> : ViewBase<TScope>, IToggleControlObject<TScope>
{
    [Behaviour] private readonly ToggleBehaviour<TScope> _toggle;   // holds ToggleCore, SetCheckedCore, IsCheckedCore
}
```

- The generator learns to read `*Core` members off a behaviour and emit wrappers on the control.
- Removes the chain entirely. Capabilities compose, so a scroll behaviour can go on a container
  and `ScrollHelper` can go.
- Could also host the shared `Run*` plumbing once, which fixes defect 2.
- **Cost: large.** A generator feature, a rewrite of eight base classes, and every override moves
  onto a behaviour. It breaks derived controls in apps, not just callers.

### Why not interfaces with default implementations

Settled in the plan (102b), and nothing here changes it: a default member cannot be called
through the class type, so `page.TestSwitch.Toggle()` would not compile.

## 5. Recommendation

**Option B now; Option A only if the `Run*` duplication starts costing real fixes.**

- Defects 1 and 3 are the ones that mislead a test author, and B fixes both in a stage the size of
  K5.
- Defect 2 is a maintenance cost with no user-visible failure today. It does not justify a
  generator feature on its own.
- Defect 4 is 90 lines. It is not worth a hierarchy change.

## 6. What needs deciding

1. **Option B, Option A, or leave the hierarchy as it is?**
2. If B: is it acceptable for toggles to lose `DoubleClick`, `RightClick`, `Hover`, `LongPress`,
   `Press` and `IsClickable`/`WaitClickable`/`AssertClickable`? No caller in this repository uses
   them; apps built on Brinell might.

## 7. Applied: Option B (2026-09-13)

- **`ToggleControlBase : FocusableControlBase`.** `Switch`, `CheckBox` and `RadioButton` keep
  `Click` - declared on the toggle base as a toggle, and overridden by `RadioButton` to select -
  plus `Toggle`, `SetChecked`, `Check`/`Uncheck` and the `Checked` trio. They lost `DoubleClick`,
  `RightClick`, `Hover`, `LongPress`, `Press` and `IsClickable`/`WaitClickable`/`AssertClickable`,
  and no longer implement `IClickableControlObject` or `IPressableControl`. The toggle base has its
  own `EnsureEnabledCore` guard, which `RadioButton` uses instead of `EnsureClickableCore`.
- **`RefreshView<TParent, TSelf>` and `SwipeView<TParent, TSelf>` are containers**, with sealed
  `RefreshView<TParent>` and `SwipeView<TParent>` forms like `ScrollView<TParent>`. Their members
  now return the container, and they implement `IRefreshableControlObject<TSelf>` and
  `ISwipeableControlObject<TSelf>`. Nothing in the repository used the old types.
- **`ContainerObjectBase` gained `IsExists()`, `IsVisible()` and `GetAttribute`**, which those
  capability interfaces require. It did not gain the Enabled trio: items derive from it and generate
  their own. The two containers declare `IsEnabledCore` instead, and the generator emits the trio.
- Not done, as recommended: the `Run*` duplication and `ScrollHelper`.

**Verified:** unit tests (Maui 123, Generator 121, Core 16); Toggle, Container, Gestures and
Background UI areas 120/120; **full UI suite 281 passed, 16 skipped, 0 failed**. The occluded
screenshot test passed this time. The Presenter UAT, CommunityToolkit and mobile UI test projects
build.

**Found on the way, fixed:** `Brinell.Maui.UITests.Mobile` did not compile.
`PhysicalInputFactAttribute` named the FlaUI driver with `typeof`, and the mobile head shares those
sources without referencing FlaUI. It now finds the type by name.

**Found on the way, not fixed - a machine problem:** partway through this stage, builds of the
sample apps and of `Brinell.sln` started failing with `NETSDK1147: the following workloads must be
installed: android`. The SDK in use is 10.0.400 (`global.json` rolls forward from 10.0.100), and
`dotnet workload list` shows nothing installed, although the Android packs are on disk and the same
builds passed earlier the same day. The UI runs above used the sample app built before that. Run
`dotnet workload restore` (or repair the MAUI/Android workloads) before the next sample or
solution build.

