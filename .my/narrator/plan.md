# Plan: narrated recordings of a scenario run

Status: **plan**, 2026-09-29; Q1 and Q3-Q6 decided. The design is [design.md](design.md).

## Goal

Run a UAT scenario and get a **video of the app with a voice-over** that says what is happening,
step by step, so that:

1. from **Visual Studio** (and VS Code and Presenter), **Record** on a scenario or a feature file
   gives a video next to the run's results;
2. an **AI agent** that built a feature ends its work with a **review**: the scenarios it wrote,
   run and recorded, with the video, pass/fail per step, and what it changed.

The scenario is the script: its steps are the actions, its prose and step text are the narration.
What the Gherkin lacks is **timing** and **spoken text**; this plan adds both.

## Prior art (what to borrow)

| Tool | What it does | What we take |
| --- | --- | --- |
| **VHS** (Charm) | a `.tape` script of keystrokes and `Sleep`s renders a terminal video | the script is the source of truth; timing is in the script, not in editing |
| **Playwright** | `recordVideo` per browser context; trace viewer with a step timeline | record while the test runs; a timeline of step start/end next to the video |
| **Doc Detective** | runs the steps written in docs as tests, captures screenshots/recordings | docs and tests are one file, as our feature files already are |
| **Guidde, Clueso, Scribe** | turn a screen recording into an AI-narrated how-to video (SaaS) | narration per action, captions, chapters; we do it locally and from the scenario |
| **Remotion, ffmpeg** | compose video, audio clips and captions from data | compose after the run from a timeline, not live |

None of them run *our* scenarios, bind them to the gate, or run MAUI and desktop apps with no
physical input; the recorder and the timeline are the new part, the rest is assembly.

## How it works

```text
feature file ──► narration (per step: text, from the step or written)
                      │
                      ▼ TTS (before the run)            ┌─► video.mp4 (app window)
run the scenario ──► pace: pause before each step ──────┤
                     until its narration has played      └─► timeline.json (step, times, status)
                      │
                      ▼ compose (ffmpeg)
            review.mp4: video + voice + captions (.vtt) + a chapter per scenario
```

- **Narration text.** Each step says what its step text says, made spoken by a small template
  ("Given I am on the Todo List page" → "We start on the Todo List page"; "When I tap Sync" → "Now
  I tap Sync"). A scenario can say it better in its own words: a `Say:` line under the step in the
  Markdown, outside the gherkin block (prose, AD-023), or an AI agent writes those lines with the
  scenario. The scenario's prose paragraph is the intro.
- **Timing.** Speech is made first (text to speech per step), so each clip's length is known. The
  run then **pauses before each step until its narration has played** (Presenter's pause-before
  model; an engine hook, like `IBddBreakHook`, that waits), plus a short beat after it for the
  result to be seen. No hand-tuned delays.
- **Recording.** The app's window only, from the placement Presenter already does
  (`PresenterWindowService` knows its bounds): ffmpeg `ddagrab`/`gdigrab` of that area at 30 fps.
  Capture is not input (AD-005). A browser can use Playwright's own `recordVideo` instead.
- **Timeline.** Every step's start, end, status and message, with the narration clip's times,
  written as `timeline.json`; captions (`.vtt`) and chapters come from it.
- **Compose.** ffmpeg puts the clips at their times (`adelay` + `amix`), burns or attaches the
  captions, adds chapters, and writes `review.mp4` into the run's results folder beside the
  report, where the preview shows a **Watch** link and deleting old results removes it.
- **One engine, every host** (AD-022): `BRINELL_RECORD=1` makes a gate run record, so Visual
  Studio's and VS Code's **Record**, Presenter's **Record** toggle, CI and an agent all use the
  same path.
- **AI review.** The language server's MCP gets `record_scenarios` (address or file → the run's
  results, video and timeline) and `review_page` (an HTML page from the preview renderer: what
  was changed, each scenario with its video, chapters and step results). A skill tells an agent to
  finish a feature with: write or update the scenarios, run them, record them, hand over the page.

## Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | Spike: record the app window during a run, write `timeline.json`, compose a video with step captions; no voice. | TOD.01.1 from Presenter gives an MP4 whose captions change with the steps. |
| 2 | Voice: a narrator (Windows' built-in speech, offline) per step; the engine's pacing hook waits for it; the audio muxed in. | The same MP4 speaks each step before it happens and the app acts in time. |
| 3 | Narration text: the spoken template, `Say:` lines, the scenario's prose as intro. | A scenario with and one without `Say:` lines both sound right. |
| 4 | Hosts: `BRINELL_RECORD` in the gate run; **Record** in the VS and VS Code preview and in Presenter; **Watch** on a result. | Record in Visual Studio on TOD.01 gives a video in its results, opened from the preview. |
| 5 | AI review: MCP `record_scenarios` and `review_page`; the agent skill. | An agent asked to add a small Todo feature ends with a review page with videos. |
| 6 | Guide, an AD for recording (engine hook, evidence files, no input), the manual test plan. | |

## Decisions

| # | Question | Decided 2026-09-29 |
| --- | --- | --- |
| Q1 | ffmpeg, or Windows' own capture API? | As [screen-capture.md](screen-capture.md): one recorder API with a recorder per platform (Windows.Graphics.Capture for a window, Playwright's screencast, adb, simctl, an ffmpeg region last); ffmpeg over a pipe encodes first, Media Foundation only if needed; ffmpeg composes. |
| Q2 | Built-in or cloud voice? | Not answered; the default stands: built-in, a cloud voice later as a setting. |
| Q3 | Where does the narration go? | In a video script (`*.video.md`), as [todo-video-script.md](todo-video-script.md), not in the feature files. |
| Q4 | App only, or with the bar? And Presenter or the extension? | The app only. One engine in the runner, used by both: see [design.md](design.md). |
| Q5 | Captions burned in, or a separate file? | A separate `.srt`. |
| Q6 | Where are the videos kept? | A Recordings folder setting; the run's `TestResults` folder when not set. |
