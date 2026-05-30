# First UAT Project Targets

This document picks the first existing UITest projects that should get corresponding UAT projects.

The goal is not to replace the existing UITests. The existing UITests remain useful as framework and control-level coverage. The UAT projects should sit beside them and express the same important user flows in Markdown so the new runner can prove its parser, binding, discovery, and execution model against real Brinell PageObjects and ControlObjects.

## Decision

Start with `Brinell.Maui.UITests`.

After the first MAUI UAT project works, add one smaller non-MAUI comparison project to prove that the UAT core is not accidentally tied to Appium or MAUI runtime details.

Recommended order:

1. `Brinell.Maui.UITests` -> `Brinell.Maui.Uat.Tests`
2. `Brinell.Wpf.UITests` -> `Brinell.Wpf.Uat.Tests`
3. `Brinell.Html.UITests` or `Brinell.Blazor.UITests` -> later web UAT proof

Do not start with Stride, collection-heavy tests, diagnostics tests, media tests, or scraper tests. Those are useful later, but they are not the cleanest first proof of a human-authored UAT flow.

## Why MAUI First

The original product direction is MAUI-first.

`Brinell.Maui.UITests` already has page objects for the sample app:

- `MainPage`
- `UserFormPage`
- `AppShellPage`
- `ListsPage`
- `CollectionDemoPage`
- `ContainerDemoPage`
- `MediaGalleryPage`

The best first pages are `MainPage` and `UserFormPage`.

They cover the basic command surface the UAT runner needs:

- page loaded assertions
- text entry
- button tapping
- label text assertions
- checkbox check/uncheck
- picker selection
- simple form submission

They also use property names such as `NameEntry`, `GreetButton`, `GreetingLabel`, `TermsCheckBox`, and `CountryPicker`, which are good examples for default UAT naming rules.

## First MAUI UAT Project

Create:

```text
testsnew/Brinell.Maui.Uat.Tests/
  Brinell.Maui.Uat.Tests.csproj
  uat.config.md
  Scenarios/
    main-page-greeting.uat.md
    main-page-validation.uat.md
    user-form-basic-input.uat.md
  Runtime/
    MauiUatCollection.cs
    MauiUatRuntime.cs
    MauiUatScenarioSource.cs
    MauiUatScenarioTests.cs
```

The first version may reference `Brinell.Maui.UITests` so it can reuse the existing sample app fixture and PageObjects. That is acceptable as a bridge. If the UAT project becomes durable, shared PageObjects can move into a separate reusable sample automation project later.

The UAT project should not hand-write a page-specific command catalog. It should create the catalog through `Brinell.Uat` runtime discovery. The runtime scans the fixture for PageObject instances, scans each current PageObject for ControlObject properties, and wires generic commands such as `I tap {control}` and `I enter {value} into {control}` to the discovered control methods.

## First MAUI Scenarios

### Main Page Greeting

```text
# UAT: MAUI Main Page Greeting

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Maui.App |
| Area | Main Page |
| Target | MAUI |
| Tags | smoke, maui, greeting |

@smoke @maui @greeting
## Scenario: Greeting appears when a name is entered

Given I am on the Main page
When I enter "Alice" into Name
And I tap Greet
Then I should see "Hello, Alice!"
```

### Main Page Validation

```text
# UAT: MAUI Main Page Greeting Validation

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Maui.App |
| Area | Main Page |
| Target | MAUI |
| Tags | smoke, maui, validation |

@smoke @maui @validation
## Scenario: Empty name shows validation message

Given I am on the Main page
When I clear Name
And I tap Greet
Then I should see "Please enter your name"
```

### User Form Basic Input

```text
# UAT: MAUI User Form Basic Input

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Maui.App |
| Area | User Form |
| Target | MAUI |
| Tags | smoke, maui, form |

@smoke @maui @form
## Scenario: User can enter basic profile information

Given I am on the User Form page
When I enter "Ada" into First Name
And I enter "Lovelace" into Last Name
And I enter "ada@example.com" into Email
And I check Terms
And I select "United States" from Country
And I tap Submit
Then I should see "Ada"
```

## First Command Surface

The first MAUI UAT project should only need these commands:

- `Given I am on the {page} page`
- `When I enter {value} into {control}`
- `When I clear {control}`
- `When I tap {control}`
- `When I check {control}`
- `When I uncheck {control}`
- `When I select {value} from {control}`
- `Then I should see {text}`
- `Then {control} should contain {value}`
- `Then {control} should be visible`
- `Then {control} should be enabled`

This is intentionally limited. More commands can be added after the first UAT project runs green.

## Config Shape

The first `uat.config.md` should be folder-local:

```text
# UAT Config

## Runtime

| Field | Value |
| --- | --- |
| Target | MAUI |
| Fixture | Appium |

## Assemblies

| Kind | Assembly |
| --- | --- |
| Pages | Brinell.Maui.UITests.dll |
| Controls | Brinell.Maui.dll |
| Commands | Brinell.Uat.dll |

## Discovery

| Field | Value |
| --- | --- |
| RequireExplicitUatAttributes | false |
| AllowNameInference | true |
```

The important part is that the config names the assemblies the runner should inspect. The runner should not require aliases for the first version.

## What To Convert First

Convert user-facing scenario tests before low-level framework tests.

Good first conversions:

- `MainPage_EnterNameAndGreet_ShowsGreetingMessage`
- `MainPage_GreetWithoutName_ShowsValidationMessage`
- `MainPage_EmailEntry_AcceptsEmailFormat`
- `CheckBox_Check_SetsCheckedToTrue`
- `CheckBox_Uncheck_SetsCheckedToFalse`
- `Picker_SelectByText_SelectsItem`

Skip for the first UAT project:

- wait-method tests
- fluent chaining tests
- nullable skip tests
- diagnostic tests
- debug tests
- gesture-heavy tests
- collection virtualization tests
- media and WebView tests

Those are valid Brinell tests, but they are not the clearest first UAT examples.

## Second Target: WPF

After the MAUI project runs one or two scenarios, add:

```text
testsnew/Brinell.Wpf.Uat.Tests/
```

Use `Brinell.Wpf.UITests` because it is small and workflow-oriented.

Good first WPF conversions:

- `Login_WithValidCredentials_NavigatesToHomePage`
- `Login_WithInvalidCredentials_ShowsErrorMessage`
- `Login_WithShortUsername_ShowsValidationError`

WPF is a good second target because it tests whether UAT execution is truly Brinell-driven instead of MAUI/Appium-driven.

## Third Target: HTML Or Blazor

After MAUI and WPF, choose either:

- `Brinell.Html.UITests`
- `Brinell.Blazor.UITests`

Both have compact login and counter examples. This should wait until the command system can cleanly separate generic commands from technology-specific runtime setup.

## Implementation Steps

1. Create `Brinell.Maui.Uat.Tests`.
2. Add `uat.config.md`.
3. Add two or three `.uat.md` files under `Scenarios`.
4. Add a small xUnit bridge that loads the config, parses the Markdown files, binds commands, and runs scenarios.
5. Reuse the existing `AppiumFixture` at first.
6. Implement the first MAUI command catalog.
7. Run one scenario end to end.
8. Add one failing scenario and verify that diagnostics point to the UAT file, line, page, control, and command.
9. Only then add `Brinell.Wpf.Uat.Tests`.

## Done Definition

This slice is done when:

- `Brinell.Maui.Uat.Tests` exists.
- At least two MAUI `.uat.md` files parse and bind.
- At least one MAUI scenario executes through real Brinell PageObjects.
- The execution can run automatically.
- The execution can run step by step.
- A failing scenario returns a useful UAT-level diagnostic.
- Existing `Brinell.Maui.UITests` remain unchanged and still useful as lower-level coverage.
