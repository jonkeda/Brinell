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
| [present/](present/plan.md) | Presenter: app icon, gesture bridge, Todo workspace, scenario parity, template doc, journeys | open | 2026-09-27 |
| [unit/](unit/plan.md) | Scenarios below the UI: a `service` head, coverage on both tiers, `Brinell.Bdd`, `[BddGiven]` words ([proposal](unit/proposal.md), D1-D6 decided); Construction pilot is phase 5 | plan | 2026-09-27 |
| [uat/complex/](uat/complex/README.md) | Guidance for service, database and other complex checks; per-user isolation | discussion | 2026-09-27 |
| [uat/phrases/](uat/phrases/plan.md) | Phrases plan phases 1-8 done, to extract and close; [promote reusable subjects](uat/phrases/subjects-plan.md) (waits for Construction's first UAT suite); [Todo users](uat/phrases/users-plan.md): assignment, "me", the web head | phases done; subjects plan drafted | 2026-09-27 |
| [uat/tools/](uat/tools/vscode-extension.md) | A VS Code extension for scenarios: editor, preview, run and debug | proposal | 2026-09-27 |
| [uat/tools/visualstudio-extension.md](uat/tools/visualstudio-extension.md) | The same for Visual Studio, sharing the language server and runner; C# to scenario CodeLens, break at a step. [Plan](uat/tools/visualstudio-plan.md): preview, run and debug, editor, the rest | proposal; plan drafted | 2026-09-27 |
| [uat/tools/scraper-in-extension.md](uat/tools/scraper-in-extension.md) | The Scraper as a headless MCP service driven by Copilot from either IDE; draft scenarios from recordings | proposal | 2026-09-27 |
| [uat/reqnroll-comparison.md](uat/reqnroll-comparison.md) | Reqnroll/SpecFlow vs Brinell.Uat: what to add (hooks, Examples blocks, set diff, Cucumber Messages) and what Brinell has extra | analysis | 2026-09-27 |
| [uat/rethinking-uat.md](uat/rethinking-uat.md) | UAT for Construction, from user journeys | discussion | 2026-09-20 |
| [Scraper/](Scraper/) | `tools/Brinell.Scraper`: roadmap, steps, RCAs, UAT runs | dormant since 2026-05; owner to decide (cleanup plan, section 5) | 2026-05-27 |

## Open items without a topic

| Item | From | Status |
| --- | --- | --- |
| Repo move leftovers: licence decision, org secrets, archive `jonkeda/Brinell` | the deleted `move/` plan (the move PR merged 2026-09-23) | todo |
| Test data for Construction: expose or move the DTOs, a Construction `ITestDataDefinition`, `brinell-data` pointed at it; test sets per module and a cached seeded DB | step 9.3 of the deleted `uat/implementation-plan.md` | todo, a plan of its own |
| Ship `brinell-data` as a `dotnet tool` package, or keep running it from source | the same plan's open points | undecided |
