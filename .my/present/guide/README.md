# Minimal Presenter Run

Status: Current
Date: 2026-09-22
Area: `srcnew/Brinell.Presenter`, `testsnew/*.Uat.Tests`
Related:

- [Presenter platform guide](../../../docs/platform-guides/presenter.md) — settings, targets, execution
- [Presenter development plan](../09%20brinell%20presenter%20development%20plan.md)
- [UAT markdown grammar](../02%20uat%20markdown%20grammar.md) — what a `.uat.md` file may contain
- [Running the Todo app in Presenter](todo-app-presenter-setup.md) — the richer demo workspace, and the three config fields it needs first

---

## What "minimal" means here

Presenter is a MAUI desktop shell that loads a **workspace folder**, binds its
scenarios to page objects in a built assembly, and steps through them against a
running app. The smallest useful run therefore needs three things built, not
one: Presenter, the app under test, and the page-object assembly.

All commands run from `Brinell/` on Windows, .NET 10 SDK with the MAUI workload
(Presenter itself is a MAUI app, whatever the workspace targets).

---

## 1. Launch Presenter

```powershell
dotnet run --project srcnew\Brinell.Presenter\Brinell.Presenter.csproj -f net10.0-windows10.0.19041.0
```

That alone opens the shell. On start it reopens `LastOpenedFolder`, or falls
back to `testsnew/Brinell.Maui.Uat.Tests` — so the first launch usually shows a
workspace you have not built yet. Diagnostics, not scenarios, is the expected
first screen.

## 2. Build the smallest workspace that actually runs

`testsnew/Brinell.Wpf.Uat.Tests` is the cheapest working target: one page-object
assembly, no Appium, no MAUI workload for the app under test.

```powershell
dotnet build samples\Brinell.Samples.Wpf.App\Brinell.Samples.Wpf.App.csproj
dotnet build testsnew\Brinell.Wpf.Uat.Tests\Brinell.Wpf.Uat.Tests.csproj
```

## 3. Run a scenario

The toolbar is icon-only — hover for tooltips.

1. **Open workspace folder** (folder icon) → `testsnew\Brinell.Wpf.Uat.Tests`.
2. **Validate workspace** (checkmark) → confirms config, assemblies and app path
   resolve. Fix anything it reports before running.
3. Select `Scenarios/home-page-visible.uat.md` → *Home page is visible* in the
   tree.
4. **Run** (play), or **Next step** to advance one step at a time. The **Delay**
   box paces execution for demoing.

---

## What a workspace minimally contains

A folder with `uat.config.md` and at least one `.uat.md` file. The config must
resolve:

| Field | Why it fails |
| --- | --- |
| `Target` | must be one of MAUI, WPF, WINFORMS, BLAZOR, HTML, STRIDE |
| `Fixture` | resolved **by reflection from the Pages assembly** — see the trap below |
| `AppPath` | must point at an existing executable |
| `Pages` assembly | must be built; `bin/` and `obj/` are otherwise ignored |

## The fixture-name trap

Presenter instantiates the fixture by name: it looks in the Pages assembly for a
non-abstract class with a parameterless constructor whose full name or name
matches `Fixture`, or matches `Fixture + "Fixture"`
([UatExecutionService.cs:191](../../../srcnew/Brinell.Presenter/Services/UatExecutionService.cs#L191)).

The xUnit runners do not — they take the fixture as a generic type argument and
treat the config's `Fixture` value as a string to *validate*. So a workspace can
pass its xUnit suite and still fail in Presenter.

`testsnew/Brinell.Maui.Uat.Tests` is exactly that case: its config says
`Fixture | Appium`, but `Brinell.Maui.UITests` contains `MauiFixture` and
`ShellFixture` — no `Appium` or `AppiumFixture`. Presenter will throw
*"Fixture 'Appium' was not found in Brinell.Maui.UITests."* Use the WPF
workspace above, or name a type that exists.

## If you do want the MAUI workspace

```powershell
dotnet build samples\Brinell.Samples.Maui.App\Brinell.Samples.Maui.App.csproj -f net10.0-windows10.0.19041.0
dotnet build testsnew\Brinell.Maui.UITests\Brinell.Maui.UITests.csproj -f net10.0-windows
```

and change `Fixture` in its `uat.config.md` to `MauiFixture` first.
