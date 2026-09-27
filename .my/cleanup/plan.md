# Cleaning up `.my`: plan

Status: **nearly done** (2026-09-27). Steps 1-6 and 9 are done; `.my/` went from 313 files to 226,
of which 192 are `Scraper/` (34 without it). Left: step 7 (`Scraper/`, skipped for now by the owner) and
`uat/callable-phrases/` (it holds uncommitted work; move steps 6 and 8 into
`uat/phrases/phrase-scopes-and-composition.md` and delete it once that work is committed).
`construction/` stays in Brinell (step 8: the owner chose not to move it).

`.my/` holds 313 files in 16 topics. Most of the work they plan is built. AGENTS.md gives `.my/`
one job, "plans, analyses and reviews for work **in flight**", but finished plans were never
removed, so a reader cannot tell what is live. This plan takes each topic to one of three ends:

| End | When | What happens |
| --- | --- | --- |
| **Keep** | the work is not done | stays; gets a status line and a row in `.my/README.md` |
| **Extract, then delete** | done, and it holds a reason, rule or finding that nothing else records | the durable part goes to `.docs/` (why) or `docs/` (how to use), then the topic is deleted |
| **Delete** | done, and `.docs/`, `docs/` or the code already say everything that matters | deleted |

Deleting loses nothing. `.my/` is tracked, so every file stays in git history, and a decision
that needs its evidence links to it at a commit (section 3).

## 1. Rules for the cleanup

1. **References first.** 77 files outside `.my/` link into it: code comments, tests, `.docs/`,
   skills and the changelog. Section 4 lists them. Repoint or remove every reference to a
   file before deleting that file. Some already point at folders that no longer exist
   (`.my/fix/`, `.my/navigation/`, `.my/maui/`, `.my/bridge/`, `.my/scroll/`,
   `.my/GetAttribute/`, `.my/reports/`, `.my/skills/`); fix them in the same pass.
2. **A code comment points at `.docs/` or at nothing.** A `// why` comment may name an AD or a
   contract. It must not name a plan. Plans end; decisions stay. If a comment's reason is not in
   `.docs/`, it either becomes a line in an AD or the comment says it itself in one line.
3. **Extract only what is durable.** A decision is recorded once, in `.docs/`, using the
   [decision-record](../../.github/skills/decision-record/SKILL.md) skill. A finding someone using
   Brinell needs goes to `docs/`. Step lists, progress tables, baselines, RCAs of fixed bugs and
   review findings that were fixed are not durable. They stay in git history.
4. **Change the upkeep rule.** [docs-upkeep](../../.github/skills/docs-upkeep/SKILL.md) step 4
   says "do not delete it - it is the evidence a decision points at". It becomes: mark the plan
   done; once its decisions are in `.docs/` and nothing links to it, delete it, and let the AD
   link to the plan at its last commit. AGENTS.md's docs rules get the same sentence.
5. **One pass per topic, one commit per topic.** Each commit is then easy to review and to
   revert.

## 2. The topics

Status is what the topic's own files say, checked against the code where they disagree.

### Delete

| Topic | Files | Status | Why nothing needs extracting |
| --- | --- | --- | --- |
| `stale-readiness/` | 13 | done 2026-09-19, reviews fixed 2026-09-20 | AD-004 and the call-model contract hold the rules. `move-down.md` became `core/` (see below). |
| `html/` | 10 | done 2026-09-26 (steps 0-9, T1-T2) | AD-013, AD-014 and the call-model contract hold the decisions. `differences-from-maui.md` needs a check first (below). |
| `multi/` | 2 | done except the Android run | AD-014 and AD-015 hold the decisions. The Android run moves to `android/plan.md` as one open step. |
| `uat/arguments/` | 2 | done 2026-09-27 | AD-016. |
| `uat/test-data-proposal.md`, `tod-02-single-journey-uat.md`, `todo-test-data-example.md`, `baseline.md` | 4 | implemented | AD-012 and `docs/guides/test-data.md`. |
| `uat/todos/` | 10 | superseded | early drafts of the Todo scenarios; the real ones are in `samples/Todo/tests/Brinell.Samples.Todo.Uat/Scenarios/`. |
| `uat/attribute-catalog/` | 6 | implemented (review complete) | AD-011. The grammar in `01-grammar.md` moves first (below). |
| `TodoApp/` | 2 | phases 0-5 and the UAT project built | `samples/Todo/README.md` and `samples/Todo/journeys.md` describe the result. |
| `communitytoolkit/mediaelement-component-plan.md` | 1 | implemented 2026-09-18 | the control exists. |
| `move/` | 1 | pushed; PR and licence open | see "Keep", unless the PR has merged by the time of the cleanup. |
| `present/` 00-09, `06`, `plan/00`, `startup/archive/` | ~20 | delivered or superseded, by their own status lines | the Presenter exists; `docs/platform-guides/presenter.md` describes it. |
| `Scraper/rca/fixed/` | 24 | fixed | RCAs of fixed bugs. |

### Extract, then delete

| From | What is durable | To |
| --- | --- | --- |
| `uat/attribute-catalog/01-grammar.md` (and its pointer `present/02`) | The formal scenario grammar. It is the only full statement of the file format. It predates doc strings, `## Phrase:` sections and `*.phrases.md`, so update it while moving it. | `docs/specs/uat-grammar.md` (`docs/specs/` is empty today); link it from `docs/README.md` and the UAT guides. |
| `communitytoolkit/probe.md` | What each toolkit control exposes to UI Automation on Windows. Six code files cite it. | `docs/controls/community-toolkit.md`, as a "What UI Automation sees" section; repoint the six comments. |
| `present/startup/00-presenter-workspace-design.md` | Why the workspace is anchored on the UAT project and not on binaries (measured in its archive). Four Presenter services cite it. | A new AD, "The Presenter workspace is the UAT project", and a short "Workspace" section in `docs/platform-guides/presenter.md`. |
| `present/10 presenter tabbed tree redesign.md` | The current UI and execution behaviour (tree, run controls, pacing, status icons). | `docs/platform-guides/presenter.md`, for what a user sees. Nothing else, since it describes code that can be read. |
| `present/guide/README.md` | A minimal Presenter run, marked "Current". | `docs/platform-guides/presenter.md`. |
| `html/differences-from-maui.md` | Any deliberate difference that AD-013 does not already list. | AD-013's "Ruled out" or a line in the call-model contract, only if one is missing. |
| `stale-readiness/design.md` | Checked against the call-model contract. Five code comments cite it (`ContainerObjectBase`, `ViewBase.tpl.cs`, `LookupCostProbeTests`, ...). | Repoint to the contract; anything the contract lacks becomes a contract line. |
| `multi/plan.md` | Cited by seven code files for the `IUatAppsHost` / heads model. | Repoint to AD-014/AD-015; if the fixture-side contract (`ModulesFor`, sessions) is not written down, add `.docs/contracts/uat-apps-host.md`. |
| `uat/callable-phrases/` | Done except plan steps 6 and 8, which wait for scopes. AD-017 holds the decisions. | Move the two open steps into `uat/phrases/phrase-scopes-and-composition.md`, then delete the folder. |
| `uat/implementation-plan.md` | Phases 0-8 are done; phase 9 (other journeys, Construction) is open. | Phase 9 becomes one row in `.my/README.md` or moves to `construction/`; delete the rest. |

### Keep (in flight)

| Topic | Files | Status | Note |
| --- | --- | --- | --- |
| `android/` | 1 | todo | Takes the open Android run from `multi/`. |
| `core/` | 7 | L1 done; the rest is a draft | `plan.md` still says "nothing built" while `review-what-can-move-now.md` says L1 is done. Update the status and drop what L1 finished. |
| `documentation/` | 2 | todo | `.docs/README.md` and the comment instructions link to it. |
| `construction/` | 7 | infrastructure in place, Todos not started | This is Exact.Construction's UI test migration, not Brinell work. Suggest moving it to the Construction repo's own `.my/` (root AGENTS.md: per-repo docs live in that repo). |
| `communitytoolkit/plan.md`, `agent.md` | 2 | per-control progress | `.claude/agents/control-source-builder.md` links to `plan.md`. |
| `uat/phrases/` | 4 | discussion; `users-plan.md` todo | Scopes, namespaces and splitting are the next UAT work. |
| `uat/complex/`, `uat/rethinking-uat.md` | 6 | discussion drafts | Part of `complex/01-multi-user.md` is now built (`multi/`). Trim it to what is still open, or delete it if nothing is. `samples/Todo/uat-walkthrough.md` links `rethinking-uat.md`. |
| `present/fix/00`, `plan/01`, `11`, `guide/todo-app-presenter-setup.md`, `userjourney.md` | 5 | in progress or proposed | WP4, the gesture bridge, scenario parity and the Todo setup are open. Collect them into one `present/plan.md` with a status per item. |
| `move/` | 1 | PR and licence open | Delete once the PR has merged. |
| `Scraper/` (except `rca/fixed/`) | 168 | dormant: last changed 2026-05-27, tool code 2026-06-16 | See section 5: a decision for the owner. |

## 3. Evidence links

An AD whose "Why" rests on a measurement or a review (`01-assembly-and-app-path-resolution.md`
measured five options; `html/plan.md` held the baseline runs) links to it at the last commit
before deletion:

```markdown
Evidence: [.my/present/startup/archive/01-assembly-and-app-path-resolution.md at 7162ac7](https://github.com/<org>/<repo>/blob/7162ac7/.my/present/startup/archive/01-assembly-and-app-path-resolution.md)
```

The repo is moving to `exact-mobile-apps/test-tooling` (`move/`). Use the commit hash, which
survives the move, and write the URL for the repo the links should work in once the move is done.

## 4. References into `.my/` to fix

Grouped by what they point at. "Dangling" means the target is already gone.

| Target | Referenced from | Action |
| --- | --- | --- |
| `html/design.md`, `html/plan.md`, `html/todo-blazor.md`, `html/review-html-vs-maui.md` | `IHtmlDriver`, `IHtmlElement`, `IHtmlElementScope`, `IHtmlTestContext`, `PlaywrightHtmlElement`, `WebAppHost`, `UatSessions`, `CallModelPinTests`, `TodoWebUatFixture`, `samples/Todo/README.md`, AD-009, AD-013, AD-014, call-model contract, CHANGELOG | code and samples: AD-013 / AD-014 / call-model; ADs: evidence links (section 3); CHANGELOG: leave (history) |
| `stale-readiness/*` | `ContainerObjectBase`, `ViewBase.tpl.cs`, `HtmlPageObjectBase`, `StaleReadinessPinTests`, `LookupCostProbeTests`, `tools/Scripts/repeat-run.ps1`, AD-004, AD-009, AD-010, call-model contract, CHANGELOG | AD-004 / call-model; ADs: evidence links |
| `multi/plan.md`, `.my/multi` | `TestApps.cs`, `UatSessions.cs`, `TodoAppsFixture`, `TodoMauiDevice`, `TodoMixedUatFixture`, `SyncAcrossAppsTests`, `UatAppsHostTests`, `UatHeadsTests` | AD-014 / AD-015, or the new apps-host contract |
| `communitytoolkit/probe.md` | `ToolkitVerbSink`, `MediaElement.tpl/gen.cs`, `Expander.tpl/gen.cs`, `CommunityToolkitAlertsTestPage`, `docs/controls/community-toolkit.md` | the new section in `docs/controls/community-toolkit.md` (regenerate the `.gen.cs` files, do not edit them) |
| `communitytoolkit/plan.md` | `.claude/agents/control-source-builder.md` | stays (kept) |
| `present/startup/00-presenter-workspace-design.md` | `FixtureInspector`, `MsBuildProjectEvaluator`, `UatWorkspaceConfigInspector`, `ConfigEditorViewModel` | the new Presenter workspace AD |
| `core/review-what-can-move-now.md`, `stale-readiness/move-down.md` | AD-010, CHANGELOG | evidence link / leave |
| `TodoApp/plan.md` | `WindowsOnlyFact.cs` | say the reason in the comment |
| `uat/rethinking-uat.md` | `samples/Todo/uat-walkthrough.md` | stays while kept; repoint if deleted |
| `android/plan.md` | `AppiumMauiElement`, `StepperControlTests` | stays (kept) |
| `documentation/plan.md` | `.docs/README.md`, `csharp-comments.instructions.md` | stays (kept) |
| **dangling** `fix/*` (5 files) | `BrinellUiaBridge`, `AppiumMauiElement`, `samples/Brinell.Maui.AppSupport/README.md`, `ClickActivationTests`, `ReadinessTests`, `MauiFixture`, `HubPage`, `NavigationStressTests`, `GestureBridgeTests`, `AppRootScopeTests` | find the reason in git history; put it in the comment or an AD (AD-008 for the gesture bridge) |
| **dangling** `navigation/*` | `AppiumMauiDriver`, `AppiumMauiElement`, `IMauiDriver`, `AppShell.xaml.cs`, `ShellSamplePage`, `ShellFixture`, `NavigationControlTests` | as above |
| **dangling** `maui/*`, `scroll/*`, `bridge/*`, `GetAttribute/*`, `skills/*` | `App.xaml.cs`, `SamplePages.cs`, `ScrollViewModel.cs`, `MauiFixture`, `CollectionIndexProbeTests`, `EntryTests`, `maui-control/references/component.md`, AD-005 | as above |
| **dangling** `reports/` | `README.md`, `docs/README.md`, `docs/architecture/structure.md`, `.github/copilot-instructions.md` | remove the mention, or point at `TestResults/` if a report location is meant |

After the pass, this finds nothing outside `.my/` except the changelog, the kept topics and
evidence links:

```bash
grep -rnE "\.my/" --include=*.md --include=*.cs --include=*.ps1 . --exclude-dir=.my --exclude-dir=bin --exclude-dir=obj --exclude-dir=TestResults
```

## 5. `Scraper/`: a decision for the owner

192 files, 60% of `.my/`: a roadmap with 13 phases, a step file per step and per test, RCAs,
UAT runs with screenshots, and reviews. The tool is `tools/Brinell.Scraper` (with a test site and
tests). Nothing has changed for four months. The roadmap has 4 of 75 boxes ticked, although the
phase files say much more was built, so the roadmap is not a reliable status either.

Options:

1. **Archive it with the tool.** Move the roadmap and a one-page "what exists" into
   `tools/Brinell.Scraper/README.md`, and delete the rest. The tool keeps its own docs.
2. **Keep it as it is** until the Scraper work restarts, and mark it `dormant` in `.my/README.md`.
3. **Delete the Scraper docs**, and the tool too if it is no longer wanted. That is a product
   decision, not a docs one.

Recommendation: option 1. The step files are how-it-was-built notes. Whoever restarts the work
needs the vision, the phase list and the state, which fit in one README.

## 6. `.my/README.md`

After the cleanup, `.my/` gets an index with one row per topic that is still open: the topic,
what is open, its status and the date. The docs-upkeep skill's step 4 keeps it current: a topic
that is done leaves the index and the folder in the same commit.

## 7. Steps

| # | Step | Verify |
| --- | --- | --- |
| 1 | Change the docs-upkeep skill and AGENTS.md docs rules (rule 4) | both say "extract, then delete" |
| 2 | Fix the dangling references (section 4, bottom four rows) | grep finds no path that does not exist |
| 3 | Extractions: grammar spec, toolkit probe, Presenter AD and guide, apps-host contract, AD-013 check, call-model check | `docs/README.md` and `.docs/README.md` list the new files; links resolve |
| 4 | Repoint the code comments to `.docs/` (regenerate `.gen.cs` from `.tpl.cs`) | `dotnet build srcnew\Brinell.sln`; the tests that changed only comments still pass |
| 5 | Delete the "Delete" topics, one commit each | the grep in section 4 is clean |
| 6 | Trim the kept topics: `core/` status, `present/` into one plan, `uat/complex/`, open steps from `multi/`, `callable-phrases/` and `uat/implementation-plan.md` moved | each kept file has a status line |
| 7 | `Scraper/`: carry out the owner's choice | |
| 8 | `construction/`: move it to the Construction repo if agreed | |
| 9 | Write `.my/README.md` | lists exactly the folders that remain |

Expected result: `.my/` goes from 313 files to about 40 (without `Scraper/`), all of them work
in flight.

## 8. Open questions

| Question | Proposal |
| --- | --- |
| `Scraper/` | Option 1 (section 5). |
| `construction/` belongs to the Construction app | Move it to `construction/.my/`. |
| The evidence-link URL, before or after the repo move | The commit hash, with the URL of wherever the repo lives when the cleanup is done. |
| `uat/complex/` and `rethinking-uat.md`: keep as discussion? | Keep what is not built; delete what `multi/` built. |
