# RCA — The intermittent "app freeze" was a stale root element

**Status: root-caused and fixed.** The app never froze. The bridge's own `UiaDisconnectProvider`
calls on page unload made UI Automation retire the test driver's element for the app's window, and
the driver held that one element for the whole run and never asked for it again.

---

## Symptom

Two of eight full MAUI UI runs ended the same way, about two minutes in:

- One test fails reaching the Container page: `Page 'ContainerPage' did not become ready ...
  Last readiness state: MissingRoot`.
- **Every later test fails within a millisecond or two** - 151 of them in both runs - with the
  same message: *"no element published on the app's bridge answers GetState"*.
- The bridge log, which records every call the app answers, stops dead and never resumes.
- The last lines before the silence are always a page arriving and the hub withdrawing:
  `withdraw 'PageHub' -> True`.

Both runs were identical to the test - 116 passed, 151 failed - which made it look deterministic.
Six runs after that, under a watcher ready to capture the app's stacks, never reproduced it.

## What it looked like, and why that was wrong

Every sign pointed at the app's UI thread having stopped: the bridge log is written on the app's
side after a call is answered, it went silent, and the tree description added to the failure
message read the root window as `class='(unreadable)'`. The working hypothesis, recorded at the
time, was a cross-process deadlock on the UI thread.

That was a reasonable reading and it was wrong. A frozen UI thread and a client that has stopped
reaching the app produce the same log: nothing.

## The reproduction

The full suite was too slow and too erratic to diagnose against. A stress walk reproduced it in
under a minute:

- open GridCollection, Container, Collection, Container, Gestures, round and round;
- with a second thread calling `CurrentRoute` and `NavigationDepth` throughout.

It failed within **3 to 38 page changes**, every time, with the suite's exact signature.

## The measurements that found it

**1. The app was not frozen.** Captured at the moment of failure from inside the test, before the
fixture could dispose the app:

```
app pid=16932 responding=True threads=33 cpu=00:00:17.6093750
3s later: responding=True cpu=00:00:17.6093750
```

and `dotnet-stack` showed the UI thread sitting in `Application.Start`, its message loop - idle.

**2. The driver's element was dead; the window was not.**

```
driver root hwnd=0xDE1264 IsWindow=True process MainWindowHandle=0xDE1264
cached root THREW COMException 0x80040201                      (UIA_E_ELEMENTNOTAVAILABLE)
fresh root Name='Brinell MAUI Sample' class='WinUIDesktopWin32WindowClass'
fresh root children: InputNonClientPointerSource, ReunionWindowingCaptionControls,
                     Microsoft.UI.Content.DesktopChildSiteBridge, BrinellUiaBridge, ?
```

Same handle, still the app's main window. The `AutomationElement` the driver had obtained at launch
answered `UIA_E_ELEMENTNOTAVAILABLE`. A fresh `FromHandle` on the same handle worked immediately -
and the bridge window was right there among its children.

**3. The trigger was ours.** The driver was changed to re-attach a retired root and count how often
it had to (below). Then the same 150-page walk under each condition:

| Condition | Root retired, 150 page changes |
|---|---:|
| As shipped, second thread calling the bridge | 7, 8 |
| No second thread | 1 |
| Quiet-run window measures off (`BRINELL_BACKGROUND_MODE=0`) | 6 |
| **Bridge skips `UiaDisconnectProvider` on withdrawal** | **0** |

The quiet-run measures - `WS_EX_NOACTIVATE` and the foreground watchdog - were suspects because
they were new, and are cleared. Client traffic during transitions makes it several times likelier
but is not required. Removing the disconnect removes it.

## Root cause

**Two defects, one in each process, and neither is harmful alone.**

**The trigger, app side.** `BrinellUiaBridge.Unregister` called `UiaDisconnectProvider` on a page's
target providers every time the page unloaded - several times per navigation. Step 28 established
that this call reaches out to connected clients to revoke their references. Measured here, it does
more than revoke the one provider it names: some fraction of the time it also retires the client's
element for the app's *top-level window*. Why UI Automation widens the invalidation is not
documented and was not established; that it does is measured above.

**The amplifier, client side.** `FlaUIMauiDriver` resolved the window's `AutomationElement` once, in
its constructor, and used it for everything afterwards - every bridge lookup, every tree search. It
had no path back from a retired element. So a single invalidation, which a fresh lookup would have
survived in a millisecond, blinded the driver for the rest of the run, and every later test failed
at its first step. That is the cascade: not 151 failures, one.

## Why it was hard to see

- **Identical numbers across two runs** read as determinism. It was the suite's fixed order putting
  the same Container transition at the same point under the same load.
- **"Unreadable" read as "gone".** The tree description could not read the root, which is true of a
  dead window and equally true of a dead *element* for a live window.
- **Silence has two causes.** A bridge log that stops means the app stopped answering, or the
  client stopped asking. Only capturing the app while it was supposedly frozen separated them.
- **The trigger is a cleanup call.** Nobody looks at the code that tidies up.

## The fix

**Remove the trigger.** A withdrawn target is now *retired* rather than disconnected
(`BridgeTargetProvider.Retire`): every verb it is asked answers `UIA_E_ELEMENTNOTAVAILABLE` at once,
and it reports itself offscreen and disabled. That keeps the promise the disconnect was making to a
client still holding the provider - a stale call fails promptly and retryably - without touching UI
Automation's connection. It is still removed from the fragment root's children, so nothing new can
find it. `UiaDisconnectProvider` stays exactly where step 28 put it to prevent hangs: when the bridge
itself is torn down with its window.

**Remove the amplifier.** `FlaUIMauiDriver.RootElement` checks the element is still available (one
property read) and re-attaches by window handle when UI Automation has retired it. The handle is the
identity; the element is a cache. `RootReattachments` counts how often that happens.

Both, deliberately. The first stops our own code causing it. The second means that if anything else
ever retires the window element - another cause, a future WinUI - the run survives it instead of
losing everything after that moment.

## Verification

- The stress walk: **150 of 150 page changes, 0 root re-attachments**, with a second thread calling
  the bridge throughout. Kept as `NavigationStressTests`, opt-in with `BRINELL_STRESS=1` because it
  takes a minute; it asserts both that the walk completes and that no re-attachment was needed.
- `Brinell.Uia.Tests`: 74 of 74, including step 28's lifetime tests - teardown still disconnects,
  and a call through a destroyed bridge still fails promptly.
- Full MAUI UI suite: see the stage J record in `plan-the-quiet-run.md`.

## What is not known

**Why a provider disconnect retires the top-level window's element.** The measurements establish
cause and effect; they do not explain UI Automation's internals. The fragment root hosts itself on a
child window (`UiaHostProviderFromHwnd`), and a guess is that disconnect invalidates client-side
elements sharing a connection with that host rather than one provider alone. A minimal repro in
`Brinell.Uia.TestHost` - a window with a bridge, a client holding the window element, targets
registered and disconnected - would settle it, and is the place to start if it matters.

## Lessons

- **A cached UI Automation element is a cache.** Anything held for a run's lifetime needs a way
  back from `UIA_E_ELEMENTNOTAVAILABLE`, or one transient becomes every later failure.
- **Capture the process in the act before theorising about it.** Two sessions went to a
  deadlock hypothesis the first `Responding=True` disproved.
- **"Identical" can be order, not determinism.** A fixed test order puts the same transition at the
  same place under the same load.
