# Replacing attribute lookups with real members

## The mistake, named

`GetAttribute(name)` was modelled on the DOM, where it is a general question: HTML elements carry
an open bag of attributes, so asking for any name is meaningful and the browser answers.

A native accessibility tree has no bag. It publishes:

- a **closed set of properties** — name, automation id, enabled, offscreen, keyboard focus, …
- a set of **control patterns** — RangeValue, Toggle, Value, Selection, SelectionItem, Scroll,
  ExpandCollapse, Invoke, …

Nothing else exists to automation, and a MAUI `BindableProperty` is not projected into either.
So a string-keyed lookup can only ever reach what the adapter hard-coded: twelve names on
Windows, UiAutomator2's fixed list on Android. Everything else silently returns null, which the
caller cannot distinguish from "the platform answered, and it was empty".

The tell was already in the code before this audit: `scroll.verticalscrollpercent` and its three
siblings sit in FlaUI's attribute switch. That is a *pattern* smuggled through a string key —
someone hit this wall and worked around it inside the bag instead of in the type system.

**So yes: each surviving attribute read should become its own member.** Which member depends on
which of three buckets the value falls into.

## The three buckets

### 1. The platform publishes it as a property → a member on `IMauiElement`

These are universal: every tree has them, each under its own name. The adapter is the right place
to know that name, and the caller should not have to.

Already done this way: `Text`, `Enabled`, `Visible`, `Selected`, `TagName`, `Rect`, and — added
during this work — `AutomationId`, `Name`, `Focused`.

### 2. The platform publishes it as a pattern → a capability interface

The framework already has this mechanism and it works: `IRangePatternElement`,
`ITogglePatternElement`, `ISelectionItemPatternElement`, `IInvokePatternElement`. A control asks
`element is IXPatternElement { SupportsX: true }` and falls back when the answer is no.

What is missing is a capability for the *container* side of selection, and for the Value pattern.

### 3. Nothing publishes it → delete, or make the app say it

`Image.Source`, `MediaElement.Volume`, `WebView.CanGoBack`, `DatePicker.MinimumDate` are app
state. They never cross into the accessibility tree, and no clever adapter will find them. Two
honest options: remove the member, or have the app publish the value deliberately — a status
label, or `AutomationProperties.HelpText` — which is exactly what the sample app already does for
things a test needs to observe.

## The mapping

| Current | Bucket | Replacement |
|---|---|---|
| `FocusableControlBase.IsFocusedCore` | 1 | **`IMauiElement.Focused`** — done, see below |
| `Entry.IsReadOnlyCore` | 2 | `IValuePatternElement.IsReadOnly` (UIA ValuePattern). Android publishes no editability; answer null there rather than false |
| `SelectorControlBase.GetSelectedIndexCore` | 2 | `ISelectionContainerElement.GetSelection()` on Windows; on Android the selected child already reports `selected`, which `IMauiElement.Selected` reads |
| `CollectionView.IsMultiSelectEnabled` (deleted) | 2 | `ISelectionContainerElement.CanSelectMultiple` — worth reinstating once the capability exists |
| `SelectorControlBase.GetItemCountCore` | 2 | Not an attribute at all: count item elements, the way every collection already does |
| `ProgressBar.IsIndeterminateCore` | 2? | Candidate: an indeterminate progress bar does not support RangeValue, so `!range.SupportsRangeValue` may be the signal. **Unverified** — measure before building |
| `RangeControlBase.*` on Android | 2 | `IRangePatternElement` is implemented by FlaUI only. Whether UiAutomator2 exposes `RangeInfo` needs checking; if not, this stays Windows-only and Android keeps the text fallback |
| `ClickableControlBase.IsPressedCore` | 3 | No pressed property in UIA or on an Android node. Delete |
| `RefreshView.IsRefreshingCore` | 3 | No property. Either derive from the spinner's visibility (as `ActivityIndicator` does) or delete |
| `Image.GetSourceCore`, `ImageButton.GetSourceCore` | 3 | Delete. A test that must know the source needs the app to publish it |
| `MediaElement.GetPlaybackStateCore` / `GetVolumeCore` / `IsMutedCore` | 3 | Delete |
| `WebView.IsCanGoBackCore` / `IsCanGoForwardCore` | 3 | Delete. In a web context these are script questions, not attribute questions |
| `DatePicker.GetMinimumDateCore` / `GetMaximumDateCore` | 3 | Delete |

Methods already reaching a pattern or a property — the `RangeControlBase` family on Windows,
`ProgressBar.GetProgress`, `ToggleControlBase.IsChecked`, `Toolbar.GetTitle`,
`SelectorControlBase.GetSelectedText`, the date/time getters — need nothing. They are the shape
everything else should take.

## Worked example: `IsFocused`

The audit measured `IsFocused()` returning **False** for an entry that was demonstrably focused on
Windows, because `IsFocusedCore` tried `HasKeyboardFocus`, `focused` and `IsFocused` — none of
which Windows publishes — and returned false when none answered.

```csharp
// before: three guesses at a name, then a lie
var hasFocus = element.GetAttribute("HasKeyboardFocus");
if (!string.IsNullOrEmpty(hasFocus)) return hasFocus.Equals("true", ...);
// ... two more ...
return false;

// after
protected virtual bool? IsFocusedCore(IMauiElement? element) => element?.Focused;
```

`IMauiElement.Focused` is implemented per adapter: FlaUI reads `Properties.HasKeyboardFocus`,
Appium reads `focused` on Android and `hasFocus` on iOS. Note the shape of the fix — the platform
difference moved from a guess in shared code into the one place that knows the platform.

Null now means "there is no element", not "not focused".

**Verified:** a new test, `EntryTests.Entry_Focus_IsReported`, focuses the entry and asserts
`AssertFocused()`. It fails on the old code on Windows and passes on the new. The text suite is
**29/29 on Windows and 29/29 on Android**.

That test is also the reason this went unnoticed for so long: nothing in the suite ever asked
whether focus worked, and a member that always answers false is only caught by a test expecting
true.

## Order of work

1. ~~`IsFocused` → `IMauiElement.Focused`~~ **done.** Highest value: a base-class member every
   control inherits, and it was actively wrong rather than merely absent.
2. **Delete bucket 3** (nine methods, ~27 generated members). No design needed, and it shrinks the
   surface that has to be trusted.
3. **`IValuePatternElement`** — gives `Entry.IsReadOnly` a real answer on Windows, and is the
   pattern behind text value reads generally.
4. **`ISelectionContainerElement`** — gives `GetSelectedIndex` and a reinstated
   `IsMultiSelectEnabled` a real answer, and is the last big pattern the framework lacks.
5. **Measure the two unknowns**: whether an indeterminate progress bar can be told by its lack of
   RangeValue, and whether UiAutomator2 exposes `RangeInfo`. Both are one probe each, and both
   decide whether a member is worth keeping.

## What stays

`GetAttribute` itself, in two roles:

- **Inside an adapter**, as the way it reads its own platform's attributes — that is what it is
  for, and `Focused`, `AutomationId` and `Name` are all implemented on top of it.
- **As `ViewBase.GetAttribute(name)`**, the deliberate escape hatch for a test that knows its
  platform and wants to ask a platform question. It should say so in its documentation: you are
  asking one platform in its own vocabulary, and another platform will answer null.

What should not survive is a *control* reading platform attributes by guessing at names. Every
such guess is a member that reports a constant, and there is no test that can tell you which
constant it should have been.

## Preventing the next one

The adapters cannot currently distinguish "this platform has no such attribute" from "the
attribute is empty", so a wrong key is indistinguishable from an empty value at every call site.
An `IMauiElement.TryGetAttribute(string name, out string? value)` returning false for an unmapped
name would make the mistake visible the first time it is written — cheap, and it turns a silent
class of bug into a compile-time-obvious one.
