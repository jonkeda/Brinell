# Design: date and time controls

## Why this exists

Two requirements:

1. **A date should be set without the mouse.** The pointer is the last resort everywhere else in
   Brinell; `DatePicker` is the one control that still opens itself with a click.
2. **When a date is set or read as text, the format must be settable.** Today the write format is
   hardcoded and the read format is *guessed*.

Both are achievable on Windows, but not the way the current code attempts them. What follows is
measured against the running sample app, not reasoned from the API surface.

## Where things stand

**`Brinell.Maui.UITests.Tests.DateTimes`: 12 / 19.** All seven failures are tests whose assertion
depends on `SetDate`/`SetTime` having actually changed the value:

```
DatePicker_DateAfterMaximum_ShowsValidationError
DatePicker_DateBeforeMinimum_ShowsValidationError
DatePicker_DateFormat_DisplaysCorrectly
TimePicker_EndOfDay_DisplaysCorrectly
TimePicker_Midnight_DisplaysCorrectly
TimePicker_MultipleTimeChanges_UpdatesEachTime
TimePicker_TimeFormat_DisplaysCorrectly
```

`DatePicker_DateFormat_DisplaysCorrectly` is the clearest: it set today + 10 days and expected the
status label to read `Thursday, September 17, 2026`. The label still read the initial date.
**`SetDate` does nothing on Windows.**

The twelve that pass are `IsExists` / `IsVisible` / `IsEnabled`, `Reset`, and — worth calling out —
`DatePicker_SetDate_UpdatesDisplay`, which asserts:

```csharp
page.TestDatePicker.SetDate(DateTimeType.Now.Date.AddDays(5))
    .DateStatusLabel.AssertTextContains("Selected Date");
```

The label is bound with `StringFormat='Selected Date: {0}'`, so it contains `"Selected Date"` from
the moment the page loads. **That test cannot fail**, which is why a completely broken `SetDate`
went unnoticed.

## What the platform actually offers

Probed against the running app on 2026-09-07 by walking the tree through the public capability
interfaces.

### `TestDatePicker` — MAUI `DatePicker` → WinUI `CalendarDatePicker`

```
tag=Button  text='07-Sep-26'  patterns=[Value, Invoke]
  child AutomationId=DateText  tag=Text  name='07-Sep-26'  patterns=[]

SupportsValuePattern      = True
GetValuePattern()         = '07-Sep-26'
IsValuePatternReadOnly()  = True          <-- the decisive fact
SupportsInvokePattern     = True
InvokePattern()           -> True
```

After `InvokePattern()`, with no pointer involved:

```
CalendarView    FOUND
PreviousButton  FOUND ('Previous')
NextButton      FOUND ('Next')
```

### `TestTimePicker` — MAUI `TimePicker` → WinUI `TimePicker`

```
tag=Group  name='time picker'  patterns=[]          <-- no patterns at all
  child AutomationId=FlyoutButton  tag=Button  name=' 1:58 PM time picker'  patterns=[Invoke]
```

### Reading these results

**The Value pattern is a read channel, not a write channel.** It is advertised, and it answers
`'07-Sep-26'`, but `IsReadOnly` is **True**. `FlaUIMauiElement.TryWriteValue` checks `IsReadOnly`
and correctly declines, so `SendKeys(text, TextInputMethod.SetValue)` falls through to typing
keystrokes — into a control that has no text host, where they do nothing. Confirmed directly:
setting through the Value path with six different format strings changed nothing.

| Format sent | Result |
|---|---|
| `2026-09-19` | no change |
| `19-Sep-26` | no change |
| `09-19-2026` | no change |
| `19-9-2026` | no change |
| `Saturday, September 19, 2026` | no change |
| `2026-09-19T00:00:00.0000000+02:00` | no change |

So there is no format that makes the current approach work. The approach is wrong, not the string.

**`Invoke` is the mouse-free way in, and it works.** It opens the `CalendarView` flyout and returns
True. Everything needed to drive that flyout — `PreviousButton`, `NextButton`, the day items —
is in the tree and reachable by pattern.

## The design

### 1. Setting: a ladder, pointer never on it

`SetDateCore` becomes an explicit ladder, in the same spirit as
[click-activation-vs-element-gestures](../maui/click-activation-vs-element-gestures.md). Each rung
is tried only when the platform advertises it:

1. **Writable `ValuePattern`** — `SetValue(date.ToString(Format))`. Not available on WinUI's
   `CalendarDatePicker`, but it *is* the right rung for HTML `<input type=date>`, WinForms
   `DateTimePicker`, and any MAUI `Entry`-backed date field. Guarded by
   `IsValuePatternReadOnly() == false`, which is what makes it skip correctly on WinUI rather than
   silently no-op.
2. **`Invoke` + calendar navigation** — open the flyout by pattern, then walk to the target month
   with `PreviousButton` / `NextButton` (both Invoke-able) and select the day item by its
   `SelectionItem` pattern. This is the Windows path. No coordinates, no clicks.
3. **Typed text into a real text host** — `Focus()` then keyboard, for platforms where the date
   control genuinely is an editable field. Formatted with the configured format.

The pointer appears nowhere. If every rung declines, `SetDate` **throws** naming the control and
the rungs tried, rather than returning quietly — the lesson
[decision-remove-windows-interaction-policy](../maui/decision-remove-windows-interaction-policy.md)
records giving up, and this is the place to buy it back cheaply.

### 2. Reading: one source, not a cascade

`GetDateValueCore` today tries, in order: a `DateText` child, an XPath walk of every descendant,
the element `Name`, then the element `Text` — and parses each through 13 candidate formats plus
two culture attempts.

Replace the whole cascade with **`GetValuePattern()`**, which the probe shows answers `'07-Sep-26'`
directly on the picker itself, then parse it with the declared format. One call, one format, no
guessing. Keep the `DateText` child only as a fallback for platforms with no Value pattern, and
delete the XPath rung entirely — see the bug below.

### 3. Format: declared, never guessed

This is the second requirement, and it is also a correctness fix.

`TryParseDateString` currently calls `DateTime.TryParse(cleaned)` **first**, under the machine's
current culture. `03/04/2025` is 4 March in `en-GB` and 3 April in `en-US`. If that misses, the
format list tries `MM/dd/yyyy` **before** `dd/MM/yyyy`, so an ambiguous date always resolves
US-first regardless of where the test runs. A suite can pass on one machine and silently assert the
wrong day on another.

The fix is to stop inferring. The control takes a format:

```csharp
public DatePicker<TScope> WithFormat(string format, CultureInfo? culture = null);
```

resolved in this order, first match wins:

1. the per-control `WithFormat(...)`,
2. a suite-wide default — built as a `DateTimeFormats` static rather than on `MauiOptions`, because
   page objects construct controls fresh on every access (`new(this, "TestDatePicker")`), so there
   is no instance for options to reach; the statics are the only way to configure a whole suite
   without editing every call site,
3. the current culture's short date/time pattern, which is what the platform actually renders —
   WinUI showed `07-Sep-26`, not an invariant `yyyy-MM-dd`.

When a format is declared, parsing uses `TryParseExact` with it and nothing else: a string that
does not match is an **error naming both the expected format and the text received**, not a silent
null. That turns today's worst failure mode, a wrong date that looks right, into a loud one. With
no format declared the default is tried first and the culture's own patterns second — never the
US-first candidate list this replaced.

Two traps to write down, because both are already live in this repo:

- **`/` in a .NET format string is not a literal slash.** It is the culture's date separator. The
  probe sent `date.ToString("MM/dd/yyyy")` and the machine produced `09-19-2026`. Any format meant
  literally must escape it (`MM\/dd\/yyyy`) or pass `CultureInfo.InvariantCulture`.
- **WinUI embeds U+200E LTR marks** in the rendered string (`'‎07‎-‎Sep‎-‎26'`). Stripping
  `\p{Cf}` before parsing stays — that part of the current code is right and should be kept.

### 4. `DatePicker` is not focusable, and should be

`DatePicker<TScope>` derives from `ViewBase`, so it has **no `Focus()` and no `IsFocused()`** — the
compiler rejects `page.TestDatePicker.Focus()` today. A control that is meant to be driven from the
keyboard must be focusable, so both pickers should derive from `FocusableControlBase`.

Worth noting the inconsistency this sits in: the HTML `DateInputControl` derives from
`RangeControlBase`, which is arguably the better fit again, since a date picker with
`MinimumDate`/`MaximumDate` *is* a range control. Picking one base for all four platforms is a
prerequisite for the constraint tests meaning the same thing everywhere.

## A bug found on the way

`GetDateValueCore` and `GetTimeValueCore` both wrap their XPath walk in:

```csharp
catch (WebDriverException)
{
    // XPath not supported by this driver - fall through to Name/Text fallbacks
}
```

The FlaUI driver throws `LocatorNotSupportedException`, which derives from `BrinellException`, not
from Selenium's `WebDriverException`. **The catch does not catch it.** Verified directly — calling
`FindElements(Locator.ByXPath(".//*"))` on the picker throws
`Brinell.Core.Exceptions.LocatorNotSupportedException` straight through.

So on Windows, any time the `DateText` lookup fails to yield a parseable date, `GetDateValue()`
does not fall back — it throws. It has gone unnoticed only because `DateText` currently happens to
answer. This disappears with the cascade in §2, which is the better fix than correcting the catch.

## Tests to change

The design is untestable against the current assertions.

- `DatePicker_SetDate_UpdatesDisplay` must assert the **date**, not the presence of the literal
  `"Selected Date"` that the binding guarantees. `AssertDateValue(expected)` round-tripping through
  the control is the assertion that was intended.
- Same shape in `TimePicker_SetTime_UpdatesDisplay`.
- The DateTimes tests share one app through `[Collection("Maui")]` and assert on a shared
  `StatusLabel` that only changes when a value actually changes. Once `SetDate` works they become
  order-dependent. Each should reset and assert its own control rather than the shared label.
- Add a format test proper: set with one format, read back with the same, and a negative case where
  a mismatched format reports the expected-vs-received error rather than null.

## What implementation found

The design above was built. Four things only showed up once it ran.

**The calendar flyout is fully drivable, and it commits.** `SelectItemPattern()` on a day cell
moved the picker from `07-Sep-26` to `17-Sep-26` and closed the flyout on its own — no Accept step
and no pointer. The time flyout carries `HourLoopingSelector`, `MinuteLoopingSelector` and
`PeriodLoopingSelector` (a `List` each, of `ListItem`s with `SelectionItem`), named `12,1..11`,
`00..59` and `AM,PM`, plus `AcceptButton` and `DismissButton`. The hour list is a 12-hour clock, so
the hour needs converting.

**The sample app's view model was swallowing the result.** `FormattedDate` is a computed property
and `SetProperty` raises `PropertyChanged` only for the property it is given, so the
`DateStatusLabel` binding never refreshed. `StatusLabel` updated because `UpdateStatus()` assigns
`StatusMessage` explicitly. That is why `DatePicker_DateFormat_DisplaysCorrectly` still failed
after the set genuinely worked. Fixed in `DateTimeViewModel` by naming `FormattedDate` and
`FormattedTime` on change.

**Verification has to distinguish "wrong" from "unreadable".** At midnight the flyout button's
name comes back as `'  time picker'` — no time in it — and it has no text descendants to fall back
on. The control publishes nothing to check against even though the set worked and the app's label
read `00:00:00`. Treating an unreadable value as failure turned a working midnight set into an
error, so `WaitForTime` now fails only on a value that *disagrees*.

**Two test premises were unreachable, in different ways.** `MinimumDate`/`MaximumDate` keep an
out-of-range day out of the calendar entirely, so the view model's "before minimum" branch can
never fire on Windows — there is nothing to select and nothing to reject. Those tests now assert
the refusal, which is the real contract. And the WinUI time flyout has no seconds selector, so
`23:59:59` and `09:45:30` were asking for something the control cannot express; those now use
whole minutes.

**Result: `Tests.DateTimes` 20 / 20, from 12 / 19** - nineteen existing tests plus a new
`DatePicker_Focus_IsReported`, which could not even be written before §4.

### §4, and the focus primitive it needed

Both pickers now derive from `FocusableControlBase`. The `.gen.cs` half is generated out of band,
so the base class was changed in the template and both files regenerated with
`tools/Brinell.Generator.Cli` — which reconstructs the class signature from the template's
`BaseList`, so the two halves cannot drift.

The change did not work as-is, because `FocusableControlBase.FocusCore` **focused by clicking**.
That is wrong for any control that opens on activation, and a date picker is exactly that: focusing
it would have opened its calendar. Clicking was standing in for an operation the element interface
did not expose - `IMauiElement` publishes `Focused` to read, and nothing to set.

So focus became a capability, in the same shape as the pattern interfaces beside it:

```csharp
public interface IFocusPatternElement
{
    bool SupportsSetFocus { get; }
    bool SetFocus();
}
```

`FlaUIMauiElement` implements it over UIA's own focus; `FocusCore` prefers it and keeps the click
as the fallback for WebDriver-backed elements, which genuinely cannot focus without touching.

**That fixed `EntryTests.Entry_Focus_IsReported`**, which had been failing since before this work
and was verified failing on unmodified code earlier the same day. `IsFocusedCore` reads
`HasKeyboardFocus`, and a click on a WinUI `Entry` does not reliably set it; a real focus call
does. The defect was never in the reading - it was that nothing ever properly set focus.

## Open questions

- **Android.** Not probed — this pass measured Windows only. MAUI's Android `DatePicker` opens a
  native dialog, and the picker suites already carry known failures there
  (`.my` records Selection at 0/8 on Android). The ladder in §1 is shaped so Android adds a rung
  rather than a platform branch, but which rung it needs is unmeasured.
- **Whether the `CalendarView` day items expose `SelectionItem`.** The probe confirmed the flyout
  opens and that `CalendarView`, `PreviousButton` and `NextButton` are reachable; it did not
  enumerate the day items. That is the one measurement rung 2 still rests on, and it should be
  taken before the ladder is built.
- **`TimePicker` has no patterns on its root at all.** Rung 2 has to start from the `FlyoutButton`
  child, so the ladder needs a per-control notion of "the element that actually carries the
  command" — the same problem `IconCommandButton` and `RoundButton` already solve by overriding
  their activation target.
