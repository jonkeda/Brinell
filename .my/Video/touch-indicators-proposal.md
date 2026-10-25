# Proposal: Show Taps And Swipes In Recorded Videos

Status: implemented 2026-10-07 (commits 690fcba4 to 3c26df55 on `move`); the decision is in
AD-024 and the user guide in `docs/guides/narrated-recordings.md#touch-marks`. Where the build
differs from this proposal:

- **No decorator.** The FlaUI, Appium and Playwright elements report from their own action
  methods: `IMauiElement` has members with default bodies, which a decorator would not forward.
- **Physical pixels on Windows.** At 125% scaling UI Automation gave the test process logical
  pixels; the driver reads bounds per-monitor aware, and the recorder's area is DWM's frame.
- **Bridge-only controls** (`RefreshView`, `SwipeView`) get their bounds from the bridge's
  `GetBounds`; toolbar items mark a tap on the item.
- **Not done:** an Android or iOS recording to confirm the device mapping; Construction's
  `contacts-*` scripts were not re-recorded.

## Problem

In the module videos it is often unclear which button was pressed, or whether a swipe
happened. The screen simply changes. A mouse cursor is not the answer:

- On Windows, MAUI tests use no physical input (AD-005). Clicks go through UI Automation
  patterns or the gesture bridge (AD-008), so a real cursor never moves.
- Android (`adb screenrecord`) and iOS recordings have no cursor at all.
- A swipe on Windows is a semantic bridge verb. Nothing on screen moves until the content
  itself moves.

We need one way to show touches that looks the same on Windows, Android and iOS, and in the
browser (Html and Blazor, see [Html](#html)).

## Proposal In One Sentence

Each interaction is logged on the test side, on the recording clock, with its target's bounds.
The video composer then draws touch marks on top of the recorded frames.

```mermaid
flowchart LR
    A[ControlObject action] --> B[Interaction log<br/>kind, bounds, time]
    B --> C[scene timeline.json<br/>interactions]
    D[Recorder<br/>WGC / adb / simctl] --> E[scene video.mp4<br/>clean]
    C --> F[BddVideoComposer]
    E --> F
    F --> G[touches.ass<br/>vector marks]
    G --> H[final mp4 with marks]
```

## Why This Approach

| Option | Verdict | Reason |
| --- | --- | --- |
| Draw marks at compose time from logged interactions | **Chosen** | Works on every platform, needs no app change, and adds no input. The composer can draw a mark slightly *before* the action, so the viewer sees it before the screen changes. |
| Overlay drawn by the app (MAUI adorner or tap ripple) | Rejected | It is app-side automation behaviour, which fails the AD-008 admission tests. The app does not know which tap was a test step, and timing depends on the UI thread. |
| Show a real mouse cursor | Rejected | Windows MAUI has no physical input (AD-005), and phones have no cursor. |
| Android "Show taps" (`show_touches`) | Rejected as the main route | Android only, a dot without context, nothing for swipes made by the bridge, and nothing on iOS or Windows. It stays off, to avoid double marks. |
| Edit the videos by hand afterwards | Rejected | Recordings are reproducible gate runs (AD-024). Manual edits drift with every rerecording. |

## What Is Logged

A new record goes into each scene's `timeline.json`, next to `steps` and `clips`:

```json
"interactions": [
  {
    "kind": "Tap",
    "at": 12.48,
    "duration": 0,
    "bounds": { "x": 1012, "y": 88, "width": 44, "height": 44 },
    "from": null,
    "to": null,
    "label": "ContactList_btnAdd",
    "step": 7
  },
  {
    "kind": "Swipe",
    "at": 20.10,
    "duration": 0.4,
    "bounds": { "x": 0, "y": 140, "width": 1280, "height": 620 },
    "from": { "x": 640, "y": 220 },
    "to": { "x": 640, "y": 520 },
    "label": "ContactList_RefreshView",
    "step": 11
  }
]
```

- **`kind`:** `Tap`, `DoubleTap`, `LongPress`, `Swipe` (with a direction derived from
  `from`/`to`), `Type`, `Clear`, `Key` (Enter, Back) or `Navigate` (back navigation without a
  target).
- **`at`:** seconds on the scene's video clock (the same `Stopwatch` clock as steps; AD-024).
  Taken just **before** the action runs, while the screen still shows the old state.
- **`bounds`:** the target element's rectangle, read before the action, because the element
  can disappear afterwards. Coordinates are in **video pixels**; see below.
- **`label`:** the AutomationId or accessible name, for debugging. It is never drawn.

### Where Interactions Are Captured

Bruinell owns the hook. Construction tests change nothing.

- A reporting decorator around `IMauiElement` (`Click`, `DoubleClick`, `LongPress`, `Swipe`,
  `SendKeys`, `Clear`, `PerformGesture`) and `IMauiDriver.PerformGesture`. It reads `Rect`, then
  calls the inner element.
- The decorator is installed only while a recording is running, exactly like
  `BddExecutionContext.Pacer`. A plain `dotnet test` run has no decorator and no cost (an
  AD-024 "broken when" rule).
- The log is a small `IInteractionLog` in `Bruinell.Core.Recording`. It is platform-neutral
  (AD-001): rectangles and points only.
- `ScrollIntoView` is not logged. It is an implementation detail, and a mark for it would be
  noise.

### Mapping Coordinates To The Video

| Head | Element coordinates | Video | Mapping |
| --- | --- | --- | --- |
| Windows (FlaUI) | UIA screen pixels | Window client area through Windows.Graphics.Capture | Subtract the captured window's origin, which the recorder stores at start. DPI is physical on both sides. |
| Android (Appium) | Device pixels | `adb screenrecord`, device pixels, possibly scaled | Scale by `videoWidth / deviceWidth`. |
| iOS (Appium) | Points | Simulator recording in pixels | Scale by `videoWidth / windowWidthInPoints`. |

The recorder writes its origin and source size into the scene timeline (`origin`, `sourceWidth`,
`sourceHeight`), so the composer does the mapping. The log stays in raw element coordinates.

## How Marks Look

All marks are drawn as an **ASS subtitle track with vector drawings** and burned in with
ffmpeg's `subtitles` filter (libass). ASS gives shapes, positions, `\move` for motion, `\t`
for scale animation and `\fad` for fades, all in one text file the composer generates. That
file can be unit-tested as text.

Every mark is drawn in an **accent colour with a white inner stroke and a dark outer halo**, so it
reads on both light and dark screens. The accent is selectable (`TouchColor`, see
[Video Script Switch](#video-script-switch)); the default is Exact red, `#E8111E` (the
`ExactRed` resource of Construction). Sizes scale with the video's short side, so phone and
desktop videos look the same.

| Interaction | Mark | Timing |
| --- | --- | --- |
| Tap | A ring closes onto the touch point. Then a filled dot plus a ripple expands and fades. The target's bounds get a thin rounded outline. | Ring closes over 250 ms **before** `at`, ripple 400 ms after `at`. Outline from `at - 250 ms` to `at + 400 ms`. |
| Double tap | Two ripples, 120 ms apart. | As for Tap. |
| Long press | A dot holds while a ring grows around it, then a ripple. | From `at` for `duration`, then the ripple. |
| Swipe | A dot travels from `from` to `to`, leaving a fading trail (3-4 smaller dots). An arrow appears at the end. | Travel over 400 ms, ending at `at`, because the bridge swipe is instant. On Android/iOS, over the real gesture's duration. |
| Pull to refresh | The same as Swipe (down) over the list. | As for Swipe. |
| Type / Clear | The field's bounds get an outline. A small chip shows a keyboard icon (no text: the text appears on screen). | While typing, minimum 400 ms. |
| Key (Enter) / Back | A chip in the bottom corner: "⏎ Enter" or "← Back". | 600 ms. |

The touch point is the centre of `bounds`, except for swipes, which use `from`/`to`. The
composer also adds:

- **Pre-roll:** because marks are drawn afterwards, a tap's ring can start 250 ms before the
  action. The viewer sees *what* will be pressed while the screen still shows it. This needs no
  waiting in the test.
- **Lead (optional):** if a step follows the previous one too quickly for the pre-roll to land
  on a still frame, the recording pacer can wait an extra `Lead` (default 0, script field
  `Lead`). This happens only during recording, as the beat already does.

## Video Script Switch

A `*.video.md` header field controls marks. The default is on:

```markdown
| Touches | on |       <!-- on | off | outline-only -->
| TouchColor | #E8111E |   <!-- any #RRGGBB; Exact red when left out -->
```

`TouchColor` can also be set where the other recording defaults are (`BRUINELL_TOUCH_COLOR`, the
IDEs' and the Presenter's recording settings); the script wins. A value that is not `#RRGGBB` is a
checker error (`VID0xx`). Like `Touches`, it only affects composition: not in the scene hash.

Raw scene videos (`<script>.scenes/scene-NN/video.mp4`) **stay clean**. Marks are added only
when the scenes are composed or stitched. As a result:

- a scene does not need rerecording to change the style or to turn marks off;
- a scene's hash does not include `Touches`, which only affects composition;
- the timeline keeps the facts, and the pixels of a raw scene never hold derived drawing.

## Effect On The Bruinell Decisions

| Decision | Effect |
| --- | --- |
| AD-005 (no physical input) | Unchanged. Nothing moves a pointer. |
| AD-008 (gestures through UIA and the bridge) | Unchanged. No new app-side bridge verbs or sinks. The app is not touched. |
| AD-024 (recording is a paced gate run) | Extended: the timeline gains `interactions`, and the recorder writes its origin and source size. "Nothing is burned in" must be refined: **captions** stay a separate `.srt`, while touch marks are drawn into the *composed* video only. Raw scenes stay clean. This needs an AD-024 amendment before implementation. |
| AD-001 (core is platform-neutral) | The log lives in `Bruinell.Core.Recording` with `System.Drawing` rectangles only. |

## Html

Yes, the same design works for Html and Blazor, and most of it is shared. Only the capture hook and
the coordinate mapping are Html's own.

| Part | MAUI | Html (Playwright) |
| --- | --- | --- |
| Log, timeline, `TouchOverlay`, composer, script fields | shared | shared, unchanged |
| Capture | decorator around `IMauiElement` / `IMauiDriver` | decorator around `IHtmlElement` (`Click`, `DoubleClick`, `RightClick`, `LongPress`, `Swipe`, `SendKeys`, `Fill`, `Clear`, `SelectOption`, `Check`, `Uncheck`, `Submit`, `Hover`) |
| Bounds | `Rect` in screen/device pixels | `Rect` is Playwright's bounding box: CSS pixels of the viewport, frames included |
| Recorder | Windows.Graphics.Capture, adb, simctl | `PlaywrightRecorder` (Chromium screencast of the viewport) or, headed under the Presenter, the window recorder |

Html specifics:

- **Scroll first, then read.** Playwright scrolls the element into view as part of the click, so
  bounds read before the action can be off-screen. The decorator calls the element's own
  scroll-into-view (`ScrollIntoViewIfNeeded`) before reading `Rect`. That scroll happens anyway, so
  nothing changes for the page; it is still not logged as an interaction.
- **Mapping, screencast:** the frames are the viewport, so the origin is `(0, 0)` and the scale is
  `videoWidth / viewportWidth` (the screencast is capped at the viewport's size, and a device scale
  factor makes it larger). The recorder writes `sourceWidth`/`sourceHeight` as the viewport size.
- **Mapping, headed window:** the window recorder captures the browser's chrome too. The recorder
  reads the viewport's origin in the window once at start (`window.screenX/Y` and the
  outer/inner size difference, in CSS pixels times `devicePixelRatio`) and writes it as `origin`.
  If the page is zoomed or the window moved, marks would be off; the screencast is the route to use
  for Html videos.
- **More kinds:** `RightClick` draws as a tap with a small "right click" chip; `Hover` gets only the
  outline (no ripple: nothing was pressed); `SelectOption`, `Check`, `Uncheck` draw as a tap on the
  control; `Submit` as a `Key` chip ("⏎ Submit").
- **A mouse pointer instead of a finger?** For a desktop web page a pointer arrow may read better
  than a touch ring. Keep the touch marks for now (one style everywhere); a `TouchStyle` field
  (`touch | pointer`) can come later if the web videos ask for it.

## Implementation Steps (Bruinell)

1. **Log:** `IInteractionLog` and `BddTimedInteraction` in `Bruinell.Core.Recording`, plus an
   `interactions` field in `BddSceneTimeline`, with JSON round-trip tests.
2. **Capture:** a reporting `IMauiElement`/`IMauiDriver` decorator, and an `IHtmlElement`
   decorator for Html and Blazor, installed when the scene recording starts. Unit tests check that each action logs one interaction, with its bounds
   read before the action, and that nothing is logged without a recording.
3. **Mapping:** recorders write `origin`, `sourceWidth` and `sourceHeight`. Windows reads the
   captured window's client origin, Android the `wm size`, iOS the window size in points, the
   Playwright screencast the viewport size.
4. **Drawing:** `TouchOverlay` generates the `.ass` file from the timeline. Text-snapshot tests
   cover each kind (tap, swipe, long press, type, back) and the scaling.
5. **Compose:** `BddVideoComposer` adds `-vf subtitles=touches.ass` when `Touches` is not `off`.
   Fall back to no marks, with a warning, when ffmpeg lacks libass.
6. **Script:** the `Touches` and `TouchColor` header fields, with checker rules (`VID0xx`) for
   invalid values; `TouchColor` also in the recording settings.
7. **Docs:** the AD-024 amendment, and a short section in `docs/guides/narrated-recordings.md`.

## Verification In Construction

- Rerecord `contacts-in-a-minute` and `contacts-walkthrough` with
  `scripts/record-module-video.ps1`.
- Check in the video:
  - every tap on the list, the sheet buttons and the New button shows a ring and a ripple on
    the right control;
  - pull to refresh shows a downward swipe over the list;
  - the marks are visible on light pages and dark headers;
  - the marks appear before the screen changes.
- Compare the `interactions` in `timeline.json` with the steps: one mark per action, and no
  marks for `ScrollIntoView`.
- Later: one Android recording of the same script, to confirm the device-pixel mapping.
- Html: record a Todo web head script with the Playwright recorder; check that a click on a
  button below the fold gets its mark where the button is after the scroll.

## Decisions

Answered 2026-10-07:

1. **Burned in or a second file?** Burned into the main mp4. Raw scene videos stay clean (marks
   are added at compose and stitch time), so the clean picture can always be composed again with
   `Touches | off`.
2. **Typing:** as proposed: the field's outline and a keyboard chip, no typed text.
3. **Lead:** `Lead` stays at 0 by default; the pre-roll does the work.
4. **Colour:** a selectable accent colour (`TouchColor`), Exact red `#E8111E` by default, with the
   white stroke and dark halo for contrast.
