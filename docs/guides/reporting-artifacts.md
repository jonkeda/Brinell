# Reporting And Artifacts

Bruinell-owned outputs use the shared artifact provider in
`srcnew/Bruinell.Core/Artifacts`.

## Environment

| Variable | Purpose |
| --- | --- |
| `BRUINELL_TEST_RESULTS_DIR` | Overrides the `TestResults` root |
| `BRUINELL_TEST_RUN_ID` | Reuses a run folder across projects |
| `BRUINELL_TEST_SUITE` | Overrides the suite folder name |

## Folder Layout

Default layout:

```text
TestResults/
  <run-id>/
    manifest.json
    summary.md
    suites/
      <suite-name>/
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

If no suite is configured, typed artifact folders are created directly under the
run directory.

The MAUI UI suite writes two reports into `attachments/` on every run: the gesture
accessibility audit (`accessibility-audit.md`) and per-class test timings
(`test-timings.md`, with the raw `test-timings.csv`). See the
[MAUI guide](../platform-guides/maui.md#run-artifacts).

## Provider

Use `DefaultTestArtifactPathProvider.Create(...)` to resolve folders. It finds a
repository root by walking up from the base directory until it finds a solution
file or `.git`.

Use `EnsureDirectories()` before writing multiple artifact types.

## Manifest And Summary

`TestArtifactManifestWriter.RecordArtifact(...)` records an artifact with:

- kind;
- name;
- status;
- path relative to the run directory;
- creation time;
- optional metadata.

The writer updates:

- `manifest.json`
- `summary.md`

Artifact indexing is best effort and must not fail the test run.

## UAT Reporting

UAT reporting uses `BddConfig.Reporting.OutputDirectory`. If omitted, it uses
the provider's `UatDirectory`.

Supported path tokens:

- `$(BruinellTestResults)`
- `${BruinellTestResults}`

Both resolve to the provider suite directory. UAT scenario result files are
registered as `uat-scenario` artifacts.

## Source Files

- `srcnew/Bruinell.Core/Artifacts/TestArtifactOptions.cs`
- `srcnew/Bruinell.Core/Artifacts/DefaultTestArtifactPathProvider.cs`
- `srcnew/Bruinell.Core/Artifacts/ITestArtifactPathProvider.cs`
- `srcnew/Bruinell.Core/Artifacts/TestArtifactManifestWriter.cs`
- `srcnew/Bruinell.Uat/UatScenarioTestBase.cs`
