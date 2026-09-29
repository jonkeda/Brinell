# Manual test plan: the narrator

Status: **to run**, 2026-09-29. The steps of [design.md](design.md) are built; these are the checks
that need ffmpeg, a voice, the Todo app built, and an emulator, which the build machine did not
have. Run from the Brinell root. Record the result and the date in the last column.

## Before

1. `winget install Gyan.FFmpeg.Essentials`; a new terminal; `ffmpeg -version` answers.
2. Settings, Time and language, Speech: at least one voice.
3. Build: `dotnet build tools\Brinell.Bdd.LanguageServer` (it copies `recorders\`), the Todo app
   (`dotnet build samples\Todo\src\Brinell.Samples.Todo.App -f net10.0-windows10.0.19041.0`) and
   the Todo UAT suite.
4. The ffmpeg tests, skipped until now:
   `dotnet test testsnew\Brinell.Recording.Tests` and `dotnet test testsnew\Brinell.Narrator.Tests`:
   0 skipped.

## Checks

| # | Do | Expect | Result |
| --- | --- | --- | --- |
| M1 | From `samples\Todo\tests\Brinell.Samples.Todo.Uat`: `BRINELL_RECORD=Features\how-todo-works.video.md`, `BRINELL_RECORDERS=<LS bin>\recorders`, `dotnet test --filter "BddScenario~Features/TOD.01-see-my-todos.feature.md#TOD.01.1"` | `TestResults\<run>\how-todo-works-*.scenes\scene-01\` has `video.mp4`, `timeline.json` and the WAVs. The app stayed behind other windows, yet the video shows it | |
| M2 | The same, filtered on all eleven scenes' addresses (VS Code's Record prints the filter) | one `how-todo-works-*.mp4` with `.srt` and `.timeline.json`; the `.scenes` folder is gone || Passed 2026-09-29: 3:43, 11 chapters, all scenes passed. Filter on `BddScenario~` (TOD.02.3 is an outline: its rows are addressed `...-1`, `...-2`). TOD.01.1 first failed: the loading line was said before the Loading check, after the 2 s delay had ended; now said with Sync |
| M3 | Play M2's video with its `.srt` | the voice speaks each line before its step happens; captions change with the voice; chapters list the eleven scenes | |
| M4 | Make a scene fail (a `Then` that cannot hold) and record | the video goes on to the failure; a caption `Failed: ...` after the step; the timeline marks the scene failed | |
| M5 | Visual Studio: open `how-todo-works.video.md`, **Record** in the preview | builds, runs, asks to watch; the preview lists the recording; Watch plays it | |
| M6 | VS Code: the same, with `brinellBdd.voice` set to another voice | the other voice speaks; Watch plays it | |
| M7 | Visual Studio and VS Code: **Record** on TOD.01.3's heading | a silent video of that scenario, with its beats, named `TOD.01.3-...-<stamp>.mp4` | |
| M8 | Presenter: Record on, run TOD.01.1 (App window: Beside the bar) | the status says Recording, then Recorded; Watch plays a video narrated like M3's scene 1 | Partly automated by `record-a-run.feature.md` (Record on, Recorded status, Watch offered). Passed 2026-09-29. A narrated TOD.01.1 run and Watch playing it: still to run |
| M9 | Presenter: Speak aloud on, run TOD.01.1 | each line is heard as it is waited for, before its step | |
| M10 | Presenter: Record on, run TOD.01.3 (no scene narrates it) | a silent video, "no video script narrates this scenario" | Automated: `testsnew\Brinell.Presenter.Bdd.Tests\Features\record-a-run.feature.md` on the Presenter sample (no script). Passed 2026-09-29: the video shows the app, its timeline marks every step passed |
| M11 | An agent with the MCP server: `record_video` on the script, then `review_page` | the page opens beside the video; chapters seek; step results match M2's timeline | |
| M12 | Blazor: record a Todo web scenario headless (`BRINELL_RECORD=1`, the Web UAT suite) | the video shows the page, not a window | |
| M13 | Android emulator: record a TOD scenario longer than 3 minutes (`BRINELL_BEAT=20000` stretches one) | one video, joined from two segments, with a warning about the join | |
| M14 | A plain `dotnet test` of the Todo UAT suite, without `BRINELL_RECORD` | as fast as before the narrator (the pacer is null) | |
| M15 | No ffmpeg (`BRINELL_FFMPEG=C:\none.exe`): Record in each host | each says ffmpeg is needed and how to get it; nothing runs | |
