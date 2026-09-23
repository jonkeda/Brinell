# Running the Todo app in Presenter

Status: Proposed — the workspace exists, three config fields block it
Date: 2026-09-22
Area: `samples/Todo/tests/Brinell.Samples.Todo.Uat`, `srcnew/Brinell.Presenter`
Related:

- [Minimal Presenter run](README.md) — the generic version of this, on the WPF sample
- [One Todo UAT, end to end](../../../samples/Todo/uat-walkthrough.md) — the scenario itself
- [Todo sample README](../../../samples/Todo/README.md) — the five test tiers
- [Presenter platform guide](../../../docs/platform-guides/presenter.md)

---

## The short version

You do not need to build a Todo UAT workspace — it is already there, at
`samples/Todo/tests/Brinell.Samples.Todo.Uat`, with four scenarios, a fixture
that launches the app hermetically, and a `uat.config.md`.

What you need is to **fix three fields in that config**. As written it runs
under `dotnet test` but cannot run in Presenter, because the two resolve the
fixture in completely different ways. One is a stale path; the other two are the
same mismatch the [WPF guide](README.md) describes.

---

## What already exists

| Piece | Where | State |
| --- | --- | --- |
| The app | `samples/Todo/src/Brinell.Samples.Todo.App` | MAUI, Windows + Android |
| Page objects | `samples/Todo/tests/…UITests/Pages/` | `TodoListPage`, `TodoDetailPage`, `TodoEditPage` |
| UAT workspace | `samples/Todo/tests/Brinell.Samples.Todo.Uat` | `uat.config.md` + 4 `.uat.md` scenarios |
| Fixture | `…Uat/Runtime/TodoUatFixture.cs` | seeds `three-todos`, starts WireMock, launches the app |

`TodoUatFixture` is a good Presenter fixture already: it has a public
parameterless constructor, and its base constructor calls `WaitForTheList()`, so
merely *constructing* it stands up the whole hermetic environment — seeded
SQLite, WireMock backend, app on screen. Presenter constructs the fixture and
gets the demo environment for free.

---

## The three blockers

Presenter instantiates the fixture by reflecting over **the Pages assembly**,
looking for a non-abstract class with a parameterless constructor whose name
matches `Fixture` or `Fixture + "Fixture"`
([UatExecutionService.cs:191](../../../srcnew/Brinell.Presenter/Services/UatExecutionService.cs#L191)).
Measured against that, the current config fails three times.

### 1. `Fixture | FlaUI` names no type

There is no `FlaUI` or `FlaUIFixture` class in the Todo sample. The concrete
fixture is `TodoUatFixture`.

### 2. `Pages` points at the wrong assembly

`Pages` is `Brinell.Samples.Todo.UITests.dll`, but `TodoUatFixture` is compiled
into `Brinell.Samples.Todo.Uat.dll`. Presenter searches only the Pages assembly,
so even the correct name would not be found there.

The concrete fixtures that *are* in `…UITests.dll` (`ThreeTodosFixture`,
`EmptyFixture`, …) are not substitutes: they lack the `[TestModuleScan]`
attribute and the `Composition` property the UAT runtime needs for page
discovery. `TodoUatFixture` exists precisely to add those two things.

Pointing `Pages` at the Uat assembly loses nothing — page discovery is driven by
the fixture's `[TestModuleScan(typeof(TodoListPage), …)]`, not by the Pages
assembly, and `…UITests.dll` sits in the same output folder as a dependency.

### 3. `AppPath` has a stale RID folder

The config says `win10-x64`. The SDK produces `win-x64` for this project — that
is the folder present under
`src/Brinell.Samples.Todo.App/bin/Debug/net10.0-windows10.0.19041.0/`, and it is
what the sibling `testsnew/Brinell.Maui.Uat.Tests` config uses. Worth
re-checking against your own build output, since this one has never been built
to completion here.

---

## The fix

In `samples/Todo/tests/Brinell.Samples.Todo.Uat/uat.config.md`:

```diff
 | Target | MAUI |
-| Fixture | FlaUI |
-| AppPath | ../../src/Brinell.Samples.Todo.App/bin/Debug/net10.0-windows10.0.19041.0/win10-x64/Brinell.Samples.Todo.App.exe |
+| Fixture | TodoUatFixture |
+| AppPath | ../../src/Brinell.Samples.Todo.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Brinell.Samples.Todo.App.exe |
 | WorkingDirectory | ../../../.. |

 ## Assemblies

 | Kind | Assembly |
 | --- | --- |
-| Pages | ../Brinell.Samples.Todo.UITests/bin/Debug/net10.0-windows/Brinell.Samples.Todo.UITests.dll |
+| Pages | bin/Debug/net10.0-windows/Brinell.Samples.Todo.Uat.dll |
 | Controls | ../../../../srcnew/Brinell.Maui/bin/Debug/net10.0/Brinell.Maui.dll |
 | Commands | ../../../../srcnew/Brinell.Uat/bin/Debug/net10.0/Brinell.Uat.dll |
```

Everything else in the file — `WorkingDirectory`, `Controls`, `Commands`,
`Discovery`, `Reporting`, `Settings` — is already correct.

Changing `Fixture` does not break `dotnet test`: `TodoUatScenarioTests` takes its
fixture as the generic argument `UatScenarioTestBase<TodoUatFixture>` and only
*validates* the config string. Update its `RuntimeValidation` to match:

```diff
 protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
-    new(Target: "MAUI", Fixture: "FlaUI");
+    new(Target: "MAUI", Fixture: "TodoUatFixture");
```

---

## Then run it

From `Brinell/`:

```powershell
dotnet build samples\Todo\src\Brinell.Samples.Todo.App\Brinell.Samples.Todo.App.csproj -f net10.0-windows10.0.19041.0
dotnet build samples\Todo\tests\Brinell.Samples.Todo.Uat\Brinell.Samples.Todo.Uat.csproj
dotnet run --project srcnew\Brinell.Presenter\Brinell.Presenter.csproj -f net10.0-windows10.0.19041.0
```

In Presenter (the toolbar is icon-only — hover for tooltips):

1. **Open workspace folder** → `samples\Todo\tests\Brinell.Samples.Todo.Uat`.
   Open the **source** folder, not `bin\Debug\…`; the relative paths above are
   written from there.
2. **Validate workspace** → config, assemblies and app path resolve.
3. Pick `Scenarios/create-a-todo.uat.md` → *A saved todo appears in the list*.
   It is the best one to demo: Add → type a title → Save → the row appears.
4. **Run**, or **Next step** to walk it a line at a time. Set **Delay** to
   ~500 ms so the audience can follow.

---

## Two things to know before demoing

**Every Run relaunches the app.** Presenter constructs the fixture per
execution, and that constructor seeds the database, starts WireMock and launches
the app. State is therefore clean between runs — but there are a few seconds of
startup each time. Start the run before you start talking.

**Presenter does not call `BeforeScenario`.** The xUnit class resets per
scenario through `BeforeScenario → Fixture.StartTest()`; Presenter's execution
path has no such hook. In practice the per-run fixture gives you the same clean
state, so this matters only if you later add a Presenter feature that reuses one
fixture across scenarios.

---

## Why the test suite does not catch any of this

`dotnet test` never exercises Presenter's fixture lookup. The xUnit base class
receives the fixture as a type argument and treats the config's `Fixture` value
as a string to validate against. Presenter resolves it by reflection over the
Pages assembly.

So a UAT workspace can be green in CI and still be unopenable in Presenter.
`testsnew/Brinell.Maui.Uat.Tests` has the identical defect (`Fixture | Appium`,
no such type). If Presenter is going to be a first-class way to run these
workspaces, a test that asserts *"every `uat.config.md` names a fixture type
that exists in its own Pages assembly"* would catch the whole class at once —
worth considering as a follow-up.
