# The device suite's skips on MauiUnit: a plan

The device suite skipped 35 tests on MauiUnit (2026-10-26, 325 pass). Steps 1 and 2a are done: it now
runs 328 and skips 33 (361 tests, one split in two). What still skips needs a real screen or the Windows
driver itself, and stays with FlaUI and Appium; the device heads stay the judge at a gate (AD-032).

## What skips, and why

| Group | Tests | Can MauiUnit run them? |
| --- | --- | --- |
| MediaElement (`CommunityToolkit/MediaElementTests`) | 12 | One (it resolves and is shown); the other 11 drive the platform's transport controls: no |
| Layout and scrolling (`ScrollVerbTests` 3, `ScrollTests.BottomLabel_IsVisible_IsFalseUntilScrolledTo`) | 4 | No: layout is the device runners' (FlaUI, Appium); step 3 is not planned |
| Carousel off-screen cards (`CarouselView_Items_AreTheSeededCardsInOrder`) | 1 | Yes, done in step 1 |
| Long picker (`LongPicker_ReportsEveryItemAndItsOwnIndex`) | 1 | Half of it, done in step 1 |
| Windows' accessibility tree leaves things out (BoxView, SwipeView, menu items) | 3 | No: they prove a Windows fact |
| The Windows driver and bridge themselves (Diagnostics 5, focus 2, bridge refusals 3, input in physical pixels 1, occluded screenshot 1) | 12 | No: they test FlaUI and the UI Automation bridge |
| Opt-in measurements (`BRUINET_PROBE`, `BRUINET_STRESS`) | 2 | Not device skips; they stay opt-in on every head |

## Step 1: two test changes, done

- **Carousel.** The test read card 4 while card 0 was shown, which only Windows reports as on screen.
  It now swipes to card 4 first and is a `[Fact]`: it passes on MauiUnit and on Windows.
- **Long picker.** Split: `LongPicker_ReportsEveryItemAndItsOwnIndex` (count, index, text) is a `[Fact]`;
  `LongPicker_Dropdown_RealizesOnlySomeItems` keeps the popup's realization as a `[DeviceFact]`. Both pass
  on Windows.

## Step 2: MediaElement, stopped after 2a

- **2a, done.** `MauiFixture.CreateMauiUnitOptions` loads `CommunityToolkit.Maui.MediaElement` before the
  first page, and the media page opens on MauiUnit: the old reason ("no type without one") was wrong.
  `MediaElement_ResolvesAndIsVisible` now runs everywhere.
- **Why it stops there.** The other 11 drive the player's transport controls by WinUI's part ids
  (`PlayPauseButton`, `ProgressSlider`, `TimeElapsedElement`, `TimeRemainingElement`), which the platform
  draws. Running them in memory would take a fake player and fake WinUI transport controls in MauiUnit's
  tree: the platform fake AD-032 rules out. They keep `[DeviceFact]` with that reason.
- **2b to 2d, dropped.** The handler seam and the fake player have no test left to serve.
- **2c, kept as a note.** The head never moves MauiUnit's virtual clock, so app timers and delayed work
  do not fire under Bruinet. No test needs it now; when one does, the head advances the clock by the
  wall-clock time since its last call.

## Step 3: layout, not planned

Decided 2026-10-26 by the owner: layout in memory (sized leaf handlers, a window, bounds, scroll offsets,
"visible" from the viewport) is too much for what it buys. Layout and scrolling are for the runners with a
real screen, FlaUI and Appium; the four tests keep `[DeviceFact]`, and `ReadScrollPosition` keeps ending
the call with `DeviceOnlyException`.

## What is left

Nothing planned. The 33 skips: 11 MediaElement transport tests, 4 layout and scrolling, 3 Windows
accessibility-tree facts, 12 Windows driver and bridge tests, the long picker's popup half, and the 2
opt-in measurements.

`TextVerbTests.Paste_LeavesTheClipboardAlone` failed once in a full MauiUnit run and passed three times
alone: it uses the real Windows clipboard, which any program on the desktop can hold.
