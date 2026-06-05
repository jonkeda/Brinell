# RCA: Brinell.Core Artifact Duplicate Types

Date: 2026-06-05

## Summary

Normal `dotnet build` and `dotnet test` are blocked because `Brinell.Core` currently compiles two artifact-support implementations that declare overlapping public types in the same namespace: `Brinell.Core.Artifacts`.

The NativeAndroid changes are not the source of the failure. NativeAndroid compiles when built against already-built Core binaries with project-reference builds disabled, but full builds fail as soon as MSBuild attempts to compile `Brinell.Core`.

## Impact

- Full project builds fail for `Brinell.NativeAndroid` because it references `Brinell.Core`.
- `dotnet test Brinell/testsnew/Brinell.NativeAndroid.Tests/Brinell.NativeAndroid.Tests.csproj` fails during project-reference build.
- Practical workaround is currently:

```powershell
dotnet build Brinell/srcnew/Brinell.NativeAndroid/Brinell.NativeAndroid.csproj -f net10.0 -p:BuildProjectReferences=false --no-restore
dotnet test Brinell/testsnew/Brinell.NativeAndroid.Tests/Brinell.NativeAndroid.Tests.csproj -p:BuildProjectReferences=false --no-restore
```

The workaround verifies NativeAndroid against existing Core binaries, but it does not validate a clean repository build.

## Detection

The first normal build after restore fails with duplicate type errors from `Brinell.Core`, for example:

- `DefaultTestArtifactPathProvider`
- `ITestArtifactPathProvider`
- `TestArtifactOptions`
- `TestArtifactManifestWriter`
- `TestArtifactManifest`
- `TestArtifactSuite`

Example command:

```powershell
dotnet build Brinell/srcnew/Brinell.NativeAndroid/Brinell.NativeAndroid.csproj
```

Example failure pattern:

```text
error CS0101: The namespace 'Brinell.Core.Artifacts' already contains a definition for 'DefaultTestArtifactPathProvider'
error CS0101: The namespace 'Brinell.Core.Artifacts' already contains a definition for 'ITestArtifactPathProvider'
error CS0111: Type 'DefaultTestArtifactPathProvider' already defines a member called 'Create' with the same parameter types
```

## Root Cause

`Brinell.Core` contains two folders that are both included by the SDK-style project default compile glob:

- `Brinell/srcnew/Brinell.Core/Artifacts`
- `Brinell/srcnew/Brinell.Core/ArtifactSupport`

Both folders declare types in the same namespace:

```csharp
namespace Brinell.Core.Artifacts;
```

Because the project file does not exclude either folder, both implementations are compiled into the same assembly. Since the type names overlap, the compiler reports duplicate definitions.

## Contributing Factors

The duplicate folders are not exact copies. They represent two versions of the artifact API that have drifted:

- `ArtifactSupport` includes legacy environment variable names such as `BRINELL_ARTIFACT_ROOT`, `BRINELL_ARTIFACT_RUN_ID`, and `BRINELL_ARTIFACT_SUITE`.
- `ArtifactSupport` exposes `RunId` and `SuiteName` on `ITestArtifactPathProvider`.
- `ArtifactSupport` writes manifest entries using `TestArtifactRecord` with a `Reason` field.
- `Artifacts` includes repository-root resolution and a stable local run ID.
- `Artifacts` has a newer `TestArtifactOptions` record and a manifest shape with `StartedAtUtc`, `RootDirectory`, status, and metadata.
- Existing tests in `Brinell/testsnew/Brinell.Core.Tests/TestArtifactPathProviderTests.cs` expect parts of the `ArtifactSupport` API shape, especially legacy env vars and manifest `Reason` semantics.
- Existing runtime code in `ScreenshotService` and UAT reporting calls `TestArtifactManifestWriter.RecordArtifact(...)`, so behavior must be preserved rather than removed blindly.

## Why NativeAndroid Can Still Be Tested

NativeAndroid compiles cleanly when the Core project reference is not rebuilt:

```powershell
dotnet build Brinell/srcnew/Brinell.NativeAndroid/Brinell.NativeAndroid.csproj -f net8.0 -p:BuildProjectReferences=false --no-restore
dotnet build Brinell/srcnew/Brinell.NativeAndroid/Brinell.NativeAndroid.csproj -f net9.0 -p:BuildProjectReferences=false --no-restore
dotnet build Brinell/srcnew/Brinell.NativeAndroid/Brinell.NativeAndroid.csproj -f net10.0 -p:BuildProjectReferences=false --no-restore
```

The isolated NativeAndroid unit test path also passes:

```powershell
dotnet test Brinell/testsnew/Brinell.NativeAndroid.Tests/Brinell.NativeAndroid.Tests.csproj -p:BuildProjectReferences=false --no-restore
```

Observed result: `17/17` passed.

## Recommended Remediation

Consolidate the artifact implementation into one folder and one API surface.

Recommended direction:

1. Keep a single canonical namespace: `Brinell.Core.Artifacts`.
2. Keep only one physical implementation folder, preferably `Artifacts`.
3. Merge required compatibility from `ArtifactSupport` into the canonical implementation:
   - legacy env var constants,
   - `RunId` and `SuiteName` on `ITestArtifactPathProvider`,
   - current tests' expected path behavior,
   - `RecordArtifact` overloads used by both screenshot and UAT code.
4. Ensure manifest models support existing tests and newer metadata/status needs.
5. Delete or exclude the superseded folder after the merge.
6. Run:

```powershell
dotnet build Brinell/srcnew/Brinell.Core/Brinell.Core.csproj
dotnet test Brinell/testsnew/Brinell.Core.Tests/Brinell.Core.Tests.csproj
dotnet test Brinell/testsnew/Brinell.NativeAndroid.Tests/Brinell.NativeAndroid.Tests.csproj
```

## Preventive Actions

- Add a CI build for `Brinell.Core` before platform projects.
- Add a small duplicate-type guard or convention check for mutually exclusive implementation folders.
- Avoid parallel folder migrations where both old and new folders use the same public namespace unless the old folder is excluded in the same change.

## Current Status

NativeAndroid changes can be compile-tested with `BuildProjectReferences=false`, but clean full builds remain blocked until `Brinell.Core.Artifacts` is consolidated.
