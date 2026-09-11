---
title: Design — Delete the activation ladder; each control overrides ClickCore
description: Replacing probe-and-hope with a control that states its own action and an element that implements it per platform
status: design
---

# Design — Each control knows how to click itself

Supersedes the `ActivationRoute` sketch in
[proposal-retire-the-activation-ladder.md](proposal-retire-the-activation-ladder.md). That
proposal kept a resolver and a declared route, which is the same indirection with a nicer name.
This one has neither.

## The whole design in four lines

```csharp
// Button                                   // Switch, CheckBox
protected override void ClickCore(...)      protected override void ClickCore(...)
    => element.Invoke();                        => element.Toggle();

// RadioButton, list items                  // ToolbarItem
protected override void ClickCore(...)      protected override void ClickCore(...)
    => element.Select();                        => element.Click();
```

`ActivationHelper`, `TryActivateByPattern` and the proposed `ActivationRoute` are all deleted.
There is no sequence, no probe, no enum, no property, and no resolver.

## The one idea that makes it work

**The control knows *what* it is doing. The element knows *how* its platform does that.**

Those are different kinds of knowledge and they belong in different places. Today they are
tangled: a control asks a shared helper to try three platform mechanisms in order, so every
control is making a platform decision it has no business making, once per call, at run time.

Split them and both halves become obvious:

| | Knows | Example |
|---|---|---|
| **Control object** | the semantics of its own activation | "a Switch is toggled, not invoked" |
| **Element / driver** | how this platform performs that semantic | "on Appium, invoking is a tap" |

So `IMauiElement` grows three semantic operations beside the pointer one it already has:

```csharp
void Invoke();   // perform the control's primary action
void Toggle();   // flip a two- or three-state control
void Select();   // choose this as one of a group, or as a list item
void Click();    // already exists: a real pointer, deliberately
```

Each driver implements them once:

```csharp
// FlaUIMauiElement — Windows has real automation patterns.
public void Invoke() => Pattern<IInvokePatternElement>().InvokePattern();
public void Toggle() => Pattern<ITogglePatternElement>().TogglePattern();
public void Select() => Pattern<ISelectionItemPatternElement>().SelectItemPattern();

// AppiumMauiElement — on a touch device, all three are a tap. That is not a fallback;
// it is how the platform performs those semantics, decided once, here.
public void Invoke() => Click();
public void Toggle() => Click();
public void Select() => Click();
```

**That is the answer to the strongest argument for the ladder.** The ladder is currently the only
thing making the suite cross-platform: on Appium every pattern probe reports unsupported and the
caller falls through to a click, which is correct mobile behaviour. But that answer is the same
every time — the ladder rediscovers it per control, per call, by asking three questions whose
answers never change. Writing it down once in the driver is not a loss of flexibility. It is the
removal of a runtime search for a compile-time fact.

## Failure becomes loud, and that is the point

`Invoke()` on Windows throws when the element exposes no Invoke pattern. It does not quietly
click instead.

That is the whole behavioural change, and it is worth being explicit that it *is* a change:
today, a control whose pattern disappears keeps working through the pointer and nobody finds out.
Afterwards, it fails, names the control and the operation, and someone decides. For a test
framework that is the right trade — a suite that silently changed how it drove the app is worth
less than one that stopped and said so.

It also fixes the failure the ladder cannot handle. The ladder assumes a rung that cannot do the
job will *say* so, and this repo has measured two rungs that lie instead:

- `LegacyIAccessible.DoDefaultAction` reports success without changing a Switch — which is why it
  was removed from the ladder for every control, to protect one.
- A MAUI `ToolbarItem` accepts `Invoke` and raises nothing — measured four ways; the variant
  "Invoke, then click if it returned false" failed 3 times in 4, because the fallback never fires.

Neither is detectable from inside a ladder. Both are trivial once a control simply states what it
uses: the Switch says `Toggle()`, the toolbar item says `Click()`.

## Every current call site

Eleven places use the ladder today. Each becomes one line.

| Site | Today | Becomes |
|---|---|---|
| `ClickableControlBase.ClickCore` | ladder, then `Click()` | `element.Invoke()` — the default for a plain command control |
| `Button<T>` | inherits the ladder | inherits `Invoke()` |
| `ToolbarButton<T>` | overrides the ladder to `false` so it falls through | `element.Click()` |
| `ToggleControlBase<T>` → `Switch`, `CheckBox` | ladder plus a Toggle rung appended | `element.Toggle()` |
| `RadioButton<T>` | same base; relies on SelectionItem being tried before Toggle | `element.Select()` |
| `ClickableItemBase.ClickCore` | ladder, then `Click()` | `element.Select()` |
| `Menu.OpenCore` | ladder, then `Click()` | `element.Invoke()` |
| `ShellFlyout.Activate` | ladder, then `Click()` | `element.Invoke()` |
| `ShellChrome` light-dismiss | ladder, then `Click()`, inside a Windows branch | `element.Invoke()` |
| `CollectionObjectBase.TryActivate` | ladder, then `Click()`, wrapped in catch | `element.Invoke()`, still wrapped — see below |
| `IconCommandButton`, `RoundButton` (Extensions) | ladder | `element.Invoke()` |

Note what `RadioButton` gains. Today its correctness depends on rung *order* inside a shared
helper: `ToggleControlBase`'s override appends Toggle and its comment carefully explains that the
rung must come last, or a radio button would toggle instead of selecting. That is a real
constraint expressed as a comment about an ordering in another file. Afterwards it is
`element.Select()`, and there is nothing to get wrong.

## The two sites that look like they still need a ladder

Both were checked, and neither does.

### `Menu`, `ShellFlyout`, `ShellChrome` — a platform choice wearing a probe's clothes

These read as probes but are not. `ShellFlyout.Activate` explains itself:

> The flyout's opener sits in the window's title-bar strip on Windows, where a synthetic pointer
> click is intercepted before it reaches the button and the flyout simply never opens. Asking the
> element to invoke itself needs no coordinates.

That is "Windows needs Invoke; mobile taps" — a fixed platform fact, expressed as a runtime
search. `element.Invoke()` says it directly and does the same thing on both platforms.
`ShellChrome` makes the point twice over: it already has an explicit `switch (platform)` and then
uses the ladder *inside* the Windows branch, having already decided the platform.

### `CollectionObjectBase.TryActivate` — a search, not a strategy

This one genuinely catches, and keeps catching. Its own remarks say why:

> Unlike a control's click, this walks a list of candidates and a given one may simply be the
> wrong element, so an unsuccessful pattern is an answer rather than a fault.

That is a different question. The `try` is there because the **element** may be wrong, not
because the **route** may be wrong — so it survives the change unaltered, wrapping
`element.Invoke()` instead of the ladder. Worth keeping the distinction visible in the comment,
because it is the one place where catching is not the anti-pattern this design removes.

## What gets deleted

- `srcnew/Brinell.Maui/Containers/ActivationHelper.cs` — the whole file.
- `ClickableControlBase.TryActivateByPattern` and its three overrides.
- The `ActivationRoute` enum and driver-side resolver from the earlier proposal, which were never
  built. Recording that here so nobody implements them from that document.

## What gets added

- Three methods on `IMauiElement`, defaulted so a driver that has not implemented them yet still
  compiles and answers honestly:

  ```csharp
  void Invoke() => throw new NotSupportedException(
      $"{GetType().Name} does not implement Invoke. ...");
  ```

  Defaulting to a throw rather than to `Click()` is deliberate — a default that quietly worked
  would be the ladder again, hidden one level down.

- One implementation each in `FlaUIMauiElement` and `AppiumMauiElement`.

## Risks, stated plainly

**A control whose pattern is missing now fails instead of clicking.** This is the intended
behaviour and also the main risk: the first run after the change may surface controls whose
declared operation is wrong. That is information, but it is information arriving all at once.
Mitigate by landing the element methods and the control overrides in one commit and running the
full suite against the documented baseline before anything else.

**MAUI or WinUI changing a control's exposed pattern becomes a failure rather than a silent
re-route.** Accepted, for the reason given above.

**Three platforms, one of which we cannot run here.** The Appium implementations are three
one-line methods and cannot be verified on this machine. They are simple enough to review, and
the mobile head's build will catch signature problems, but the first real Android run is the
proof.

**Not everything the ladder touched is a control.** `ShellChrome` operates on chrome the app did
not draw. Those keep working because `Invoke()` means the same thing there, but they are the
places to check first if something regresses.

## How we would know it worked

The bar is the documented baseline, unchanged: **24 failures — 11 Stepper, 13 Shell.** Anything
else the change causes is a control whose declared operation is wrong, and the failure will name
it.

Beyond that, one new property becomes testable that is not testable today: with the ladder gone,
`PhysicalInput` in `Audited` mode should record a real click only from controls that ask for one
by name. Any other pointer use is a defect. Today that assertion cannot be written, because a
click is what the ladder does when it runs out of ideas.

## Sequencing

Not during stage B. The bridge work has already changed the contract, the provider, the fixture
and the scope model; adding a framework-wide change to how every control activates would mean one
green suite standing for five separate claims.

**After step 19** (gesture control objects), which is the last step that adds control objects —
doing it earlier means writing overrides for controls that do not exist yet.
