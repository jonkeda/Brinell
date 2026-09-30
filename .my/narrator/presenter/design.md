# Design: the Narrator mode in Presenter

Status: **design, decided**, 2026-09-30 (reviewed twice: D1 and D5 changed, scene checkboxes added, Q1-Q4 answered); not built. Builds on
[../design.md](../design.md) (one recording engine, video scripts) and Presenter's run toolbar.
Today Presenter's Record makes **one clip per run**; a whole video script (the 11-scene Todo video)
can only be made with `dotnet test` and `BRINELL_RECORD`. This mode makes the whole video from
Presenter, scene by scene, watched and steered.

## The ask

1. A new **Narrator** mode in Presenter.
2. It **loads a video script** (`*.video.md`).
3. It finds the suite's **config by walking up** from the script.
4. It shows the **scenes and their steps in a tree**.
5. It has the **same run buttons** as Present mode.
6. And the **Record** button.
7. **Settings** to pick the voice.

From the review: a **checkbox before each scene**; the checked scenes are the ones recorded.

## Decisions

| # | Decision | Review |
| --- | --- | --- |
| D1 | **A third mode**, beside Present mode and Workspace mode: a **Narrator mode** button with a video icon in the header, next to the two there. It swaps the page's content as those do; the window, dock side and theme stay. | changed: was a page of its own |
| D2 | **A `NarratorViewModel` of its own**, held by the shell, for the script, the tree and the recording. **The run engine stays in the shell**, in a file of its own (`PresenterShellViewModel.Run.cs`) with one entry both modes call, `RunScenariosAsync`. It was to be a separate `PresenterRunController`; the engine is bound to what the page shows (the step rows, their statuses, the preview, the timings), so a separate class would re-plumb every binding for no change in behaviour. | agreed; built as a partial, 2026-09-30 |
| D3 | **Config by walking up**: `BddVideoScriptCheck.FindConfigDirectory` from the script's folder, the walk the gate and the IDEs use. The folder found is loaded as the workspace; no config found is an error in the mode, not a failed run. | agreed |
| D4 | **The script is checked when it loads** (`BddVideoScriptCheck.Check`): a `VID0xx` error shows on its scene or step, and a scene with an error cannot be recorded. | agreed |
| D5 | **Recorded per scene, stitched after.** Each scene is recorded on its own into the script's scenes folder, and **kept**. Redoing one scene replaces that scene only; **Stitch** joins the latest recording of every scene into the video. | changed: was one pass over the whole script |
| D6 | **A failed scene does not stop the rest**: it is recorded to its failure, marked failed, and the next checked scene runs. | agreed |
| D7 | **The voice is a setting, the script can override it**: the voice list with a **Try** button; a script's `Voice` field wins, and the mode says so. | agreed |
| D8 | **The tree does not edit the script.** A `Say` line is changed in the IDE; the mode reloads the file and marks the scene out of date. | decided (Q2) |
| D9 | **A recorded scene can be watched on its own**: a play icon on its row opens that scene's `video.mp4`, before any stitch. | decided (Q3) |
| D10 | **The scenes folder lives in the Recordings folder** (default the suite's `TestResults\recordings`), not beside the script and not in git. | decided (Q4) |

## The mode

```text
┌ Brinell Presenter ──────────── [▶ Present] [▤ Workspace] [🎥 Narrator] [⚙] ┐
│ [Open script] how-todo-works.video.md  ▾ recent                             │
│ Suite: Brinell.Samples.Todo.Uat · MAUI · TodoUatFixture · App ok            │
├─────────────────────────────────────────────────────────────────────────────┤
│ [■] How the Todo app works                  11 scenes · 9 recorded · ~4 min │
│  [ ] ▾ ● 1 Opening the app          TOD.01.1       recorded 0:35  ▷         │
│          🗨 This is Todo: a small app to keep track of ...          (intro)  │
│          ✓ Given I am on the Todo List page                                 │
│            🗨 The app opens on your list.                                    │
│          ✓ And the server also has todos                          (silent)  │
│  [x] ▸ ✕ 5 The title is required    TOD.02.3 [1]   failed                   │
│  [x] ▸ ◐ 6 The detail page          TOD.03.1       out of date              │
│  [x] ▸ ○ 7 Editing                  TOD.04.2       not recorded             │
├─────────────────────────────────────────────────────────────────────────────┤
│ ▶ Run  ⏭ Next  ↓ Into  ↑ Out  ■ Stop  ⟲ Restart   ● Record (3)   ⧉ Stitch   │
│ Recording scene 6 of 11 · The detail page · step 2 of 5                     │
│ how-todo-works.mp4 · stitched 10:15 · 2 scenes newer      [Watch] [Folder]  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Loading (asks 2 and 3)

- **Open script** picks a `*.video.md`; the last five are kept like the recent folders.
- A `*.video.md` in the Workspace mode's tree gets **Open in Narrator**.
- The config is found by walking up (D3), then the workspace loads as in the other modes: config,
  parse, bind, the app. The suite line shows the summary Presenter shows today.
- The script is reloaded when the file changes on disk, as feature files are.

### The tree (ask 4, and the checkboxes)

| Level | Node | Shows |
| --- | --- | --- |
| 1 | The script | A checkbox for all scenes (three states); title, scene count, how many are recorded, the length |
| 2 | A scene | **A checkbox**; number, name, the scenario's id (`TOD.02.3 [1]` for an outline row); its recording state and length; a **play icon** once it has a recording (D9) |
| 3 | Intro, outro | The line, marked *intro* or *outro* |
| 3 | A step | The step as written, its result in the last run; its `Say` line under it, or *silent* |

A scene's **recording state**, from what is in the scenes folder:

| State | Means | Checked when the script loads |
| --- | --- | --- |
| ○ not recorded | No recording of this scene | yes |
| ✕ failed | Recorded, but a step failed | yes |
| ◐ out of date | Recorded from another text: the scene's lines, its scenario's steps, the voice or the beat changed since | yes |
| ● recorded | Recorded, passed, and current | no |

So after a change, opening the script has exactly the scenes that need redoing checked: **Record**
redoes those. Out of date is decided by a hash stored with the scene's recording
(the scene's script text, its scenario's steps, the voice, the beat).

- Step results use `BddStatusPresentation`, as the other modes do.
- A `VID0xx` diagnostic shows on its scene or step (D4); that scene's checkbox is disabled.
- A step can carry a breakpoint, as in Present mode (a rehearsal stops there; a recording ignores it).

### The run buttons (ask 5)

Present mode's toolbar, on the shell's run engine (D2):

| Button | In Narrator mode |
| --- | --- |
| Run | A **rehearsal** of the checked scenes, in the script's order, nothing recorded: paced by the Speed setting, or by the lines when Speak aloud is on |
| Next, Into, Out | Step through the current scene |
| Stop | Ends the run. A scene being recorded is dropped; scenes already finished are kept |
| Restart | The current scene from its first step |

Speed does not apply while recording: the lines and the beat pace the run, as in the gate.

### Record and Stitch (ask 6, D5)

- **Record (n)** records the **checked** scenes, one after another, each into
  `<Recordings>\<script>.scenes\scene-NN` (its `video.mp4`, WAVs and `timeline.json`), replacing
  that scene's earlier recording. The folder has no time stamp: it is the script's working set.
- A scene that passes is unchecked and shows ● recorded; one that fails stays checked (D6).
- **Stitch** joins the latest recording of every scene, in the script's order, into
  `<Recordings>\<script>-<stamp>.mp4` with its `.srt`, `.timeline.json` and chapters
  (`BddVideoComposer.ComposeAsync` over the scenes folder; the folder stays).
  - Off until every scene has a recording; a failed or out-of-date scene is stitched with a
    warning naming it, so a draft can be watched before everything is right.
  - After a Record in which all scenes are ● recorded, Stitch runs by itself.
- The status line follows it: *speaking scene 6*, *recording scene 6 of 11*, *stitching*, then the
  video's name with **Watch** and **Open folder**; *2 scenes newer* when scenes were redone since
  the last stitch.
- Needs ffmpeg: without it Record and Stitch are disabled, with `Ffmpeg.HowToGet` as the tooltip.

What this changes in the engine: `BddRecordingSession` deletes the scenes folder after composing
and takes a stamped one. It gets a mode that keeps a named folder, and the scene's hash goes into
`timeline.json`. The gate (`BRINELL_RECORD`) keeps its behaviour; `BRINELL_RECORD_KEEP=1` gives it
the same kept folder, so a video started from the gate can be finished in Presenter.

### Settings (ask 7)

A **Narrator** section on the Settings page, holding Presenter's recording settings plus the preview:

| Setting | Today | Change |
| --- | --- | --- |
| Voice | A list of the installed voices | **Try** speaks a sample; the script's `Voice` shown when it overrides (D7). Changing it makes every scene out of date |
| Beat | ms after a step | none |
| Speak aloud | Play the lines while recording | also paces a rehearsal |
| Recordings folder | Where videos go | none |

## Before this mode: Presenter must run a suite in its own process

Running the Todo suite from Presenter on 2026-09-29 failed four times over, each for a suite
loaded into Presenter's process instead of a test host. Step 0 (built 2026-09-30) fixed these:

| Failed with | Fix |
| --- | --- |
| `uat.config.md` and `Brinell.Samples.Todo.slnx` not found | The Todo fixtures and `TodoMauiLaunch` find their files from their own assembly (`AssemblyFile.FolderOf`) |
| `e_sqlite3` not found | Presenter resolves native libraries from the suite's `runtimes\<rid>\native` |
| Kestrel not found; then `Method not found` in WireMock on a second run | ASP.NET Core is probed from the shared framework folder into the session's context (the lookup went up one folder too few). Not referenced by Presenter itself: in the host it loads a second copy of the suite's WireMock by name |
| TOD.01.1 never shows Loading | Presenter did not run the suite's `BeforeFile` (the file's `TestData`) and `BeforeScenario`: `IUatScenarioLifecycle` on the fixture, called by `UatScenarioTestBase` and by Presenter |
| The suite's DLLs locked after a failed start | The session's context is unloaded only once the failure's frames are gone |
| The suite's DLLs locked after any Todo run | The context cannot unload: SQLite leaves a timer and `AppDomain` handlers, ASP.NET Core its event sources and more. Assemblies built here (a `.pdb` beside them) are read into memory instead, so a build can always overwrite them |

The rule, in the Presenter guide: **a suite finds its files with `AssemblyFile.FolderOf`, never
from `AppContext.BaseDirectory` or `Assembly.Location`**.

Known, not fixed: a session whose context cannot unload stays in memory until Presenter closes
(Presenter held about 620 MB after seven Todo runs).

## Build order

| Step | What | Checked by |
| --- | --- | --- |
| 0 | The fixes above (**done**) | TOD.01.1 runs from Presenter, again and again; the suite rebuilds while Presenter is open |
| 1 | The run engine in `PresenterShellViewModel.Run.cs`, with `RunScenariosAsync`; Present mode unchanged (**done**) | Presenter's test project, identical: 73 passed |
| 2 | The Narrator mode button; open a script, walk up to the config; the tree with its lines and diagnostics (**done**) | UAT `narrator-mode.feature.md`: the script loads, its scenes and lines show, a checkbox takes a scene out. Unit tests: a bad address shows `VID012`, a bad step `VID013`, no config `VID010` |
| 3 | Run and the step buttons over the checked scenes; a failure goes on (D6) | UAT: a rehearsal of two checked scenes passes both; an unchecked scene is skipped |
| 4 | The engine: a kept scenes folder, the scene hash, recording states | Unit tests: a changed line makes its scene out of date, and only that one |
| 5 | Record the checked scenes; Stitch; Watch, Open folder | UAT: two scenes recorded and stitched, two chapters; redo one, stitch again. Manual: the Todo video |
| 6 | Settings: the Narrator section, Try | UAT: the voice list; manual: the voice is heard |
| 7 | Docs: the guide gets a "From Presenter" section; the manual plan gets M16 | |

## Built differently, or not yet

| Design said | Built |
| --- | --- |
| The last five scripts are kept, like the recent folders | The last script is reopened (`LastScript` in the settings); no list yet |
| A `*.video.md` in the Workspace tree gets **Open in Narrator** | Not yet: Narrator mode opens the workspace's last or first script; **Open script** picks another |
| The script is reloaded when the file changes on disk | **Reload** reads it again; no file watcher |
| The script's checkbox has three states | Two: checked when every scene is |
