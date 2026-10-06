# Narrated Recordings

Record a scenario, or a whole video script of scenarios, on video with a voice-over that says
what is happening. The scenarios run as the gate runs them (AD-024); the app is recorded while
they run, and each spoken line is waited for before its step, so the voice and the app stay in
step. Record from Visual Studio, VS Code, Presenter, an AI agent (MCP) or `dotnet test`.

## You Need

- **ffmpeg**, on `PATH` or named by `BRINELL_FFMPEG`: it encodes the video, lays in the voice and
  joins the scenes. Windows: `winget install Gyan.FFmpeg.Essentials`, then a new terminal. Record
  checks for it first and says so when it is missing.
- **A voice**: a Windows voice (Settings, Time and language, Speech), or a cloud voice
  ([Cloud Voice](#cloud-voice)). Without one, lines are timed from their words and shown as
  captions only.
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
| `Voice`, `Beat` | the voice by (part of) its name (`azure:coral` for a cloud voice), and the wait after each step in ms; the settings when left out |
| `Instructions` | how a cloud voice says the lines (its tone); the settings when left out |
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
| `BRINELL_VOICE_INSTRUCTIONS` | a cloud voice's tone, when the script does not say |
| `BRINELL_VOICE_KEY`, `BRINELL_VOICE_ENDPOINT` | a cloud voice's key and endpoint, over `voice.secrets.json` ([Cloud Voice](#cloud-voice)) |
| `BRINELL_RECORDERS` | a folder with `Brinell.Narrator.Windows` and the window recorder: a `net10.0-windows` suite cannot reference them, so the language server keeps a copy in its `recorders` folder |
| `BRINELL_FFMPEG` | ffmpeg's executable, when it is not on `PATH` |

## Cloud Voice

Azure OpenAI's text to speech (`gpt-4o-mini-tts`), or an audio chat model such as `gpt-audio`, speaks the lines when a script, `BRINELL_VOICE`
or the IDEs' Voice setting names `azure:<voice>` (`azure:coral`, `azure:alloy`, `azure:sage`, ...),
or names no voice while a cloud voice is set up. It sounds the same on every machine and works for
every platform's runs. A script can set its tone:

```markdown
| Field | Value |
| --- | --- |
| Voice | azure:coral |
| Instructions | calm and friendly, a product demo |
```

Its endpoint and key go in `voice.secrets.json`, which git ignores. The Presenter writes it for you
(Settings, Narrator, Cloud voice: [the Presenter](../platform-guides/presenter.md#record)), with
Try to hear it first; or write it by hand, as `voice.secrets.example.json` at the repository root
shows:

```json
{
  "endpoint": "https://<resource>.openai.azure.com",
  "deployment": "gpt-4o-mini-tts",
  "apiVersion": "2025-03-01-preview",
  "key": "<the resource's key>",
  "voice": "coral"
}
```

The endpoint is the resource; a URL copied from Foundry with its path (`/openai/...`) works too, a
Realtime URL (`wss://`) does not.

**An audio chat model.** A deployment whose name says `audio` (`gpt-audio`, `gpt-audio-1.5`,
`gpt-4o-audio-preview`), or an endpoint copied from a chat completions URL, is asked through chat
completions (`/openai/v1/chat/completions`) to read each line as written, with the script's
`Instructions` as its tone. A chat model may reword or answer a line, so what it says it said is
checked: nearly all the line's words, in order (numbers may come back spelled out). A misread line
is asked again, twice; then the recording fails, quoting what it said. For a script that must be
read as written, text to speech is the surer voice. Its voices are those of the Presenter's list
(`marin` and `cedar` are its newest).

The first of these that has an endpoint and a key is used:

1. `BRINELL_VOICE_KEY`, `BRINELL_VOICE_ENDPOINT` (and `BRINELL_VOICE_DEPLOYMENT`,
   `BRINELL_VOICE_API_VERSION`): for a build agent, from its secret store; they override the file.
2. `voice.secrets.json` beside the suite's `uat.config.md`, or in a folder above it up to the
   repository's root.
3. `%USERPROFILE%\.brinell\voice.secrets.json`: one key for every repository, outside them all. The
   place to start with. (A repository other than Brinell ignores the file only if its own
   `.gitignore` says so: `**/voice.secrets.json`.)

The key goes only into the request's `api-key` header; it is never in a log, a timeline or a
message. A line is asked for once: clips are kept in `%LOCALAPPDATA%\Brinell\speech-cache` by
their voice, instructions and text, so recording a scene again costs nothing. A recording that
names a cloud voice fails when none is set up, or when the service refuses (the message says
why); it never falls back to another voice. `BRINELL_VOICE_INSTRUCTIONS` gives the tone when the
script does not.

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
