# iOS: the fixes and the runs

Section 6 of [the 2026-10-21 plan](../reviews/2026-10-21-plan.md) (I1, I2), on its own, checked
against the code at 8df76015. Status: **P1-P5 written**, 2026-10-22, not committed: unit tests green on a Mac (`Bruinell.Maui.Tests` 209 of 211, the one failure the known `FindItem_ReturnsNullWhenNoMatch` timing; `Bruinell.Appium.Tests` 24 of 24); R1-R5 open. The fixes are written on Windows; the
runs need a Mac. [README.md](README.md) is the run's reference (routes, commands, tree dumps).

Each item says who: **Claude** (code, docs, unit tests on Windows) or **a person** (a Mac).

## Where the review's five stand

| Review item | Now | Left |
| --- | --- | --- |
| Idle bound on iOS | done in M1: `MauiDriverFactory.SessionDefaults` sets `waitForQuiescence` false and `settings[waitForIdleTimeout]` 1 | measured in R2 |
| `IsAtNavigationRoot` by `BackButton` | unchanged (`AppiumMauiDriver.cs:340-353`) | P1 |
| `IsFlyoutOpen` by presence | unchanged (`AppiumMauiElement.cs:1354-1364`) | P2 |
| `TurnToIndex` row by row, no budget | unchanged (`AppiumMauiElement.Wheels.cs:71-90`) | P3 |
| Defaults `iPhone 15` / `17.0` | unchanged in two places: `MauiDriverFactory.cs:249-250`, `MauiTestFixtureBase.cs:276-277` | P4 |

Found while checking: M3 made the scroll routine budgeted on Android only. `ScrollIntoViewiOS`
(`AppiumMauiElement.cs:715-740`) ignores the call's budget and has three empty catches (P5).
M7's iOS answers (`IosShellChrome`, the page indicator, the `./*` child paths) are new since the
README and are not run either (R3).

There is no fake `IWebDriver` for `AppiumMauiDriver` in `Bruinell.Appium.Tests` or
`Bruinell.Maui.Tests`. So the locators themselves are proved only on a simulator. Unit tests on
Windows cover each fix's pure part: the decision from names and values, and the budget
arithmetic.

## 1. The fixes (Windows)

| # | Item | Who | Contents | Done when |
| --- | --- | --- | --- | --- |
| P1 | **The root by the navigation bar's back button** | Claude | iOS: at the root when the `XCUIElementTypeNavigationBar` has no back button. Its back button is its first `XCUIElementTypeButton`, named after the previous page's title or `Back`. It is not the back button when its name is one of the flyout opener's (`OK`, `Menu`, `FlyoutIcon`, as `OpenFlyout` knows them). No navigation bar at all counts as the root, since `GoBack` already swipes from the edge when the bar is hidden. The decision (bar present, first button's name) goes into a static helper with unit tests; the remark "not yet run on a simulator" stays | Unit tests: no bar, a bar with no buttons, a bar whose first button is `Back` or a page title, a bar whose first button is the flyout opener; `Bruinell.Maui.Tests` green |
| P2 | **The flyout by `visible`** | Claude | iOS: open when the flyout host is found **and** its `visible` attribute is `true`. Android keeps presence. A host found with `visible="false"` is shut. One `GetAttribute` on the host, inside the `Settle.Until` poll that already exists | A unit test over the helper reading `"true"`, `"false"` and null; Android's path unchanged |
| P3 | **`TurnToIndex` under the budget** | Claude | Every step gated by `CallBudget.RemainingOr(...)`. When the budget runs out, a `ElementNotReadyException` names the row reached instead of a silent stop at `MaxWheelRows`. `TurnToIndex` serves only `Picker.SelectIndex` (dates and times are sent as text), and a picker's wheel names its row, not its index, so it still rewinds to the first row; the steps are a pure function (`TurnWheel`) with unit tests | Unit tests: steps from value to index both ways; a spent budget throws with the row reached; no loop without a deadline in `Wheels.cs` |
| P4 | **The runbook's defaults** | Claude | `iPhone 17 Pro Max` and `26.5` in `MauiDriverFactory` and `MauiTestFixtureBase`, as one pair of constants both read, so they cannot drift again | A unit test: an iOS configuration with no device name or version gets the runbook's; `docs/run/maui-ios.md` unchanged |
| P5 | **iOS scroll-into-view under the budget** | Claude | `ScrollIntoViewiOS` takes `ScrollIntoView`'s timeout as Android's does. `scrollToElement`, then `scroll toVisible`, and the blind swipe down only while budget remains. No empty catches: a refused script is `TryScript`'s false, with a comment, and the next way is tried; `ScrollIntoView` reports nothing itself, the caller's poll sees whether the element came into view | `grep` finds no empty `catch` in `ScrollIntoViewiOS`; a unit test that a 0 ms budget runs only the first strategy |

One commit per item, on the checked-out branch (the owner pushes). After all five:
`dotnet build Bruinell.sln`, `Bruinell.Maui.Tests` and `Bruinell.Appium.Tests` green, and the
Windows MAUI head's tier 2 (`Tests.Container|Tests.Collection`) to show the shared code did not
move. [README.md](README.md)'s table is updated for P1 and P2, and a row is added for P5.

## 2. The runs (a Mac)

Set up as [docs/run/maui-ios.md](../../docs/run/maui-ios.md) says: both apps built, the simulator
booted, Appium 3 with XCUITest, the four `APPIUM_*` variables set. Run with the fixes in.

| # | Run | Who | What it proves | Done when |
| --- | --- | --- | --- | --- |
| R1 | **A session starts** | a person | `ButtonTests.Button_IsExists_ReturnsTrue` alone. WebDriverAgent builds and the app installs and launches; session start time noted | One green test; the start time in the results |
| R2 | **The root and the idle bound** (P1, M1) | a person | `ButtonTests` whole. No test spends about 10 s in `ReturnToRoot`; a command on a page with an ActivityIndicator returns at once | Per-test time under 2 s outside its own calls; the call log shows no `ReturnToRoot` near its budget |
| R3 | **Class by class** | a person | The README's order: pickers, date and time (P3), alerts, Shell tabs, flyout and stack (P2), IndicatorView (M7's page indicator), then the whole mobile head | Each class's pass count; for each failure, the tree dump (`/session/<id>/source`) of its page |
| R4 | **The "to measure" items** | a person | From the review: wheels need MAUI's pickers in wheel style; the alert's cancel is the first button; the hub leaves the tree on a push; the edge swipe from x = 1 with a 0.1 s drag goes back; RatingView, slider and Expander's `./*` paths | Each answered yes or no with the tree or timing that shows it |
| R5 | **Fixes from the runs** | Claude, from R3 and R4's dumps | Each wrong locator or value fixed in the file the README names; its "not yet run on a simulator" remark removed once it passes | A second R3 by a person, green except MediaElement, CarouselView and DrawingView (they fail on Android too) |

Results go to `.my/ios/results.md`: date, Mac and Xcode version, simulator and runtime, commit,
pass counts per class, session start and per-test overhead, and the R4 answers.

## Order and closing

P4, P1, P2, P3, P5 (the defaults first, since every run uses them; then the root, which every
test pays for), then R1 to R4 on one Mac day, then R5 and a second R3. When R3 passes: what was
measured (names, values, timings) goes into
[docs/run/maui-ios.md](../../docs/run/maui-ios.md), I1 and I2 are struck from the 2026-10-21
plan, and this topic is deleted in the same commit.

Expected next action: commit P1-P5, then R1 on a Mac with a simulator and Appium.
