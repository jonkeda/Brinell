---
title: Design — Actions do not Try; Brinell is a test framework, not a try framework
description: Why TryPerformGesture, TryAppendText and TryClearFocus should not exist, what replaces them, and how little is left once questions stop needing the prefix too
status: design
---

# Design — Actions do not Try

## The rule

**A method that performs an action either performs it or throws. It does not return whether it
worked.**

The exception is a **search**, where absence is a legitimate answer to a legitimate question:
`TryFindElement`, `TryItem`, `IsExists`. Asking "is this here?" and being told no is information.
Asking "do this" and being told "no" is not — it is the absence of information, dressed as a
return value.

## Why this is not a naming preference

A `bool`-returning action produces the same call site every time:

```csharp
if (!element.TryDoTheThing()) { doSomethingElse(); }
```

**That is a ladder.** Not a metaphor for one — the activation ladder that was deleted this week
was exactly this shape, repeated at eleven call sites, and it hid two defects for months because
a rung that reports failure can be fallen back from while a rung that *lies* cannot. Every
`Try` on an action is a ladder waiting for someone to write its `else`.

One already has. `Entry.AppendCore`:

```csharp
if (element.TryAppendText(text)) return;
element.SendKeys(text);          // ← the else. A different mechanism, silently.
```

The suite cannot tell you which of those two ran on any given day.

## The evidence from this week

Three separate incidents, one shape.

**`TryActivateByPattern`.** Returned false so the caller could click instead. A `ToolbarItem`
accepts Invoke and raises nothing, so the false never came and the click never happened;
navigation silently did not occur and the run degraded from 5 s to 58 s. Deleted — see
[design-controls-know-how-to-click.md](design-controls-know-how-to-click.md).

**`TryAppendText`.** Its `else` types the text instead. Typing raises `TextChanged` per character
where setting raises it once, applies `MaxLength` as it goes, and can be refused by a numeric
keyboard — so the two branches are not equivalent, and which one ran is invisible.

**`TryNavigateBack`.** The clearest case, because the bool provably caused a defect rather than
merely permitting one. It answers `false` for three different situations:

| The app means | The caller should |
|---|---|
| there is no bridge in this app | click the affordance instead |
| the page has not published its target yet | wait, briefly |
| we are at the root; nothing to pop | stop, immediately |

Three answers, one bit. The caller cannot distinguish them, so it guesses — and the guess written
into the driver was a two-second timeout, which now fires on the commonest negative and costs
every test that starts at the hub two seconds. Full account in
[rca-navigation-tests-stall.md](rca-navigation-tests-stall.md).

**The bool did not fail to prevent that bug. It required it.**

## What replaces each

### `TryAppendText` → `AppendText`

```csharp
void AppendText(string text);   // throws where the platform has no route
```

`Entry.AppendCore` becomes `element.AppendText(text)`. The platform decides how: Windows through
the bridge, Appium by typing — the same split as `Invoke`/`Toggle`/`Select`, where the control
names the operation and the element implements it. A test that specifically wants keystrokes
still asks for them by name with `TextInputMethod.Keys`, which is a different request and should
read like one.

### `TryClearFocus` → `ClearFocus`

```csharp
void ClearFocus();              // throws where the platform has no route
```

`BlurCore` becomes `element.ClearFocus()`. On Appium that is a Tab keystroke, written once in the
driver rather than discovered per call — and written there honestly, since Tab moves focus on
rather than clearing it, which is a platform limitation worth stating in one place.

### `TryPerformGesture` → delete it

`PerformGesture` already exists and throws. `SupportsGesture` already exists and answers the
question. `TryPerformGesture` is those two glued together, and the gluing is what makes it
harmful: it invites a call site to proceed as though the gesture happened.

A caller that genuinely wants to branch writes the branch, and it reads better:

```csharp
if (element.SupportsGesture(MauiGesture.SwipeRight))
{
    element.PerformGesture(MauiGesture.SwipeRight);
}
```

Nothing is lost, and the two questions stay separate: *can this app do it* is a property of the
app under test, *did it work* is not a question a test should have to ask.

### `TryNavigateBack` → `NavigateBack` plus a real question

This one needs more than a rename, because the bool is carrying three answers.

```csharp
void NavigateBack();            // pops, or throws saying why not
bool IsAtNavigationRoot { get; }  // the question, asked separately
```

`ReturnToHub` becomes: if not at the root, go back; then confirm. No timeout, no guess, and the
two-second stall in the RCA disappears rather than being retuned — because the caller can finally
tell "nothing to pop" from "not ready yet" from "no bridge".

This also settles the RCA's open fix: the hub page must publish a target so something can answer
`IsAtNavigationRoot` authoritatively.

## The Try that stays

`TryFindElement`, and its relatives `TryItem`, `TryFindElementAfterScroll`, `IsExists`.

These are searches. "Is there an element matching this?" has two true answers, and a test that
asks is entitled to either. The distinction is not subtle and does not need a judgement call at
each new method:

| | Question | Command |
|---|---|---|
| Shape | `TryFind…`, `Is…`, `Supports…` | `Invoke`, `SetText`, `NavigateBack` |
| A "no" means | the thing is not there | nothing — it is a failure |
| Caller may branch | yes, that is the point | no, it should stop |

**`Try` on a question is an answer. `Try` on a command is a shrug.**

### And most questions do not need the word either

The table above is about which *operations* may answer no. It is not a licence to put `Try` on
every one of them, and the distinction matters because "it is a search" is about to become the
reason a prefix survives review.

`TryFindElement` earns the word for a specific reason: `FindElement` sits beside it and throws.
The prefix tells a pair apart. Where there is no throwing twin, a nullable return type already
says "may be absent", and the prefix adds nothing except the suggestion that a second branch
exists somewhere.

So: **`Try` is a disambiguator, not a category.** Write the question as `TapRecognizer(view,
taps)` returning `TapGestureRecognizer?` and there is nothing left for the word to do. Keep it
only where a throwing sibling needs telling apart, or where the language forces a `bool` plus
`out` — which is the BCL's problem, not ours: `TryGetValue`, `TryParse` and
`WeakReference.TryGetTarget` keep their names because they are not ours to rename, and
`GetValueOrDefault` is usually the better call anyway.

This came up while rewriting step 18, where four `Try` methods in the gesture surface
(`TryTap`, `TrySwipeRecognizer`, `TryOpenSwipeView`, `TryStartRefresh`) split into questions and
commands — and not one of the resulting halves wanted the prefix. See
[design-gesture-dispatch-binds-at-publish.md](design-gesture-dispatch-binds-at-publish.md).

One internal exception is worth naming so it is not mistaken for a violation:
`CollectionObjectBase.TryActivate` walks a list of candidate rows and catches, because a given
candidate may be the *wrong element*. That is a search wearing an action's clothes, and its
comment says so. It stays.

## Migration

| Remove | Add | Call sites |
|---|---|---|
| `IMauiElement.TryAppendText` | `AppendText` | `Entry.AppendCore` |
| `IMauiElement.TryClearFocus` | `ClearFocus` | `FocusableControlBase.BlurCore` |
| `IMauiElement.TryPerformGesture` | — | none in the suite |
| `IMauiDriver.TryPerformGesture` | — | none in the suite |
| `IMauiDriver.TryNavigateBack` | `NavigateBack`, `IsAtNavigationRoot` | `MauiFixture.ReturnToHub`, `NavigationVerbTests` |

The first four are small and independent. The fifth is the one with a defect attached and should
be done with the RCA's fix, not separately.

**Also worth reviewing under the same rule, not proposed here:** `TryScrollContent`,
`ScrollHelper.TryScrollForward` / `TryScrollBack` / `TrySwipeForward`, and
`ToggleControlBase.TrySetStateByPattern`. Each returns a bool from an action, and each has an
`else` somewhere. They are left out because scrolling has a genuine "already at the end" answer
that is closer to a question than to a failure, and separating those cases needs its own look.

## What this costs

**A test that used to limp will now fail.** That is the intent, and it is the risk: paths that
have been quietly taking the second branch will surface all at once, exactly as removing the
activation ladder surfaced `TabItem` naming the wrong operation.

That is the right trade for a test framework and it is worth saying why: the purpose of a test
suite is to report what the application did. A framework that quietly substitutes a different
mechanism when the first one fails is reporting on a run that nobody specified, and a green
result from it means less than it appears to. **Trying is what an application does to stay up.
A test framework has the opposite job.**
