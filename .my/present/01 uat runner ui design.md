# UAT Runner UI Design

This document sketches a first MAUI UI for the Markdown-driven Brinell UAT runner.

The runner is designed to sit side-by-side with the application under test. It is used to give demos to stakeholders and to step through UAT scenarios interactively. The layout must stay narrow and compact so it does not compete with the app window for screen space.

## Design Goals

- Stay as narrow and compact as possible so the runner can sit beside the app under test.
- The scenario step list is the primary view; everything else is secondary.
- Run controls are a compact toolbar, not a panel.
- Diagnostics and loaded UAT files are in expanders, collapsed by default.
- Make automatic execution and manual step execution equally easy.
- Make failures actionable by showing the failing step, page object binding, and available diagnostics.

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
# Brinell UAT Runner

[#1] [#2] [#7]  Suite: <Checkout smoke suite____>  Tags: (smoke x)

[#3 Run] [#4 Pause] [#5 Stop] [#6 Next]  [- 500ms +]  [====......]
2 passed, 1 running, 0 failed, 4 waiting

+--- Scenario Steps -------------------------------------------+
| # Checkout can place an order                                |
| pass  Given I am on Login            LoginPage               |
| pass  When I enter credentials       LoginPage.SetCredentials|
| run   And I tap Sign in              SignInButton.Tap        |
| wait  Then I should see Dashboard    DashboardPage.AssertOpen|
|                                                              |
| Bound page: LoginPage                                        |
| Bound action: SignInButton.Tap                               |
+--------------------------------------------------------------+

v Loaded UAT Files (collapsed)
  | - checkout.md                     |
  |   - Create basket                 |
  |   - Apply discount                |
  |   - Place order                   |
  | - login.md                        |
  |   - Valid login                   |
  |   - Locked account                |
  | Filter: <smoke__________>         |
  | [x] Selected only                 |

v Diagnostics (collapsed)
  | Current result: running            |
  | Last event: Invoked SignInButton    |
  | Screenshot: available              |
  | Trace: available                   |
  | [#8 Export Report]                 |
  | 10:14:21 Loaded 3 files            |
  | 10:14:25 Waiting for Dashboard     |
```

## Failure State

```markui
# Brinell UAT Runner

[#1] [#2] [#7]  Suite: <Login suite_____________>

[#3 Run] [#4 Pause] [#5 Stop] [#6 Next]  [- 500ms +]  stopped after failure

+--- Scenario Steps -------------------------------------------+
| # Locked account cannot sign in                              |
| pass  Given I am on Login            LoginPage               |
| pass  When I enter locked user       LoginPage.SetCredentials|
| pass  And I tap Sign in              SignInButton.Tap        |
| fail  Then I should see Locked out   ErrorBanner.AssertText  |
|                                                              |
| Expected: Locked out                                         |
| Actual: Invalid credentials                                  |
+--------------------------------------------------------------+

v Failure Detail (auto-expanded on failure)
  | File: login.md                     |
  | Scenario: Locked account           |
  | Step: Then I should see Locked out |
  | Page: LoginPage                    |
  | Control: ErrorBanner               |
  | Action: AssertText                 |
  | [#6 Next] [#3 Retry Step]          |

v Diagnostics (auto-expanded on failure)
  | Screenshot: captured               |
  | Automation tree: captured          |
  | Runtime log: captured              |
  | Page object trace: captured        |
  | [#8 Export Report]                 |
```

## Interaction Notes

The runner is a narrow companion window. The toolbar wraps if the window is very narrow; buttons and delay stepper flow onto a second line. The progress bar and status counts sit directly below the toolbar to keep run state visible without taking vertical space.

The center step list is the main trust surface. Each row moves through clear statuses: waiting, running, pass, fail, and skipped. When a step is selected, the bound page object and action appear inline below the step list.

The Loaded UAT Files and Diagnostics sections are expanders, collapsed by default. During a demo, only the step list and toolbar need to be visible. The expanders open on demand or auto-expand on failure so diagnostics are immediately available without hunting through logs.

Failure detail auto-expands both the Failure Detail and Diagnostics expanders so the presenter or tester can see the failing step, expected vs actual values, screenshot, and trace links without any clicks.

## First Version Scope

For the first MAUI version:

- File and folder loading.
- Scenario list in expander.
- Step list with status (primary view).
- Compact toolbar: Run, Pause, Stop, Next, delay stepper, progress bar.
- Status summary line.
- Auto and step execution modes.
- Diagnostics expander.
- Failure detail expander with screenshot and page-object binding.
