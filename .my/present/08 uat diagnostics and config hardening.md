# UAT Diagnostics And Config Hardening

Status: Delivered — every item in the Done Definition is met
Date: 2026-07-07, status pass 2026-09-21
Area: `srcnew/Brinell.Uat`, `srcnew/Brinell.Presenter`
Related:

- [Runner code binding](03%20runner%20code%20binding.md) — where command ids come from
- [UAT phrases and flows](../../docs/guides/uat-phrases-and-flows.md) — authoritative phrase list
- [Presenter current UI and execution design](10%20presenter%20tabbed%20tree%20redesign.md) — where these reports surfaced

This document captures the first hardening slice after the MAUI UAT proof ran successfully.

> **Delivered (2026-09).** This is the doc in the set that aged best: both report
> formats below match the shipped output line for line, and the expected-failure
> pattern shipped as designed. The one change is that command **ids** are no longer
> chosen in a central table — they come from `[UatStep]`, as noted in §Command
> Catalog Report.

## Goal

Make UAT failures useful before building the MAUI runner UI.

The runner should not only say that a step failed. It should explain what the UAT engine discovered, what command was selected, what page/control was active, and where the failing Markdown step came from.

## Config Is Runtime Input

Each UAT project should include a folder-local `uat.config.md`.

The runtime should load it before parsing or executing scenarios. For the MAUI proof, the config declares:

- `Target = MAUI`
- `Fixture = Appium`
- page assemblies
- control assemblies
- command/runtime assemblies
- discovery settings

The first MAUI implementation still receives the real `AppiumFixture` from xUnit, but it validates the config and treats it as the runtime profile. Later, the MAUI runner UI can use the same file to decide which target, fixture, and assemblies to load.

> **Delivered (2026-09).** Eight `uat.config.md` files exist — one per UAT test
> project (MAUI, WPF, WinForms, Blazor, HTML, STRIDE, Presenter) plus the Todo
> sample. `Target` and `Fixture` are declared exactly as described, and validation
> is real: `UatTargetRegistry` rejects any target outside `MAUI`, `WPF`,
> `WINFORMS`, `BLAZOR`, `HTML`, `STRIDE` with a diagnostic naming the supported
> set, and `UatRuntimeValidationOptions` lets a test assert the pair it expects.
>
> The "later" also happened: the Presenter reads the same file to pick target,
> fixture and assemblies, which is what made it a runner for all six technologies
> rather than a MAUI tool.
>
> The file grew three sections beyond this list — `Reporting`, `Settings` and
> `Skip Rules` (tag → environment variable, for opting out of hardware or
> live-API scenarios). Full shape in
> [03](03%20runner%20code%20binding.md) §`uat.config.md`.

## Discovery Report

The UAT runtime should be able to report discovered pages and controls.

Example shape:

```text
Discovered UAT pages:
- Main: Name, Email, Greet, Greeting, Counter
- User Form: First Name, Last Name, Email, Terms, Country, Submit
```

This report should appear when:

- binding fails
- execution fails because a page is missing
- execution fails because a control is missing
- the runner UI shows a binding preview

> **Delivered (2026-09), format unchanged.** This is
> `UatReflectionRuntime.DescribeDiscovery()`, surfaced as
> `UatRuntime.DiscoveryReport` — pages sorted by name, controls sorted within
> each page, and `(no controls)` where a page has none. It is appended by
> `UatDiagnosticsFormatter.FormatBindFailure` and `FormatResults`, so all four
> situations above are covered: a bind failure and a run failure both carry it,
> and the Presenter's **Discovery** tab shows it on demand.

## Command Catalog Report

The runtime should be able to report the generated command catalog.

Example shape:

```text
Command catalog:
- Given: I am on the {page} page -> Builtin.Page.Open
- When: I tap {control} -> Builtin.Control.Tap
- When: I enter {value} into {control} -> Builtin.Control.Enter
- Then: {control} should contain {value} -> Builtin.Control.AssertTextContains
```

This matters because users will write Markdown first. If a phrase does not bind, the runner should show the phrases it understands.

> **Delivered (2026-09), format unchanged — but the ids now come from elsewhere.**
> This is `UatDiagnosticsFormatter.FormatCatalog`, which prints
> `- {Keyword}: {Phrase} -> {CommandId}` sorted by keyword then phrase. The
> example above is accurate output.
>
> What changed is where a `CommandId` is decided. `Builtin.Page.Open` is still
> chosen in the runtime, because the three page verbs are registered there. But
> `Builtin.Control.Tap` is now assembled at discovery: `[UatStep]` on
> `IClickableControlObject<TScope>.Click` carries the suffix `Control.Tap`, and the
> projection prepends `Builtin.` — or `Spec.` for the binding-only catalog. So the
> report a user reads is generated from the same attributes that implement the
> behaviour, and cannot describe a phrase the engine does not actually have.
>
> The report is reachable three ways: in a bind or run failure, from the
> Presenter's **Command Catalog** tab, and directly via `FormatCatalog` in a test.

## Expected Failure Scenarios

UAT projects should include an expected-failure folder for diagnostics tests.

Recommended shape:

```text
ExpectedFailures/
  main-page-missing-control.uat.md
```

Expected-failure files are not normal user scenarios. They are tests for the runner itself.

The first expected failure should prove that a missing control reports:

- UAT file path
- line number
- failed step text
- command ID
- current page
- missing control name
- available controls on the current page

> **Delivered (2026-09), at exactly the proposed path.**
> `testsnew/Brinell.Maui.Uat.Tests/ExpectedFailures/main-page-missing-control.uat.md`
> exists, and the pattern is first-class rather than hand-rolled:
> `UatScenarioTestBase.RunExpectedFailureUatFileAsync(filePath, params string[])`
> asserts the scenario fails **and** that the message contains each fragment. The
> MAUI test pins `"Imaginary Button"`, `"Available controls"` and the file name:
>
> ```csharp
> [Theory(Timeout = 120000)]
> [MemberData(nameof(ExpectedFailureScenarioFiles))]
> public Task ExpectedFailureUatFile_ReturnsUsefulDiagnostics(string filePath) =>
>     RunExpectedFailureUatFileAsync(
>         filePath, "Imaginary Button", "Available controls", Path.GetFileName(filePath));
> ```
>
> The expected-failure folder is a separate theory source
> (`GetScenarioFiles("ExpectedFailures")`), so these files never run as ordinary
> scenarios — the distinction this section asked for is enforced by the harness,
> not by convention.

## First Hardened Built-In Commands

The first runtime command surface should include:

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
- `Then {control} should not be visible`
- `Then {control} should be enabled`
- `Then {control} should be checked`
- `Then {control} should be unchecked`
- `Then {control} should have selected {value}`
- `Then I should see {text}`

This is still intentionally small. It is enough for the first MAUI Main Page and User Form UATs.

> **Delivered (2026-09), and it never grew.** With
> `{control} should not be visible` added above, this list is the complete
> built-in vocabulary — 18 phrases, which is still the whole set across six UI
> technologies. "Intentionally small" turned out to be the permanent size, not a
> starting point: what grew instead was the ability for a project to declare its
> own verbs (`[UatStep]` on an app control, `[UatPhrase]` for anything wider).
>
> Three of the 18 are engine-owned page verbs; the other 15 are discovered from
> `[UatStep]`. Authoritative list:
> [uat-phrases-and-flows.md](../../docs/guides/uat-phrases-and-flows.md)
> §Built-In Phrases.

## Runner UI Implication

The future MAUI runner UI should have a diagnostics panel that can show:

- parse diagnostics
- bind diagnostics
- discovery report
- command catalog report
- execution trace
- per-step result
- failure exception

The UI should not need to invent these diagnostics. It should display the reports produced by the UAT core/runtime.

> **Delivered (2026-09), and the principle held.** The Presenter displays these
> reports rather than generating any of its own — the panel became a row of tabs:
> `ConfigTabButton`, `DiagnosticsTabButton`, `DiscoveryTabButton` and
> `CommandCatalogTabButton`, alongside the tree. The last two are exactly the two
> reports specified above.
>
> This is also the section that quietly settled a larger question. Because the
> reports are produced by the core, the Presenter needed no vocabulary of its
> own — which is why deleting its private phrase table later was possible at all.

## Done Definition

This hardening slice is done when:

- The MAUI UAT runtime loads `uat.config.md`.
- Passing MAUI UAT scenarios still pass.
- At least one expected-failure UAT file exists.
- The expected-failure test passes by proving the failure message is useful.
- Missing page failures list available pages.
- Missing control failures list available controls.
- The command catalog includes the first assertion and selection commands.

## Closed

Every item above is met.

| Item | Evidence |
| --- | --- |
| Runtime loads `uat.config.md` | `UatConfigParser.ParseFile`; 8 config files in the tree |
| Passing MAUI scenarios still pass | `Brinell.Maui.Uat.Tests`, 3 scenarios |
| At least one expected-failure file exists | `ExpectedFailures/main-page-missing-control.uat.md` |
| The expected-failure test proves the message is useful | `RunExpectedFailureUatFileAsync` pins `"Imaginary Button"` and `"Available controls"` |
| Missing page failures list available pages | `OpenPageAsync` → `"Available pages: …"` |
| Missing control failures list available controls | `InvokeControlMethodAsync` → `"Available methods: …"`, plus the discovery report |
| Catalog includes assertion and selection commands | all 18 phrases, `should have selected {value}` among them |

What this slice got right, in hindsight, was treating the reports as **core
output that a UI displays** rather than as UI features. Both reports survived
unchanged into a six-technology runner and a desktop app, and the expected-failure
idea became a reusable harness method instead of one test.
