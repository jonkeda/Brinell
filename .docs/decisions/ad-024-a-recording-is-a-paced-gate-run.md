# AD-024: A Recording Is A Paced Gate Run

A narrated video of a scenario (the narrator) is made by the engine the xUnit gate runs, not by
the tool that asks for it:

| Decision | What holds |
| --- | --- |
| **The runner records** | A gate run with `BRINELL_RECORD` records its scenes (`Brinell.Uat` through `Brinell.Narrator`). Visual Studio, VS Code, the MCP tool `record_video` and CI only set the variables and run `dotnet test`; Presenter asks its suite's runner process for the same pacer and recorder (AD-025). One engine, so a recording shows what the gate does. |
| **Timing is a seam, not a wait** | `BddExecutionContext.Pacer` (`IBddStepPacer`) is waited on before and after each step; it is null unless something records. The voice is made before the run, so each line's length is known; the run only takes as long as the speech, and the voice is laid in afterwards at its recorded time. |
| **Narration lives in a video script** | `*.video.md`: scenes, each a scenario address, an intro, `Step | Say` rows and an outro. A feature file stays a test. The script is checked like a feature file (AD-022's one checker, `VID0xx`). |
| **Capture is not input** (AD-005) | A recorder reads what is already drawn: Windows.Graphics.Capture for a window (it records a covered one), Chromium's screencast for a page, `adb screenrecord` for a device. It never focuses, activates or moves the app. The screen-region recorder, which records whatever covers the area, is only the last resort. |
| **One clock, separate outputs** | Every time is on the `Stopwatch` clock, so a step's time in `timeline.json` is its time in the video. The video, the `.srt` captions and the timeline are separate files; nothing is burned in. ffmpeg encodes, lays in the voice and joins the scenes; Record checks for it first. |
| **Scenes can be kept** | A script's scenes can stay in `<Recordings>\<script>.scenes` (`BddKeptScenes`; Presenter's Narrator mode, or `BRINELL_RECORD_KEEP` on the gate). Each scene's `timeline.json` holds a hash of what it was recorded from (its lines, its scenario's steps, the voice, the beat), so a scene is known to be recorded, failed or out of date, one scene is recorded again without the rest, and the video is stitched from the latest recording of each. |
| **A voice names its synthesizer; a cloud voice's key never enters the repository** | `azure:<voice>` is Azure OpenAI text to speech (`AzureOpenAiSpeech`, in `Brinell.Narrator`: it needs only HTTP); any other name is a platform voice. With none named, a configured cloud voice comes first. A named cloud voice without settings fails the recording; it never becomes another voice. Its key comes from `BRINELL_VOICE_KEY` or a `voice.secrets.json` that git ignores (a suite's folder up to the repository root, then `%USERPROFILE%\.brinell`), and goes only into the request's header: never a log, a timeline, a hash or a message. A script's `Instructions` (the tone) is part of a scene's hash. |
| **Platform recorders are loaded, not referenced** | A recorder or voice that needs a platform's SDK (`Brinell.Narrator.Windows` holds both for Windows) declares itself with an assembly attribute and is found in the run's folder or in `BRINELL_RECORDERS`: a `net10.0-windows` suite cannot reference a Windows 10 SDK package. |

**Why:** a video made by another runner would show a run the gate never did, and delays tuned by
hand drift from the speech and slow every test run. Evidence (at 9f27d29d):
[design.md](https://github.com/exact-mobile-apps/test-tooling/blob/9f27d29d/.my/narrator/design.md),
[screen-capture.md](https://github.com/exact-mobile-apps/test-tooling/blob/9f27d29d/.my/narrator/screen-capture.md),
[the Narrator mode](https://github.com/exact-mobile-apps/test-tooling/blob/a8b3fc75/.my/narrator/presenter/design.md) (at a8b3fc75).

**Ruled out:**
- a host recording the screen itself, or a separate runner for videos;
- waits or `Say:` lines in feature files for narration, and a spoken template for steps a script
  does not narrate (they run silently);
- a pacer, or any narration cost, on a test run that does not record;
- activating or bringing the app forward to record it; the region recorder as the default;
- captions burned into the video.

**Broken when:**
- a scenario or step definition waits for a voice, or a plain `dotnet test` run is slower after
  this change;
- `BddExecutionContext.Pacer` is set without `BRINELL_RECORD`, Presenter's Record, or a rehearsal
  in Presenter's Narrator mode with Speak aloud on;
- a kept scene is stitched as current when its lines, steps, voice or beat changed since;
- a recorder calls `SetForegroundWindow`, focuses an element or moves a window;
- a recording's step times disagree with its video, or its captions are in the pixels.
- a cloud voice's key is read from a script, written anywhere but its request's header, or a
  `voice.secrets.json` is committed.
