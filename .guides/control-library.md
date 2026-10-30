# Adding Controls For A New Control Library

How to give a third-party MAUI control library (a vendor suite, a toolkit) its own Bruinet
ControlObjects. [CommunityToolkit.Maui](../docs/controls/community-toolkit.md) was done this way.

The work is done by the `control-source-builder` agent
([.claude/agents/control-source-builder.md](../.claude/agents/control-source-builder.md)).
You run it several times, each time with one step and the library's folder. Between runs, you
read what it wrote and decide what comes next.

## Before You Start

- **One library, one package.** The controls go in their own project,
  `src/Bruinet.Maui.<Library>`, so a suite that does not use the library does not get it
  ([AD-026](../.docs/decisions/ad-026-a-project-earns-its-place.md)). The project wraps
  automation elements; it does not reference the library itself. Only the sample app does.
- **A clean working tree.** The agent stops when it finds changes it did not make. It never
  commits: you review and commit after each run.
- **MAUI only.** The agent follows the `maui-control` and `maui-ui-test` skills and builds
  against the MAUI sample app. For an Html or Blazor library, say in the plan to use the
  `html-control` skill and the Blazor sample instead, and expect to steer it.
- **Android** steps run only when an emulator or device is attached (`adb devices`).
  Otherwise they are marked "not run".

## The Steps

Ask for the agent by name, with the step and the folder, for example
"run control-source-builder: `survey .my/syncfusion`". The folder name is yours to choose.

| # | Step | What the agent does | What you do after |
| --- | --- | --- | --- |
| 1 | `survey .my/<library>` | Finds the package and the version for the repo's target framework, lists its views, and writes `.my/<library>/plan.md`: current state, an inventory (Bruinet name, base class, likely route, priority) and the phases. Changes no code. | Review the plan. Drop controls, change priorities, mark what is out of scope (hardware, OS dialogs, things that are not views). |
| 2 | `groundwork .my/<library>` | Phase 0: the package version, the project in `Bruinet.sln`, the generator's input folders, an empty sample page and test page. | Check that the solution builds. Commit. |
| 3 | `probe .my/<library>` | Puts every P1-P2 control on the sample page and records what UI Automation sees for each one in `.my/<library>/probe.md`. Corrects the plan's "likely route" column. Builds no controls. | Read the probe. This is where the plan changes most: it decides which controls have a real pattern and which would need a bridge verb. |
| 4 | `build .my/<library> <Control>` | One control: sample, page object, tests, `.tpl.cs`, generate, build, run that control's tests. Fills in its progress row. | Read the row and the generated API. Commit. Repeat for the next control. |
| 5 | `closeout .my/<library>` | The full UI suite, `docs/controls/<library>.md` linked from the docs index, a CHANGELOG entry. | Close the `.my/<library>/` topic: anything a user needs is now in `docs/`. |

[The CommunityToolkit plan](https://github.com/exact-mobile-apps/test-tooling/blob/8ad62ed2/.my/communitytoolkit/plan.md) is the worked example the agent copies; read it to see what a
finished plan looks like.

## What The Agent Will Not Do

These hold for every step; when one gets in the way, the agent stops and reports instead of
working around it:

- **Routes, in order:** a UI Automation pattern, then an existing gesture-bridge verb, then a
  new verb only when it passes the three tests of
  [AD-008](../.docs/decisions/ad-008-gestures-and-semantic-actions-go-through-ui-automation.md).
  No coordinates, no physical input on MAUI Windows.
- No sleeps or longer timeouts to make a test pass.
- No changes to shared infrastructure (fixture, navigation, `MauiProgram`, drivers).
- Only the smallest test tier for a control; the full suite runs in closeout only.

## What To Expect

- The survey's routes are guesses. For CommunityToolkit, three of seven controls turned out to
  need the app's gesture bridge.
- Some controls cannot be driven on a platform at all (CommunityToolkit's Snackbar and Toast
  throw in an unpackaged Windows app). The plan records them as skipped, with the reason.
- A control whose generated API is not enough gets hand-written members; the progress row says
  which and why.
