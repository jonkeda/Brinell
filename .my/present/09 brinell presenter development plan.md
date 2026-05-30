# Brinell Presenter Development Plan

This document defines the first build plan for `Brinell.Presenter`, the MAUI application that presents, validates, and runs Markdown UAT suites.

## Readiness

We are ready to start the UI.

The important backend pieces now exist:

- UAT Markdown parser.
- UAT validation diagnostics.
- Command binding.
- Reflection runtime over PageObjects and ControlObjects.
- MAUI UAT project using real `.uat.md` files.
- Passing MAUI demo-backed UAT tests.
- Expected-failure diagnostics proving missing controls produce useful output.

The UI should not invent new UAT behavior. It should present the existing UAT core/runtime state.

## Product Name

The MAUI runner app should be called:

```text
Brinell.Presenter
```

The name is intentionally not `Runner`. The app should present a UAT suite clearly: files, scenarios, steps, binding state, execution state, and diagnostics.

## Source Project

Create:

```text
srcnew/Brinell.Presenter/
  Brinell.Presenter.csproj
  App.xaml
  App.xaml.cs
  MauiProgram.cs
  Models/
  Services/
  ViewModels/
  Views/
  Resources/
```

Reference:

```text
srcnew/Brinell.Uat
```

Use the sample app as the MAUI architecture reference:

```text
samples/Brinell.Samples.Maui.App
```

## Architecture Style

Use simple MVVM.

Follow the sample app conventions:

- XAML views in `Views`.
- ViewModels in `ViewModels`.
- State models in `Models`.
- App services in `Services`.
- Thin code-behind that mostly calls `InitializeComponent`.
- Commands exposed from ViewModels.
- Bindable properties with change notifications.
- `AutomationId` on every user-relevant control.
- `MauiProgram` owns app setup, services, handlers, and logging.

The sample currently uses `ParentViewModel`, `RelayCommand`, and `AsyncRelayCommand` from `Brinell.Samples.Shared`. For `Brinell.Presenter`, do not reference the sample shared project directly. Either move durable MVVM helpers into a Brinell-owned shared project later, or copy a small local `ViewModelBase` and command implementation into Presenter for the first slice.

## Initial UI Shape

The first screen should be the usable runner surface, not a landing page.

Suggested narrow layout:

```markui
+--- Brinell Presenter ------------------------------------+
| Workspace: <Maui UAT suite________________>              |
| [Open Folder] [Reload] [Validate]                        |
| Ready  3 files  3 scenarios                              |
|                                                          |
| [Run Selected] [Run All] [Stop] [Next]                   |
| Mode: <Step v>  Delay: [- 250 +] ms                      |
| Progress: [====......]                                   |
|                                                          |
| [Scenarios ^]                                            |
| sel  Greeting appears when a name is entered             |
|      Empty name shows validation message                 |
|      User can enter basic profile information            |
|                                                          |
| [Steps ^]                                                |
| pass  Given I am on the Main page                        |
| pass  When I clear Name                                  |
| run   And I enter "Alice" into Name                      |
| wait  And I tap Greet                                    |
| wait  Then Greeting should contain "Hello, Alice!"       |
|                                                          |
| [Files v]                                                |
| [Diagnostics v]                                          |
| [Discovery v]                                            |
| [Command Catalog v]                                      |
+----------------------------------------------------------+
```

Keep the UI work-focused, dense, and narrow enough to sit on the left side of a laptop screen while the application under test sits on the right. This is an operational tool, not a marketing page.

Default expander state:

- Expanded: `Scenarios`, `Steps`.
- Collapsed: `Files`, `Diagnostics`, `Discovery`, `Command Catalog`.

Files and diagnostics are important, but they should not compete with the current scenario and step list. The user should be able to expand them when investigating a load, bind, or execution failure.

## First Presenter ViewModels

Create these ViewModels first:

```text
PresenterShellViewModel
UatWorkspaceViewModel
UatFileViewModel
UatScenarioViewModel
UatStepViewModel
UatDiagnosticsViewModel
```

Responsibilities:

- `PresenterShellViewModel`: top-level commands and selected workspace state.
- `UatWorkspaceViewModel`: loaded folder/file, config, files, scenarios, summary.
- `UatFileViewModel`: file path, parse status, parsed document, diagnostics.
- `UatScenarioViewModel`: scenario name, tags, bind status, execution status.
- `UatStepViewModel`: source line, text, command ID, status, message.
- `UatDiagnosticsViewModel`: parse, bind, discovery, command catalog, execution trace.

## First Presenter Services

Create these services:

```text
IUatWorkspaceService
IFilePickerService
IUatPresenterRuntimeService
IUatExecutionCoordinator
```

Responsibilities:

- `IUatWorkspaceService`: load one file or folder, find `uat.config.md`, parse UAT files.
- `IFilePickerService`: MAUI file/folder picker wrapper.
- `IUatPresenterRuntimeService`: create command catalog and runtime diagnostics for a loaded workspace.
- `IUatExecutionCoordinator`: run selected scenarios automatically or step by step.

The first implementation can be local and direct. Avoid a plugin system in the UI MVP.

## Runtime Scope For First UI

Start with parse, bind, and display for any folder containing `.uat.md` files.

Then add execution against the existing MAUI demo UAT project as the first runnable profile:

```text
testsnew/Brinell.Maui.Uat.Tests
```

Do not solve arbitrary external assembly loading on day one. That can become a later runtime profile feature.

## First Presenter Commands

The UI should expose:

- Open File
- Open Folder
- Reload
- Validate
- Run Selected
- Run All
- Stop
- Next Step
- Toggle Auto/Step mode
- Set execution delay
- Copy Diagnostics

These commands should call the UAT core, not duplicate parser/binder logic.

## Presenter Automation IDs

Every important control should have an `AutomationId` from the start.

Required first IDs:

```text
PresenterRoot
OpenFileButton
OpenFolderButton
ReloadButton
ValidateButton
RunSelectedButton
RunAllButton
StopButton
NextStepButton
ExecutionModePicker
ExecutionDelayEntry
FileList
ScenarioList
StepList
DiagnosticsText
StatusSummaryLabel
```

The Presenter UATs should use these IDs through PageObjects.

## Presenter UAT Project

Create a UAT project for Presenter itself:

```text
testsnew/Brinell.Presenter.Uat.Tests/
  Brinell.Presenter.Uat.Tests.csproj
  uat.config.md
  Scenarios/
    load-folder.uat.md
    validate-suite.uat.md
    run-step-mode.uat.md
  Runtime/
    PresenterUatCollection.cs
    PresenterUatRuntime.cs
    PresenterUatScenarioSource.cs
    PresenterUatScenarioTests.cs
  PageObjects/
    PresenterPage.cs
```

This is dogfooding: Presenter should be tested by the Markdown UAT runner.

## First Presenter UATs

### Load Folder

```text
# UAT: Presenter Loads A UAT Folder

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Presenter |
| Area | Workspace |
| Target | MAUI |
| Tags | smoke, presenter, load |

@smoke @presenter @load
## Scenario: User loads a UAT folder

Given I am on the Presenter page
When I open the sample MAUI UAT folder
Then Scenario List should contain "Greeting appears when a name is entered"
And Scenario List should contain "User can enter basic profile information"
And Status Summary should contain "Ready"
```

### Validate Suite

```text
# UAT: Presenter Validates A UAT Suite

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Presenter |
| Area | Validation |
| Target | MAUI |
| Tags | smoke, presenter, validation |

@smoke @presenter @validation
## Scenario: User validates a loaded suite

Given I am on the Presenter page
When I open the sample MAUI UAT folder
And I tap Validate
Then Diagnostics should contain "Parse: ok"
And Diagnostics should contain "Bind: ok"
```

### Step Mode

```text
# UAT: Presenter Step Mode

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Presenter |
| Area | Execution |
| Target | MAUI |
| Tags | smoke, presenter, step-mode |

@smoke @presenter @step-mode
## Scenario: User advances one step

Given I am on the Presenter page
When I open the sample MAUI UAT folder
And I select scenario "Greeting appears when a name is entered"
And I select Step mode
And I tap Next Step
Then Step List should contain "Given I am on the Main page"
And Status Summary should contain "Running"
```

The first Presenter UAT runtime may need custom `[UatPhrase]` methods for operations that are not generic control actions, such as opening a known sample folder or selecting a scenario by name.

## First Implementation Slices

### Slice 1: App Shell

- Create `Brinell.Presenter` MAUI project.
- Add it to the Brinell solutions.
- Use XAML and MVVM conventions from `Brinell.Samples.Maui.App`.
- Build a single shell view with static placeholder data.
- Add AutomationIds.

Acceptance:

- App builds.
- App launches.
- Presenter page is visible.

### Slice 2: Workspace Loading

- Add workspace service.
- Load a folder path.
- Find `uat.config.md`.
- Find `.uat.md` files.
- Parse files with `UatMarkdownParser`.
- Show files and scenarios.

Acceptance:

- Sample MAUI UAT folder appears in the UI.
- Parse errors appear in diagnostics.

### Slice 3: Binding Preview

- Create command catalog through the existing UAT runtime.
- Bind scenarios.
- Show command IDs and unresolved steps.
- Show discovery report and command catalog report.

Acceptance:

- Passing sample UATs show bind success.
- Expected failure diagnostics are visible.

### Slice 4: Execution State

- Wire `UatScenarioRunner` into the Presenter execution coordinator.
- Support run selected.
- Support step-by-step mode.
- Support stop/cancellation.
- Show per-step statuses.

Acceptance:

- One scenario can run from the Presenter UI.
- The current step is visible.
- Failure stops the run and shows diagnostics.

### Slice 5: Presenter UATs

- Create `Brinell.Presenter.Uat.Tests`.
- Add Presenter PageObject.
- Add the first three `.uat.md` files.
- Run against `Brinell.Presenter`.

Acceptance:

- Presenter can be tested by UAT Markdown.
- At least the load/validate scenario passes.

## Deferred

Defer these until the Presenter MVP can load and run the sample MAUI UAT suite:

- Full dynamic assembly loading for arbitrary projects.
- Plugin/profile marketplace.
- Rich reports.
- Historical run storage.
- CI mode.
- Multi-window execution.
- Mobile-specific Presenter UX.
- Visual timeline.
- Editing `.uat.md` files inside Presenter.

## Done Definition

The first Presenter MVP is done when:

- `Brinell.Presenter` exists and builds.
- It follows the sample MAUI app MVVM conventions.
- It loads a UAT folder.
- It parses and binds `.uat.md` files.
- It shows scenarios, steps, diagnostics, discovery report, and command catalog.
- It can run one scenario from the sample MAUI UAT suite.
- `Brinell.Presenter.Uat.Tests` exists.
- At least one Presenter UAT runs successfully.
