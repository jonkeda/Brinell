# Design: deleting old run results (a scenario, a feature file, a project)

Status: **steps 1-3 built** 2026-09-29 (server, request and MCP tool, Visual Studio), not yet seen
in Visual Studio (manual R-9 to R-11); steps 4 (VS Code) and 5 (its guide) open. Date: 2026-09-29.

As built: the preview's **✕** is beside a scenario heading's result (not a menu), and **Delete
results** is a bar button shown when the file has results; an outline row's result has no button
of its own (the heading's deletes every row). "Running" is a manifest written in the last 30
seconds, or a run the IDE has going. The buttons were checked in a browser against TOD.01's preview.

## Problem

The preview and the BDD Explorer show each scenario's newest run from `TestResults`. Old results
look like the current state (a "passed · 2.6 s" from a run before the last edit), and `TestResults`
grows with every run: reports, screenshots, logs. The only way to clear them is to delete folders by
hand, which also removes other scenarios' results and other tests' artifacts.

## What a run holds

`TestResults/<run-id>/` (AD-007) is one test process's run:

- `manifest.json`: per suite, a list of artifact records (`kind`, `name`, `path`, `status`,
  `createdAtUtc`, `metadata`). A scenario's record is `kind: "uat-scenario"`, `name` its scenario,
  `metadata.suite` its project, `metadata.file` its feature file under `Features/`.
- `suites/<suite>/uat/<file>__<scenario>__result.json`: the scenario's report, which names the
  files taken for it (screenshots, page dumps) under the same run folder.
- Other artifacts of the run (UI tests' screenshots, `summary.md`, accessibility audits), which
  are not a scenario's.

A run folder usually holds one suite's scenarios, often several files' (a whole `dotnet test`).

## The three scopes

| Scope | Deletes, in every run | Offered from |
| --- | --- | --- |
| **A scenario** (each row of an outline) | its `uat-scenario` records and the files they name | the preview: a scenario heading's result ("passed · 2.6 s") menu, **Delete results** |
| **A feature file** | the records of every scenario of the file, for its suite | the preview's bar: **Delete this file's results**; the BDD Explorer's Results: a file's row |
| **A project** (a suite) | every `uat-scenario` record of the suite | the BDD Explorer's Results: **Delete all results** for the picked suite |

In each scope:

1. **Find the runs** as the preview does (`BddRunResults`): every `TestResults/<run-id>/manifest.json`
   of the suite's results folder (found from the suite's project, the folder its runs write).
2. **Delete the scenario's files**: its report, and the files the report names, when they are
   inside the run folder (never a path outside it).
3. **Rewrite the manifest** without those records; a suite left with no records is removed from it.
4. **Delete the run folder** when nothing is left in its manifest but the run's own files
   (`manifest.json`, `summary.md`). A run that still holds another scenario's results, or any
   artifact that is not a scenario's, is kept.

A run that is going (its manifest written in the last minutes, or a BDD run the IDE has going) is
skipped, and the message says so: its test process still writes the manifest.

## Where it runs

In the language server, one new request, so both IDEs and the MCP server share it:

```text
brinell/deleteResults { scope: "scenario" | "file" | "suite", uri?, line?, suite }
  -> { runsChanged, runsDeleted, filesDeleted, skipped: [{ run, reason }] }
```

- `scenario`: the scenario at the heading's line (all of an outline's rows); `file`: the file;
  `suite`: the whole suite. The suite is the file's picked suite, as the preview binds it.
- Deleting is done by the server with the same code that reads runs (`BddRunResults`), so what it
  deletes is exactly what the preview would have shown.
- The results watcher then fires and the previews refresh: the scenario shows its previous run, or
  none.

## In the IDEs

- **Asked first, every time**: "Delete the results of TOD.01.2 in 3 runs (12 files)?" for a
  scenario; the counts come from a dry run of the same request (`dryRun: true`). There is no undo:
  the files are deleted, not moved to the Recycle Bin.
- **Visual Studio**: the preview's page offers the actions (heading result menu, bar button); the
  BDD Explorer's Results tab offers the file and suite ones. The Output pane lists what was deleted.
- **VS Code**: the same page and requests, with `showWarningMessage(..., { modal: true })` to ask.
- **MCP**: `delete_results` with the same arguments, so an agent can clear a suite before a clean
  run; it asks nothing, so it defaults to `dryRun: true` unless told otherwise.

## Not in scope

- The UI tests' own artifacts (screenshots of `*.UITests`, audits): they are not scenarios.
- Age-based clean-up ("older than 7 days"): a later option on the suite scope if it is wanted.
- Hiding results without deleting them: the preview could show only runs after a time; a
  different, smaller feature.

## Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | `BddRunResults.Delete(scope, …, dryRun)` in the language server: find, delete files, rewrite manifests, delete empty runs, skip running ones. | Unit tests on a `TestResults` built in the test: a scenario in a run with others (only its files go, the manifest keeps the others), a file, a suite, a run left empty (deleted), a run with a UI screenshot artifact (kept), a path outside the run (never deleted), a running manifest (skipped), a dry run (nothing changes, same counts). |
| 2 | The request `brinell/deleteResults` and the MCP tool. | LanguageServer tests over JSON-RPC. |
| 3 | Visual Studio: the page's actions, the confirmation, the explorer's buttons. | Manual checks (new) in the test plan. |
| 4 | VS Code: the same. | The same checks. |
| 5 | The guides (Visual Studio, VS Code): where results come from and how to delete them. | |

## Decisions (2026-09-29)

| # | Question | Decided |
| --- | --- | --- |
| Q1 | Delete for good, or move run folders to the Recycle Bin? | **Delete for good**; the confirmation names the counts. |
| Q2 | A scenario's scope: every run of it, or all but the newest? | **Every run**: nothing old shows afterwards. |
| Q3 | Should the suite scope also delete runs' non-scenario artifacts? | **No**: only scenario results; a run folder goes only when nothing else is in it. |
