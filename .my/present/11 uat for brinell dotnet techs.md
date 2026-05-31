# UAT For Brinell .NET Techs

Status: implemented first pass.

This plan adds first-class Markdown UAT coverage across the Brinell .NET technology adapters. The goal is not to duplicate every UI test. The goal is to make each supported app technology runnable from the same `.uat.md` grammar, the same reflection runtime, and the same Presenter workflow.

## Goal

A tester or agent should be able to open a workspace for any supported Brinell .NET tech, select a Markdown scenario, press `Run` or `Next`, and see the same execution model:

- parse `.uat.md`
- bind phrases
- discover pages and controls
- launch or connect to the AUT
- execute steps
- report timing and diagnostics
- dispose the AUT cleanly

## Current Baseline

Existing Brinell technology projects:

- `Brinell.Maui`
- `Brinell.Wpf`
- `Brinell.WinForms`
- `Brinell.Blazor`
- `Brinell.Html`
- `Brinell.Html.Playwright`
- `Brinell.Stride`
- `Brinell.Uat`
- `Brinell.Presenter`

Existing UAT shape:

- `Brinell.Uat` owns Markdown parsing, binding, reflection runtime, and generic phrase execution.
- `Brinell.Maui.Uat.Tests` proves the first `.uat.md` flow.
- `Brinell.Presenter` can load UAT workspaces and execute target-aware sessions.
- STRIDE UAT is now proven through Wair-specific UAT, but a generic Brinell STRIDE UAT sample still belongs in Brinell.

## Target Tech Matrix

| Target | Driver shape | Initial test project | Launch model |
| --- | --- | --- | --- |
| `MAUI` | MAUI page objects over Appium/FlaUI helpers | `Brinell.Maui.Uat.Tests` | process/app package |
| `WPF` | WPF controls over UI Automation/FlaUI | `Brinell.Wpf.Uat.Tests` | process |
| `WINFORMS` | WinForms controls over UI Automation/FlaUI | `Brinell.WinForms.Uat.Tests` | process |
| `BLAZOR` | component/browser page objects | `Brinell.Blazor.Uat.Tests` | web host + browser |
| `HTML` | Playwright page objects | `Brinell.Html.Uat.Tests` | static/server + browser |
| `STRIDE` | Stride named-pipe automation context | `Brinell.Stride.Uat.Tests` | process + pipe |

Target names should be stable, uppercase, and accepted by Presenter config validation.

## Shared UAT Contract

Every tech should support the same workspace shape:

```markdown
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | WPF |
| Fixture | SampleWpfUatFixture |
| AppPath | ../../samples/Brinell.Samples.Wpf.App/bin/Debug/net10.0-windows/Sample.exe |
| WorkingDirectory | ../.. |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Brinell.Wpf.Uat.Tests.dll |
```

Common built-in phrases must work where the tech can reasonably support them:

- `Given I am on the {page} page`
- `Then I should be on the {page} page`
- `When I tap {control}`
- `When I enter {value} into {control}`
- `When I set {control} to {value}`
- `When I clear {control}`
- `When I check {control}`
- `When I uncheck {control}`
- `When I select {value} from {control}`
- `Then {control} should contain {value}`
- `Then {control} should equal {value}`
- `Then {control} should be visible`
- `Then {control} should be enabled`
- `Then {control} should be checked`
- `Then {control} should be unchecked`
- `Then {control} should have selected {value}`
- `Then I should see {text}`

Custom `[UatPhrase]` methods remain the extension point for tech-specific or app-specific behavior.

## Architecture Direction

Keep `Brinell.Uat` generic. It should not know MAUI, WPF, WinForms, Blazor, HTML, or STRIDE details.

Presenter should depend on target-neutral execution services:

```text
IUatTargetRuntime
  TargetName
  CreateEnvironment(config, appPath, workspace)
  CreateFixture(config, loadedAssemblies)
  DescribeLaunch()
  Dispose()
```

The first implementation can stay simple and internal to Presenter, but the model should point toward pluggable target runtimes.

Technology packages own:

- app launch/connect behavior
- page object base classes
- control wrappers
- wait semantics
- screenshots where applicable
- technology-specific diagnostics

UAT projects own:

- sample app build/reference
- `uat.config.md`
- page objects
- fixtures
- `.uat.md` scenarios
- tests that run all scenario files

## Phase 1: Target Model And Presenter Validation

Work items:

- Replace scattered target string checks with a small target registry.
- Accept `MAUI`, `WPF`, `WINFORMS`, `BLAZOR`, `HTML`, and `STRIDE`.
- Map target-specific app path environment variables only where needed.
- Preserve `WorkingDirectory`.
- Add validation diagnostics for unsupported targets.
- Show target and fixture clearly in the `Config` tab.

Acceptance:

- Presenter can load configs for all target names.
- Unsupported targets fail with a readable diagnostic.
- Existing MAUI and STRIDE flows remain green.

## Phase 2: Generic UAT Test Harness Template

Work items:

- Create a reusable scenario test pattern for Brinell tech UAT projects.
- Keep the xUnit theory over `Scenarios/**/*.uat.md`.
- Standardize failure output:
  - parse diagnostics
  - bind diagnostics
  - runtime discovery
  - command catalog
  - step results
  - target diagnostics
- Add a short README/template for new tech UAT projects.

Acceptance:

- A new tech UAT project can be scaffolded by copying the template and replacing fixture/page objects.

## Phase 3: MAUI UAT Hardening

Work items:

- Treat `Brinell.Maui.Uat.Tests` as the reference implementation.
- Align page object names and automation names with the shared phrase catalog.
- Ensure Presenter can run the MAUI sample workspace from recent folders.
- Keep delay/timing Presenter tests around the MAUI greeting scenario.

Acceptance:

- MAUI UAT stays the green reference gate.
- Presenter delay and run-scope tests continue to use MAUI sample scenarios.

## Phase 4: WPF UAT

Work items:

- Add `Brinell.Wpf.Uat.Tests`.
- Build or reuse a small WPF sample AUT.
- Add page objects for:
  - main page/window
  - text entry
  - button
  - label
  - checkbox
  - selection control
- Add scenarios matching the MAUI greeting and form flows.
- Add Presenter config validation for `Target | WPF |`.

Acceptance:

- WPF scenarios pass through direct xUnit UAT.
- Presenter can load the WPF workspace and create a run session.

## Phase 5: WinForms UAT

Work items:

- Add `Brinell.WinForms.Uat.Tests`.
- Build or reuse a small WinForms sample AUT.
- Add page objects for the same baseline controls.
- Cover greeting and form scenarios.
- Capture WinForms-specific diagnostics for missing controls and disabled controls.

Acceptance:

- WinForms scenarios pass through direct xUnit UAT.
- Built-in phrases behave the same as MAUI/WPF where controls support them.

## Phase 6: Blazor UAT

Work items:

- Add `Brinell.Blazor.Uat.Tests`.
- Decide whether the driver uses Playwright directly or a Brinell Blazor wrapper over HTML controls.
- Launch a test web host with deterministic port assignment.
- Add page objects for routed pages and components.
- Cover greeting, form input, navigation, and validation scenarios.

Acceptance:

- Blazor UAT can start/stop its host cleanly.
- Browser/page diagnostics are captured on failure.
- Scenarios do not rely on arbitrary sleeps.

## Phase 7: HTML/Playwright UAT

Work items:

- Add `Brinell.Html.Uat.Tests`.
- Use `Brinell.Html.Playwright` as the first browser driver.
- Support static HTML and hosted HTML examples.
- Add screenshot-on-failure as optional diagnostics.
- Cover the shared greeting and form scenario set.

Acceptance:

- HTML UAT proves the grammar works for browser-only apps.
- Playwright artifacts are useful but do not make normal runs noisy.

## Phase 8: STRIDE UAT

Work items:

- Add `Brinell.Stride.Uat.Tests`.
- Create or reuse a minimal STRIDE sample AUT with named automation controls.
- Keep Wair-specific behavior outside Brinell.
- Prove page/control discovery, custom phrases, app launch, pipe connection, and disposal.
- Add Presenter session coverage for `Target | STRIDE |`.

Acceptance:

- Generic STRIDE UAT passes without depending on Wair.
- Wair remains a downstream consumer and heavier real-world regression suite.

## Phase 9: Shared Samples And Scenario Parity

Each tech should have the same baseline scenario names where possible:

- `main-page-greeting.uat.md`
- `main-page-validation.uat.md`
- `user-form-basic-input.uat.md`
- `missing-control-diagnostics.uat.md`

Parity matters because it makes adapter differences obvious. The same scenario wording should bind across techs unless a tech cannot support the control type.

Acceptance:

- The same Markdown scenario set runs across at least MAUI, WPF, WinForms, Blazor, HTML, and STRIDE where practical.
- Any phrase gap is documented as unsupported with a reason.

## Phase 10: Presenter Tech Workspaces

Work items:

- Add sample workspace discovery for all Brinell tech UAT projects.
- Let Presenter open any tech workspace from recent folders.
- Show target-specific launch diagnostics.
- Keep AUT placement best-effort and target-aware.
- Add Presenter UAT tests that load each workspace and validate tree/config/command catalog.

Acceptance:

- Presenter is a real manual UAT runner for all supported Brinell techs.
- Direct UAT projects remain suitable for CI/local regression.

## Implementation Notes

Implemented in the first pass:

- Presenter target registry accepts `MAUI`, `WPF`, `WINFORMS`, `BLAZOR`, `HTML`, and `STRIDE`.
- Direct UAT projects now exist for WPF, WinForms, Blazor, HTML, and STRIDE, alongside the existing MAUI UAT project.
- Blazor and HTML UAT projects start the sample Blazor app on a free local port, wait for HTTP readiness, and dispose the host after each run.
- STRIDE automation now honors the `--pipe` argument through default automation options, so unique test pipe names work without app-specific parser duplication.
- UAT name inference now discovers generic controls and readable display names such as `Greeting`, `Error`, and `Count`.
- HTML/Playwright exposes the page and element evaluation APIs needed by Blazor controls, and HTML async helpers are aligned with concrete text and label controls.

Current direct UAT coverage:

- MAUI greeting/reference scenarios.
- WPF home and login scenarios.
- WinForms login/form input scenario.
- Blazor counter and invalid-login scenarios.
- HTML counter and form-control scenarios.
- STRIDE greeting scenario.

## Test Gates

Direct UAT gates:

```powershell
dotnet test testsnew/Brinell.Maui.Uat.Tests/Brinell.Maui.Uat.Tests.csproj -v:minimal /nr:false
dotnet test testsnew/Brinell.Wpf.Uat.Tests/Brinell.Wpf.Uat.Tests.csproj -v:minimal /nr:false
dotnet test testsnew/Brinell.WinForms.Uat.Tests/Brinell.WinForms.Uat.Tests.csproj -v:minimal /nr:false
dotnet test testsnew/Brinell.Blazor.Uat.Tests/Brinell.Blazor.Uat.Tests.csproj -v:minimal /nr:false
dotnet test testsnew/Brinell.Html.Uat.Tests/Brinell.Html.Uat.Tests.csproj -v:minimal /nr:false
dotnet test testsnew/Brinell.Stride.Uat.Tests/Brinell.Stride.Uat.Tests.csproj -v:minimal /nr:false
```

Presenter gate:

```powershell
dotnet test testsnew/Brinell.Presenter.Uat.Tests/Brinell.Presenter.Uat.Tests.csproj -v:minimal /nr:false
```

Core gate:

```powershell
dotnet test testsnew/Brinell.Uat.Tests/Brinell.Uat.Tests.csproj -v:minimal /nr:false
```

## Risks

- WPF and WinForms control automation may expose different accessibility trees for the same visual control.
- Blazor/HTML need host lifecycle cleanup so ports and browser processes do not leak.
- STRIDE startup is heavier than normal desktop UI tests and needs robust pipe diagnostics.
- Presenter must avoid becoming a switch statement full of tech-specific launch hacks.
- Screenshot artifacts are useful, but they can make simple UAT runs noisy if enabled by default.
- Cross-tech scenario parity may expose real naming differences; fix page object aliases before changing the Markdown wording.

## Non-Goals

- Do not build a full Cucumber clone.
- Do not require every tech to support every possible control phrase before shipping a first UAT slice.
- Do not move app-specific commands into generic Brinell packages.
- Do not add arbitrary sleeps to hide synchronization gaps.
- Do not make Presenter own technology-specific control logic.

## Acceptance

- `11` exists as the planning document for adding UAT to Brinell .NET techs.
- Every supported Brinell .NET tech has a target name, planned UAT project, launch model, and scenario parity target.
- The plan keeps `Brinell.Uat` generic and pushes tech behavior into tech adapters/tests.
- The plan defines direct test gates and Presenter gates.
- The plan explicitly covers MAUI, WPF, WinForms, Blazor, HTML/Playwright, and STRIDE.
