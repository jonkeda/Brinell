---
title: Stage B Results — Work While The Tests Run
description: What steps 13 to 16 delivered, what the physical-input inventory looks like now, and the three findings that change later steps
status: result
---

# Stage B Results — Work While The Tests Run

Stage A proved the bridge could reach a control. Stage B is the payoff: the suite no longer
needs the machine. A run can happen while someone is typing in Visual Studio, and nothing moves
the cursor, steals the foreground or overwrites the clipboard.

| What | Where | State |
|---|---|---|
| Focus, text and navigation verbs | `samples/Brinell.Maui.AppSupport/Uia/MauiCapabilities.cs` | 3 target frameworks build |
| Transport map (which method a verb travels on) | `srcnew/Brinell.Uia.Contracts/BrinellVerb.cs` | pinned by `ContractTests` |
| General verb client | `srcnew/Brinell.Maui.FlaUI/Bridge/BridgeVerbRunner.cs` | — |
| Background tests | `testsnew/.../Tests/Background` | 15 green |
| Parallelism gate | `testsnew/Brinell.Maui.UITests/DesktopLease.cs` | — |

---

## The headline numbers

| | Before stage B | After |
|---|---|---|
| Full MAUI UI suite | 192 passed / 24 failed / 218 | **207 passed / 24 failed / 233** |
| Runtime | 4 m 25 s | **2 m 43 s** |
| Physical-input uses across the suite | 400 | see the audit below |

The 24 failures are the documented baseline unchanged — 11 Stepper, 13 Shell — and the 15 extra
passes are the new `Tests/Background` class. **The suite is also 38% faster than it was before
any of this work started**, which was not a goal and is worth explaining: the old
`ReturnToHub` clicked a toolbar item and then waited for the transition, once per test. Asking
the app to pop itself removes both.

---

## Step 13 — The Focus verb

`VisualElement.Focus()` behind `BrinellVerb.Focus`, with `Unfocus` and `IsFocused` beside it.

**What made it worth doing** is not that focus was hard, but that the old route conflated two
things. `EnsureRootWindowFocused` calls `SetForeground` before every physical action, and its own
remarks say why: global keystrokes go wherever the foreground is. So *asking for focus* dragged
in *taking the machine*, for callers that only ever wanted the first.

Three places changed, and the split between them is the point:

- **`SetFocus()`** prefers the verb. Focus is the whole request, so nothing else is needed.
- **`FocusForKeyboardInput()`** deliberately does not. Every caller of it is about to send real
  keystrokes, and those need the foreground however the focus was obtained. Routing it through
  the bridge would have looked tidier and made the keystrokes land in the wrong window.
- **`Blur()`** prefers `Unfocus`. Its physical route is a Tab keystroke, which does not clear
  focus at all — it moves it to the next control, along with whatever that control does on
  focus.

### The finding: `HasKeyboardFocus` is false on an occluded app

Windows keyboard focus belongs to the foreground thread. A window that is not in front has no
focused control as far as UI Automation is concerned, whatever the app itself thinks — so on the
one configuration this whole stage exists to support, every assertion about focus would have been
wrong.

`IMauiElement.Focused` now reads UI Automation first and asks the app only when UI Automation
says no. A true is already the answer and needs no round trip; a false is the case that needs
checking, and it is the rarer one.

---

## Step 14 — Text input verbs

`SetText`, `AppendText`, `ClearText`, `GetText` and `Submit`, all through `Exchange`.

**Typing is kept, and should stay kept.** A keyboard raises `TextChanged` per character, applies
`MaxLength` as it goes and lets a numeric keyboard refuse a letter; setting `Text` raises one
change for the whole value. `TextInputMethod.Keys` still means "type it", and a test *of* the
input pipeline should say so. What moved to the bridge is *arrangement* — getting a field into
the state a test wants to start from.

The ladder that resulted is worth reading in order, because the bridge is not always first:

| Operation | Rungs |
|---|---|
| `SendKeys(SetValue)` | Value pattern → bridge → type |
| `SendKeys(Paste)` | bridge → clipboard + Ctrl+V |
| `Clear` | Value pattern → bridge → Ctrl+A, Delete |
| `Append` | bridge → type |
| `Submit` | bridge → Enter |

`SetValue` keeps the Value pattern first because both routes are semantic and the pattern is two
calls against an element already in hand, where the bridge is a raw walk of the window's children
before it can start. The bridge earns its place on the elements the pattern refuses. `Paste` puts
the bridge first for a different reason entirely: the fallback is destructive.

`Append` is the one text operation that could not be assembled from a read and a write out here.
Between the two calls the app is still running, and anything it does to the field lands in the
middle — so it typed, which made it the last routine keyboard input in the suite.

### Two findings

**The clipboard is unreachable from an xUnit thread.** The Windows clipboard is OLE and needs a
single-threaded apartment; xUnit runs on MTA thread-pool threads, where every `Clipboard` call
throws `ThreadStateException` before touching anything. That explains a line in the step 3
inventory that had looked like luck — `SendKeys(Paste)` scored zero uses, and it would have
failed on this before it ever reached the clipboard. The hazard is real for anything running STA
and was simply not reachable from that suite. The canary test runs its own STA thread, which is
the only way to make it a canary.

**A read-only field had to be refused explicitly.** The bridge writes a MAUI property, and MAUI's
own setter does not enforce `IsReadOnly` — only the platform control does. Left alone, the bridge
would have been a way to put text in a field no user could type in, and a test asserting on the
result would have passed while describing something impossible. `ReadOnlyEntry` declares the
write verbs on purpose, so that the refusal is exercised: declaring a verb says the element will
be *asked*, not that it will agree.

---

## Step 15 — Background mode passes

### One line was the whole footprint

396 of the original 400 uses traced to a single mouse click in `MauiFixture.ReturnToHub`, once
per test, plus the `SetForeground` each click implies. Every control-object click in the suite
already went through an automation pattern; this one could not, because the affordance is a MAUI
`ToolbarItem` rendered into native chrome. Step 2 measured four ways of activating it — Invoke
alone, Invoke with a click fallback, the shared activation ladder, and the ladder with a retry —
and all four fail identically: Invoke reports success, the command does not run, and the run
degrades from nine seconds to about a minute.

**So the page answers instead of the button.** `HubPage.AddBackToHub` declares `NavigateBack` on
the page it is attaching the item to, and the bridge routes it to `Navigation.PopAsync`. The
toolbar item is untouched and still works for a person; what changed is that the app now
publishes a semantic route to the same outcome.

This pulls a piece of step 22 forward, which step 15 could not be finished without.

### Two pointer paths that never needed to be pointer paths

Working the inventory turned up two places reaching for the wheel where the scroll pattern was
right there:

- **`ScrollView.ScrollForwardCore`** swiped unconditionally. `ScrollHelper` has had a
  pattern-first ladder all along and `CollectionObjectBase` climbs it; `ScrollView` was the one
  container that did not.
- **`CollectionObjectBase.ScrollToTop`** ran the pattern in a loop until it stopped making
  progress — which ends at the top by definition — and then swiped anyway. A pointer action taken
  every single time, which could not change the outcome.

Neither is a bridge verb. Both are the framework's own existing semantic route, unused.

### And the navigation stack was growing, which nobody had noticed

Replacing the click made an existing fault visible, and it is the most useful thing this step
found.

`ReturnToHub` decided whether it had anything to do by asking whether the hub's own elements
could be found. **On Windows that inference is wrong**: a `NavigationPage` keeps the pages
underneath the top one in the visual tree, so the hub stays findable while a page is open over
it. `ReturnToHub` concluded it was already home, `Open()` pushed anyway, and the stack grew — the
bridge's diagnostics caught it four deep during an eleven-test run.

Each extra level then cost a full `ShortTestTimeoutMs` waiting for a hub that could not appear
until the levels above it were popped. That is where the ten-second stalls came from, and it is
why the suite is now faster than the baseline rather than merely as fast.

The fix is to stop inferring: pop until the app says its stack is at the root.
`TryNavigateBack` is authoritative, self-terminating and cannot drift the way a visibility check
can. The click loop stays for platforms with no bridge, where the inference is also correct —
neither Android nor iOS keeps the previous page addressable behind the current one.

**`ReturnToHub` now checks its own postcondition.** Every round of grief this routine has caused
has had the same shape: it fails quietly, and `Open()` then reports `Page 'PageHub' is not
loaded` for whichever test happened to be next. Wrong control, wrong test, wrong area of the
suite. Three separate investigations have started in the wrong place because of it, including
one during this stage.

---

## Step 16 — Windows-only parallelism

The assembly-wide `DisableTestParallelization = true` is gone from the Windows head. The comment
beside it had named the real constraint rather than a runner one: two apps on screen at once
compete for the foreground, the pointer follows one of them, and keystrokes land wherever focus
went. All three are statements about **the desktop**, and stages 13 to 15 removed the suite's
need for it.

An attribute cannot express "unless background mode" — it is fixed at compile time, and the
policy is read from the environment. So the gate moved to where it can be asked at runtime:
`DesktopLease`, taken for the life of each fixture and released with it. With physical input
allowed it serialises the collections exactly as the attribute used to; with input refused or
audited, it lets go and the two apps run side by side.

**The mobile head keeps the old attribute**, in its own copy of `AssemblyInfo.cs` — which is why
that file is no longer shared between the two heads. Two Appium sessions share one emulator
whatever the input policy says, and that half of the original comment is still true.

---

## Findings that change later steps

<!-- FINDINGS -->

---

## Running it

```powershell
# background mode: nothing touches the desktop, collections run side by side
$env:BRINELL_BACKGROUND_MODE = "1"
$env:BRINELL_AUT_PLACE = "offscreen"
dotnet test testsnew\Brinell.Maui.UITests -v:minimal /nr:false

# the audit, which changes nothing and records everything
$env:BRINELL_BACKGROUND_MODE = "audit"
$env:BRINELL_PHYSICAL_INPUT_LOG = "E:\path\to\physical-input.log"
dotnet test testsnew\Brinell.Maui.UITests -v:minimal /nr:false
```

The log path must be absolute. The test host does not share the shell's working directory, and a
relative path writes somewhere that looks like nowhere.
