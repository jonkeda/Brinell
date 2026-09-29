# Design: the narrator

Status: **design**, 2026-09-29. Built on [plan.md](plan.md) and its decisions, the capture design
in [screen-capture.md](screen-capture.md) and the script format of
[todo-video-script.md](todo-video-script.md).

## Decisions this design takes

| From | Decision |
| --- | --- |
| Q1 | As [screen-capture.md](screen-capture.md): one recorder API (`Brinell.Recording`) with a recorder per platform: **Windows.Graphics.Capture** for a Windows window, Playwright's screencast for a browser page, `adb screenrecord` for Android, `simctl` for the iOS simulator, an ffmpeg region as the last resort. Frames are encoded by **ffmpeg over a pipe** first, behind the recorder, so a Media Foundation encoder can replace it if the pipe costs too much. ffmpeg also lays in the voice and joins the scenes. |
| Q2 | Not answered: the default stands, Windows' own voices, a cloud voice later as a setting. |
| Q3 | The narration lives in a **video script** (`*.video.md`), a file of scenes like todo-video-script.md, not in the feature files. A feature file stays a test; one scenario can be in several videos. |
| Q4 | The **app only**. Recording is **one engine, used by both** the extension and Presenter (below). |
| Q5 | Captions as a separate **`.srt`** beside the video; nothing burned in. |
| Q6 | A **Recordings folder** setting; the run's `TestResults` folder when it is not set. |

## Presenter or extension? Both, one engine

The recording is done by the **runner**, not by a host: the host only says "record this". So each
host gets what it is good at, for free:

| Host | Use | The app during the recording |
| --- | --- | --- |
| **Visual Studio, VS Code** | **Record** on a video script or a scenario: runs the gate with `BRINELL_RECORD`, you keep working. | Behind your windows (a test run's Background): Windows.Graphics.Capture records a covered window. |
| **Presenter** | **Record** in the run toolbar: you watch (and hear) it being made, and can stop it. | Beside the bar (the App window setting). |
| **An AI agent, CI** | MCP `record_video` / `BRINELL_RECORD` on `dotnet test`. | Behind, as for the IDEs. |

Build order: the engine and the IDEs' Record first (the original ask: "from Visual Studio"),
Presenter's button second (a few lines once the engine exists).

## The video script (`*.video.md`)

Beside the feature files, found by the suite like them. Scenes run in order; each is one scenario.

```markdown
# Video: How the Todo app works

| Field | Value |
| --- | --- |
| Voice | Microsoft Jenny |
| Beat | 600 |

## Scene: Opening the app

Scenario: Features/TOD.01-see-my-todos.feature.md#tod-01-1-opening-the-app-shows-the-list

This is Todo: a small app to keep track of what you have to do.

| Step | Say |
| --- | --- |
| Given I am on the Todo List page | The app opens on your list. |
| When I tap Sync | You tap Sync to fetch it. |
```

- **Scene**: a `## Scene:` heading; its `Scenario:` is the scenario's address (R1 of AD-022), so the
  preview and the gate find it the same way. The paragraph under it is the **intro**, spoken before
  the first step; a paragraph after the table is the **outro**.
- **Say**: a row per step to narrate, matched on the step as written (keyword and text; a `## Step:`
  call by its call line). A step with no row is run **silently** with the beat only; no template
  voice (a script says what it means to say).
- **Checked like a feature file** (AD-022's one checker): an unknown scenario address, or a `Say`
  row that matches no step, is a diagnostic in the editor, the preview and the gate.
- `Voice` and `Beat` (ms after a step, for its result to be seen) are optional; the settings give
  the defaults.

## How a recording runs

```text
video script ─► plan: scenes, and per step its Say line
             ─► speech: each line to a WAV (Windows speech), its length known before the run
for each scene:
    start the scenario's session (its fixture opens the app)
    start the window recorder on the app's window        ── clock: Stopwatch ticks
    intro clip, then per step: [Say clip] ─ step ─ beat   ── the pacer waits; nothing is played
    stop the recorder: scene-N.mp4, and the scene's timeline
compose: scene videos joined, each Say WAV laid at its clock time, chapters per scene
     ─► <name>.mp4, <name>.srt, <name>.timeline.json in the Recordings folder
```

- **Pacing without playing.** The run only has to *take as long as* the speech; the audio is laid
  in afterwards at the recorded times. So a recording from the IDE is silent on your machine. In
  Presenter a **Speak aloud** setting also plays each clip as it is waited for.
- **The pacer is an engine seam**, like the break hook: `BddExecutionContext.Pacer`
  (`IBddStepPacer`: `BeforeAsync(invocation)`, `AfterAsync(result)`), null unless recording. The
  gate run and Presenter both call it, so both record the same way.
- **The recorder** comes from screen-capture.md's registry: the scenario's test context names its
  target (`IRecordable.RecordingTarget`: the MAUI, WPF, WinForms or Stride window, a browser page, an
  Android device, an iOS simulator), and the first recorder that `CanRecord` it records. On Windows
  that is `WindowsCaptureRecorder` for a window, `PlaywrightRecorder` for a page. One recording per
  scene, because each scene's fixture opens its own app.
- **A failed step** is recorded as it happened: the remaining steps are not run, their lines are
  not spoken, the `.srt` shows the failure message for a few seconds, and the timeline marks the
  scene failed. The video is still written; it is evidence.
- **The `.srt`** has one cue per Say line (from its clip's start to its end) and one per failure;
  times from the timeline, so they match the video.
- **The timeline** (`.timeline.json`) holds each scene's start, each step's start, end, status and
  message, and each clip's start and end, on the video's clock: the review page and later edits use
  it.

## The gate's variables

| Variable | What it says |
| --- | --- |
| `BRINELL_RECORD` | a `*.video.md` (full, or relative to the suite's folder) records its scenes; `1` records every scenario, silently |
| `BRINELL_RECORD_DIR` | the Recordings folder; the run's `TestResults/<run>` folder when not set |
| `BRINELL_RECORD_OUT` | the scenes folder, for a host that composes after the run; else `<name>-<yyyyMMdd-HHmmss>.scenes` in the Recordings folder |
| `BRINELL_VOICE`, `BRINELL_BEAT` | the settings' voice and beat (ms), when the script does not say |
| `BRINELL_RECORDERS` | a folder with `Brinell.Narrator.Windows` (built as a plugin, with the Windows SDK's projections): a suite that targets `net10.0-windows` cannot reference it, so the host points here |
| `BRINELL_FFMPEG` | ffmpeg's executable, when it is not on `PATH` |

Each scene gets `scene-NN/` in the scenes folder: `video.mp4`, `timeline.json`, and its lines'
WAVs. With no voice (not Windows), the lines' lengths are estimated from their words, so the pace
and the captions still work.

## Settings and output

| Setting | Where | Default |
| --- | --- | --- |
| **Recordings folder** | Presenter's Settings; the VS and VS Code extension settings (`brinellBdd.recordingsFolder`); `BRINELL_RECORD_DIR` for a gate run | the run's `TestResults/<run>/` folder |
| Voice | the script's `Voice`, else Presenter's Settings or the extension setting | the system's default voice |
| Beat | the script's `Beat`, else the setting | 600 ms |
| Speak aloud | Presenter's Settings | off |
| ffmpeg | on `PATH`, or `BRINELL_FFMPEG` | not found: Record says so, and how to get it, before anything runs |

Output, in the Recordings folder: `<script name>-<yyyyMMdd-HHmmss>.mp4`, `.srt`, `.timeline.json`,
and the scene files under `<same name>.scenes/` until the compose has finished. A recording in
`TestResults` is listed with the run's results (the preview's **Watch**), and deleting the run's
results deletes it; one in another folder is never deleted by Brinell.

## Projects

| Project | Holds |
| --- | --- |
| `Brinell.Bdd` | only the seam: `IBddStepPacer` on the execution context |
| `Brinell.Recording` (net10.0) | the recorder API, the clock, the registry, `FfmpegRegionRecorder` (screen-capture.md) |
| `Brinell.Recording.Windows` (net10.0-windows10.0.19041) | `WindowsCaptureRecorder` |
| the platform packages | `PlaywrightRecorder` (`Brinell.Html.Playwright`), `AndroidRecorder` (`Brinell.Maui.Appium`), `IosSimulatorRecorder`; `IRecordable` on each test context |
| `Brinell.Narrator` (net10.0) | the video script parser and checker, the plan, the pacer, the timeline, the `.srt` writer, the ffmpeg composer |
| `Brinell.Narrator.Windows` | the Windows speech synthesizer (`Windows.Media.SpeechSynthesis` → WAV) |
| hosts | the language server (`brinell/record`, MCP `record_video`), both extensions' Record commands, Presenter's button and settings |

## Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | **Done.** Recording, as screen-capture.md steps 1-3: the API and the ffmpeg region recorder, `WindowsCaptureRecorder` over an ffmpeg pipe, `IRecordable` on the contexts. | A covered window records (`WindowsCaptureRecorderTests`). |
| 2 | **Done.** The video script: parser, checker (`VID0xx`), preview, the IDEs; `Features/how-todo-works.video.md` from the Todo script. | The script shows in the preview with no diagnostics. |
| 3 | **Done, not run end to end** (no ffmpeg here). Speech to WAV (`WindowsSpeech`) and the pacer seam (`IBddStepPacer`); a gate run with `BRINELL_RECORD` makes scene videos and timelines (`BddRecordingSession`, `BddSceneRecording`). | Scene 1 of the Todo script records from `dotnet test`. |
| 4 | The composer: joined scenes, laid-in voice, chapters, `.srt`; the Recordings folder. | The whole Todo video plays with voice and captions in step. |
| 5 | Record in Visual Studio and VS Code; **Watch** on a result. | Record on `todo.video.md` in Visual Studio gives the video. |
| 6 | Presenter: Record, Speak aloud, the settings. | A recording made from Presenter matches the IDE's. |
| 7 | MCP `record_video` and the review page (plan step 5). | An agent's review page shows the video. |
| 8 | The other recorders, as screen-capture.md steps 4-5: Playwright's screencast, Android. | A Blazor scene records headless; a scene on the emulator records past 3 minutes. |
| 9 | Guide, an AD (the pacer seam, output, no input), the manual test plan. | |

## Risks

- **Clock drift** between the capture's frame times and the Stopwatch: both are QPC on Windows, so
  they agree; other platforms convert at the first frame.
- **The first frames** before the app has painted: the recorder starts after the fixture reports
  the app ready, and the intro plays over the first frame.
- **ffmpeg is a download** for the user, and every recording needs it (the encoder pipe, the voice,
  the join), so Record checks for it first. The Media Foundation encoder (screen-capture.md step 6)
  is only for when the pipe costs too much; it would not remove ffmpeg, which still composes.
