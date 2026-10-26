# The device suite's skips on MauiUnit: a plan

The device suite skips 35 tests on MauiUnit (2026-10-26, 325 pass). Thirteen can run in memory after
two pieces of work, one splits into a half that runs and a half that skips, and twenty-one keep
skipping. Done, the suite would be 339 pass and 22 skip, and the device heads stay the judge at a gate
(AD-032).

## What skips, and why

| Group | Tests | Can MauiUnit run them? |
| --- | --- | --- |
| MediaElement (`CommunityToolkit/MediaElementTests`) | 12 | Yes, against a fake player: steps 2a to 2d |
| Layout and scrolling (`ScrollVerbTests` 3, `ScrollTests.BottomLabel_IsVisible_IsFalseUntilScrolledTo`) | 4 | No: layout is the device runners' (FlaUI, Appium); step 3 is not planned |
| Carousel off-screen cards (`CarouselView_Items_AreTheSeededCardsInOrder`) | 1 | Yes, with a test change: step 1 |
| Long picker (`LongPicker_ReportsEveryItemAndItsOwnIndex`) | 1 | Half of it: step 1 |
| Windows' accessibility tree leaves things out (BoxView, SwipeView, menu items) | 3 | No: they prove a Windows fact |
| The Windows driver and bridge themselves (Diagnostics 5, focus 2, bridge refusals 3, input in physical pixels 1, occluded screenshot 1) | 12 | No: they test FlaUI and the UI Automation bridge |
| Opt-in measurements (`BRUINET_PROBE`, `BRUINET_STRESS`) | 2 | Not device skips; they stay opt-in on every head |

## Step 1: two test changes, no head change

- **Carousel.** The test reads card 4's text while card 0 is shown; it passes on Windows only because
  Windows reports every card as on screen, and would fail on a phone too. Bring each card on screen first
  (`SwipeNext` on the control, or whatever member moves the carousel), then read it: the test then holds
  on Windows, Android and MauiUnit, and `[DeviceFact]` goes.
- **Long picker.** Split it: the item count, the selected index and its text (true on every head) become
  a `[Fact]`; the check that the platform's popup realizes fewer than 200 items stays a `[DeviceFact]`,
  since it demonstrates a platform's virtualization.
- Gain: 2 passing in memory; 1 skip remains (the popup half).

## Step 2: MediaElement against a fake player

The skip reason ("no type without one") looks wrong: the toolkit's `net10.0` build has `MediaElement`
and its handler. The likelier cause is the Todo one: the page's XAML parses before the MediaElement
assembly is loaded, and the runtime parser knows only loaded assemblies.

- **2a. Load the assembly first.** `MauiFixture.CreateMauiUnitOptions` loads
  `CommunityToolkit.Maui.MediaElement` before the first page, as `TodoAppFixtureBase` does for the toolkit.
  Check: the media page opens on MauiUnit. If it does not, stop and rethink this step.
- **2b. A handler seam in MauiUnit.** `MauiTestContextOptions` lets a test give a view type a handler
  (for example `Handlers.Add<TView, THandler>()`), set when the view is realized in the tree. MauiUnit stays
  free of the toolkit; the seam is general.
- **2c. The head moves the virtual clock.** Today the head only drains the queue, so app timers and
  delayed work never fire under Bruinet. The head advances MauiUnit's clock by the wall-clock time that
  passed since its last call, before each drain. MauiUnit's own tests keep the pure virtual clock. This
  also lets any app timer run under the head.
- **2d. A fake MediaElement handler** in the device suite (`testsnew/Bruinet.Maui.UITests/MauiUnit/`): play,
  pause, stop and seek change `CurrentState`; while playing a timer moves `Position`; `Duration` is the
  clip's length (the fixture says 6 s). It moves to a package of its own only if a second suite needs it.
- Gain: 12 in memory. They then prove the control object and the page's wiring against a fake player,
  not playback itself; the device run still proves that.

## Step 3: layout, not planned

Decided 2026-10-26 by the owner: layout in memory (sized leaf handlers, a window, bounds, scroll offsets,
"visible" from the viewport) is too much for what it buys. Layout and scrolling are for the runners with a
real screen, FlaUI and Appium; the four tests keep `[DeviceFact]`, and `ReadScrollPosition` keeps ending
the call with `DeviceOnlyException`.

## Unknowns

- Whether the media page loads after 2a; it decides step 2.
- Whether a handler on a MediaElement makes MAUI call into its mapper in ways a null platform view breaks.
- Whether a carousel move works on MauiUnit through the control as it is, or needs a head route.

## Proposed order

Step 1 first (small, and the carousel test is wrong on a phone today). Step 2 next: 2c helps every app
with a timer, not only MediaElement.

Expected next action: do step 1, then 2a to see whether the media page loads.
