# Study: Should The Config Point At Projects Instead Of Binaries?

Status: Study — complete. Five experiments run, recommendation settled; nothing built
Date: 2026-09-22 (experiments run the same day)
Area: `srcnew/Brinell.Uat`, `srcnew/Brinell.Presenter/Services`
Related:

- [Anchoring the workspace on the UAT project](02-project-anchored-workspace.md) — where this study's conclusions were taken further: one project row, derived fields, a Build button
- [Presenter config edit screen](00-config-edit-screen.md) — the screen this feeds
- [Running the Todo app in Presenter](../../guide/todo-app-presenter-setup.md) — the stale `win10-x64` bug, which is the motivating case
- [Minimal Presenter run](../../guide/README.md)

---

## The question

`uat.config.md` names build outputs:

```markdown
| AppPath | ../../src/…/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Brinell.Samples.Todo.App.exe |
| Pages   | bin/Debug/net10.0-windows/Brinell.Samples.Todo.Uat.dll |
```

Two alternatives were asked about:

1. **Point at the `.csproj`** and derive the output.
2. **Point at the assembly name** and find the file by searching folders.

Both are answerable from the code and from measurement rather than opinion, so
this is a study, not a proposal. The short answer is that option 2 already exists
and is already load-bearing, option 1 is cheaper and more exact than it sounds,
and the interesting design question is not which one — it is *which paths matter
at all*, because two of the three rows in the `Assemblies` table turn out to be
decorative.

---

## What the paths actually do today

### Only `Pages` is ever loaded

`UatExecutionService` loads exactly one assembly from the config:

```csharp
var pagesAssembly = resolver.LoadRequired(GetRegisteredAssembly(config, "Pages"));
```

([UatExecutionService.cs:57](../../../../srcnew/Brinell.Presenter/Services/UatExecutionService.cs#L57))

`Controls` and `Commands` are never passed to `LoadRequired`. Their only runtime
effect is that `BuildProbeDirectories` adds their *containing folders* to the
probe list ([UatExecutionService.cs:332](../../../../srcnew/Brinell.Presenter/Services/UatExecutionService.cs#L332)).
Their only other effect is that `UatWorkspaceConfigInspector` reports an error
when the file is missing — an error about a file nothing opens.

Worse, those two rows normally point at assemblies **Presenter has already
loaded**. `Brinell.Presenter.csproj` project-references `Brinell.Maui`,
`Brinell.Maui.FlaUI` and `Brinell.Uat`, and `LoadRequired` short-circuits on
simple name against `AssemblyLoadContext.Default.Assemblies` before it looks at
any path at all. The `Controls | …/Brinell.Maui.dll` row cannot change which
`Brinell.Maui` the run uses.

> Pre-existing hazard, unrelated to this study but worth writing down: a
> workspace built against a *different* `Brinell.Maui` silently gets Presenter's,
> because everything loads into the default `AssemblyLoadContext`. Neither
> option below changes that. A real fix is a collectible ALC per session.

### Under `dotnet test`, the paths are dead text

`UatRuntime.Validate` checks `Target`, `Fixture` and `config.Assemblies.Count == 0`
([UatRuntime.cs:100](../../../../srcnew/Brinell.Uat/UatRuntime.cs#L100)). It never
resolves a path — the test host already has every assembly loaded.

So path accuracy is a **Presenter-only** requirement. Changing the format cannot
break the xUnit suites; it can only change what Presenter reads.

### The name search already exists — twice

Option 2 is not a proposal. It is what the code does now, in two places:

| Where | Trigger | Ranking |
| --- | --- | --- |
| `UatWorkspaceConfigInspector.ResolveAssemblyPath` | configured path missing | `bin/Debug` > `net10.0` > newest write |
| `UatRuntimeAssemblyResolver.ResolveAssemblyPath` | any dependency the CLR cannot resolve | probe dirs first, then the same ranked walk |

Both call `Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories)`
under `FindSolutionRoot(workspacePath)` (or `WorkingDirectory`, when set), minus
`obj/`.

This is why a wrong `Pages` path often still works, and it is why the failure
mode is confusing when it doesn't: the search found *an* assembly with that file
name, just not the one you meant.

---

## Measurements

All from `Brinell/` on this machine, warm cache, 2026-09-22.

| What | Result |
| --- | --- |
| `dotnet msbuild srcnew/Brinell.Uat/Brinell.Uat.csproj -p:TargetFramework=net10.0 -getProperty:TargetPath` | exact path, **0.45 s**, no build, no restore |
| Same on a scratch `.csproj` with **no `obj/`, no `bin/`** | exact path, **0.46 s** — answers *where it will be* before the first build |
| Same on the Todo MAUI app, `-getProperty:RunCommand` | `…/net10.0-windows10.0.19041.0/win-x64/Brinell.Samples.Todo.App.exe` |
| `-getProperty:TargetPath` **without** `-p:TargetFramework` on a multi-targeted project | `""` — empty, silently |
| Full-tree name walk under `Brinell/` (33 278 files) | **~1.0 s per miss** |
| Same under the container `MainConstruction2/` (131 637 files) | **~4.1 s per miss** |
| Copies of `Brinell.Core.dll` outside `obj/` | **63** |
| Copies of `Brinell.Maui.dll` / `Brinell.Uat.dll` outside `obj/` | 13 / 14 |
| Copies of `Brinell.Samples.Todo.Uat.dll` outside `obj/` | 1 |

Two numbers decide most of this study.

**63.** A name search for a *shared* assembly is choosing between sixty-three
files by write timestamp. For a *page-object* assembly it is choosing between
one. The search is not uniformly good or bad — it is excellent for the row that
matters and unprincipled for the rows that don't.

**0.45 s.** MSBuild evaluation-only is not the multi-second, workload-heavy
operation it is assumed to be. It is four times *faster* than one failed name
search, and it is exact.

### Every checked-in config that writes a path is stale

Surveying all eight `uat.config.md` files in the tree settles the argument more
bluntly than any measurement:

| Config | `Pages` row | State |
| --- | --- | --- |
| `Brinell.Wpf.Uat.Tests` | `Brinell.Wpf.Uat.Tests.dll` | bare name — correct |
| `Brinell.WinForms.Uat.Tests` | `Brinell.WinForms.Uat.Tests.dll` | bare name — correct |
| `Brinell.Blazor.Uat.Tests` | `Brinell.Blazor.Uat.Tests.dll` | bare name — correct |
| `Brinell.Html.Uat.Tests` | `Brinell.Html.Uat.Tests.dll` | bare name — correct |
| `Brinell.Stride.Uat.Tests` | `Brinell.Stride.Uat.Tests.dll` | bare name — correct |
| `Brinell.Presenter.Uat.Tests` | `Brinell.Presenter.Uat.Tests.dll` | bare name — correct |
| `Brinell.Maui.Uat.Tests` | `…/bin/Debug/**net10.0-windows7.0**/…UITests.dll` | **stale — that folder does not exist** |
| `Brinell.Samples.Todo.Uat` | `bin/Debug/net10.0-windows/…Todo.Uat.dll` | path correct; its `AppPath` has the stale `win10-x64` |

Six of eight already use a **bare assembly name with no path at all** — which
means option 2 is not merely implemented, it is the format six configs in this
repo are written in, and the one the working "minimal run" workspace depends on.

Both configs that spell out a path are wrong. The Todo `AppPath` bug was known;
the `net10.0-windows7.0` in `Brinell.Maui.Uat.Tests` was not — `Brinell.Maui.UITests`
builds to `net10.0-windows`. It has been silently rescued by the name search this
whole time.

That is the study in one table: **the paths rot, the names do not, and the
mechanism that keeps the repo working is the fallback nobody configured.**

---

## Option A — point at the `.csproj`

```markdown
| Kind  | Project                                          |
| Pages | ../Brinell.Samples.Todo.Uat/…Todo.Uat.csproj     |
```

Resolution: spawn `dotnet msbuild <csproj> -p:TargetFramework=<tfm> -getProperty:TargetPath`,
parse the JSON, use the path.

### What it buys

- **Correct by construction.** The stale `win10-x64` in the Todo config becomes
  impossible: MSBuild computes the RID folder, the TFM folder and the
  configuration from the project itself.
- **Answers before the first build.** It returns the path the build *will*
  produce. The single most common first-run state — "a workspace you have not
  built yet" — stops being an error and becomes *"not built yet: expected at
  `…/bin/Debug/net10.0/X.dll`"*, with a **Build** button as the obvious next
  step.
- **Survives Debug→Release** and any output-path change, given the configuration
  is passed through.
- **`RunCommand` solves `AppPath` exactly**, including the apphost `.exe` rather
  than the managed `.dll`, and it is the same property `dotnet run` uses.
- **A project path is reviewable.** A wrong `.csproj` in a diff is obvious; a
  wrong TFM folder in a 140-character path is not.

### What it costs

- **A multi-targeted project needs a TFM, or you get `""`.** `Brinell.Uat`
  targets `net8.0;net9.0;net10.0`; the Todo app targets
  `net10.0-windows10.0.19041.0;net10.0-android`. This is the sharp edge: the
  failure is an empty string, not an error. Any implementation must treat empty
  `TargetPath` as "ambiguous — pick a TFM", and the config needs a way to say
  which. Reading `TargetFrameworks` in the same call gives the candidate list for
  free, so the edit screen can offer a picker rather than a text box.
- **Requires the .NET SDK.** Presenter is itself an SDK-built MAUI app, so the
  machine running it has one. It does **not** require the project's workloads —
  experiment 1 settles that: evaluation never touches them. The price is that a
  clean evaluation says nothing about whether the project can build.
- **A process spawn.** Needs a timeout, cancellation, and stderr captured into
  diagnostics. 0.45 s is fine interactively; it is not free in a loop, so cache
  per (project, TFM, configuration) and invalidate on the project's write time.
- **Only works for SDK-style projects.** Fine for everything in this tree; would
  not cover a hypothetical non-SDK or non-.NET app under test.
- **`AppPath` is not always a .NET project.** An Electron app, a packaged MSIX,
  or an already-shipped binary has no `.csproj`. Whatever is chosen, a literal
  path must remain legal.

---

## Option B — find by assembly name

```markdown
| Kind  | Assembly                     |
| Pages | Brinell.Samples.Todo.Uat.dll |
```

Resolution: the ranked tree walk that already exists.

### What it buys

- **Already implemented and already proven** — it is the fallback that makes
  today's slightly-wrong configs work.
- **Zero dependencies.** No SDK, no process, no MSBuild version question.
- **Shortest possible config.** A name is a name; nothing to get stale.

### What it costs

- **Ambiguity scales with how shared the assembly is** — 1 candidate for a page
  assembly, 63 for `Brinell.Core.dll`. The tie-break (`bin/Debug`, then
  `net10.0`, then newest) is a heuristic, and "newest write" is decided by
  whatever you happened to build last. Two developers, same config, different
  assembly.
- **Hard-codes `Debug` and `net10.0` into a ranking rule.** A Release-only
  machine is ranked *last* on its own build. This is invisible until it bites.
- **Cost per miss is the whole tree** — ~1 s under `Brinell/`, ~4 s under the
  container. The `AssemblyLoadContext.Resolving` handler runs this walk for every
  name the CLR cannot resolve, and type enumeration during page discovery
  produces a burst of those. This is a plausible part of why a failing run feels
  slow.
- **Scope depends on `WorkingDirectory`.** `ProbeRoot` is `workingDirectory ??
  FindSolutionRoot(...)`. The Todo config's `WorkingDirectory | ../../../..`
  lands on `Brinell/` by luck; one more `..` and every miss walks 131 637 files.
- **Silent.** Nothing tells you resolution fell back, or what it chose. The
  config keeps claiming a path that is not the one in use.

### Where it is genuinely right

For `Pages` in a normal workspace: one candidate, no ambiguity, no SDK needed.
The problem is not the mechanism; it is using an unranked global search as a
*primary* strategy for assemblies that exist sixty-three times.

---

## Option C — the `.csproj` without MSBuild

Read `AssemblyName` and `TargetFrameworks` out of the project XML, then glob
**inside that project's own folder**: `bin/*/<tfm>/**/<AssemblyName>.dll`.

- Keeps option A's decisive advantage — the project folder is what makes the
  answer unambiguous, not MSBuild.
- No process, no SDK, no workloads, ~milliseconds.
- Tolerates Debug/Release and RID subfolders by globbing rather than computing.
- But: cannot answer before the first build; does not see `AssemblyName`
  inherited from `Directory.Build.props`; would not survive a custom
  `OutputPath` or .NET 8+ artifacts layout. (Neither is used in this tree today —
  checked: no `UseArtifactsOutput`, no `ArtifactsPath`, no `OutputPath`
  overrides.)

This is the cheap 80 % version of A, and a reasonable fallback *within* A when
MSBuild is unavailable.

---

## Recommendation

> **Overtaken on the `AppPath` half — see [02](02-project-anchored-workspace.md).**
> Everything below about *assembly* resolution stands. What it gets wrong is
> treating `AppPath` as a field worth resolving well: the fixture already owns the
> app (`GetDefaultAppPath` is abstract on every fixture base), and the environment
> variables this config feeds are read by no production code except Stride's
> fallback. The right move is to stop requiring `AppPath`, not to resolve it from
> a project. The asymmetry below is still the reason it fails loudly.

The survey and the experiments together point somewhere more specific than
"projects instead of binaries". There is an asymmetry in the current code that
explains both known bugs and decides where the project reference actually pays:

| Row | Stale value → | Because |
| --- | --- | --- |
| `Assemblies` | silently rescued by the tree search | `ResolveAssemblyPath` falls back |
| `AppPath` | **hard failure** | `ResolveRequiredAppPath` does `File.Exists`, then throws — **no fallback at all** |

That is exactly the two bugs found: `net10.0-windows7.0` in an assembly row has
been invisible for months; `win10-x64` in `AppPath` blocks a documented walkthrough.

So:

**Point `AppPath` at the project. Leave the assembly rows alone — they already
solved this with bare names — and stop pretending the two decorative rows matter.**

Concretely:

1. **`AppPath` accepts a `.csproj`**, resolved per target: `RunCommand` for
   desktop targets, `ApplicationId` + `$(OutputPath)…-Signed.apk` for a future
   Android head. This is the row with no fallback, the row that blocks people, and
   the row where a hand-written path encodes a TFM *and* a RID *and* a
   configuration — three things a human should never be maintaining.
2. **Assembly rows keep bare names as the documented default.** Six of eight
   configs already do this and none of them have rotted. Accept a `.csproj` too,
   for the case the name cannot answer — a workspace that has never been built —
   but do not migrate the working configs. The `net10.0-windows7.0` row should
   become `Brinell.Maui.UITests.dll`, not a project reference.
3. **`Assemblies` shrinks to what is loaded.** `Controls` and `Commands` are never
   passed to `LoadRequired`; their folders are already in the probe list. Keep
   parsing them so old configs work; stop validating them as errors. That deletes
   two of the three rows the Todo guide spends a section on.
4. **`Runtime` gains `Configuration`** (default `Debug`), since experiment 2 shows
   evaluation tracks it, plus a session-level Debug/Release override in the
   toolbar.
5. **A TFM field appears only when the project is multi-targeted** — detected by
   reading `TargetFrameworks` in the same call. Single-TFM workspaces stay
   one-liners.

The resulting config for the Todo workspace:

```markdown
## Runtime

| Field         | Value                                                   |
| Target        | MAUI                                                    |
| Fixture       | TodoUatFixture                                          |
| AppProject    | ../../src/Brinell.Samples.Todo.App/…Todo.App.csproj     |
| AppTfm        | net10.0-windows10.0.19041.0                             |

## Assemblies

| Kind  | Assembly                     |
| Pages | Brinell.Samples.Todo.Uat.dll |
```

Three fields that can rot become zero. `AppPath` and the existing path spellings
stay legal — nothing in the repo must be rewritten, and a non-.NET app under test
keeps working.

6. **Every resolution carries how it was resolved** — *configured*, *evaluated*,
   *found by search* — and the edit screen shows that word beside the resolved
   path. Silent fallback is what let a wrong TFM live in a checked-in config
   without anyone noticing.
7. **Scope the fallback search**: project folder, then workspace, then probe
   directories, then the tree — and never above `FindSolutionRoot`, whatever
   `WorkingDirectory` says. At ~1 s per miss under `Brinell/` and ~4 s under the
   container, an unscoped walk in the `Resolving` handler is a latency bug
   waiting to be blamed on the app under test.

### Why this is not "option A everywhere"

Because option B already won where it was allowed to compete. A bare assembly
name has exactly one candidate for a page-object assembly, costs nothing, needs
no SDK, and has survived unchanged in six configs. Replacing that with an MSBuild
spawn would be motion, not progress.

What option B cannot do is answer for a project that has never been built, or for
`AppPath`, where there is no search to fall back on and the answer encodes a RID.
That is the gap the project reference fills — and only that gap.

---

## What this changes in the edit screen

The [config edit screen](00-config-edit-screen.md) gets simpler, not harder:

- **The app Browse picks a `.csproj`**, not an `.exe` buried under
  `bin/Debug/<tfm>/<rid>/`. Picking a project from a folder tree is a thing
  people do correctly; picking the right RID folder is not.
- **The resolved path becomes a computed, read-only line** under the field —
  path, resolution route (*configured* / *evaluated* / *found by search*), and
  existence. Not an editable string that drifts.
- **"Not built" stops being an error.** Evaluation answers before the first build,
  so the field reads *"not built — expected at `…`"* with a **Build** button, and
  the build's own NETSDK error is what gets shown if it fails.
- **The fixture picker gets an upstream fix**: *"not built — build it to list
  fixtures"* instead of an empty candidate list.
- **The TFM picker appears only for multi-targeted projects.**
  `TargetFrameworks` comes back in the same ~0.5 s call.
- **Resolution is async and cached** per (project, TFM, configuration), keyed on
  the project's write time. 0.5 s is fine on demand, wrong on every keystroke —
  and there is no warm-up to wait for, since cold and warm measure the same.
- **Slice 4 changes shape**: the Assemblies table mostly disappears instead of
  getting an editor — two rows deleted, the third staying a bare name.

---

## Experiments — run 2026-09-22

Machine: SDK 10.0.401, workloads `android`, `ios`, `maccatalyst`, `maui-windows`
(from VS 18.11). All five ran; none changed the recommendation, two sharpened it.

| # | Experiment | Result | Effect |
| --- | --- | --- | --- |
| 1 | Missing workload | **No error, no hang** — 0.47–0.65 s, exit 0, correct path | no timeout fallback needed |
| 2 | Release configuration | **Tracks exactly** — 0.57 s | config gains a `Configuration`, defaulting to Debug |
| 3 | Cold MSBuild node | **0.53 s — identical to warm** | no cold-start penalty; still cache |
| 4 | In-process `MSBuildLocator` | 30 ms register, 366 ms first, ~215 ms after | **not worth it** — spawn stays |
| 5 | Android | `RunCommand` is `"dotnet"`; APK path is **not evaluable** | Android needs its own rule |

### 1. Missing workload — settled, and better than hoped

Two probes: `net10.0-tizen` (no Tizen workload at all) and `net10.0-browser`
with `browser-wasm` (`wasm-tools` is in the SDK manifest and **not installed**).

Both returned the correct path, exit 0, in under 0.7 s:

```text
…\probe\wasm\bin\Debug\net10.0-browser\browser-wasm\Wasm.dll
```

Then `dotnet build` on the same project:

```text
error NETSDK1147: To build this project, the following workloads must be installed: wasm-tools
```

**Evaluation does not touch workloads.** `TargetPath` is property arithmetic;
the workload check lives in a target (`Microsoft.NET.Sdk.ImportWorkloads.targets`)
that evaluation never runs. So option A needs no hard timeout and no fallback to
option C for this reason.

The corollary is the caveat worth carrying: **a successful evaluation is not
evidence the project can build.** Resolution answers *where*, never *whether*.
When the file is absent, Presenter should say *"not built — expected at `<path>`"*
and offer the build, and it is the build that produces NETSDK1147 — a clean,
actionable error that we should surface verbatim rather than paraphrase.

### 2. Release — tracks, so name the configuration

```text
-p:Configuration=Release → …\bin\Release\net10.0-windows10.0.19041.0\win-x64\…exe
```

So the config needs a `Configuration` field (default `Debug`) or Presenter needs
a Debug/Release switch. Leaning toward a `Configuration` row in `Runtime`,
because a workspace that only ever runs Release is a property of the workspace,
not of the person looking at it — with a session-level override in the toolbar
for the person who needs the other one today.

This also retires an option-B weakness properly: the name search ranks `bin/Debug`
above everything, so on a Release-only machine it ranks that machine's only build
*last*. Evaluation has no such bias.

### 3. Cold node — there is no cold node

After `dotnet build-server shutdown` (which reported both the MSBuild server and
the VB/C# compiler server shutting down), the next evaluation took **0.53 s** —
inside the spread of the warm runs (0.55, 0.55, 0.58 s).

Expected in hindsight: `-getProperty` with no target spawns no worker nodes, so
there is no node pool to be cold. The ~0.5 s is `dotnet` host startup plus SDK
resolution plus evaluation, every time.

That is fast enough to run on demand and too slow to run on every keystroke.
Resolution must be async, cached per (project, TFM, configuration), invalidated
on the project's write time — as designed, now with a number behind it.

### 4. In-process MSBuild — measured, and rejected

A prototype console app using `MSBuildLocator.RegisterDefaults()` and
`Project.FromFile`, against the Todo MAUI app:

```text
register: 30 ms (.NET Core SDK 10.0.401)
first : 366 ms
second: 212 ms
third : 223 ms
```

Identical `TargetPath` and `RunCommand` to the spawn. So in-process is ~2.5×
faster per repeat call — 215 ms against 550 ms — and that is the whole prize.

Not worth what it costs:

- `Microsoft.Build` must be referenced with `ExcludeAssets="runtime"`, and
  `MSBuildLocator.RegisterDefaults()` must run **before any MSBuild type loads**.
  In a MAUI app with a generated entry point, that ordering constraint is a real
  trap and an unpleasant class of startup bug.
- It pins Presenter to an SDK it can find and load, in-process, forever.
- With caching, the saved 335 ms happens a handful of times per session.

**Spawn `dotnet msbuild`, cache the answer.** Revisit only if a workspace with
many projects makes resolution visibly slow.

### 5. Android — `RunCommand` is a Windows-only answer

On `net10.0-android` for the same project:

```text
TargetPath  → …\bin\Debug\net10.0-android\Brinell.Samples.Todo.App.dll
RunCommand  → dotnet
OutputPath  → bin\Debug\net10.0-android\
ApplicationId → com.brinell.samples.todo
ApkFileIntermediate / ApkFileSigned / AndroidApplicationPackageName → "" (empty)
```

The APK path properties are **empty at evaluation** — they are set inside
targets. What *is* evaluable is `ApplicationId` and `OutputPath`, and the built
APKs in this tree confirm the convention:

```text
samples/Brinell.Samples.Maui.App/bin/Debug/net10.0-android/com.brinell.samples.maui-Signed.apk
```

So `$(OutputPath)$(ApplicationId)-Signed.apk` reconstructs it, by convention
rather than by MSBuild — the one place where the "ask the project" approach
degrades into the same guessing it was meant to replace.

`UatTargetRegistry` has no Android entry, so nothing is blocked today. But the
resolution rule must be **per target**, not one global `RunCommand` lookup:
`RunCommand` for desktop targets, `ApplicationId` + convention for Android, and
for Android the thing a driver actually wants is usually the package id and not a
file path at all.

### Still unknown

- **A machine without the .NET SDK at all.** Presenter is an SDK-built MAUI app,
  so this is close to hypothetical — but it is the one case where option C
  (project XML + scoped glob) earns its keep as a fallback. Untested.
- **A workspace with many projects.** All timings are single-project. Resolving
  six rows serially is ~3 s; parallelise or resolve lazily per field.
- **Non-SDK-style or non-.NET apps under test.** Literal paths must stay legal
  for these; no experiment needed, just a rule not to break.

## Reproducing the measurements

From `Brinell/`:

```powershell
# path resolution, single-TFM and multi-TFM
dotnet msbuild srcnew\Brinell.Uat\Brinell.Uat.csproj -p:TargetFramework=net10.0 -getProperty:TargetPath -getProperty:TargetFrameworks
dotnet msbuild samples\Todo\src\Brinell.Samples.Todo.App\Brinell.Samples.Todo.App.csproj -p:TargetFramework=net10.0-windows10.0.19041.0 -getProperty:TargetPath -getProperty:RunCommand -getProperty:RuntimeIdentifier

# experiment 2 - Release
dotnet msbuild samples\Todo\src\Brinell.Samples.Todo.App\Brinell.Samples.Todo.App.csproj -p:TargetFramework=net10.0-windows10.0.19041.0 -p:Configuration=Release -getProperty:TargetPath -getProperty:RunCommand

# experiment 3 - cold node
dotnet build-server shutdown
Measure-Command { dotnet msbuild samples\Todo\src\Brinell.Samples.Todo.App\Brinell.Samples.Todo.App.csproj -p:TargetFramework=net10.0-windows10.0.19041.0 -getProperty:RunCommand }

# experiment 5 - Android
dotnet msbuild samples\Todo\src\Brinell.Samples.Todo.App\Brinell.Samples.Todo.App.csproj -p:TargetFramework=net10.0-android -getProperty:TargetPath -getProperty:RunCommand -getProperty:OutputPath -getProperty:ApplicationId -getProperty:ApkFileSigned

# tree-walk cost
Measure-Command { Get-ChildItem -Recurse -Filter Brinell.Core.dll | Where-Object FullName -notmatch '\\obj\\' }
```

Experiment 1 needs a throwaway project targeting a workload you do not have
(`net10.0-browser` with `<RuntimeIdentifier>browser-wasm</RuntimeIdentifier>`
works if `wasm-tools` is not installed): evaluate it, then `dotnet build` it, and
compare.

Two mechanics worth knowing before writing the parser:

- `-getProperty` needs no `-t:` target. MSBuild evaluates and prints, running no
  build — which is why it is fast and why it ignores workloads.
- **One** `-getProperty` prints the bare value; **several** print a JSON object.
  Always pass at least two, or handle both shapes.
