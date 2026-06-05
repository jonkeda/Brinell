# Test Artifact Layout Design

## Problem

Brinell test projects currently choose their own artifact locations:

- Core screenshot defaults use `TestResults/Screenshots`.
- Stride has `TestResults/Screenshots` and `TestResults/Logs`.
- UAT configs commonly use `artifacts/uat`.
- App projects can introduce their own `Reports` folders.

That makes local debugging and CI publishing harder than it needs to be. The
layout should be Brinell-owned and close to the conventions used by common .NET
test runners: a `TestResults` root, one directory per run, and report files plus
attachments under that run.

## Design Goals

- One default root for all Brinell-produced test artifacts.
- Stable subfolders for screenshots, logs, UAT output, coverage, videos, traces,
  downloaded files, and runner reports.
- Easy CI publishing: archive or publish one run folder.
- Easy local cleanup: delete old run folders without touching source files.
- Compatible with `dotnet test --results-directory TestResults` and TRX/XML
  logger output.
- Projects may override the root, but not invent a different structure.

## Non-Goals

- Do not put runtime test artifacts under `.my`; `.my/reports` is only for
  planning/design notes.
- Do not require every test project to produce every artifact type.
- Do not replace xUnit/VSTest/NUnit runner output formats; Brinell should store
  its artifacts beside those outputs.

## Proposed Root

Default root:

```text
<repo-root>/TestResults
```

Environment override:

```text
BRINELL_TEST_RESULTS_DIR=<absolute-or-repo-relative-path>
```

Run id override:

```text
BRINELL_TEST_RUN_ID=<ci-run-id-or-local-run-id>
```

If no run id is supplied, Brinell creates:

```text
yyyyMMdd-HHmmss-<short-guid>
```

Example:

```text
TestResults/20260605-142215-a18f3c/
```

## Run Folder Layout

Every Brinell run writes under one run directory:

```text
TestResults/
  20260605-142215-a18f3c/
    summary.md
    manifest.json
    runner/
    logs/
    screenshots/
    uat/
    coverage/
    traces/
    videos/
    downloads/
    snapshots/
    attachments/
```

Folder responsibilities:

| Folder | Contents |
| --- | --- |
| `runner/` | Test runner outputs such as `.trx`, xUnit XML, NUnit XML, blame logs, sequence files. |
| `logs/` | Brinell framework logs, app logs, driver logs, Appium/Playwright logs, stdout/stderr captures. |
| `screenshots/` | PNG/JPEG screenshots captured by `IScreenshotService`. |
| `uat/` | UAT scenario summaries, bind diagnostics, runtime discovery reports, per-scenario traces. |
| `coverage/` | Cobertura, OpenCover, LCOV, coverage JSON/XML/HTML. |
| `traces/` | Playwright traces, UI automation dumps, page sources, timing traces. |
| `videos/` | Screen recordings or platform video captures. |
| `downloads/` | Files downloaded by the app or test runner for verification. |
| `snapshots/` | Golden/current snapshots, visual diffs, page-source snapshots. |
| `attachments/` | Miscellaneous files linked from runner results or diagnostics. |

The root files are:

| File | Contents |
| --- | --- |
| `summary.md` | Human-readable run summary with links/relative paths to important artifacts. |
| `manifest.json` | Machine-readable artifact index for CI, report viewers, and cleanup tools. |

## Suite-Level Layout

For parallel runs or multi-project test jobs, each suite gets its own folder
inside the run:

```text
TestResults/
  20260605-142215-a18f3c/
    suites/
      Brinell.Maui.Uat.Tests/
        runner/
        logs/
        screenshots/
        uat/
      BodyCam.UAT/
        runner/
        logs/
        screenshots/
        uat/
```

Rule:

- Single-suite local runs may write directly to the run folder.
- CI and multi-project runs should use `suites/<test-project-name>/`.
- Brinell APIs should support both, with the suite folder as the preferred
  internal model.

## File Naming

Files should be readable, sortable, and filesystem-safe:

```text
<scope>__<test-or-scenario>__<reason>__<timestamp>.<ext>
```

Examples:

```text
screenshots/BodyCamUatScenarioTests__UAT-003-6-sub-button-hides-action-rows__failure__20260605-142216.png
logs/BodyCam.UAT__testhost__20260605-142215.log
uat/UAT-003-6-sub-button-hides-action-rows__trace.json
runner/BodyCam.UAT.trx
coverage/coverage.cobertura.xml
```

Naming rules:

- Use invariant timestamps: `yyyyMMdd-HHmmss` or `yyyyMMdd-HHmmss-fff`.
- Replace invalid filename characters with `_`.
- Keep the human part under 100 characters.
- Put uniqueness in timestamp/run id, not random text in every filename.
- Use lower-case folder names.

## Configuration Model

Brinell should have one artifact path resolver, owned by `Brinell.Core`:

```csharp
public sealed record TestArtifactOptions(
    string RootDirectory,
    string RunId,
    string? SuiteName);
```

Suggested API:

```csharp
public interface ITestArtifactPathProvider
{
    string RootDirectory { get; }
    string RunDirectory { get; }
    string SuiteDirectory { get; }

    string RunnerDirectory { get; }
    string LogsDirectory { get; }
    string ScreenshotsDirectory { get; }
    string UatDirectory { get; }
    string CoverageDirectory { get; }
    string TracesDirectory { get; }
    string VideosDirectory { get; }
    string DownloadsDirectory { get; }
    string SnapshotsDirectory { get; }
    string AttachmentsDirectory { get; }
}
```

Default environment variables:

| Variable | Purpose |
| --- | --- |
| `BRINELL_TEST_RESULTS_DIR` | Overrides `TestResults` root. |
| `BRINELL_TEST_RUN_ID` | Reuses a run folder across projects in one CI job. |
| `BRINELL_TEST_SUITE` | Overrides suite folder name when needed. |

Project-specific variables such as `BODYCAM_UAT_REPORTS` can remain as short-term
compatibility shims, but should map into the Brinell provider rather than
choosing their own folder layout.

## Mapping Current Defaults

| Current path | Proposed path |
| --- | --- |
| `TestResults/Screenshots` | `TestResults/<run-id>/suites/<suite>/screenshots` |
| `TestResults/Logs` | `TestResults/<run-id>/suites/<suite>/logs` |
| `artifacts/uat` | `TestResults/<run-id>/suites/<suite>/uat` |
| app-specific `Reports/Screenshots` | `TestResults/<run-id>/suites/<suite>/screenshots` |
| ad hoc `TestResults/<feature>` | `TestResults/<run-id>/suites/<suite>/downloads` or another typed folder |

## UAT Reporting

`uat.config.md` should accept either the existing `OutputDirectory` field or a
newer Brinell-owned artifact root. Recommended next step:

```markdown
## Reporting

| Field | Value |
| --- | --- |
| OutputDirectory | $(BrinellTestResults)/uat |
| ScreenshotOnFailure | true |
| IncludeRuntimeTrace | true |
```

Longer term, `OutputDirectory` should be optional. If omitted, UAT uses:

```text
<suite-directory>/uat
```

Screenshots should not be configured separately in UAT. They should use the
shared screenshot service:

```text
<suite-directory>/screenshots
```

## Runner Integration

Recommended `dotnet test` shape for CI:

```powershell
$env:BRINELL_TEST_RUN_ID = $env:GITHUB_RUN_ID ?? (Get-Date -Format "yyyyMMdd-HHmmss")
dotnet test --results-directory TestResults/$env:BRINELL_TEST_RUN_ID/runner --logger trx
```

For multi-project CI, set the same `BRINELL_TEST_RUN_ID` for every project and
set `BRINELL_TEST_SUITE` per project or let Brinell infer it from the test
assembly name.

Brinell should not require a specific runner logger. The `runner/` folder simply
keeps whatever the runner emits.

## Manifest

Each run should write a manifest:

```json
{
  "runId": "20260605-142215-a18f3c",
  "startedAtUtc": "2026-06-05T12:22:15Z",
  "rootDirectory": "TestResults/20260605-142215-a18f3c",
  "suites": [
    {
      "name": "BodyCam.UAT",
      "target": "MAUI",
      "artifacts": [
        {
          "kind": "screenshot",
          "path": "suites/BodyCam.UAT/screenshots/BodyCamUatScenarioTests__scenario__failure__20260605-142216.png"
        }
      ]
    }
  ]
}
```

The manifest enables:

- CI publishing without guessing folders.
- Future Brinell Presenter/report viewers.
- Cleanup tools that understand run age and artifact type.

## Retention

Local default:

- Keep the latest 20 run folders.
- Keep failed-run artifacts until explicitly cleaned.
- Allow `BRINELL_TEST_RESULTS_KEEP_DAYS` as a future cleanup option.

CI default:

- Let the CI system own retention after publishing `TestResults/<run-id>`.

## Implementation Plan

Status after the first implementation slice:

| Item | Status |
| --- | --- |
| `TestArtifactOptions` / `ITestArtifactPathProvider` in `Brinell.Core` | Implemented |
| `DefaultTestArtifactPathProvider` with `BRINELL_TEST_RESULTS_DIR`, `BRINELL_TEST_RUN_ID`, and `BRINELL_TEST_SUITE` | Implemented |
| Screenshot defaults and platform fixture screenshot directories | Implemented for MAUI, WPF, WinForms, and Stride |
| Stride screenshot/log options | Implemented |
| UAT omitted `OutputDirectory` defaulting to `<suite-directory>/uat` | Implemented |
| BodyCam UAT report compatibility shim mapping to Brinell UAT directory | Implemented |
| Manifest writing for screenshots and UAT scenario results | Implemented |
| CI docs beyond this design note | Not implemented |

Remaining plan:

1. Add CI/user docs beyond this design note for `dotnet test --results-directory`.
2. Add a cleanup command or helper for local retention.
3. Consider adding richer report aggregation once more artifact producers are wired in.

## Open Questions

- Should the default suite name be the test assembly name or the xUnit
  collection name? Recommendation: test assembly name.
- Should Brinell always create a run folder locally, or only when more than one
  artifact is produced? Recommendation: always create it for predictability.
- Should `summary.md` be UAT-only at first or shared by all Brinell test layers?
  Recommendation: shared file, UAT contributes a section.
