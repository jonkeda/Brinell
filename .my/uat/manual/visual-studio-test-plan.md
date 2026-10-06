# Manual test plan: the Brinell BDD extension for Visual Studio

Status: **ready to run**, not run yet. Date: 2026-09-28.
For: a person at a Windows machine with Visual Studio. About 2 hours for everything; section 3
alone takes about 30 minutes.

The extension's code has automated tests for its engine side (the language server). Nothing
automated has ever opened it in Visual Studio. This plan is that check. It replaces checklists
6.1-6.3 of the [extension plan](https://github.com/exact-mobile-apps/test-tooling/blob/e7c990c9/.my/uat/tools/visualstudio-plan.md) (built; its decisions are
[AD-022](../../../.docs/decisions/ad-022-the-ides-run-the-gates-engine.md)), and answers its spike
questions S1-S6 (section 13).

What each feature should do is described in the user guide,
[docs/guides/visual-studio-preview.md](../../../docs/guides/visual-studio-preview.md). When this
plan and the guide disagree, note it as a finding.

## How to use this plan

- Run the sections in order. Later ones assume the earlier setup.
- Each check has an ID, what to do, and what you should see. Mark it **Pass**, **Fail** or
  **Blocked** (you couldn't get to it), and write a note for anything not a plain pass.
- When something fails, carry on with the next check if you can. Take a screenshot, and copy the
  **Brinell BDD** Output pane (View > Output, "Show output from: Brinell BDD").
- Record the results in the table in section 14, or copy this file and tick the boxes.

## 1. What you need

| | |
| --- | --- |
| Visual Studio | 2022 17.10 or later, or 2026, with the **Visual Studio extension development** workload |
| .NET | the .NET 10 SDK (`dotnet --version` shows 10.x) |
| Repository | the Brinell repository, branch `move`, at or after commit `4c4955e` |
| Time | about 2 hours; section 3 alone is about 30 minutes |

Close every other Visual Studio window before you start. The UI test runs in section 5 start the
Todo app, and they need the desktop to themselves: don't use the mouse or keyboard while a run
goes.

## 2. Setup

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| S-1 | From the Brinell root, run `dotnet build samples\Todo\Brinell.Samples.Todo.slnx`. | The build succeeds. | ☐ |
| S-2 | Open `tools\Brinell.Bdd.VisualStudio\Brinell.Bdd.VisualStudio.slnx` in Visual Studio and press **F5**. | The build succeeds, and a second Visual Studio starts with "Experimental Instance" in its title bar. Everything below happens in this experimental instance. | ☐ |
| S-3 | In the experimental instance, open `samples\Todo\Brinell.Samples.Todo.slnx`, then **Build > Build Solution**. | The Todo solution builds. | ☐ |
| S-4 | **Extensions > Manage Extensions > Installed**. | "Brinell BDD" is listed and enabled, and no "Brinell UAT". If you see "Brinell UAT", the experimental instance and the build hold the extension from before the rename: close the experimental instance, delete `tools\Brinell.Bdd.VisualStudio\bin` and `obj` (and the CodeLens project's), run **Reset the Visual Studio 2026 Experimental Instance** from the Start menu, and F5 again. | ☐ |

The files used below are all under `samples\Todo\tests\Brinell.Samples.Todo.Uat\`:
- `Features\TOD.01-see-my-todos.feature.md`, called **TOD.01** from here on;
- `Features\Todo.steps.md`, called **the step file**;
- `ExpectedFailures\TOD.11-without-using.feature.md`, called **the failing file**;
- `uat.config.md`, called **the config**.

## 3. The Scenario Preview

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| P-1 | Open TOD.01. Press **Alt+Shift+V** twice, then **Ctrl+K**, release Ctrl, and press **V**. Open a plain `.md` (the repository's `README.md`). | TOD.01 opens with the BDD preview beside the text, and not Visual Studio's Markdown preview. Each key press hides or shows it. The README still shows Visual Studio's Markdown preview. It shows the feature as readable steps, with the keywords in a gutter, and tables drawn as tables. The bar at the top says **Bound**. | ☐ |
| P-2 | Right-click in the editor, **Show/Hide Scenario Preview**. Then **View > Other Windows > Scenario Preview**. Drag the preview's left edge. | The menu item hides and shows the preview beside the text. The tool window shows the same preview. The preview keeps its new width, also for the next file you open. | ☐ |
| P-3 | Hover over the step `When I tap Sync` (line 24) in the preview. | A tooltip says what it binds to and on which page (the Todo List page). | ☐ |
| P-4 | Find the step `Given the synced list has todos` (line 34) in the preview, and unfold it. | It opens into the steps of the `## Step:` it calls, with the values filled in. | ☐ |
| P-5 | Put the caret on a step in the editor, then on another. | The preview highlights the step under the caret each time. | ☐ |
| P-6 | Click a step in the preview. | The editor's caret moves to that step's line. | ☐ |
| P-7 | In the editor, change `When I tap Sync` to `When I tapp Sync`, and stop typing. | Within about a second, the preview marks that step as binding to nothing, with the reason. | ☐ |
| P-8 | Undo the change. | The mark goes away. | ☐ |
| P-9 | Open the step file. | The preview follows it and shows its `## Step:` sections. | ☐ |
| P-10 | Open the failing file. | The preview shows the diagnostic `UATB010` at its step, with its message. | ☐ |
| P-11 | Switch Visual Studio's theme (**Tools > Theme**) between Dark, Light and Blue. | The preview changes with it, without a reload, and stays readable. | ☐ |
| P-12 | Turn on Windows high contrast (**Left Alt + Left Shift + Print Screen**), look, and turn it off again. | The preview stays readable in high contrast. | ☐ |
| P-13 | Drag the preview window out to float, then dock it again. | It keeps working after each move (this is S6). | ☐ |
| P-14 | Click in the preview, and try **Ctrl+F**, **Escape** and **Ctrl+C** on some selected text. | Note what each does. Nothing should hang or lose focus (S6). | ☐ |
| P-15 | Open `samples\Todo\tests\Brinell.Samples.Todo.Web.Uat` in Solution Explorer, and open one of its linked feature files (it shows TOD.01 again). | The preview's bar has a suite picker offering the MAUI and web suites. Picking web binds the file in the web suite. | ☐ |

**Before a build** (optional, 10 minutes):

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| P-16 | Close the experimental instance. Delete `samples\Todo\tests\Brinell.Samples.Todo.Uat\bin`. Start the experimental instance again (S-2) and open TOD.01 without building. | The preview shows the steps, and the bar says **Parsed only**, with the reason (build the project first). | ☐ |
| P-17 | Build the solution. | The preview binds by itself after the build. | ☐ |

## 4. Diagnostics and the Error List

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| D-1 | Open the failing file. Open **View > Error List**. | The Error List has `UATB010` for that file, with the same message as the preview, at the step's line. A squiggle marks the step. | ☐ |
| D-2 | In TOD.01, type a new step `When I polish the list` inside a gherkin fence. | Within a second, a squiggle and an Error List entry `UATB001` appear. | ☐ |
| D-3 | Remove the step and save. | The squiggle and entry go away. | ☐ |
| D-4 | Save any feature file. | The Error List also shows problems of the suite's other feature files, not only the open one (the failing file's `UATB010` stays listed after you close it). | ☐ |

## 5. Run from the preview

These start the Todo app. Leave the mouse and keyboard alone while a run goes.

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| R-1 | Open TOD.01. On the heading of `TOD.01.3` in the preview, click **▷ Run as test**. | The suite's project builds first. Then the **Brinell BDD** Output pane shows a `dotnet test ... --filter "BddScenario=..."` command and its output, the Todo app opens, and the scenario runs. The heading shows "running…" meanwhile. | ☐ |
| R-2 | Wait for the run to end. | The preview shows each step's status (a pass mark per step), and the scenario's result and duration. | ☐ |
| R-3 | Click **▷ Run file as test** on the title. | Every scenario of TOD.01 runs, and the preview shows all their results. | ☐ |
| R-12 | Click **▶ Run** on TOD.01.3. | No `dotnet test`: the Output pane says "Running 'TOD.01.3 ...' on the runner", the app opens, the scenario runs past any step breakpoint, and the pane lists its result. The preview shows it. | ☐ |
| R-13 | Change a step's text in TOD.01.3 to another valid phrase (or only add a blank line), save, and click **▶ Run** again. | The Output pane says "Nothing but scenario files changed ... not built again"; the run starts within seconds and runs the text as it is now. Undo the change. | ☐ |
| R-14 | Click **▶ Run file**, then **Cancel run** while it goes. | The file's scenarios run one after another on the runner; Cancel ends it, the app closes, and the pane says the run ended canceled. | ☐ |
| R-4 | While a run goes, click **Run** again on another scenario. | The second run is refused, with a message (in the status bar and the Output pane). | ☐ |
| R-5 | While a run goes, start **Build > Build Solution**. | The build is cancelled, and the Output pane says why. | ☐ |
| R-6 | While a run goes, click **Cancel run** in the preview's bar. | The test process stops and the Todo app closes. | ☐ |
| R-7 | Make a run fail. Change a value in a `Then` step of a copy of TOD.01.3 (in the editor, don't save it elsewhere), run it, then undo. | The failed step shows its message in the preview, with a screenshot thumbnail. Clicking the thumbnail opens the screenshot full size. The step is also an error `UATRUN` in the Error List. | ☐ |
| R-8 | Run the same scenario from **Test > Test Explorer** instead (search for "TOD.01.3"). | It runs, and the preview shows its new results afterwards. | ☐ |
| R-9 | Wait half a minute after the last run. Click **✕** beside TOD.01.3's result; press **Cancel**, then again and **OK**. | The question names the runs and files. Cancel changes nothing; OK removes TOD.01.3's result from the preview, and the Output pane says what was deleted. Other scenarios keep theirs. | ☐ |
| R-10 | Click **Delete results** in the preview's bar, then **OK**. | Every scenario of TOD.01 loses its result; the button hides. `TestResults` keeps the runs that hold other files' results. | ☐ |
| R-11 | In the BDD Explorer's **Results**, click **Delete** on a file, then **Delete all results**. | The file's results, then all of the suite's, go; the view refreshes each time. | ☐ |

## 6. Step breakpoints and Debug

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| B-1 | In the preview, click the keyword of `When I tap Sync` (line 54, in TOD.01.3). | A red dot appears in the preview's gutter, and in the editor's left margin at line 54. | ☐ |
| B-2 | Click the keyword again. Then, in the editor, put the caret on line 54 and press **Ctrl+K, F9**, then **Ctrl+K, Ctrl+F9** (Ctrl held). | The first click removes the dot. **Ctrl+K, F9** sets it again, in both places, and **Ctrl+K, Ctrl+F9** removes it again. | ☐ |
| B-3 | Put the caret on a blank line and press **Ctrl+K, F9**. | No dot. The status bar says the line is not a step. | ☐ |
| B-4 | Right-click the editor on a step line. | The menu has **Toggle BDD Step Breakpoint**, and it works. | ☐ |
| B-5 | Add two empty lines above line 54. | The dot moves down with its step. | ☐ |
| B-6 | With the dot on `When I tap Sync`, click **● Debug** on TOD.01.3's heading. | The run starts, Visual Studio's debugger attaches (the title bar shows it is debugging), and the run stops **before** that step. You are in `BddStepExecutionSession.RunNextAsync`. | ☐ |
| B-7 | At that stop, look in **Locals**. | A local `stepBreak` shows the step's text, its arguments, the app, the page (Todo List) and the call chain. | ☐ |
| B-8 | Press **F11** until you are in the step's code. | You reach the control verb's code for tapping (in Brinell). | ☐ |
| B-9 | Note whether the debugger stopped anywhere **before** your breakpoint, in the test host's own code (S4). | It shouldn't have. If it did, press F5, and note it: `VSTEST_DEBUG_NOBP` did not work here. | ☐ |
| B-10 | Press **F5** to continue. | The run ends normally, and the preview shows the results. | ☐ |
| B-11 | Open the step file. Set a breakpoint on a step inside `## Step: the synced list has todos` (for example its `When I tap Sync`). Debug TOD.01.2. | The run stops at that step each time the scenario calls the `## Step:`. | ☐ |
| B-12 | With a breakpoint set, run TOD.01.3 with **Debug** from **Test Explorer** (right-click the test > Debug), not from the preview. | It stops at the same breakpoint. | ☐ |
| B-13 | Set an ordinary C# breakpoint (F9) in `samples\Todo\tests\Brinell.Samples.Todo.UITests\Pages\TodoListPage.cs`, on the `SyncButton` line (22). Debug TOD.01.3. | Visual Studio stops there as usual. | ☐ |
| B-14 | Set an ordinary C# breakpoint in the Todo **app's** code: `samples\Todo\src\Brinell.Samples.Todo.App\Pages\TodoListPage.xaml.cs`, in a handler the Sync tap reaches. Debug TOD.01.3. | The Output pane says "The debugger is attached to the app too". Visual Studio stops in the app's code. The Debug Location toolbar lists two processes (testhost and the app). | ☐ |
| B-16 | Set a step breakpoint on TOD.01.2's first step (`Given I am on the Todo List page`). Debug TOD.01.2. | The run stops there. The preview shows a yellow bar "Paused before: …" with Continue, Step over, Step into (disabled), Step out (disabled) and Stop; the step is outlined with a ▶. | ☐ |
| B-17 | Press **Alt+Shift+F10** (Step over). | The run stops at the next step, `Given the synced list has todos` (line 34), "Paused at the call"; Step into is enabled now. | ☐ |
| B-18 | Press **Alt+Shift+F11** (Step into). | The step file opens at the call's first step, "Paused before: …", without the app having done anything; Step out is enabled. | ☐ |
| B-19 | Press **Ctrl+Shift+F11** (Step out). | Back in TOD.01, at the first step after the call. | ☐ |
| B-20 | Use the bar's buttons instead of the keys for a few moves, then **Debug** menu > **Step Over BDD Step**; then **Continue**, and last **Stop**. | Each does the same as its key. Continue runs to the next breakpoint or the end; Stop ends the run and the bar goes. | ☐ |
| B-21 | With a step breakpoint on `When I tap Sync` (line 24, TOD.01.1), click **⏭ Step** on TOD.01.1's heading. | The suite's project builds, then the run pauses before the first step (no debugger: the title bar does not say debugging). The bar shows "Paused before: …" and a **📷 Screenshot** button. | ☐ |
| B-22 | Press **F5**, then **F10** a few times. | F5 runs to line 24; F10 moves a step each time. Under "Paused before" the bar shows the step's page and arguments, e.g. `Page: Todo List · control = "Sync"` at line 24. | ☐ |
| B-23 | At a stop, click **📷 Screenshot**. | A screenshot of the Todo app as it is now opens in Visual Studio; the run stays paused. | ☐ |
| B-24 | Press **F5** to the end. | The run passes; the preview shows TOD.01.1's result. | ☐ |
| B-15 | Remove every step breakpoint. Close Visual Studio. Open `%LOCALAPPDATA%\Brinell\bdd-breakpoints.json`. | It lists no lines for TOD.01 (or has no entry for it), and no `next`. | ☐ |

## 7. Editing help

Open TOD.01 for these.

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| E-1 | Open **View > Other Windows > Document Outline**, or use the navigation bar above the editor. | The feature's headings as a tree: the Metadata section and each Scenario. | ☐ |
| E-2 | Hover in the margin left of a `## Scenario:` heading, a gherkin fence or a table. | Each can be folded and unfolded. | ☐ |
| E-3 | Look at a gherkin fence. | The step keywords, the quoted values (`"Buy bread"`), `{x}`/`<x>` placeholders, the tags (`@smoke`) and comments in a fence have their own colours. | ☐ |
| E-4 | In a fence, start a new line and type `Wh`. | Completion offers **When**. | ☐ |
| E-5 | Type `When tap` (not `I tap`). | Completion offers `I tap {control}`, found by the word, not the start. Picking it puts in `I tap control` with `control` selected. | ☐ |
| E-6 | With `control` selected, delete it and wait, or press **Ctrl+Space**. | Completion offers the Todo List page's controls, among them **Sync**. | ☐ |
| E-7 | Type a new line `Given I am on the ` and press **Ctrl+Space**. | Completion offers the pages, among them **Todo List**. | ☐ |
| E-8 | Type `Given synced todos`. | Completion offers `the synced list has todos`, a `## Step:` of the step file. | ☐ |
| E-9 | On an empty line outside a fence, type `## Sc`. | Completion offers `## Scenario:`. | ☐ |
| E-10 | In the Metadata table, clear the value of `TestData` and press **Ctrl+Space** in that cell. | Completion offers the test sets: `empty`, `one-todo`, `three-todos` and the others. | ☐ |
| E-11 | Hover over `When I tap Sync`. | A tooltip: what it binds to, the page, and its argument `control` = Sync. | ☐ |
| E-12 | Hover over `And the server answers after 2 seconds` (line 23). | It shows the C# method it binds to, and `seconds` as written and as read. | ☐ |
| E-13 | Put the caret on line 23 and press **F12**. | `BackendPhrases.cs` opens at the method with `[BddGiven("the server answers after {seconds} seconds")]` (line 40). | ☐ |
| E-14 | **F12** on `When I tap Sync`. | The page object opens at its Sync control. | ☐ |
| E-15 | **F12** on `Given I am on the Todo List page`. | The Todo List page class opens. | ☐ |
| E-16 | **F12** on `Given the synced list has todos`. | The step file opens at `## Step: the synced list has todos` (line 3). | ☐ |
| E-17 | **Shift+F12** on `When I tap Sync`. | Find All References lists every tap step across the suite, in several feature files. | ☐ |
| E-18 | In the step file, **Shift+F12** on the `## Step: the synced list has todos` heading. | It lists every call of that step, TOD.01 line 34 among them. | ☐ |

## 8. Quick fixes and rename

Work on a copy, so nothing real changes: copy TOD.01 to `Features\TOD.98-manual-test.feature.md`
in Solution Explorer, and open the copy. Delete it at the end of the section.

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| F-1 | Add a step `When I polish the list "kitchen" 3 times`. Put the caret on it and press **Ctrl+.** | The light bulb offers **Create step definition in …**. | ☐ |
| F-2 | Pick it. | A C# file opens (a step-definition class of the suite) with a new method: `[BddWhen("I polish the list {text} {number} times")]` and `public void IPolishTheListTimes(string text, int number)`. Undo the change in that file. | ☐ |
| F-3 | Select three step lines of one scenario and press **Ctrl+.** | **Extract to '## Step: the extracted steps'**. Picking it puts a call in their place, and a new `## Step:` section at the end of the file. The file still binds (no new errors). | ☐ |
| F-4 | Put the caret on that new call and press **Ctrl+.** | **Inline 'Step: the extracted steps'**. Picking it puts the three steps back. | ☐ |
| F-5 | Extract again. Then put the caret on the new `## Step:` heading and press **F2**. Rename it to `the steps I moved`. | The heading and the call both change. | ☐ |
| F-6 | Press **F2** on the heading again and give a name with a placeholder, such as `the steps for {who}`. | The rename is refused with a message about keeping the placeholders. | ☐ |
| F-7 | Change a date in a table or step to the form `2026-05-12` (where the file uses `12-May-2026`, if there is one), and press **Ctrl+.** on it. | **Write '12-May-2026'** is offered, and fixes it. | ☐ |
| F-8 | Delete the copy. | The Error List drops its entries. | ☐ |

## 9. The suite config

Open the config (`uat.config.md`). Undo every change afterwards.

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| C-1 | Look at the Error List for the config. | No errors. (Warnings for assemblies not built are fine; note them.) | ☐ |
| C-2 | Change `| Head | maui |` to `| Head | desktop |`. | An error `UATG004`: not a head, listing the heads. | ☐ |
| C-3 | Change `| AllowNameInference | true |` to `yes`. | An error: true or false. | ☐ |
| C-4 | Add a row `| Fixtur | x |` to the Runtime table. | A warning `UATG002`: not a Runtime field. | ☐ |
| C-5 | Remove the `Fixture` row. | An error `UATG003`: the Runtime table names no Fixture. | ☐ |
| C-6 | In an empty value cell of the Runtime table's `Head` row, press **Ctrl+Space**. | The heads: maui, web, mixed, service. | ☐ |
| C-7 | Hover over the word `Fixture`. | A tooltip saying what the field is for. | ☐ |
| C-8 | Open `samples\Todo\tests\Brinell.Samples.Todo.TestSupport\TestData\testdata.schema.json` and add a space somewhere, save, then look at the config. | A warning `UATG009` on the `TestData` row: the schema is out of date. **Ctrl+.** on it offers to regenerate it, and doing so clears the warning. Check with `git diff` that the schema is back as it was. | ☐ |

## 10. CodeLens in C#

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| L-1 | Open `samples\Todo\tests\Brinell.Samples.Todo.TestSupport\Uat\BackendPhrases.cs`. Wait a few seconds. | Above the method at line 40, CodeLens says "used in N BDD steps · M scenarios". | ☐ |
| L-2 | Click it. | A table lists each step: file, line, step text, scenario. | ☐ |
| L-3 | Open `samples\Todo\tests\Brinell.Samples.Todo.UITests\Pages\TodoListPage.cs`. | Above `SyncButton`, CodeLens counts the tap steps on the Todo List page. | ☐ |
| L-4 | Find a method no step uses (any private helper). | No BDD CodeLens above it. | ☐ |
| L-5 | Open a C# file in a solution with no suite (any other solution). Watch Task Manager. | No BDD CodeLens, and no `dotnet` process running `Brinell.Bdd.LanguageServer.dll` is started for it. | ☐ |

## 11. BDD Explorer

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| X-1 | **View > Other Windows > BDD Explorer**. | The window opens on **Steps** for a suite, with a suite picker. | ☐ |
| X-2 | Look through Steps. | Groups by class, by page, control verbs and built-ins, then the step file's `## Step:` sections, then the hooks of each step-definition class. | ☐ |
| X-3 | Click a C# step, a `## Step:`, and a hook. | Each opens its source at its line. | ☐ |
| X-4 | Click **Pages**. | Each page with its controls (type, and where tapping goes) and its page steps. Clicking a control opens it. | ☐ |
| X-5 | Click **Results**. | The last run of each scenario you ran in section 5, by feature file, with status and duration. Clicking one opens its heading. | ☐ |
| X-6 | Pick another suite in the picker. | The views change to that suite. | ☐ |
| X-7 | Run a scenario (R-1) with Results showing. | Results refreshes when the run ends. | ☐ |
| X-8 | With the explorer open, switch Visual Studio's theme (**Tools > Theme**) between a dark and a light one. | The explorer's colours follow at once, as the preview's do (P-11). | ☐ |

## 12. Copilot (optional)

Only with GitHub Copilot, Visual Studio 17.14 or later.

| ID | Do | You should see | Result |
| --- | --- | --- | --- |
| M-1 | Add the `.mcp.json` from the guide's "Copilot And Other Agents" section to the Brinell root, and reopen the solution. | Copilot Chat's tools list has **brinell-bdd** with six tools. | ☐ |
| M-2 | In agent mode, ask: "Using brinell-bdd, list the steps with 'tap' in the Todo MAUI suite." | It calls `list_steps` and shows `I tap {control}` among them. | ☐ |
| M-3 | Ask it to write a scenario "the user adds a todo called Buy milk" for the Todo MAUI suite, and validate it. | It uses only existing steps, calls `validate_scenario`, and fixes what it reports. Remove `.mcp.json` afterwards. | ☐ |

## 13. Observations for the spike questions

Write what you saw; these have no pass or fail.

| # | Question | If not, the change to make | What you saw |
| --- | --- | --- | --- |
| S1 | Would anything above need a feature the in-process extension has and an out-of-process one lacks? (Background: the extension is in-process.) Note anything that felt slow or froze Visual Studio. | Stay in-process (VSSDK); nothing to change. | |
| S2 | Did Markdown still work in feature files: its colours, and Visual Studio's own Markdown preview? Did the semantic colours (E-3) show? | A content type of our own for `*.feature.md` and `*.steps.md`, derived from Markdown (not `*.feature`, which Reqnroll's extension owns), and semantic colours through a classifier fed by the server. | |
| S3 | Did runs from the preview show up in Test Explorer's results? (They are not expected to; note it.) | Nothing: the preview runs `dotnet test` and writes the Output pane. | |
| S4 | From B-9: did the debugger stop in the test host before your breakpoint? | Start the test host ourselves with `vstest.console` under the debugger. | |
| S5 | In Test Explorer, does **Go To Test** (or double-click) on a scenario open the `.feature.md` at its heading? | Our own *Go to scenario* on the case, from its `BddScenario` trait. | |
| S6 | From P-13 and P-14: focus, keys, dock and undock. | Fix each issue in the preview window. | |

## 14. Results

| Section | Pass | Fail | Blocked | Notes |
| --- | --- | --- | --- | --- |
| 2 Setup | | | | |
| 3 Preview | | | | |
| 4 Diagnostics | | | | |
| 5 Run | | | | |
| 6 Breakpoints and Debug | | | | |
| 7 Editing help | | | | |
| 8 Quick fixes and rename | | | | |
| 9 Config | | | | |
| 10 CodeLens | | | | |
| 11 BDD Explorer | | | | |
| 12 Copilot | | | | |

Tester: ______  Date: ______  Visual Studio version: ______  Commit: ______

When done:
1. Put the results and the findings (each with its screenshot and Output pane text) in a
   `results-<date>.md` beside this file.
2. Write a line in [.my/README.md](../../README.md).
3. Any failure becomes a fix. When everything passes, move what the run teaches to AD-022 or the
   guide, then delete this plan.
