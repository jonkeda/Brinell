# Anchoring The Workspace On The UAT Project

Status: Proposed — measured; supersedes parts of [00](00-config-edit-screen.md) and [01](01-assembly-and-app-path-resolution.md)
Date: 2026-09-22 (revised the same day — see *What changed in this revision*)
Area: `srcnew/Brinell.Presenter`, `srcnew/Brinell.Uat`, `testsnew/Brinell.Presenter.Uat.Tests`
Related:

- [Presenter config edit screen](00-config-edit-screen.md) — the editor this reshapes
- [Study: projects instead of binaries](01-assembly-and-app-path-resolution.md) — the measurements this builds on
- [Running the Todo app in Presenter](../../guide/todo-app-presenter-setup.md) — two of its three blockers turn out to be for a field with no effect

---

## The proposal

The config names **the UAT project and the fixture**. Nothing else. Not the app,
not the assemblies, not the target — those are the fixture's business or are
derivable from it. Presenter builds, lists fixtures, and lets the person pick
Debug/Release and the platform at runtime.

Two rows:

```markdown
## Runtime

| Field   | Value                             |
| ---     | ---                               |
| Project | ./Brinell.Samples.Todo.Uat.csproj |
| Fixture | TodoUatFixture                    |
```

---

## Why the config must not know the app

**`AppPath` is dead weight that fails loudly.** Traced through the code, the chain
is:

1. `UatWorkspaceConfigInspector` **requires** it, and reports an error when the
   file is missing.
2. `ResolveRequiredAppPath` resolves it with **no fallback** — `File.Exists`,
   then throw ([01](01-assembly-and-app-path-resolution.md)).
3. `UatExecutionService` exports it to the target's environment variable
   (`APPIUM_APP_PATH` for MAUI, `WPF_APP_PATH` for WPF, …).
4. **Nothing reads it.**

That last step is not an inference. Searching every `.cs` in the repo for reads
of those variables returns exactly one file — `UatExecutionServiceTargetTests`,
Presenter's own test asserting that Presenter sets and restores them. No driver,
no fixture, no factory reads `APPIUM_APP_PATH`, `WPF_APP_PATH`,
`WINFORMS_APP_PATH`, `BLAZOR_APP_PATH` or `HTML_APP_PATH`. The strings appear in
`MauiDriverFactory` only inside *error messages* suggesting you set them.

The single exception is `STRIDE_APP_PATH`, read at
[StrideTestFixtureBase.cs:156](../../../../srcnew/Brinell.Stride/Testing/StrideTestFixtureBase.cs#L156)
— and there it is the middle of a three-way fallback:

```csharp
return Configuration?.Stride?.AppPath
    ?? Environment.GetEnvironmentVariable("STRIDE_APP_PATH")
    ?? GetDefaultAppPath();
```

So the `win10-x64` blocker that the Todo setup guide spends a section on blocks a
value that **has no effect on the run**. Fix the path and nothing downstream
notices; leave it wrong and Presenter refuses to start.

### The fixture already owns this, by design

`GetDefaultAppPath` is **abstract** on every fixture base — `MauiTestFixtureBase`,
`WpfTestFixtureBase`, `WinFormsTestFixtureBase`, `StrideTestFixtureBase`. A
fixture cannot exist without answering "which app". And the MAUI base uses the
fixture's answer directly:

```csharp
driverOptions.AppPath = GetDefaultAppPath(Configuration.Maui.Platform);
```

([MauiTestFixtureBase.cs:166](../../../../srcnew/Brinell.Maui/Testing/MauiTestFixtureBase.cs#L166))

`TodoAppFixtureBase.GetDefaultAppPath` then does, in the fixture, everything the
previous revision of this document proposed to build into the config:

- **per platform** — `Brinell.Samples.Todo.App.exe` on Windows,
  `com.brinell.samples.todo-Signed.apk` on Android;
- **Debug preferred, then Release**;
- **matched to the TFM** (`net10.0-windows` / `net10.0-android`);
- **found by walking up** to the folder holding `Brinell.Samples.Todo.slnx`.

It even carries the build command to suggest when the app is missing.

**So the config knew the app only because the config used to name a *generic*
fixture** — `Fixture | FlaUI`, `Fixture | Appium`: driver-shaped fixtures with no
knowledge of any particular app, which had to be told what to launch. That era is
over. Every working fixture in this repo is concrete and app-aware.

The two configs that still carry a full `AppPath` are exactly the two that still
name a generic fixture (`FlaUI` in Todo, `Appium` in `Brinell.Maui.Uat.Tests`) —
the same values Presenter cannot instantiate, which is the "fixture-name trap"
the guides document. **They are the same fossil, and they rot together.**

### What that deletes

Everything the previous revision built to *find* the app:

- walking `ProjectReference` to locate the app project;
- `UseMaui` / `UseWPF` / `IsTestProject` detection;
- `RunCommand`, the `win-x64` RID folder, `WindowsPackageType`;
- the `$(OutputPath)$(ApplicationId)-Signed.apk` Android convention;
- the app-TFM picker.

All of it correct, all of it answering a question the fixture already answers
better — because the fixture answers it *in code, next to the thing that
launches the app*, where a drifting path fails a build rather than a demo.

---

## What is left, and where it comes from

| Field | Source |
| --- | --- |
| `Project` | **the one row in the file** |
| `Fixture` | **the other row** — a choice among valid types, so it stays explicit |
| `Pages` assembly | `TargetPath` of the project (~0.5 s, no build needed) |
| `Target` | **the fixture's base chain** — see below |
| App path, platform binary, RID | the fixture, entirely |
| `Controls` / `Commands` | deleted — never loaded ([01](01-assembly-and-app-path-resolution.md)) |
| `WorkingDirectory` | project directory, unless overridden |

### `Target` comes from the fixture too

Reflection-only, the base chain is readable without loading or running anything:

```text
TodoUatFixture
    TodoAppFixture [Brinell.Samples.Todo.UITests]
 -> TodoAppFixtureBase [Brinell.Samples.Todo.UITests]
 -> MauiTestFixtureBase [Brinell.Maui]
```

`MauiTestFixtureBase` → `MAUI`, `WpfTestFixtureBase` → `WPF`,
`WinFormsTestFixtureBase` → `WINFORMS`, `StrideTestFixtureBase` → `STRIDE`. A
fixture that declares `Target | WPF` while deriving from `MauiTestFixtureBase` is
a contradiction Presenter can now *detect*, instead of a string it takes on faith.

`Target` keeps a row in the file only as a declaration for the xUnit side, which
validates it via `UatRuntimeValidationOptions(Target: "MAUI")`. Its remaining
runtime role is narrow: choosing whether to place the AUT window beside Presenter.

The same base chain sharpens the fixture picker. Of three types with parameterless
constructors in the Todo assembly, only one has a fixture base at all:

```text
AutoGeneratedProgram   (no base)
TodoUatCollection      (no base)
TodoUatFixture         → … → MauiTestFixtureBase [Brinell.Maui]   ✓ [TestModuleScan]
```

Rank on "derives from a Brinell fixture base", annotate with `[TestModuleScan]`,
and the picker has one obvious answer instead of three plausible ones.

---

## Debug/Release and Windows/Android

Still a runtime choice, but a **smaller** one than the previous revision claimed.

**Platform already has a mechanism**: `APPIUM_PLATFORM`, read by
[BrinellMauiConfiguration.ApplyPlatformOverride](../../../../srcnew/Brinell.Maui/Configuration/BrinellMauiConfiguration.cs#L62)
and accepting `windows`, `android`, `ios`. The fixture then resolves the matching
binary itself. So the Presenter toolbar sets one environment variable for the
session — it does not pick TFMs, and it does not need to know that Android means
an APK.

The picker should offer only what the fixture supports, and `TodoAppFixtureBase`
states that by throwing `NotSupportedException` for anything but Windows and
Android. Until that is introspectable, offer all three and let the fixture's own
exception be the error — it is a good one.

Android is still blocked for an unrelated reason: `UatTargetRegistry` has no
Android entry and Presenter's AUT placement is Windows-only. Showing Android
disabled with that reason is honest; silently offering it is not.

**Configuration** (Debug/Release) still matters, but only for *building and
loading the Pages assembly*. The fixture picks the app's configuration itself,
Debug first. Worth saying plainly in the UI: the toggle builds the tests, it does
not switch the app.

Both are session state in `PresenterUserSettings` per workspace, not config rows.

---

## Build to get the fixtures — the part that needs care

This works and is the best part of the idea. One hard constraint, measured.

### A loaded assembly locks its file, permanently

```text
before load                    → writable YES
LoadFromAssemblyPath(dll)      → loaded
after load                     → writable NO  (file in use by another process)
```

`UatExecutionService` loads the Pages assembly into `AssemblyLoadContext.Default`,
which is **not collectible**, so the lock lasts the life of the process:

1. Open workspace, **Build** → works.
2. Run a scenario → assembly loaded and locked.
3. Edit a page object, **Build** → **fails**, file in use.
4. Restart Presenter.

### Listing fixtures must be reflection-only

`MetadataLoadContext`, measured against the real Todo assembly:

```text
opened in 28 ms, enumerated in 46 ms
lock WHILE open    → writable NO
lock AFTER dispose → writable YES
```

46 ms per rescan, and the lock ends on dispose — so build → list → build again
all works. This is also what produced the base chains above: the fixture picker
gets its ranking, its `[TestModuleScan]` annotation and its target check from one
46 ms reflection-only pass, with no side effects and nothing loaded.

### Running must move to a collectible context

Necessary for step 3, and worth having anyway: today everything loads into
Presenter's own context, so a workspace built against a different `Brinell.Maui`
silently gets Presenter's. A collectible `AssemblyLoadContext` per session fixes
both.

This is the risky slice. The fixture and every page object must not outlive the
context, and `UatRuntimeAssemblyResolver` currently hooks
`AssemblyLoadContext.Default.Resolving`.

### What a Build costs

No-op incremental builds, nothing changed:

| Project | Elapsed |
| --- | --- |
| `Brinell.Samples.Todo.Uat` | **6.5 s** |
| `Brinell.Samples.Maui.App` (Windows TFM) | **9.1 s** |

A progress log with cancellation, not a spinner.

Building the **app** is now clearly out of scope for the config — but a person
still needs it built. The `ReferenceOutputAssembly="false"` convention that
`Brinell.Wpf.Uat.Tests` already uses covers it:

```xml
<ProjectReference Include="..\..\samples\Brinell.Samples.Wpf.App\Brinell.Samples.Wpf.App.csproj"
                  ReferenceOutputAssembly="false" />
```

"Build it, don't reference it." One build produces the tests and the app, MSBuild
decides what is up to date, and **Presenter never learns the app's path** — the
build graph carries it, the fixture finds the output. `Brinell.Presenter.Uat.Tests`
does the same thing with an explicit `<MSBuild>` task. Todo, Maui and WinForms
UAT projects have no such reference; adding one is a project-file change, not a
config change.

---

## What changes in the editor

[00](00-config-edit-screen.md) shrinks to roughly a third of what it described.

**Gone entirely:** the Assemblies table, the AppPath field and its browse button,
`Controls`/`Commands` validation, the RID/TFM path editing.

**What remains:**

- **One editable path** — the project. Browse picks a `.csproj`.
- **The fixture picker**, now the main event: reflection-only, ranked by base
  type, annotated with `[TestModuleScan]`, with **Build and rescan**.
- **A derived panel, read-only**: target (from the fixture's base), Pages
  assembly (from `TargetPath`), each with its resolution route and a **Pin**
  action to write it into the file as an override.
- **Toolbar**: Debug/Release, platform (`APPIUM_PLATFORM`), **Build** with a
  progress log, cancellation, and MSBuild's own error text shown verbatim.

The no-config startup case becomes: pick the UAT project → Presenter derives the
target, resolves the Pages assembly, offers **Build**, then lists fixtures and
asks you to choose one. Two decisions, both made from a list.

---

## Revised delivery

| Slice | Contents | Done when |
| --- | --- | --- |
| 1 | `UatConfigWriter` + round-trip tests | identity holds on every config in the repo |
| 2 | MSBuild evaluation service (spawn, cached, async) + `Project` row | Todo workspace resolves with no paths in its config |
| 3 | **`AppPath` becomes optional** — stop requiring and validating it; keep exporting it when present, for Stride | Todo workspace loads with `AppPath` deleted |
| 4 | `MetadataLoadContext` fixture picker, ranked by base type; `Target` derived and cross-checked | `edit-fixture-name.uat.md` green |
| 5 | Editor: project field, derived read-only panel with route + Pin | `create-missing-config.uat.md` green |
| 6 | Configuration + `APPIUM_PLATFORM` toolbar, persisted per workspace | switching Release rebuilds and reloads without editing the file |
| 7 | **Collectible ALC for the Pages assembly** | run → edit → build → run, in one session |
| 8 | Build button, progress log, cancellation | build failure surfaces MSBuild's own error text |

**Slice 3 is the cheap win and should go first after 2.** It is a deletion, it
unblocks the Todo workspace on its own, and it removes two of that guide's three
blockers — one by making the field optional, the other because a concrete fixture
name is what the remaining blocker was always asking for.

Slice 7 is the one that can fail on its merits; slice 8 should not ship before it.

## What changed in this revision

The first version had Presenter derive the app from the UAT project's
`ProjectReference` graph — walking references, detecting `UseMaui`/`UseWPF`,
reading `RunCommand`, reconstructing APK paths. All of that is deleted. The
fixture already owns the app, the environment variables the config feeds are read
by nobody except Stride, and the right move is to stop asking rather than to ask
more cleverly.

Kept from the first version, unchanged and still measured: the project anchor and
`TargetPath` resolution, the file-lock finding, `MetadataLoadContext` for
listing, the collectible-ALC requirement, and the build timings.

## What I would still check

- **Collectible ALC with a MAUI page-object graph.** FlaUI, Appium and MAUI
  handlers may hold statics that pin the context. Prototype before slice 7.
- **Whether a fixture can declare its supported platforms.** `TodoAppFixtureBase`
  throws `NotSupportedException` for iOS; a small virtual property would let the
  platform picker show only what works.
- **Whether `Target` should stay in the file at all**, given it is derivable and
  the xUnit side could validate against the derived value instead.
