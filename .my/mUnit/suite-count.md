# The MAUI device suite on the MUnit head: the count

The first count, 2026-10-25. `testsnew/Brinell.Maui.UITests.Munit` links the device suite
(`Brinell.Maui.UITests`) unchanged: its `MauiFixture`, 64 test files, containers, page objects
and UAT scenarios. It runs them against the sample app compiled for plain `net10.0`
(`samples/Brinell.Samples.Maui.App.Headless`, also linked unchanged). Only one test file is left
out: `FlaUIErrorsTests` unit-tests the Windows driver's internals, which only the original
assembly can see.

## Outcome

220 of the 354 tests pass, 2 are skipped, and 132 fail. Leaving out the 46 that cannot or need not
run here, 220 of 308 pass (71%). Every other failure is a gap in the head or in MUnit, with a known
fix; none is a wrong pass.

| Group | Tests | What it means |
| --- | --- | --- |
| Passed | 220 | Buttons, text, toggles, pickers, dates, sliders, labels, collections, containers, navigation, popups, and the 4 UAT feature files |
| Not run yet | 17 | The Shell app (`Brinell.Samples.Maui.ShellApp`) is not compiled headless yet, so `ShellFixture` and the two-app parallelism checks cannot start |
| Device-only | 17 | The test is about the Windows driver itself (FlaUI casts, the UI Automation bridge's messages), pixels (screenshots, physical input logs), or what Windows' tree leaves out |
| MediaElement | 12 | The toolkit's `MediaElement` has no type on plain `net10.0`, so its XAML does not load; media playback is a device matter |
| Head gaps | 65 | Fixable in `Brinell.Maui.Munit`; listed below |
| MUnit gaps | 14 | Alerts and prompts (8, Phase 4) and focus (6, Phase 6) |
| To look at | 7 | Menu bar and context menu items, a picker's dropdown, an empty-view label |
| Skipped | 2 | The suite's own probe and stress tests, skipped there too |

## Speed

The 220 passing tests take 4.4 s in all, 20 ms each on average. The slowest takes 0.5 s, and those
slow ones are tests whose own design is a wait.

The run took 3 min 11 s. 3 min 7 s of that is the 132 failing tests: Brinell polls each failed
condition until the test's own timeout, which is correct for a device and wasted here.

The first run was much slower: every test spent 10 s in `MauiFixture.Open`. The cause was a head
bug. `CurrentRoute()` named the top page by its type name instead of its `AutomationId`, so
`PageNavigation.WaitArrived` never saw the page arrive and always waited out its timeout. It is
fixed, and `ButtonTests` went from 60 s to 0.1 s.

## The head gaps, largest first

- **State the bridge reads, and toolkit and carousel parts (18).** `IMauiElement.ReadState` returns null, so Expander, RatingView, AvatarView, DrawingView, CarouselView and IndicatorView have nothing to answer with. Proposal: answer `ReadState(name)` from the element's bindable property of that name, as `GetAttribute` already does, and refuse unknown names.
- **Semantic gestures (15).** `PerformGesture` is not mapped. Proposal:
  - tap, double tap and long press go through the element's recognizers;
  - a swipe on a `SwipeView` opens it, a swipe down on a `RefreshView` refreshes it, and a swipe on a `CarouselView` moves its position;
  - pan and draw go through `PanGestureRecognizer` and the `DrawingView`'s lines.
- **Refusal types (14).** The suite expects Brinell's own exception types when something is refused, such as going back at the hub, an unknown toolbar item or a date past the maximum. The head throws MUnit's `ElementNotInteractableException`. Proposal: catch those in the head and throw the types the device drivers throw.
- **Stepper's + and − (10).** Brinell's `Stepper` control presses the two buttons a device draws inside the stepper. Proposal: publish those two parts as children of a `Stepper`, through an MUnit adapter, so the control's own code still runs.
- **Scroll routes (8).** `ScrollTo`, `ScrollToIndex`, `ScrollContent` and `ReadScrollPosition` are not implemented. Proposal: the first three succeed at once, since every row is realized; `ReadScrollPosition` is device-only, as it needs layout.

## Next

1. Close the head gaps in the order above; they are worth about 65 tests.
2. Compile the Shell app headless, which adds 17.
3. Give the fixture base a proper head choice (`BRINELL_HEAD=munit`), replacing the static `InMemoryHead`.
4. Run the UAT scenarios on both heads, `@device` where needed, as the bUnit head's step 4 did.

The full list of failures by group, with each one's first message line, is kept in
[suite-count-failures.md](suite-count-failures.md).
