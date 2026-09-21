# 05 — Implementation review

Status: Review / complete
Date: 2026-09-21
Reviews: [04-implementation-plan.md](04-implementation-plan.md)
Area: `srcnew/Brinell.Core`, `srcnew/Brinell.Uat`, `srcnew/Brinell.Presenter`,
`testsnew/Brinell.Uat.Tests`, `.docs/`

## Verdict

**Delivered.** The goal the plan set out — *delete every hand-written phrase table* — is
met. The built-in UAT vocabulary is now declared by `[UatStep]` on `Brinell.Core` control
interfaces and discovered into every catalog by a single pass in `Brinell.Uat`. All three
former copies of the table are gone, the decision is recorded as AD-011, and the whole
`srcnew\Brinell.sln` builds green (20 pre-existing, unrelated warnings; none from this
change).

The implementation improved on the plan in one place (a shared command-id suffix across
projections) and diverged in one place that leaves a real, if small, coverage gap (the
Stage 0 snapshot oracle was replaced by consistency tests that cannot catch a whole verb
losing its attribute). Details below.

## Stage-by-stage

| Stage | Plan | State | Notes |
| --- | --- | --- | --- |
| 1 | Move `UatEffectiveStepKeyword`; add `UatStepAttribute` to Core | ✅ | Both in `Brinell.Core/Testing/`. `UatStepKeyword` correctly stayed in `Brinell.Uat`. |
| 2 | `[UatStep]` on the 13 control verbs; +2 members on `ITextControlObject`; fix `AndroidText` | ✅ | All 13 decorated across 6 interfaces. `AssertText`/`AssertTextContains` added to `ITextControlObject`. `AndroidText` satisfies them via its base (see Findings). |
| 3 | `UatCatalogBuilder`; runtime catalog = 3 page verbs + discovery | ✅ | `CreateCommandCatalog` reduced to the 3 intrinsic verbs plus one discovery call. |
| 4 | Spec catalog + Presenter use the shared projection | ✅ | `UatSpecCommandCatalog.RegisterDefault` discovers from `CoreStepTypes`; `UatWorkspaceService` calls `UatSpecCommandCatalog.CreateDefault()`. |
| 5 | Custom-verb test + one end-to-end MAUI scenario | ⚠️ | Custom-verb test present. End-to-end covered by existing `Brinell.Maui.Uat.Tests` scenarios, not a new Todo scenario (equivalent). |
| 6 | Record the decision | ✅ | `.docs/decisions/ad-011-uat-vocabulary-is-attribute-declared.md`. |

## What matches the plan well

- **One discovery pass, two projections.** `UatCatalogBuilder.DiscoverBindings` is the sole
  source; the runtime passes a `handlerFactory`, the spec passes `null`. This is exactly the
  plan's §3.1 shape and is what makes drift structurally impossible.
- **Dedup on `(keyword, phrase)`** via a `HashSet`, matching the plan's guard for verbs
  inherited through several interfaces.
- **Name-based invocation preserved.** `InvokeControlMethodAsync` gained
  `valueArgumentName`/`literalArgument` params and binds the single non-`{control}` capture
  positionally, per §2.3 — no `Value=` knob was added speculatively.
- **The three page verbs stayed engine-owned**, registered once each in the runtime and spec
  catalogs. This is the one thing that is *not* a table, and it was left alone correctly.
- **The Presenter's third table is gone**; it now delegates to the shared projection.

## Improvement over the plan

The plan suggested pinning ids like `CommandId = "Builtin.Control.SetText"` on the
attribute (§5). The implementation instead sets the *suffix* on the attribute
(`CommandId = "Control.SetText"`) and has `RegisterControlVerbs` prepend a per-projection
prefix (`"Builtin."` / `"Spec."`). This is cleaner: the attribute carries one stable suffix,
and `RuntimeAndSpecProjections_AgreeOnControlVerbs` can compare the two projections on that
shared suffix. Good call.

## Findings

### 1. The Stage 0 snapshot oracle was not implemented; the replacement is weaker

The plan opened with a snapshot of the *current* catalog as the behaviour-identical oracle
(Stage 0), so any later drift — including a verb silently losing its `[UatStep]` — would be
caught. The shipped tests are:

- `SpecCatalog_ControlVerbs_ComeFromCoreStepAttributes`
- `RuntimeAndSpecProjections_AgreeOnControlVerbs`
- `CustomControlVerb_OnAppControl_IsDiscovered`

The first two derive **both** sides of the comparison from the same `[UatStep]` attributes.
They prove the projections agree with each other and with Core — but if a maintainer deletes
`[UatStep]` from, say, `IToggleControlObject.Uncheck`, both sides drop the phrase together
and both tests stay green. There is no test asserting the expected phrase set (the plan's
"Core is the source: … finds all 13 built-in control phrases", which was meant to pin the
count). See [UatCatalogParityTests.cs](../../../testsnew/Brinell.Uat.Tests/UatCatalogParityTests.cs).

**Impact:** low-to-moderate. The MAUI UAT scenarios (below) exercise several verbs
end-to-end, so a dropped *common* verb would surface there. But a rarely used verb
(`should be unchecked`, `should be enabled`) could regress unnoticed.

**Recommendation:** add one test that pins the built-in vocabulary explicitly — either the
literal 13 `(keyword, phrase)` pairs, or at minimum `Assert.Equal(13, count)` over the
discovered Core bindings. This restores the guard the Stage 0 oracle was there to provide.

### 2. `AndroidText` satisfies the new interface members through its base, not directly

The plan (§2.2, and the §7 risk row) expected `AndroidText<TScope>` to *define* `AssertText`
and add `AssertTextContains`. In the code, `AndroidText` defines neither directly; the build
succeeds because `NativeAndroidControl<TScope>` (its base) supplies them. This is fine — the
contract is satisfied and the build proves it — but it means the plan's note "it already has
`AssertText`; add `AssertTextContains` if missing" was moot. Worth recording only so a future
reader does not go looking for these methods on `AndroidText` itself. No action needed.

### 3. End-to-end coverage is via existing scenarios, not a new Todo scenario

Stage 5 asked for "one MAUI UAT scenario (Todo 'needs a title')". No Todo scenario was added;
instead the existing `Brinell.Maui.Uat.Tests` scenarios already exercise the discovered
catalog end-to-end — e.g. `main-page-validation.uat.md` (`should contain`),
`main-page-greeting.uat.md` (`should contain`, `should be visible`),
`user-form-basic-input.uat.md`. These build and drive the real discovered vocabulary, which
satisfies the intent of the stage. The `.my/uat/todos/scenarios/*.uat.md` set (including
`05-create-needs-title.uat.md`) exists as authored specs but is not wired to a MAUI head here.
Acceptable; the intent is met.

## Verification

From the Brinell root:

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false          # succeeded, 20 unrelated warnings
dotnet test testsnew\Brinell.Uat.Tests\Brinell.Uat.Tests.csproj -v:minimal /nr:false   # green (exit 0)
```

`Brinell.Core`, `Brinell.Uat`, `Brinell.Uat.Tests`, `Brinell.Presenter`, and
`Brinell.Maui.Uat.Tests` all build. The end-to-end MAUI UI tier was not run as part of this
review (per AGENTS.md, the narrow change is falsifiable by the Uat unit tests and the
scenario build).

## Follow-ups

1. **(Recommended)** Add the missing vocabulary-pinning test (Finding 1) so a dropped
   `[UatStep]` fails a test rather than silently shrinking both projections.
2. **(Optional)** If a Todo MAUI head is intended as the canonical demo, wire
   `05-create-needs-title.uat.md` to it; otherwise note in the plan that the existing MAUI
   scenarios are the end-to-end proof.

Neither blocks closing the work. Marking [04-implementation-plan.md](04-implementation-plan.md)
delivered.
