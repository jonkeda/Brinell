# Plan: the app window under Presenter

Status: **built** 2026-09-29, but for WPF and WinForms from Presenter (step 4, waiting on the app path task). Q1-Q4 decided. Follows [presenter-update.md](presenter-update.md) (the docked bar).

## Problem

A test run keeps the app under test out of the way; Presenter needs the opposite.

- **MAUI** ([QuietWindow.cs](../../srcnew/Brinell.Maui.FlaUI/Windowing/QuietWindow.cs)): the
  driver marks the app's window "no activate", sends it to the bottom of the z-order, hands the
  foreground back, and a watchdog pushes it back whenever it comes forward, for the whole session.
  Presenter's placement ([PresenterWindowService.cs](../../srcnew/Brinell.Presenter/Services/PresenterWindowService.cs))
  moves it beside the bar, but it stays behind the IDE or whatever else is open.
- **WPF, WinForms**: the drivers do the reverse (`SetForeground`), so the app lands on top, and
  takes the keys Presenter needs (Space, F5). Neither starts from Presenter today (the app path task).
- **Blazor, HTML**: Playwright starts the browser headless or headed from `HtmlTestContextOptions.Headless`;
  a headless run has no window to show.

## Goal

One setting, **App window**, on Presenter's Settings page, that every target honours:

| Choice | The app's window | Default for |
| --- | --- | --- |
| **Beside the bar** | shown, in front of other windows, sized into the rest of the bar's monitor | Presenter |
| **Maximized, other monitor** | shown maximized on a monitor without the bar (a projector); falls back to Beside the bar | |
| **Maximized** | shown maximized on the bar's monitor, the bar kept on top | |
| **Where it opens** | shown, not moved | |
| **Background** | behind everything, never activated (today's MAUI) | test runs |

Whatever the choice, the keys stay with Presenter: the app is shown without taking the focus, and
Presenter takes the foreground back after a launch.

A second setting, **When a run ends**:

| Choice | The app |
| --- | --- |
| **Close the app** (default) | closed when the run passes, fails or is stopped, as today |
| **Keep it open** | left open where it is, showing where the run ended, until the next run starts (which closes it first), Presenter closes, or **Stop** is pressed with no run on |

## How

- **One contract, read by every launcher:** `BRINELL_AUT_WINDOW` = `background` | `shown`.
  `background` is today's behaviour and the default, so test runs do not change. `shown` turns
  QuietWindow off in the MAUI driver, and `SetForeground` off in the WPF and WinForms drivers
  (shown, not activated). Presenter sets `shown` for every choice but Background.
- **Placement stays in Presenter**, for every target, as built: the new top-level window is found
  and moved (`PresenterLayout` gains Maximized, Other monitor, Leave). The driver's own
  `BRINELL_AUT_PLACE` stays for test runs; Presenter stops setting it.
- **Z-order and focus**: Presenter shows the app with `HWND_TOP` and `SWP_NOACTIVATE`, then brings
  its own window to the foreground. For Maximized on the bar's monitor, the bar is topmost while a
  run is on.
- **Browsers**: under Presenter, `Headless` is off (an environment override the Playwright context
  reads), and the browser starts with `--window-position` and `--window-size` for the target area,
  so it does not flash elsewhere first. Placement still moves it if it did.
- **AD-005 holds**: placement, z-order and focus are not input. The background default protects
  the person at the machine during test runs; Presenter asks for the other case explicitly. A
  line in AD-005 says so.

## As built

- `Brinell.Core.Diagnostics.AutWindow` reads `BRINELL_AUT_WINDOW` and `BRINELL_AUT_BOUNDS`
  (`AutWindowTests`). The MAUI driver skips `QuietWindow` when shown; the Playwright context is headed,
  opens Chromium at the bounds and lets the page fill the window.
- **WPF and WinForms are not changed**: their drivers take the foreground before a click because the
  click may be real input (AD-005), so turning that off would type into Presenter. Presenter takes the
  focus back after each step instead (`ReclaimFocus`, only from the app it showed).
- Presenter: `PresenterWindowService` shows the app per `PresenterAppWindow`, fills rather than
  maximizes (a real maximize activates the window), keeps the bar topmost for Maximized while a run
  is on. It no longer sets `BRINELL_AUT_PLACE_RIGHT`; the session's own placement report is gone.
  `PresenterRunEnd.KeepOpen` keeps the ended session (fixture and app) until the next run, Stop with
  no run on, another workspace, a build, or `Shutdown` when the window closes.
- Found and fixed on the way: a run resolved assemblies through the default context's Resolving
  event, where the workspace check's shadow copy (`BddSuiteLoad`) answered first; Playwright loaded
  from it found no browser driver. `BddSessionLoadContext` now treats as the host's only what the
  framework and Presenter's folder ship.
- Checked by hand: the MAUI sample beside the bar, z-order right under the bar, Presenter keeping the
  keys; closed after a pass, kept open with Keep it open and closed by Stop; Maximized fills the
  monitor with the bar on top. A Blazor scenario runs from Presenter with its browser beside the bar.
  Not checked: Maximized, other monitor (one monitor here).
## Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | `BRINELL_AUT_WINDOW`: MAUI's QuietWindow off for `shown`; WPF and WinForms show without activating. **Done** (WPF and WinForms: focus taken back instead). | A unit test per driver option; the MAUI UI suite unchanged with the variable unset. |
| 2 | Presenter: the App window setting, the five choices in `PresenterWindowService`, focus back to Presenter after a launch. **Done.** | The MAUI sample shows beside the bar in front of the IDE; Space still steps. |
| 2b | **Done.** When a run ends: Keep it open holds the ended session's fixture (and its app) until the next run, Stop or closing Presenter, instead of disposing it at the end. | With Keep it open, the MAUI sample stays open after a pass and closes when the next run starts. |
| 3 | Browsers: headed under Presenter, started at the target area. **Done.** | A Blazor scenario runs from Presenter with its browser beside the bar. |
| 4 | WPF and WinForms from Presenter (after the app path task). | A WPF scenario shows beside the bar. |
| 5 | Guide, AD-005 line, journeys PR15 and PR16. | |

## Decisions

| # | Question | Decided 2026-09-29 |
| --- | --- | --- |
| Q1 | Should the app get the focus, so a viewer can click in it? | No: Presenter keeps the keys; a click on the app still works (it is not refused activation any more). |
| Q2 | Maximized on the bar's monitor: bar topmost always, or only while a run is on? | Only while a run is on. |
| Q3 | Other monitor: which one, when there are several? | The largest that does not hold the bar. |
| Q4 | When the run ends: leave the app open, or close it? | A setting, **When a run ends**: Close the app (default) or Keep it open. |
