---
title: RCA — Navigation tests stall for two seconds at a time
description: A grace period keyed on the wrong question, and why a bool return made the wrong question look like the only one
status: rca
---

# RCA — Navigation tests stall two seconds at a time

## Symptom

The navigation tests appear to hang. They do not hang — every wait is bounded — but they spend
whole seconds doing nothing visible, which is indistinguishable from hanging while watching a run.

Measured, same machine, same session:

| Filter | Tests | Duration | Per test |
|---|---:|---:|---:|
| `Tests.Buttons` | 11 | 1 s | 0.09 s |
| `Tests.Gestures` | 7 | 5 s | 0.7 s |
| `NavigationVerbTests` | 3 | 7 s | **2.3 s** |
| `NavigateBack_AtTheHub` alone | 1 | 4 s | **4 s** |
| `NavigationControlTests` | 21 | 24 s | 1.1 s |

**There are two separate causes**, and they are unrelated to each other beyond a shared shape:
in both, the framework waits out a timeout to confirm something it could have known at once.
One is a defect introduced this week; the other is a deliberate cost that is larger than it needs
to be.

## Cause A — `TryNavigateBack` waits two seconds to say no

### The measurement that identifies it

Running the single slowest test with `BRINELL_UIA_LOG` on:

```
Duration: 4 s
=== gaps over 0.5s ===          (none)
=== NavigateBack decisions ===  1
```

**One** `NavigateBack` call reached the app in four seconds, and the app-side log has no gaps at
all — it was idle throughout. So the four seconds are spent entirely in the test process, in code
that never asks the app anything.

That rules out the app, the bridge, UI Automation and the navigation itself, and points at one
place.

### Root cause

`FlaUIMauiDriver.TryNavigateBack` waits up to two seconds before answering "no", and at the hub
that wait can never succeed.

```csharp
if (Ask() == HResults.S_OK) return true;
if (!HasBridge(...)) return false;          // no bridge: answer now

var deadline = DateTime.UtcNow.AddMilliseconds(2000);
while (DateTime.UtcNow < deadline) { ... }  // otherwise: wait
```

The grace period exists for a real race: a page publishes its bridge target when it *loads*,
which is a later moment than its root appearing in the automation tree, so there is a window in
which the app truthfully reports nothing to pop about a page that is on its way in. Falling
straight through to a mouse click in that window is what background mode refuses, and that is how
the race was found.

**But the condition guarding the wait is "does this app have a bridge at all", and that is the
wrong question.** It is true whenever any element anywhere has declared a verb — which, in this
app, is always. So the wait fires for the commonest negative answer there is: *the app is at its
root and there is genuinely nothing to pop*.

At the hub there is no target that declares `NavigateBack` at all, because only pushed pages
declare it — `HubPage.AddBackToHub` attaches the declaration to the page it is opening. So the
loop polls a tree that will never contain what it is looking for, for two seconds, and then
reports the answer it already had at the start.

`MauiFixture.ReturnToHub` calls this once per `Open`, so every test that begins with the app
already at the hub pays two seconds before it starts.

### Why the wrong question looked like the right one

`TryNavigateBack` returns `bool`, and it uses that one bit for three different situations:

| The app means | The caller should |
|---|---|
| there is no bridge in this app | click the affordance instead |
| the page has not published its target yet | wait, briefly |
| we are at the root; nothing to pop | stop, immediately |

Three answers, one bit. The caller cannot tell them apart, so it has to guess — and the guess I
wrote was a timeout. **The bool did not merely fail to help; it is what forced a guess to exist.**

That makes this RCA an instance of a general problem rather than a one-off, and it is the same
one described in
[design-actions-do-not-try.md](design-actions-do-not-try.md): an action that reports failure as a
boolean tells the caller *that* something did not happen and never *what*, so every call site
invents its own recovery. The activation ladder was the same shape and was deleted for the same
reason.

### The fix

**Not yet applied.** Two parts, and the second is the one that matters.

**Make the root answer authoritatively.** Declare `NavigateBack` on the hub page as well as on
pushed pages, so a target always exists to answer. At the root it replies `S_FALSE` — succeeded
and did nothing — immediately, and no waiting is possible or needed. The wait then only ever
happens in the window it was written for.

**Stop conflating the three answers.** With a target always present, "no target answered" means
"no bridge in this app" and nothing else, so the caller can distinguish all three cases without
timing anything. That is the change that removes the guess rather than retuning it.

A shorter grace period would hide this symptom and keep the cause. Two seconds is not too long
for the race it was written for; it is being spent on a question it was never meant to answer.

## Cause B — two negative assertions pay ten seconds each

`NavigationControlTests` is 24 s for 21 tests, and 20 of those seconds are two tests:

```
TabMenu_UnknownCaption_Throws          [10 s]
Toolbar_DoesNotReachItemsOutsideItself [10 s]
```

Every other test in the class is under 400 ms.

Both assert that naming an item that is not there throws:

```csharp
Assert.Throws<ElementNotFoundException>(
    () => Page.Tabs.Item("Nonexistent", TestConstants.ShortTestTimeoutMs));
```

`Item(key, timeout)` **waits** for the item to appear before giving up — deliberately, and for a
good reason it documents: naming an item is a search, and the thing being searched for often
arrives a frame after whatever revealed it, as in `Menu.Open()["New"]`. Asserting an absence
therefore costs the whole timeout, and `ShortTestTimeoutMs` is 10 seconds.

**This is not a defect.** The author of `Toolbar_DoesNotReachItemsOutsideItself` knew precisely
what it costs and left a comment saying so, and asserted the fast form first:

> `TryItem` answers now; `Item` waits, so it is given a short timeout rather than the full one it
> would otherwise spend confirming an absence the line above proved.

The framework is also right: "the cost lands on absence" is the honest price of "is it really not
there?", and it is stated in `ViewBase` as a design position.

**What is wrong is only the number.** These two assertions are about *which exception* a waiting
API throws, not about how long it waits first. A 500 ms timeout proves exactly the same thing and
returns 19 seconds to the suite — about 16% of the whole run. `ShortTestTimeoutMs` was chosen as
"short" against the 15-second default; against an assertion that needs no waiting at all, 10
seconds is not short.

Neither test is currently doing anything wrong, which is why this is recorded as a cost rather
than a bug. It is the largest single block of dead time left in the suite.

## What this cost, and what caught it

The stall was introduced in the same change that fixed the background-mode refusal, and it went
out with a green suite: total runtime actually *fell* in that run, because a separate fix to
`ReturnToHub` was saving more than this was costing. A green suite and a faster run together hid
a 25× per-test regression in one area.

It was noticed by a person watching the tests run, not by anything in the suite. Worth stating
plainly: there is no assertion anywhere on how long an operation should take, so a change that
makes the framework wait is invisible until someone sits and watches it. The per-filter timings
at the top of this document took one command and would have caught it.
