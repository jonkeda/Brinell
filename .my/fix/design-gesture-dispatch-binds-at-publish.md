---
title: Design — The bridge binds a verb to one handler at publish time
description: Replacing the dispatcher's sink → MAUI API → recognizer ladder with a resolution done once, when the element is published
status: design
---

# Design — Bind once, dispatch by lookup

Rewrites **step 18**, which as planned said:

> Ladder: sink → public MAUI API → `TapGestureRecognizer.Command` → `UIA_E_NOTSUPPORTED`.

That is [the activation ladder](design-controls-know-how-to-click.md) again, one layer down and
in the other process. Same shape, same failure mode, same fix.

## The whole design

```csharp
// At publish, once per element: what can this element actually do?
var bound = VerbBindings.Resolve(element, declaredVerbs, sink);

// At call time, per verb: one lookup.
var handler = bound.GetValueOrDefault(verb);
return handler is null
    ? HResults.UIA_E_NOTSUPPORTED
    : handler(element, arg1, arg2);
```

No sequence, no probe, no `Try`, no fallthrough. `Capabilities` becomes `bound.Keys`.

## Where the line is

A resolution is not a ladder just because it considers more than one source. The test is:

> **A ladder runs a thing to find out whether it was the right thing.** A resolution looks at
> what is there and picks one. The difference is whether a failed attempt can leave the app
> changed.

By that test, today's dispatcher is a ladder, and one line of it is worse than anything the
activation ladder did:

```csharp
case BrinellVerb.Tap when MauiCapabilities.TryTap(element, 1):
case BrinellVerb.DoubleTap when MauiCapabilities.TryTap(element, 2):
    return HResults.S_OK;
```

**The guard performs the gesture.** `TryTap` executes the recognizer's command, and its return
value decides whether the `case` matches. When it returns false the switch falls through to the
next rung — having already run the app's code, or not, with nothing recording which. A `when`
clause that mutates the app under test is the one construct here that cannot be reasoned about
from the outside at all.

The `SwipeView` rung has the quieter version of the same thing: if the type-specific rung
declines, dispatch carries on to the recognizer rung, so one swipe verb has two possible meanings
on one element and which one it got depends on a runtime answer nobody logged.

## The declaration is already there — it is just not used

The app already states, per element, what it answers:

```xml
<SwipeView AutomationId="TestSwipeView" uia:GestureAutomation.Verbs="SwipeRight" />
```

`GestureAutomation` calls this out itself: *"Declared, never inferred."* But the declaration is
used only to decide **what to publish**. The dispatcher then re-derives at every single call what
the markup already said, by asking the element three questions whose answers were fixed when the
page was written.

So the rethink is not a new mechanism. It is using the one that exists.

## Resolution

One rule, applied once, over facts that are all static:

1. **A sink owns the verbs it names.** Not "first refusal" — ownership.
2. **Otherwise the binding table decides**, keyed by element type and verb.
3. **A declared verb that resolves to nothing is reported at publish and not published.**

| Verb | Bound to | Needs |
|---|---|---|
| `Tap` | `TapGestureRecognizer.Command` | a recognizer with `NumberOfTapsRequired == 1` |
| `DoubleTap` | `TapGestureRecognizer.Command` | one with `NumberOfTapsRequired == 2` |
| `LongPress` | **nothing** | a sink, by name |
| `SwipeLeft/Right/Up/Down` | `SwipeView.Open(...)` | element is a `SwipeView` |
| `SwipeLeft/Right/Up/Down` | `SwipeGestureRecognizer.Command` | a recognizer whose `Direction` has the flag |
| `SwipeDown` | `RefreshView.IsRefreshing = true` | element is a `RefreshView` |
| `Pan` | **nothing** | a sink, by name |
| `Pinch` | **nothing** | a sink, by name |

Two rows for the swipe verbs, and the type row wins — stated here rather than implied by rung
order in another file. It is still a resolution and not a ladder, because neither candidate is
*run* to find out.

**The three empty rows are the interesting ones.** `SendPan` and `SendPinch` are internal in
MAUI 10, and the file that would call them already says why it will not reflect into them.
`LongPress` is emptier still: **MAUI has no long-press recognizer at all.** The obvious binding is
`PointerGestureRecognizer.PointerPressedCommand`, and it is wrong — pressing is not holding, an app
that starts a timer on press and cancels it on release would never see a long press, and a test
asking for one would be told it got one. That is precisely the substitution this whole programme
exists to stop; `MauiCapabilities`' own rule already covers it: *"Where no public API exists, the
verb is not supported and says so."*

Under a ladder none of that is visible — the verb falls all the way through and returns
`UIA_E_NOTSUPPORTED`, the same answer a typo gets. Under a binding table each is a hole with a
name, reported at startup by the app that declared it. So the honest count is **six of the nine
gesture verbs bind; three are reachable only through a sink**, and nobody has to discover which
from a failing test.

## The sink stops being a rung

```csharp
// Today - a ladder rung wearing an interface.
bool TryInvoke(VisualElement element, BrinellVerb verb, int arg1, int arg2);

// After - it says what it owns, and it owns it.
IReadOnlyCollection<BrinellVerb> Verbs { get; }
int Invoke(VisualElement element, BrinellVerb verb, int arg1, int arg2);
string Exchange(VisualElement element, BrinellVerb verb, string argument);
```

The `bool` is exactly the `Try` that [design-actions-do-not-try](design-actions-do-not-try.md) is
about: it means "I did not handle this, carry on", which is only a sentence a ladder needs. A sink
that names `Pan` handles `Pan`. If it cannot, it throws, and the bridge reports a failure rather
than silently doing something else to the app.

## Three things become true that are not true today

**1. `GetCapabilities` stops lying.** `MauiVerbTarget.Capabilities` is handed the *declared* verbs
and serves them verbatim, so an element that declares `Tap` with no tap recognizer reports `Tap`
as a capability and refuses it on use. After binding, capabilities are the bound verbs: true by
construction, because the thing that answers is the thing that was counted.

**2. "I don't do that" separates from "not right now".** A command's `CanExecute` is checked
inside today's probe, and a `false` falls through to the next rung and eventually to
`UIA_E_NOTSUPPORTED` — the same code as an unknown verb. The two are different facts about the app
and a test should be told which it hit. After binding, the recognizer was found at publish, so at
call time a refusing command is `S_FALSE`: *the element does this verb and declined this time*.

**3. Every `Try` in the gesture surface disappears — four of them, to none.** `TryTap` searches for
a recognizer **and** executes its command, which is why it cannot report which half failed.
Splitting it gives a question and a command, and *neither* wants the word:

```csharp
// Bind time - questions. The return type already says "may be absent".
static TapGestureRecognizer?   TapRecognizer(View view, int taps);
static SwipeGestureRecognizer? SwipeRecognizer(View view, SwipeDirection direction);
static OpenSwipeItem?          SwipeItemFor(BrinellVerb verb);   // already written this way

// Call time - commands. Nothing to branch on, because the search already answered.
static void Execute(ICommand command, object? parameter);
static void OpenSwipeView(SwipeView view, OpenSwipeItem item);
static void StartRefresh(RefreshView view);
```

`TryOpenSwipeView`, `TryStartRefresh`, `TryTap` and `TrySwipeRecognizer` all go, and nothing
replaces the prefix.

**Why a search does not need it either.** [design-actions-do-not-try](design-actions-do-not-try.md)
keeps `Try` for questions, and that is right, but it is worth being exact about what the word is
*for*: `TryFindElement` needs it because `FindElement` exists beside it and throws. The prefix
distinguishes a pair. Where there is no throwing twin — and there is no `TapRecognizer` that
throws, because "this element has no tap recognizer" is an ordinary answer — the nullable return
type says everything the prefix would, and the prefix only suggests a second branch that does not
exist. `SwipeItemFor` in this very file is already written that way.

So the rule underneath the rule: **`Try` is a disambiguator, not a category.** A question earns
the word only when the language cannot express "absent" in its return type, or when a throwing
sibling needs telling apart.

## What gets deleted

- The four-rung ladder in `MauiVerbDispatcher.PerformInvoke`, including both `when` clauses that
  perform a gesture to decide whether they match.
- The sink's two `bool`-returning methods, and the phrase "first refusal" wherever it appears.
- `MauiCapabilities.TryTap`, `TrySwipeRecognizer`, `TryOpenSwipeView` and `TryStartRefresh` —
  the whole `Try` prefix leaves this file, replaced by nullable-returning questions and
  void-returning commands.

## What gets added

- `VerbBindings` — the table, and `Resolve(element, declaredVerbs, sink)`.
- A publish-time report naming every declared verb that bound to nothing, with the reason:
  *"`GestureDoubleTapTarget` declares `DoubleTap`, but its `TapGestureRecognizer` requires 1 tap
  and no sink names it."*
- Tests that every row of the table binds to what it should, including the inverted
  swipe-to-`OpenSwipeItem` mapping that step 18 singles out as the thing most likely to be
  "fixed" backwards.

  **Not as headless unit tests, though it was written here that they would be.** `Resolve` is a
  pure function of an element and a declaration, so it ought to be testable without a window -
  but it takes MAUI types, and MAUI 10's `Microsoft.Maui.Controls` package has no plain `net10.0`
  assembly at all: it is a metapackage over platform-specific ones supplied by the workload.
  `Brinell.Maui` and `Brinell.Maui.Tests` both target plain `net10.0` and reference no MAUI, so
  there is nowhere in this repo such a test could live. It would need a new
  `net10.0-windows10.0.19041.0` test project with `UseMaui`, which is its own piece of
  infrastructure.

  So the table is verified through the gestures page instead, where `GetCapabilities` reports
  exactly what bound and each row was built to exercise one branch. That is slower and a stronger
  test of the real thing; the headless version is worth having for the speed, and is parked.

## Risks

**Bindings are resolved once, and recognizers are mutable.** An element that gains or loses a
gesture recognizer after `Loaded` has a stale binding. Markup-declared recognizers cannot do this;
code that manipulates `GestureRecognizers` at run time can. Mitigation: subscribe to the collection
and rebind. `Publish` already runs per `Loaded`, so the page-level case is covered.

**A declaration that used to work silently now fails loudly at startup.** That is the intent, and
it is the same risk the activation ladder change took. The sample app is where it lands first, and
step 17 is writing those declarations anyway — so the cost is paid while the page is being written
rather than a stage later.

**Bind time and call time can disagree about `CanExecute`.** Deliberately: bind time asks whether a
command *exists*, call time asks whether it will run now. Conflating them is what produces today's
single unhelpful error code. The same split covers `RefreshView`: the handler reads `IsRefreshing`
and answers `S_FALSE`, rather than calling something named `TryStartRefresh` and interpreting a
bool. Both are call-time questions with real answers, asked plainly.

## How we would know it worked

- The Gestures filter passes, and `GetCapabilities` on each row of the gestures page returns
  exactly the verbs that row answers — checkable, for the first time, against the page's markup.
- `GestureNoDeclarationTarget` — the negative case — is absent from the bridge, and a row that
  declares a verb it cannot perform is named on the debug output at startup rather than by a test.
- `Brinell.Maui.Tests` covers the binding table with no app running.

## Sequencing

Step 17 is unaffected: it writes the page and the declarations, and those are the input to this.
This replaces step 18, and step 19 then moves the tests onto control objects that call verbs which
are known to exist.
