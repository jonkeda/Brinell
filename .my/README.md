# .my

Plans, analyses and reviews for work **in flight**, one row per topic. When a topic is done,
its decisions go to `.docs/`, what a user needs goes to `docs/`, and the topic and its row are
deleted in the same commit (the [docs-upkeep](../.github/skills/docs-upkeep/SKILL.md) skill).
Deleted topics stay in git history; the last full tree is at
[7162ac7](https://github.com/exact-mobile-apps/test-tooling/tree/7162ac7/.my).

| Topic | What is open | Status | Updated |
| --- | --- | --- | --- |
| [android/](android/plan.md) | Every Android UI failure fixed or explained; TOD.07.15 on an emulator (step 9) | doing: baseline taken | 2026-09-27 |
| [cleanup/](cleanup/plan.md) | This cleanup: only `Scraper/` is left | nearly done | 2026-09-27 |
| [communitytoolkit/](communitytoolkit/plan.md) | Android runs of the toolkit controls; the agent template for the next control source | Windows done | 2026-09-27 |
| [construction/](construction/plan.md) | Exact.Construction's UI test migration onto Brinell | infrastructure in place, Todos not started | 2026-09 |
| [core/](core/README.md) | One `ICore*` shape for every stack: L2 (busy signal), then the interfaces and bases | draft; L1 done | 2026-09-27 |
| [documentation/](documentation/plan.md) | The comment policy applied to the code, phase by phase | todo | 2026-09 |
| [narrator/](narrator/plan.md) | [Design](narrator/design.md): one recording engine in the runner, used by the IDEs, Presenter and agents; video scripts (`*.video.md`); `.srt`; a Recordings folder. Narrated recordings of a scenario run: the app recorded, a voice-over per step, paced by the narration; Record from Visual Studio, VS Code and Presenter; an AI review page with the videos. [Screen capture](narrator/screen-capture.md): Windows.Graphics.Capture and one recorder API per platform. [Todo video script](narrator/todo-video-script.md). [Narrator mode](narrator/presenter/design.md): a Presenter mode that loads a video script, shows its scenes and steps, runs them and records the video scene by scene (built 2026-09-30; AD-025) | built; [manual test plan](narrator/manual-test-plan.md) to run (needs ffmpeg); guide and AD-024 written | 2026-09-29 |
| [rca/](rca/rca-001-presenter-memory-grows-per-run.md) | RCA-001: Presenter's memory grows with every run of a suite that starts a mock server or SQLite (a leaked load context: about 4 MB private, 30 MB working set a run). Recommended: reuse one context per build of the suite | analysed, not fixed | 2026-09-30 |
| [present/](present/plan.md) | Presenter: app icon, gesture bridge, Todo workspace, scenario parity, template doc, journeys; [update](present/presenter-update.md): a docked bar beside the app (or browser), scenario tree plus step list, pause-before stepping with keys, run from/until here, the preview on demand, loading through `Brinell.Bdd.Workspace`; recording deferred ; [app window plan](present/app-window-plan.md): the app shown beside the bar, maximized or on another monitor, for every target | open; update built (steps 1-5; recording deferred), journeys not run | 2026-09-29 |
| [solution/](solution/plan.md) | Every project in `srcnew/Brinell.sln` (53 of 92 today), in folders: core, platforms, Bdd, tools, samples, each test project beside its namesake; the VSIX projects in but not built | steps 1-4 done (91 projects); root `Brinell.sln` kept as a root marker (Q6); step 6, the completeness check, open | 2026-09-28 |
| [uat/complex/](uat/complex/README.md) | Guidance for service, database and other complex checks; per-user isolation | discussion | 2026-09-27 |
| [uat/phrases/](uat/phrases/plan.md) | Phrases plan phases 1-8 done, to extract and close; [promote reusable subjects](uat/phrases/subjects-plan.md) (waits for Construction's first UAT suite); [Todo users](uat/phrases/users-plan.md): assignment, "me", the web head | phases done; subjects plan drafted | 2026-09-27 |
| [uat/tools/](uat/tools/vscode-extension.md) | A VS Code extension for scenarios: editor, preview, run and debug | proposal | 2026-09-27 |
| [uat/tools/visualstudio-extension.md](uat/tools/visualstudio-extension.md) | The same for Visual Studio, sharing the language server and runner; C# to scenario CodeLens, break at a step. [Plan](uat/tools/visualstudio-plan.md): preview, run and debug, editor, the rest | proposal; plan drafted | 2026-09-27 |
| [uat/tools/visualstudio-inline-preview.md](uat/tools/visualstudio-inline-preview.md) | The BDD preview in the editor, where Visual Studio's Markdown preview is, for feature and step files: a right-hand margin, Visual Studio's preview hidden for them | built, to check in Visual Studio (manual P-1, P-2) | 2026-09-28 |
| [uat/tools/bdd-step-debugging.md](uat/tools/bdd-step-debugging.md) | Step over, into and out of BDD steps while debugging: Alt+Shift+F10, Alt+Shift+F11, Ctrl+Shift+F11, and a paused bar with buttons in the preview; a one-shot move in the breakpoints file | built (engine, VS, VS Code, AD-022); to try in the IDEs (B-16 to B-20) | 2026-09-29 |
| [uat/tools/bdd-delete-runs.md](uat/tools/bdd-delete-runs.md) | Deleting old run results for a scenario, a feature file or a project, from the preview, the BDD Explorer and MCP; one server request; manifests rewritten, empty runs removed | design, Q1-Q3 decided | 2026-09-29 |
| [uat/tools/scraper-in-extension.md](uat/tools/scraper-in-extension.md) | The Scraper as a headless MCP service driven by Copilot from either IDE; draft scenarios from recordings | proposal | 2026-09-27 |
| [uat/format/](uat/format/plan.md) | Feature files are Markdown: Gherkin only in a ```` ```gherkin ```` block (a step outside one is prose; old files fixed with `--fix`), prose in any Markdown form everywhere else; the parser reads blocks (Markdig); migrate 16 files and the test inputs | plan, Q1-Q4 decided | 2026-09-28 |
| [uat/manual/](uat/manual/visual-studio-test-plan.md) | Manual test plan for the Visual Studio extension, for a person: setup, preview, run, debug and breakpoints, editing help, quick fixes, config, CodeLens, UAT Explorer, Copilot; answers spike S1-S6 | ready to run | 2026-09-28 |
| [uat/rename/](uat/rename/plan.md) | Bdd is the main, Uat a specialization: every `Uat*` type and `Brinell.Uat.*` tool project outside Brinell.Uat becomes `Bdd*` (Bdd, Core, Presenter, Todo, Workspace, Preview, LanguageServer, both IDE extensions and their user-facing names); only `UatPlatformsAttribute` stays | done; AD-021 holds it, to delete | 2026-09-28 |
| [uat/reqnroll-comparison.md](uat/reqnroll-comparison.md) | Reqnroll/SpecFlow vs Brinell.Uat: what to add (hooks, Examples blocks, set diff, Cucumber Messages) and what Brinell has extra | analysis | 2026-09-27 |
| [uat/rethinking-uat.md](uat/rethinking-uat.md) | UAT for Construction, from user journeys | discussion | 2026-09-20 |
| [Scraper/](Scraper/) | `tools/Brinell.Scraper`: roadmap, steps, RCAs, UAT runs | dormant since 2026-05; owner to decide (cleanup plan, section 5) | 2026-05-27 |

## Open items without a topic

| Item | From | Status |
| --- | --- | --- |
| Repo move leftovers: licence decision, org secrets, archive `jonkeda/Brinell` | the deleted `move/` plan (the move PR merged 2026-09-23) | todo |
| Test data for Construction: expose or move the DTOs, a Construction `ITestDataDefinition`, `brinell-data` pointed at it; test sets per module and a cached seeded DB | step 9.3 of the deleted `uat/implementation-plan.md` | todo, a plan of its own |
| Ship `brinell-data` as a `dotnet tool` package, or keep running it from source | the same plan's open points | undecided |
