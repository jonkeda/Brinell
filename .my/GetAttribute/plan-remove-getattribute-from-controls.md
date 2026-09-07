# Plan: no control reads an attribute by string

## Goal

After this work, **no control object in `Brinell.Maui` calls `element.GetAttribute("...")`**.
Every value a control reads comes from one of three places instead:

| Shape | When | Example |
|---|---|---|
| A member on `IMauiElement` | the platform publishes it as a property | `element.Focused`, `element.Name` |
| A capability interface | the platform publishes it as a control pattern | `IRangePatternElement`, `IValuePatternElement` |
| Nothing — the member goes | no platform publishes it | `MediaElement.GetVolume` |

`GetAttribute` survives in exactly two roles: **inside an adapter**, which is where a platform's
own vocabulary belongs, and as **`ViewBase.GetAttribute(name)`**, the escape hatch for a test that
deliberately asks one platform a platform-specific question.

The reasoning is in [audit-what-getattribute-can-answer.md](audit-what-getattribute-can-answer.md)
and [design-replace-attributes-with-real-members.md](design-replace-attributes-with-real-members.md).
This document is the work list.

## Status: done

No control in `Brinell.Maui` reads an attribute by name any more. `ViewBase.GetAttributeCore` is
the single remaining call, which is the escape hatch this plan always intended to keep.

Two corrections to the audit came out of doing the work, both recorded below in place:

- `SelectorControlBase.GetSelectedIndex` and `GetItemCount` were **not** dead. They fall back to
  sibling Core methods - deriving the index by matching the selected text against the item texts,
  and counting item elements - which the classifier missed because it only looked for pattern
  interfaces and element properties. They were rewritten, not deleted.
- `MediaElement` was **worse** than the table showed. `IsPlaying`, `IsPaused`, `GetPosition` and
  `GetDuration` build their attribute names from variables, so the same classifier missed them
  too. All seven of its readers were dead.

## What has to be added first

**`IMauiElement.Hint`** — the placeholder/help text. UIA `HelpText`, Android `hint`, iOS
`placeholderValue`. Needed by `Entry.GetPlaceholder`, which is the only surviving reason anything
reads `HelpText`.

**`IValuePatternElement`** — UIA's Value pattern: `GetValuePattern()` and `IsValueReadOnly`.
FlaUI implements it properly; Appium answers the value from text and reports read-only as null,
because Android does not publish editability.

~~**`ISelectionContainerElement`**~~ — turned out not to be needed; see the order of work.

`IMauiElement.Focused` was added already, as the worked example.

## Per control

`property` = rewritten to read an element property. `pattern` = rewritten to use a capability.
`delete` = the member goes, with nothing behind it.

| Control | Core method | Action | Becomes |
|---|---|---|---|
| `FocusableControlBase` | `IsFocusedCore` | ✅ done | `element.Focused` |
| `ActivityIndicator` | `IsRunningCore` | property | `element.Visible` (already the fallback; the two dead keys go) |
| `Entry` | `GetPlaceholderCore` | property | `element.Hint` |
| `Entry` | `IsReadOnlyCore` | pattern | `IValuePatternElement.IsValuePatternReadOnly` |
| `Picker` | `GetTitleCore` | property | `element.Name` — the accessible name, and the docs will say so |
| `Stepper` | `GetValueCore` | property | `element.Name` |
| `Toolbar` | `GetTitleCore` | property | `element.Text` |
| `DatePicker` | `GetDateValueCore` | property | `element.Text` then `element.Name` |
| `TimePicker` | `GetTimeValueCore` | property | `element.Text` then `element.Name` |
| `WebView` | `GetPageTitleCore` | property | `element.Text` |
| `WebView` | `GetUrlCore` | property | `element.Text` |
| `SelectorControlBase` | `GetSelectedTextCore` | property | `element.Text` then `element.Name` |
| `ProgressBar` | `GetProgressCore` | pattern | range pattern only; the three keys go |
| `RangeControlBase` | `GetValueCore` | pattern | range pattern, `element.Text` fallback for Android |
| `RangeControlBase` | `GetMinimumCore` / `GetMaximumCore` / `GetStepCore` | pattern | range pattern only |
| `SelectorControlBase` | `GetSelectedIndexCore` | property | **corrected**: derived from the selected text and the item texts; only the two dead probes went |
| `ClickableControlBase` | `IsPressedCore` | **delete** | no pressed state in UIA or on an Android node |
| `ToggleControlBase` | `IsCheckedCore` | pattern | ten attribute guesses removed; toggle pattern then `element.Selected` |
| `Image` | `IsLoadedCore` | property | rendered size alone - the source it also checked was never observable |
| `MediaElement` | `IsPlayingCore` / `IsPausedCore` / `GetPositionCore` / `GetDurationCore` | **delete** | found late: they build attribute names from variables |
| `ProgressBar` | `IsIndeterminateCore` | **delete** | not published; the "no RangeValue support" idea is unverified |
| `RefreshView` | `IsRefreshingCore` | **delete** | not published; a test can watch the spinner instead |
| `Image` / `ImageButton` | `GetSourceCore` | **delete** | the source never reaches the tree |
| `MediaElement` | `GetPlaybackStateCore` / `GetVolumeCore` / `IsMutedCore` | **delete** | none of it is published |
| `WebView` | `IsCanGoBackCore` / `IsCanGoForwardCore` | **delete** | browser state, not element state |
| `DatePicker` | `GetMinimumDateCore` / `GetMaximumDateCore` | **delete** | not published |
| `SelectorControlBase` | `GetItemCountCore` | property | **corrected**: it already counts item elements; only the dead `ItemCount` probe went |

Sixteen deletions, sixteen rewrites. `RefreshView.IsRefreshing` is the one member kept without a
source: `IRefreshableControlObject` requires it and other platforms can answer it, so it returns
**null** - unknown - instead of the false it used to invent.

## Order of work

Each phase ends green before the next begins, and each is independently revertible.

1. **Element properties.** Add `Hint`; rewrite the eleven `property` rows. No new interfaces, no
   API removed — the safest and largest chunk.
   *Verify:* Text, Selection, Display, Range, DateTimes, Navigation on Windows.
2. **`IValuePatternElement`.** Add the interface, implement in both adapters, rewrite
   `Entry.IsReadOnly`.
   *Verify:* Text on Windows and Android.
3. ~~**`ISelectionContainerElement`.**~~ **Not needed for this goal.** Once the dead attribute
   probes were removed, `GetSelectedIndex` already derived a real answer from the selected text
   and the item texts, and `GetItemCount` from the item elements. The capability is still worth
   adding one day - it would make the index direct rather than derived, and would let
   `CollectionView.IsMultiSelectEnabled` come back - but it is new capability, not attribute
   removal.
4. **Deletions.** Remove the twelve members and their generated wrappers.
   *Verify:* full solution build, then the whole Windows suite.
5. **Guard the door.** With no control calling it, `ViewBase.GetAttribute` is documented as the
   platform-specific escape hatch, and the adapters' supported keys are documented on the method
   that implements them.

## Verification

| Suite | Windows | Android |
|---|---|---|
| Whole UI suite | **187 passed / 18 failed / 2 skipped** — the 18 are the parked Stepper (11) and DateTimes (7) baseline, confirmed by name | — |
| Text (focus, placeholder, read-only) | 30 / 30 | 30 / 30 |
| Toggle (ten attribute guesses removed) | in the full run | 16 / 16 |
| Display (progress, activity, image) | in the full run | 15 / 15 |
| Selection | 8 / 8 | **0 / 8 — pre-existing**, measured by stashing this work, rebuilding and re-running: the same 8 fail without any of it. A MAUI Picker on Android opens a native dialog the selector does not reach |
| Unit (`Brinell.Maui.Tests`) | 98 / 99, 1 skipped | — |

Two members are now measurably real where they answered a constant before:

- `IsFocused` — False on a focused entry on Windows before; True now, with a test.
- `Entry.IsReadOnly` — probed at the element: `supports=True readOnly=True` on a read-only entry
  and `readOnly=False` on an editable one. The sample gained a read-only entry so the member has
  something to be true about, and the test guards on the capability rather than on the platform,
  so it skips where the pattern is genuinely absent instead of faking a pass.

## Risks, stated up front

- **Public API removal.** Twelve members × three generated forms is roughly 36 public members.
  Nothing in this repo uses any of them (checked: only `Toolbar.GetTitle`, `Picker.GetSelectedText`
  and the *collection* `GetItemCount` appear in tests, and none of those is being deleted). A
  consumer outside this repo would notice; that is the cost of removing members that answered a
  constant.
- **`Picker.GetTitle` and `Stepper.GetValue` read the accessible name**, which is not the same
  thing as the MAUI `Title` or `Value` property. They work today only because `Name` happens to
  carry it. Keeping them means keeping that coincidence; the doc comment will say so rather than
  implying a real binding.
- **The range family stays Windows-only.** `IRangePatternElement` is implemented by FlaUI alone,
  and whether UiAutomator2 publishes `RangeInfo` is untested. This plan does not change that; it
  only stops pretending an attribute might.
