---
name: narrated-review
description: Finish a Brinell feature with a review a person can watch - write or update its UAT scenarios, run and record them on video with a voice-over (record_video), and hand over the review page (review_page). Use when a feature or fix in a Brinell suite is done and should be shown, or when asked for a video, a recording, a narrated demo or a review of what was built.
---

# Hand over a narrated review

A feature is done when someone can see it work. The scenarios are the proof and the script: the
suite runs them, the narrator records the app while they run, and a video script says what is
said at each step. The Brinell MCP server does the recording and writes the page.

## Workflow

1. **The scenarios.** Add or update the feature's scenarios in the suite's `Features` folder.
   Use `list_steps` for every phrase and `validate_scenario` before saving; never invent a step.
2. **The video script.** Add `Features/<feature>.video.md` beside them (format below), one scene
   per scenario you want in the video. `validate_scenario` checks it like a feature file: an
   address or a `Say` row that finds nothing is an error (`VID0xx`).
3. **Record.** Call `record_video` with the script's path. It builds the suite and runs the
   scenes with `BRINELL_RECORD`; it takes as long as the scenarios plus their speech. It
   returns the video, its captions and its timeline, and whether every scene passed.
   - No ffmpeg: it says so and how to install it. Tell the person; do not install it yourself.
   - A failed scene is still recorded: the video shows where, and the page marks it.
4. **Review page.** Call `review_page` with a title, a short summary (what you did and why), the
   changes (a line each, file names in backticks) and the video `record_video` gave. Give the
   person the page's path; it opens in a browser, next to the video.

## The video script

```markdown
# Video: Archiving a todo

| Field | Value |
| --- | --- |
| Beat | 600 |

## Scene: Archive from the detail page

Scenario: Features/TOD.12-archive-a-todo.feature.md#TOD.12.1-Archive-hides-a-todo-from-the-list

A todo you are done with, but want to keep, can be archived.

| Step | Say |
| --- | --- |
| When I tap Archive | Tap Archive on the detail page, |
| Then Todos should not have todo "Old notes" | and it leaves the list. |
```

- `Scenario:` is the scenario's address: its file relative to the suite's config, `#`, and its
  name with every run of other characters as one `-`.
- A `Say` row names a step exactly as the scenario writes it, keyword included; a step with no
  row runs silently. The paragraph before the table is said first, one after it last.
- Say what the person sees and why it matters, in the second person, one short sentence a step.
  Do not read the step back.

## Rules

- Record only a suite that passes `validate_scenario`; a recording is not a test run.
- One recording at a time: it drives the app like a test run does (no physical input, AD-005).
- The video, captions and timeline go to the Recordings folder (the suite's
  `TestResults/recordings` unless the person set one); the page goes beside the video.
