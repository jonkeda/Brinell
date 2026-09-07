# What `GetAttribute` can actually answer

## Why this exists

Converting the collection controls turned up members that answered the same value for every app:
`CollectionView.GetSelectionMode()` was always null, `CarouselView.GetPosition()` always 0. Both
read a MAUI bindable property through `element.GetAttribute("...")`, and neither platform
publishes those to automation. That raised the obvious question — how much of the control library
does this — so every call site was audited.

**32 `*Core` methods across the MAUI controls read attributes, through 68 call sites. 15 of them
can never answer on either platform, and one more is dead on Windows only.**

## What each adapter does with a key

**Windows (`FlaUIMauiElement`) maps exactly twelve names**, case-insensitively, and returns null
for everything else. This is not a lookup that might succeed on a different control — it is a
`switch` with a `_ => null` arm, so an unmapped key is *unanswerable by construction*:

```
automationid   class     classname    controltype   enabled   helptext
name           visible   scroll.verticalscrollpercent    scroll.horizontalscrollpercent
scroll.verticallyscrollable           scroll.horizontallyscrollable
```

**Android (`AppiumMauiElement`) forwards to UiAutomator2**, which supports a fixed set of
accessibility attributes (`text`, `content-desc`, `resource-id`, `checked`, `selected`,
`focused`, `hint`, `enabled`, `clickable`, `scrollable`, `password`, `bounds`, `displayed`,
`package`, `class`, …). Anything else raises `NotImplementedException`, which the adapter catches
and turns into null.

The important word in both cases is **null**. A key that cannot be answered is indistinguishable,
at the call site, from a key that is answered as empty — which is why this went unnoticed.

### MAUI properties are not automation attributes

The keys that fail are all MAUI `BindableProperty` names — `Value`, `Minimum`, `Maximum`,
`Source`, `Title`, `Intent`, `Position`, `SelectedIndex`, `IsRunning`, `CurrentState`. Nothing
carries them across the platform boundary: what reaches automation is what the platform's
accessibility layer publishes, and MAUI does not project arbitrary bindable properties into it.
Reading them back was never going to work.

## Measured, not just reasoned

One probe, the same code on both platforms, against a **focused** `Entry` on the sample app's
text page. It asked for nineteen keys and called `IsFocused()`:

| | Windows | Android |
|---|---|---|
| Keys that answered | `Name` | `focused`, `Name` |
| Keys that returned null | the other 18 | the other 17 |
| `IsFocused()` on a focused control | **False** | **True** |

`IsFocused()` answering False about a control that is demonstrably focused is the whole finding
in one line. `IsFocusedCore` tries `HasKeyboardFocus`, then `focused`, then `IsFocused`, and
**returns false when none of them answers** — so on Windows it does not report "unknown", it
reports "not focused", and `AssertFocused()` fails for a control that is focused.

Both measurements match what the adapter code predicts, so the table below can be trusted for the
keys the probe did not cover.

## Every attribute-reading Core method

`attribute` = at least one key is answerable on that platform. `fallback` = the attribute may
fail, but the method falls through to an automation pattern or an element property that works.
`dead` = no answerable key and no fallback: the method returns its default for every app.

| Control | Core method | Keys read | Windows | Android |
|---|---|---|---|---|
| `ActivityIndicator` | `IsRunningCore` | `IsRunning`, `isRunning` | fallback | fallback |
| `ClickableControlBase` | `IsPressedCore` | `IsPressed` | **dead** | **dead** |
| `DatePicker` | `GetDateValueCore` | `Date`, `SelectedDate`, `Value`, `value.value`, `Name` | attribute | attribute |
| `DatePicker` | `GetMaximumDateCore` | `MaximumDate`, `Maximum` | **dead** | **dead** |
| `DatePicker` | `GetMinimumDateCore` | `MinimumDate`, `Minimum` | **dead** | **dead** |
| `Entry` | `GetPlaceholderCore` | `Name`, `HelpText`, `hint`, `placeholderValue`, `placeholder` | attribute | attribute |
| `Entry` | `IsReadOnlyCore` | `readonly`, `isReadOnly`, `editable` | **dead** | **dead** |
| `FocusableControlBase` | `IsFocusedCore` | ~~3 attribute names~~ → `element.Focused` | property | property |
| `Image` | `GetSourceCore` | `Source`, `source`, `src` | **dead** | **dead** |
| `ImageButton` | `GetSourceCore` | `Source`, `src` | **dead** | **dead** |
| `MediaElement` | `GetPlaybackStateCore` | `CurrentState` | **dead** | **dead** |
| `MediaElement` | `GetVolumeCore` | `Volume` | **dead** | **dead** |
| `MediaElement` | `IsMutedCore` | `IsMuted` | **dead** | **dead** |
| `Picker` | `GetTitleCore` | `Title`, `Name` | attribute | attribute |
| `ProgressBar` | `GetProgressCore` | `Value`, `Progress`, `RangeValue.Value` | fallback | fallback |
| `ProgressBar` | `IsIndeterminateCore` | `IsIndeterminate`, `isIndeterminate` | **dead** | **dead** |
| `RangeControlBase` | `GetMaximumCore` | `RangeValue.Maximum`, `Maximum` | fallback | fallback |
| `RangeControlBase` | `GetMinimumCore` | `RangeValue.Minimum`, `Minimum` | fallback | fallback |
| `RangeControlBase` | `GetStepCore` | `RangeValue.SmallChange`, `Step` | fallback | fallback |
| `RangeControlBase` | `GetValueCore` | `RangeValue.Value`, `Value` | fallback | fallback |
| `RefreshView` | `IsRefreshingCore` | `IsRefreshing`, `Refreshing` | **dead** | **dead** |
| `SelectorControlBase` | `GetItemCountCore` | `ItemCount` | **dead** | **dead** |
| `SelectorControlBase` | `GetSelectedIndexCore` | `Selection.SelectedIndex`, `SelectedIndex` | **dead** | **dead** |
| `SelectorControlBase` | `GetSelectedTextCore` | `Selection.Item.Name` | fallback | fallback |
| `Stepper` | `GetValueCore` | `Name` | attribute | attribute |
| `TimePicker` | `GetTimeValueCore` | `Time`, `SelectedTime`, `Value`, `value.value`, `Name` | attribute | attribute |
| `ToggleControlBase` | `IsCheckedCore` | (none — pattern only) | fallback | fallback |
| `Toolbar` | `GetTitleCore` | `Title`, `text` | fallback | attribute |
| `WebView` | `GetPageTitleCore` | `title` | fallback | fallback |
| `WebView` | `GetUrlCore` | `url`, `Source` | fallback | fallback |
| `WebView` | `IsCanGoBackCore` | `CanGoBack` | **dead** | **dead** |
| `WebView` | `IsCanGoForwardCore` | `CanGoForward` | **dead** | **dead** |

`ViewBase.GetAttributeCore` is excluded: it is the deliberate pass-through that lets a test ask
for a named attribute itself, and it is honest about returning whatever the platform gives.

## The dead list

Fifteen methods, each generating three public members (`Get`/`Wait`/`Assert` or
`Is`/`Wait`/`Assert`), return a constant on both platforms:

| Member | Answers, always |
|---|---|
| `ClickableControlBase.IsPressed` | false |
| `Entry.IsReadOnly` | false |
| ~~`FocusableControlBase.IsFocused`~~ | ~~false on Windows~~ — **fixed**, see the design doc |
| `DatePicker.GetMinimumDate` / `GetMaximumDate` | null |
| `Image.GetSource`, `ImageButton.GetSource` | null |
| `MediaElement.GetPlaybackState` / `GetVolume` / `IsMuted` | null |
| `ProgressBar.IsIndeterminate` | false |
| `RefreshView.IsRefreshing` | false |
| `SelectorControlBase.GetItemCount` / `GetSelectedIndex` | null / -1 |
| `WebView.IsCanGoBack` / `IsCanGoForward` | false |

None is covered by a test, which is the other half of the explanation: a member that always
answers false is only caught by a test that expects true.

## What already works, and how

The controls that survive this audit do so for one of two reasons, and both are worth copying.

**They fall through to an automation pattern.** `ProgressBar.GetProgress` and the whole
`RangeControlBase` family try attributes first and then read the UIA RangeValue pattern through
`IRangePatternElement`. `ToggleControlBase.IsChecked` goes straight to the toggle pattern. This is
the route that fixed `ProgressBar` earlier.

**They fall through to a first-class element property.** `Toolbar.GetTitle`,
`SelectorControlBase.GetSelectedText` and the date/time getters end at `element.Text` or
`element.Name`, which every adapter implements properly.

Note the asymmetry the audit exposes: `IRangePatternElement` is implemented by the **FlaUI**
element only. `AppiumMauiElement` implements `ITogglePatternElement` and
`ISelectionItemPatternElement`, not the range one — so the `RangeControlBase` family reads as
`fallback` on Android in the table above, while in practice only `GetValue` (which ends at
`element.Text`) survives there. That matches the known Android range-test baseline.

## What to do about it

In order of value:

1. **Delete what cannot work and has no source.** `Image.GetSource`, `MediaElement.*`,
   `WebView.IsCanGoBack`/`IsCanGoForward`, `DatePicker` min/max, `ProgressBar.IsIndeterminate`,
   `SelectorControlBase.GetItemCount`. Nothing observes these from outside the app; a test that
   needs them needs the app to publish them. This is what was done for the collection controls.
2. ~~**Fix `IsFocused` on Windows.**~~ **Done** — `IMauiElement.Focused` now reads UIA's
   `HasKeyboardFocus` and Android's `focused`, and a new test
   (`EntryTests.Entry_Focus_IsReported`) covers it: 29/29 on both platforms. See
   [design-replace-attributes-with-real-members.md](design-replace-attributes-with-real-members.md).
3. **Give Appium a range capability.** `IRangePatternElement` on `AppiumMauiElement` (Android
   publishes `RangeInfo` on a seek bar) would make the range family work on Android instead of
   falling back to text.
4. **Make absence honest where a member stays.** `IsFocusedCore`, `IsPressedCore`,
   `IsRefreshingCore` and friends return `false` when nothing answered. Returning `null` —
   "unknown" — would at least stop them asserting a state they never observed.
5. **Stop `GetAttribute` swallowing the difference.** The adapters cannot currently say "this
   platform does not have that attribute" as distinct from "it is empty". An
   `IMauiElement.TryGetAttribute(name, out value)` — or a documented supported list per adapter —
   would make a mistake like this visible the first time it is written rather than years later.

## Re-running this audit

The table is generated, not hand-written. The method:

1. Walk `srcnew/Brinell.Maui/Controls/**/*.tpl.cs`, extract every `protected virtual|override
   *Core` body, and collect its `GetAttribute("...")` keys.
2. Classify each method: does any key appear in the platform's supported set, and does the body
   fall through to a pattern interface or an element property?
3. Cross-check the supported sets against the adapters themselves — the Windows list is the
   `switch` in `FlaUIMauiElement.GetAttribute`; the Android list is UiAutomator2's.
4. Confirm with one probe per platform that asks a real element for each key and calls the public
   member.

Step 4 is what turns the analysis into evidence, and it costs about thirty seconds per platform.
