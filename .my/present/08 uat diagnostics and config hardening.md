# UAT Diagnostics And Config Hardening

This document captures the first hardening slice after the MAUI UAT proof ran successfully.

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
- `Then {control} should be enabled`
- `Then {control} should be checked`
- `Then {control} should be unchecked`
- `Then {control} should have selected {value}`
- `Then I should see {text}`

This is still intentionally small. It is enough for the first MAUI Main Page and User Form UATs.

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

## Done Definition

This hardening slice is done when:

- The MAUI UAT runtime loads `uat.config.md`.
- Passing MAUI UAT scenarios still pass.
- At least one expected-failure UAT file exists.
- The expected-failure test passes by proving the failure message is useful.
- Missing page failures list available pages.
- Missing control failures list available controls.
- The command catalog includes the first assertion and selection commands.
