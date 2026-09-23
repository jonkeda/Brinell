# Construction UI test migration - plan

Goal: grow the UI test suite that already lives in the **Construction** repo
(`E:\repos\MainConstruction\construction`) into full per-module coverage, backed by
generatable ControlObjects for every custom Construction control the tests touch. Everything
stays in the Construction repo; **Brinell is referenced as a NuGet package set**
(`eng/brinell/Brinell.references.props` + the `IncludeBrinell*` flags), never copied. The
`Todos` module is the PoC: it lands first, end-to-end, and locks the pattern the other
modules follow.

Follows the shape of [.my/communitytoolkit/plan.md](../communitytoolkit/plan.md): current
state → inventory → phases with progress.

## Read this first

| File | What it contains |
| --- | --- |
| [stepguide.md](stepguide.md) | Ordered, per-phase steps: exactly what to add, build and run. Start here when working. |
| [controls.md](controls.md) | Every ControlObject the migration produces, with priority and state. |
| [modules.md](modules.md) | Every Construction module, its tests, its target folder, and priority. |
| [layout.md](layout.md) | The exact file/folder layout inside the existing Construction projects. |

Related references:

- [.my/TodoApp/plan.md](../TodoApp/plan.md) - showcase test pyramid the module suites mirror.
- Brinell rules (this repo, referenced from Construction): [AGENTS.md](../../AGENTS.md),
  `.github/copilot-instructions.md`, `docs/architecture/decisions.md` (AD-004 platform
  neutrality, AD-005 no physical input on MAUI Windows, AD-008 bridge admission).
- Skills (in `Brinell/.github/skills/`): [maui-control](../../.github/skills/maui-control/SKILL.md),
  [maui-ui-test](../../.github/skills/maui-ui-test/SKILL.md),
  [convert-control](../../.github/skills/convert-control/SKILL.md).

Status: **infrastructure in place, Todos not started**. The Construction repo already has the
ControlObject library, the test host with Auth/fixtures/mock backend, the 8 seed controls,
and `OverviewPagePatternObject`. No Todos ControlObjects, page objects, or tests exist yet.

---

## Current state

### Construction repo (`E:\repos\MainConstruction\construction`)

| Item | Location | Notes |
| --- | --- | --- |
| Construction app | `Exact.Construction/` | MAUI app. 25 modules under `Modules/`. |
| Custom control library | `Exact.Core.Construction/Controls/` | Search, Loading, Messaging, HeaderActions, TitleBar, FormInputs, FormSections, Lists, Favorites, FilterSheet, Cells, Collections, DisplayFields, QuickActions, RichContent, RichEditor, Settings. |
| Page pattern library | `Exact.Core.Construction/PagePatterns/` | `OverviewPagePattern`, `DetailPagePattern`, `EditPagePattern`. |
| **ControlObject library** | `Exact.Core.Construction.UITest/` | **Exists.** `Controls/` holds the 8 seed controls; `PagePatterns/OverviewPagePatternObject.cs` is done. References Brinell via the shared props. |
| **Test host project** | `Exact.Construction.UITests/` | **Exists.** xUnit + Brinell: `ConstructionFixture`, `ConstructionTestBase`, `ConstructionCollection`, `Auth/`, `MockBackend/` (WireMock), `Diagnostics/`, `Configuration/`, `TestData/`, `TestSettings/`. Brinell pulled in through `eng/brinell/Brinell.references.props`. |
| Loose test-only controls | `Exact.Construction.UITests/Controls/` | `RoundButtonControl`, `SearchOverlay`, `TabMenuItemControl`, `ToggleRow` - hand-written, need conversion. |
| Test modules present | `Exact.Construction.UITests/Modules/` | Authentication, Contacts (List + Add), Dashboard, Startup. **No Todos tests today.** |

### How Brinell is consumed

Construction references Brinell through NuGet packages, wired by
`Exact.Construction.UITests.csproj`:

```xml
<IncludeBrinellHtml>true</IncludeBrinellHtml>
<IncludeBrinellCommunityToolkit>true</IncludeBrinellCommunityToolkit>
<IncludeBrinellFlaUI>true</IncludeBrinellFlaUI>
...
<Import Project="..\eng\brinell\Brinell.references.props" />
```

Nothing from Brinell is copied into Construction, and nothing Construction-specific is added
to the Brinell repo. Work in the Construction repo; treat Brinell as a package dependency.

### Gaps

- No Todos ControlObjects, page objects, or tests exist anywhere - Todos is the PoC and is
  built from scratch into the existing projects ([controls.md](controls.md) T4-T11,
  [modules.md](modules.md) Todos).
- The 4 loose test-only controls need conversion to `.tpl.cs`+`.gen.cs`
  ([controls.md](controls.md) C6-C9).
- `DetailPagePatternObject` and `EditPagePatternObject` are not written yet
  ([controls.md](controls.md) P2, P3/T4).

---

## Phases at a glance

Detailed steps in [stepguide.md](stepguide.md); per-phase progress in
[controls.md](controls.md) and [modules.md](modules.md).

| Phase | Scope | Exit gate | State |
| --- | --- | --- | --- |
| 0 - Baseline | Confirm library + test host + 8 seeds + Overview pattern build. Nothing to create. | `Construction.sln` builds green. | - |
| 1 - Todos PoC controls | T4-T10: EditPagePatternObject, FormContainer, FormSection, Text/Note/DateTime/Selection form inputs, added to `Exact.Core.Construction.UITest`. | Library builds; unit tests green. | - |
| 2 - Todos PoC tests | `Exact.Construction.UITests/Modules/Todos` with 3 smoke classes against WireMock, reusing the existing Auth login. | 3 smoke classes green on Windows. **PoC done.** | - |
| 3 - P1 modules | Authentication, Startup, Dashboard, Contacts. Convert C6-C9. | All 4 modules green; no loose controls left. | - |
| 4 - P2 library controls | FilterSheet, QuickActions, Cells, DisplayFields on demand. | Every P2 control a module touches is done + unit-tested. | - |
| 5 - P2 module suites | Projects, Hours, Journal, Materials, Purchases, Equipment, Employees, Files. | Each module has ≥1 smoke + 1 flow test, green. | - |
| 6 - P3 controls and modules | Rich*, Settings, and remaining modules as they're picked up. | Opened only after phase 5. | - |
| 7 - Android | Todos first, then others as the Android baseline (Selection/DateTime) recovers. | Windows Todos suite stable for a week. | - |

---

## Rules the migration follows

Called out because they are easy to break in a copy-paste job:

- **No physical input on MAUI Windows.** Every interaction goes through a UI Automation
  pattern or the gesture bridge (AD-005, AD-008). Never expose raw pointer moves.
- **No `Context.Driver`, `IMauiElement`, `FindElement`, or `Locator` construction inside a
  test method.** See [forbidden APIs](../../.github/skills/maui-ui-test/references/forbidden-apis.md).
  Helpers that reach for the driver stay in ControlObjects / page objects, not in test classes.
- **Reuse the existing Auth.** Login goes through `Exact.Construction.UITests/Auth/`
  (`ConstructionAuthenticationManager`, `Auth/Pages/`). Do not write a new login path.
- **One test project.** All module tests live in the existing `Exact.Construction.UITests`
  project under `Modules/<Module>/`. No per-module `.csproj`.
- **No back-compat.** Per user preference, don't preserve shapes that fight the target
  layout. Add cleanly.
- **One UI test process at a time.** Never run two Construction module suites in parallel.
- **Run scope matches the change.** After a control change, run only the modules that
  reference it. Full runs only at phase gates.
- **Rebuild the Construction app when the app changed.** `dotnet test` builds the test
  project, not the app it launches.

---

## Verification commands

From the Construction repo root (`E:\repos\MainConstruction\construction`):

```powershell
# Baseline build (every phase)
dotnet build Construction.sln -v:minimal /nr:false

# ControlObject library only (after phase 1)
dotnet build Exact.Core.Construction.UITest\Exact.Core.Construction.UITest.csproj -v:minimal /nr:false

# Todos PoC / any module - filter to the module's tests
dotnet test Exact.Construction.UITests\Exact.Construction.UITests.csproj `
  --filter "Module=Todos" -v:minimal /nr:false
```

Rebuild the app before a UI run if `Exact.Construction` changed:

```powershell
dotnet build Exact.Construction\Exact.Construction.csproj -f net10.0-windows10.0.19041.0
```

---

## Open questions

Track here; do not block phase 0 on them.

1. **Where do the Construction app binaries live for the tests to launch?** Resolved by the
   existing `brinell.maui.config.json` / `construction.maui.config.json` in the test project.
   No change needed.
2. **Does a Construction-flavoured `EditPagePattern` need an app-side test host?** The XAML
   is a `ContentView` deriving from `EditPagePattern`; a page-object base is enough.
   Confirmed during phase 1 T4.
3. **Filter sheet as a modal**: `FilterSheetControl` needs a root scope that walks the
   modal window, not the page. Confirm on the first module that uses it (Contacts, phase 3)
   before writing C11 in phase 4.
