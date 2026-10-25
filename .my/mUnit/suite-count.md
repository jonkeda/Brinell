# The MAUI device suite on the MauiUnit head: the count

## Now: the suite on its own MauiUnit head

Since 2026-10-26 the device suite runs on either head itself.
`$env:BRUINELL_HEAD='mauiunit'; dotnet test testsnew/Bruinell.Maui.UITests` gives **325 passed, 35
skipped, 0 failed, in 7 s**. The 35 skipped are:
- the 33 tests below, now marked `[DeviceFact]` or `[WindowsOnlyFact(Device = ...)]`, each skip naming what it needs the device for;
- the suite's own 2 probe and stress skips.

`FlaUIErrorsTests` runs now too: it is a unit test, and passes on either head. Without the variable
the suite runs on Windows as before.

The count below was made before that, through a linked copy of the suite
(`Bruinell.Maui.UITests.Munit`, since retired), and is kept as the record of what fails in memory
and why.

## The count, run 7 (2026-10-25)

The linked copy took the device suite unchanged: `MauiFixture`, `ShellFixture`, 64 test files,
containers, page objects and UAT scenarios. It ran them against both sample apps compiled for plain
`net10.0` (`samples/Bruinell.Samples.Maui.App.Headless` and `.ShellApp.Headless`, also linked
unchanged), leaving out only `FlaUIErrorsTests`.

319 of the 354 tests passed, 2 were skipped, and 33 failed, in 6 s. Of those 33, 32 are tests no
in-memory head can or should pass, and one is a known fidelity difference. Of the 320 tests that
can run in memory, 319 passed.

| Group | Tests | What it means |
| --- | --- | --- |
| Passed | 319 | Every control family, containers, collections, the Shell app, navigation, menus and toolbars, dialogs, gestures through the app's sinks, toolkit controls, focus, and the 4 UAT feature files |
| MediaElement | 12 | The toolkit's `MediaElement` has no type on plain `net10.0`, so its XAML does not load; media playback is a device matter |
| The Windows tree and bridge | 7 | What Windows' tree leaves out (a BoxView, menu items, a SwipeView), the bridge's own refusal messages, and its rule against physical input |
| The FlaUI driver itself | 6 | The test casts the driver to `FlaUIMauiDriver` |
| Layout and pixels | 6 | Scroll offsets and extents, a label below the fold, screenshots, input logged in physical pixels |
| Popup virtualization | 1 | It shows that a long picker's popup realizes only some items; MauiUnit realizes them all |
| Known difference | 1 | Windows reports every carousel card as on screen; MauiUnit, like a phone, shows only the current one, so an off-screen card's text is not readable |
| Skipped | 2 | The suite's own probe and stress tests, skipped there too |

## Speed

The whole run takes 6 s: 5.8 s for the 319 passes (18 ms each) and 0.7 s for the 33 failures. A
failure on this head refuses at once (`DeviceOnlyException`, a Bruinell refusal) rather than waiting
out a timeout, which is what brought the run down from 3 min.

## How the count went

| Run | Passed | Time | What changed |
| --- | --- | --- | --- |
| 1 | 6 of 6 (Buttons only) | 60 s | Every `Open` waited 10 s: `CurrentRoute()` named the page by type, not by `AutomationId` |
| 2 | 220 | 3 min 11 s | The first full count |
| 3 | 278 | 3 min 10 s | State as the bridge answers it; refusals as Bruinell's exceptions; scroll routes; Stepper parts; the app's sinks; swipes |
| 4 | 295 | 2 min 9 s | The Shell app compiled headless; Shell's tab bar and flyout in the tree; hidden elements out of lookups |
| 5 | 308 | 2 min 8 s | A sweep no longer answered app-wide (a container's miss had become a hit outside it); focus; one carousel card on screen; the scroll refusal's wording |
| 6, 7 | 319 | 6 s | Dialogs; off-screen cards back in lookups, as on Windows |

## What this says

The device suite's page objects, controls and scenarios run unchanged in memory, at about a
hundredth of a device run's time. The 32 that cannot are about the device itself, so none of them
is lost coverage: the device heads stay the judge at a gate, and these tests stay there.

The full list of the 33, by group, with each one's first message line, is kept in
[suite-count-failures.md](suite-count-failures.md).
