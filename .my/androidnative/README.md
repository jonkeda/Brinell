# Bruinell.NativeAndroid to the MAUI and Html standard

Start here.

| File | What it is | Authoritative for |
| --- | --- | --- |
| [plan.md](plan.md) | Steps, order, status, verification, baselines | **what to do next, and what is done** |
| [leftovers-plan.md](leftovers-plan.md) | What is open after the main work: commit, readiness cost, controls not built, reports, checks for a person, closing the topic | **what is left, and who does it** |
| [design.md](design.md) | Where the stack stands, the target shape (interfaces, calls, readiness, controls, collections), how Appium and Android differ from MAUI and Html, open decisions **?N1**-**?N9** | **what to build** |

## Origin

MAUI was made right first on its own interfaces, and Html followed on 2026-09-26 with its own set
(AD-010, AD-013), in the slice planned in
[`.my/html/` at 7162ac7](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/html/plan.md).
This topic is the same slice for `Bruinell.NativeAndroid`: the Appium/UiAutomator2 stack for
Android apps that are not MAUI apps.

`Bruinell.NativeAndroid` is still on the model both other stacks left behind: Core's generic
`IElement<T>`, `IPageObject<T>` and `ControlObjectBase<T>`, hand-written public members, its own
`Thread.Sleep` poll loops with catch-alls, `PageLoadException`, no readiness chain, no
collections with item keys, actions that are never confirmed, and no app to test against.
[design.md](design.md) section 1 lists the gaps.

## Relation to `.my/core/`

[`.my/core/`](../core/README.md) plans one `ICore*` shape for every stack; its step 6 moves
NativeAndroid. That plan is a draft with L2 next, and it assumes each stack already stands alone
the way MAUI and Html do. This topic gets NativeAndroid to that point on its own
`INativeAndroid*` interfaces, as Html did, so core step 6 becomes as mechanical as MAUI's step 4.
The two do not conflict: nothing here changes `Bruinell.Core` beyond giving the new stack access
to the calls layer that already moved down (decision **?N7**).

## Scope

- **In:** `srcnew/Bruinell.NativeAndroid`, its unit tests, a native Android sample app, a new
  `Bruinell.NativeAndroid.UITests` project with its UAT suite, the shared Appium code it needs
  (**?N1**), the UAT target and head wiring, screen recording, docs and skills.
- **Out:** iOS native; a Scraper source for Android; the Presenter; any change to what a call
  *does* on MAUI or Html. Jetpack Compose is a later, optional step (plan step C1).
- **Free to break:** nothing outside the project references `Bruinell.NativeAndroid`
  (checked 2026-10-07), so the public surface changes without shims.

## Status

Plan drafted 2026-10-07; implementation started the same day, with the decisions in design
section 10 taken as proposed. Step status is in [plan.md](plan.md).
