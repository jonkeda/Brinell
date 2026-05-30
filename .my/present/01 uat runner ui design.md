# UAT Runner UI Design

This document sketches a first MAUI UI for the Markdown-driven Brinell UAT runner.

The design assumes the app is a practical test workbench: load Markdown UATs, inspect the parsed scenarios, run them against Brinell MAUI page objects, and control execution speed or step manually.

## Design Goals

- Keep UAT files, scenario steps, and run state visible at the same time.
- Make automatic execution and manual step execution equally easy.
- Prefer explicit run status over hidden logs.
- Make failures actionable by showing the failing step, page object binding, and available diagnostics.
- Keep the first version MAUI-focused while leaving room for later adapters.

## Icon Legend

- `#1` Open file
- `#2` Open folder
- `#3` Run
- `#4` Pause
- `#5` Stop
- `#6` Next step
- `#7` Refresh/reparse
- `#8` Export report

## Main Runner Screen

```markui
# Brinell UAT Runner - MAUI

[#1 File] [#2 Folder] [#7 Reparse]    Target: <MAUI v>    Adapter: <FlaUI v>
Suite: <Checkout smoke suite________________>    Tags: (smoke x) (maui x)

+--- Loaded UAT Files ----------------+
| - checkout.md                       |
|   - Create basket                   |
|   - Apply discount                  |
|   - Place order                     |
| - login.md                          |
|   - Valid login                     |
|   - Locked account                  |
| Filter: <smoke__________>           |
| [x] Selected only                   |
+-------------------------------------+

+--- Scenario Steps ---------------------------------------------+
| # Checkout can place an order                                  |
| pass  Given I am on Login            LoginPage                 |
| pass  When I enter credentials       LoginPage.SetCredentials  |
| run   And I tap Sign in              SignInButton.Tap          |
| wait  Then I should see Dashboard    DashboardPage.AssertOpen  |
|                                                                |
| Bound page: LoginPage                                          |
| Bound action: SignInButton.Tap                                 |
+----------------------------------------------------------------+

+--- Run Control ---------------------+  +--- Diagnostics ----------------------+
| Mode                                |  | Current result: running              |
| (*) Auto run                        |  | Last event: Invoked SignInButton     |
| ( ) Step run                        |  | Screenshot: available                |
| Speed: [=====.....] 1.0x            |  | Trace: available                     |
| Delay: [- 500 +] ms                 |  | [#8 Export Report]                   |
| [#3 Run All] [#4 Pause]             |  | 10:14:21 Loaded 3 files              |
| [#5 Stop]    [#6 Next]              |  | 10:14:25 Waiting for Dashboard       |
| Progress: [====......]              |  |                                      |
+-------------------------------------+  +--------------------------------------+

Status: 2 passed, 1 running, 0 failed, 4 waiting
```

## Failure State

```markui
# Brinell UAT Runner - Failure Detail

[#1 File] [#2 Folder] [#7 Reparse]    Target: <MAUI v>    Adapter: <FlaUI v>

+--- Scenario Steps ---------------------------------------------+
| # Locked account cannot sign in                                |
| pass  Given I am on Login            LoginPage                 |
| pass  When I enter locked user       LoginPage.SetCredentials  |
| pass  And I tap Sign in              SignInButton.Tap          |
| fail  Then I should see Locked out   ErrorBanner.AssertText    |
|                                                                |
| Expected: Locked out                                           |
| Actual: Invalid credentials                                    |
+----------------------------------------------------------------+

+--- Failure --------------------------+  +--- Diagnostics --------------------+
| Result: failed                       |  | Screenshot: captured               |
| File: login.md                       |  | Automation tree: captured          |
| Scenario: Locked account             |  | Runtime log: captured              |
| Step: Then I should see Locked out   |  | Page object trace: captured        |
| Page: LoginPage                      |  |                                    |
| Control: ErrorBanner                 |  | [#8 Export Report]                 |
| Action: AssertText                   |  |                                    |
| [#6 Next] [#3 Retry Step]            |  |                                    |
+--------------------------------------+  +------------------------------------+

Status: stopped after failure
```

## Interaction Notes

The main screen keeps the workflow in one place. A user can load one Markdown file or a folder, select scenarios, and immediately see how the Markdown was parsed into executable steps.

The run controls are always visible. In automatic mode, the speed slider and delay stepper control pacing. In step mode, the `Next` button advances one executable step at a time.

The center step list is the main trust surface. Each row should move through clear statuses such as waiting, running, pass, fail, and skipped. When a step is selected, the app should show the page object and action that the runner resolved.

Failure detail should not require hunting through logs. The failure view should show the Markdown step, the bound page object/control/action, expected and actual values, plus screenshot and trace links when available.

## First Version Scope

For the first MAUI version, this design can be reduced to:

- File and folder loading.
- Scenario list.
- Step list with status.
- Run all.
- Stop.
- Auto speed control.
- Manual Next mode.
- Failure detail with screenshot and page-object binding.

The layout intentionally leaves space for later technology targets, but the only selectable target in the first build should be MAUI.
