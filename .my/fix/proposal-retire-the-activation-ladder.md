---
title: Proposal — Retire the activation ladder; let each control declare what works
description: Why probing a sequence of patterns and hoping is unsound, what the repo has already measured about it, and a staged way out
status: proposal
---

# Proposal — Retire the activation ladder

## The proposal in one paragraph

Replace "try Selection, then Invoke, then click, and take the first that reports success" with
"this control, on this platform, is activated *this* way." A control object already knows what it
is. It should carry that knowledge explicitly, be tested on it, and fail loudly when the
declaration is wrong — instead of quietly taking a different route each run and leaving nobody
able to say which one it took.

## Why this is not a style argument

A ladder is only as good as its ability to detect that a rung failed. **Every rung failure this
repo has actually measured was a silent success.** Two of them, both documented in the code:

**One.** `ActivationHelper`'s own remarks:

> LegacyIAccessible is deliberately **not** in the ladder. A WinUI toggle advertises it and its
> `DoDefaultAction` reports success without changing the control's state, so including it makes a
> click silently do nothing on a Switch.

**Two.** `ToolbarButton`, added this week, after four separate measurements:

| Attempt on a MAUI `ToolbarItem` | Result |
|---|---|
| Invoke alone | 4 failures in 4 |
| Invoke, then click if it returned false | 3 failures in 4 |
| The shared `ActivationHelper` ladder | 1 failure in 3, run time 5 s → 58 s |
| A plain click | stable |

Read the second row again. The fallback **never fires**, because `Invoke` says it worked. A ladder
cannot fall back from a rung that lies, and a rung that lies is not exotic — it is two for two in
the only cases anyone here has looked at closely.

That is the whole argument. Everything below is corroboration.

## Four more things the repo has already established

**The lower rungs are unexercised, therefore untested.** The step 3 audit instrumented all 24 raw
input calls across 15 entry points and ran the full suite: *zero* control-object clicks fell
through to the pointer, and `SendKeys(SetValue fallback)` was reached no times at all. So the
fallback rungs are code that runs only in conditions the suite never reproduces — which is the
definition of untested. They are kept for safety and are the least safe code in the ladder.

**The ladder's membership is already being tuned per-control, globally.** LegacyIAccessible was
removed from the ladder *for everyone* to protect `Switch`. `ToggleControlBase` overrides the
ladder to add Toggle, and its comment carefully explains why the rung must come last so
`RadioButton` still gets SelectionItem. `ToolbarButton` overrides it to nothing. Three
per-control truths, expressed as edits to a shared sequence. **The mechanism for what is being
proposed here already exists and is already in use** — `TryActivateByPattern` is `virtual`, and
its own doc comment says controls needing a different rung should override it. What is missing is
the decision to make that the rule rather than the exception.

**A ladder that reaches the bottom reports the wrong thing.** `DatePicker.SetDateCore` has three
rungs and ends in "Could not set date without the pointer". That message names the last rung. It
does not say why rung 1 declined, which is the only interesting fact.

**Nondeterminism is a diagnosis tax.** Which rung ran depends on what the platform advertised that
run. When something breaks, the first question is "what did it actually do?", and today the answer
requires an audit log. Two of this week's investigations began by working that out.

## The counter-argument, stated fairly

The ladder buys three real things, and a proposal that pretends otherwise is not worth reading.

1. **Cross-platform reach from one implementation.** On Appium every pattern probe reports
   unsupported and the caller falls through to a click, which is correct on Android and iOS. The
   ladder *is* the platform abstraction.
2. **Resilience to framework churn.** When MAUI or WinUI changes which pattern a control exposes,
   a ladder keeps working and a declaration goes stale.
3. **Nobody needs the matrix up front.** Declaring per control × per platform means someone has
   to know it, and that knowledge does not exist in one place today.

Point 1 is the strongest and any proposal must answer it. Points 2 and 3 are real but cut both
ways: a stale declaration fails visibly, where a ladder silently changes behaviour under you —
which is worse, and is what "hoping for the best" means in practice.

## What "the control knows" would look like

Not a new mechanism — a change in where the default sits.

```csharp
// Today: every control inherits the ladder; three override it.
protected virtual bool TryActivateByPattern(IMauiElement element)
    => ActivationHelper.TryActivateByPattern(element);

// Proposed: every control states its route; the ladder is one named choice among them.
protected override ActivationRoute Activation => ActivationRoute.Invoke;      // Button
protected override ActivationRoute Activation => ActivationRoute.Toggle;      // Switch, CheckBox
protected override ActivationRoute Activation => ActivationRoute.SelectionItem; // RadioButton
protected override ActivationRoute Activation => ActivationRoute.Pointer;     // ToolbarItem
```

with the platform dimension answered where it belongs — by the driver, not by each control:

```csharp
// Appium has no UIA patterns. It answers every route it cannot serve with Pointer, once,
// in one place, instead of every control discovering it by probing.
ActivationRoute IMauiDriver.Resolve(ActivationRoute declared) => ActivationRoute.Pointer;
```

That is the answer to counter-argument 1: **the platform difference is a property of the driver
and belongs in the driver.** Today it is rediscovered at runtime, per control, per call, by
asking three questions that always have the same answer.

## Options

### Option 1 — Declare the route, keep a fallback, make the fallback shout

Each control declares its route. If the declared route reports failure, the framework still falls
back to a click — but records that the declaration was wrong, and a run with any such record
fails at the end.

- **Safest migration.** Nothing breaks on day one; wrong declarations surface as reports, not
  outages.
- Keeps working where a declaration goes stale.
- **Does not fix the lying-rung problem.** A route that falsely reports success is still believed.
- Best used as the *transition*, not the destination.

### Option 2 — Declare the route, no fallback

The declared route is the only route. A control whose route fails, fails.

- **Deterministic.** The same call does the same thing every run, on every machine.
- Failures name the actual problem at the actual place.
- Removes untested code paths entirely rather than keeping them for reassurance.
- **Cost:** the matrix must be right before this lands, and framework churn becomes a test
  failure rather than a silent re-route. That is the trade being proposed, and it is the right
  one for a *test* framework — a test that quietly changed how it drove the app is worth less
  than one that stopped.

### Option 3 — Probe once, then commit

Probe at session start, per control type, and cache the winner for the run.

- Determinism within a run, no matrix to maintain.
- **Rejected.** It still believes a rung that lies, and it makes behaviour depend on what the
  first probe happened to find — the same nondeterminism, moved somewhere harder to see. It also
  cannot probe safely: activating a control to find out whether activation works has side
  effects.

### Option 4 — Do nothing, document the ladder's limits

- **Rejected as a destination**, though it is the status quo and the status quo mostly works.
  The reason to reject it is that the two failures we know about were both found by accident,
  after costing days, and neither was findable from the outside. There is no reason to think
  there are exactly two.

## How we would know a declaration is right

This is the part that makes the proposal a proposal rather than an opinion, and it is cheap
because most of it exists.

**A conformance test per control, per platform.** For each control object, drive it once through
its declared route and assert the observable outcome the app reports. This is close to what
`AutomationProbeView` and `AutomationProbeTests` already do for addressability; the same shape,
asking about activation instead. The output is a table of control × platform × route that is
*measured*, not asserted by a comment — and it is exactly the matrix Option 2 needs before it can
land.

**Then the audit becomes a regression test.** `PhysicalInput` in `Audited` mode already records
every real click with its call site. Once routes are declared, any pointer use that is not
declared as `ActivationRoute.Pointer` is a defect, and the run can say so.

## Recommendation

**Option 1 now, Option 2 once the conformance matrix exists.**

Land the declaration and the loud fallback together. Run the conformance tests to produce the
matrix. When the matrix has been green across the three platforms for a while, delete the
fallback.

Do not do this during the current stage. The bridge work has already changed the contract, the
provider and the fixture in one go, and adding a framework-wide change to how every control
activates would mean a green suite proving four things at once. The same argument kept the
readiness RCA's options separable, and it was right.

**Suggested placement: after step 19** (gesture control objects), which is the last step that
adds control objects. Doing it before then means declaring routes for controls that do not exist
yet.

## What this is really about

The bridge already works the way this proposes. An app declares its verbs in markup;
`GetCapabilities` reports them; a client that asks for something undeclared is refused by name.
Stage A's design note puts it plainly — *"Declared, never inferred"* — and gives the reason:
inference makes an element automatable by accident, changes behaviour when someone refactors, and
leaves no single place to look.

Every word of that applies to activation. The bridge got it right because it was designed after
the ladder had already taught the lesson. This proposal is only asking the older half of the
framework to catch up with the newer half.
