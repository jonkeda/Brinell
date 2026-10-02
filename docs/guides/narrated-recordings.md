# Narrated Recordings

Record a scenario, or a whole video script of scenarios, on video with a voice-over that says
what is happening. The scenarios run as the gate runs them (AD-024); the app is recorded while
they run, and each spoken line is waited for before its step, so the voice and the app stay in
step. Record from Visual Studio, VS Code, Presenter, an AI agent (MCP) or `dotnet test`.

## You Need

- **ffmpeg**, on `PATH` or named by `BRINELL_FFMPEG`: it encodes the video, lays in the voice and
  joins the scenes. Windows: `winget install Gyan.FFmpeg.Essentials`, then a new terminal. Record
  checks for it first and says so when it is missing.
- **A voice** (Windows): the voices in Settings, Time and language, Speech. Without one, lines are
  timed from their words and shown as captions only.
- A suite whose test class uses `[UatScenarioTheory]`, so scenarios run by their address.

## A Video Script

`*.video.md` beside the feature files (copied to the output with them:
`<None Update="Features\**\*.video.md" CopyToOutputDirectory="PreserveNewest" />`):

```markdown
# Video: How the Todo app works

| Field | Value |
| --- | --- |
| Voice | Microsoft Zira |
| Beat | 600 |

## Scene: Opening the app

Scenario: Features/TOD.01-see-my-todos.feature.md#TOD.01.1-Opening-the-app-shows-the-list-a-loading-state-shows-first-then-the-content

This is Todo: a small app to keep track of what you have to do.

| Step | Say |
| --- | --- |
| Given I am on the Todo List page | The app opens on your list. |
| When I tap Sync | You tap Sync to fetch it. |

That was the list.
```

| Part | Means |
| --- | --- |
| `# Video:` | the video's title |
| `Voice`, `Beat` | the voice by (part of) its name, and the wait after each step in ms; the settings when left out |
| `## Scene:` | a chapter; one scenario, run and recorded |
| `Scenario:` | the scenario's address: its file relative to the suite's config, `#`, its name with every run of other characters as one `-` (the `BddScenario` trait) |
| paragraph before the table | said before the first step |
| `Step`, `Say` | said before that step; the step as the scenario writes it, keyword included (a `## Step:` call by its call line). A step with no row runs silently |
| paragraph after the table | said after the last step |

The preview shows the script as it will play: every step of each scene, with its line or
*silent*. It is checked like a feature file: an address or a `Say` row that finds nothing is an
error (`VID011`-`VID013`), a row out of step order a warning (`VID014`).

Example: [samples/Todo/.../Features/how-todo-works.video.md](../../samples/Todo/tests/Brinell.Samples.Todo.Uat/Features/how-todo-works.video.md).

## Record

| From | How |
| --- | --- |
| Visual Studio, VS Code | **Record** on a video script's title in the Scenario Preview, or beside **Run** on a scenario heading. It builds, runs, and offers **Watch** when the video is made. The preview lists a file's recordings to watch |
| Presenter, a whole script | **Narrator mode** (the video camera): open the script, **Record** the checked scenes, **Stitch**. Scene by scene: a scene recorded again replaces that scene only ([Presenter](../platform-guides/presenter.md#narrator-mode)) |
| Presenter, one run | **Record** (the dot) in Present mode's run toolbar: each run is recorded while it is on, narrated by the scene a video script has for the scenario. **Watch** opens the last one |
| An agent | MCP `record_video` (a script or a scenario), then `review_page`; the `narrated-review` skill |
| `dotnet test` | the variables below, with a filter on the scenes' addresses |

```powershell
# From the suite's project folder
$env:BRINELL_RECORD = "Features\how-todo-works.video.md"
$env:BRINELL_RECORDERS = "..\..\..\..\tools\Brinell.Cli\bin\Debug\net10.0\recorders"
dotnet test --filter "BddScenario~Features/TOD.01-see-my-todos.feature.md#TOD.01.1"
```

| Variable | Says |
| --- | --- |
| `BRINELL_RECORD` | a video script (full, or relative to the suite's folder) records its scenes; `1` records every scenario, silently |
| `BRINELL_RECORD_DIR` | the Recordings folder; `TestResults\<run>` when not set (the IDEs and Presenter: `TestResults\recordings`) |
| `BRINELL_RECORD_OUT` | the scenes folder, for a host that names the video |
| `BRINELL_RECORD_KEEP` | with a script: its scenes are kept in `<Recordings>\<script>.scenes` when the video is made, so a scene can be recorded again on its own, here or in Presenter's Narrator mode, and the video stitched again |
| `BRINELL_VOICE`, `BRINELL_BEAT` | the voice and the beat (ms), when the script does not say |
| `BRINELL_RECORDERS` | a folder with `Brinell.Narrator.Windows` and the window recorder: a `net10.0-windows` suite cannot reference them, so the language server keeps a copy in its `recorders` folder |
| `BRINELL_FFMPEG` | ffmpeg's executable, when it is not on `PATH` |

## What You Get

In the Recordings folder, for a script `how-todo-works.video.md`:

| File | Holds |
| --- | --- |
| `how-todo-works-<yyyyMMdd-HHmmss>.mp4` | the scenes joined, the voice laid in, a chapter per scene |
| `.srt` | the captions: a cue per spoken line, and one after a failed step |
| `.timeline.json` | each scene's steps (start, end, result, message) and lines, on the video's clock |

The video is made when the last scene of the script has run; a failed scene is recorded as it
happened and marked in the timeline. A scenario recorded on its own is named after it.

Kept scenes (Narrator mode, or `BRINELL_RECORD_KEEP`) stay in `<script>.scenes\scene-NN`: the
scene's `video.mp4`, its spoken lines and its `timeline.json`, which holds what the scene was
recorded from. A scene whose lines, steps, voice or beat changed since is out of date.

## Recorders

| App | Recorded with | Note |
| --- | --- | --- |
| MAUI (Windows), WPF, WinForms | Windows.Graphics.Capture | records the window while it is covered; a minimized window gives no frames; Windows draws a yellow border around it |
| A Chromium page (Playwright, Blazor) | Chromium's screencast | headless or not |
| Android | `adb shell screenrecord` | restarted every 3 minutes and joined; a join may miss a fraction of a second |
| Anything else on Windows | ffmpeg's screen region | the last resort: it records whatever covers the area |

Recording is not input (AD-005): no recorder focuses, activates or moves the app.
