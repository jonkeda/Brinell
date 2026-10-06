# Recording tests

Status: **built, steps 1-8**, 2026-10-03 (section 9). Q2 is a setting; Q1 and Q3-Q7 as proposed (section 8). Step 9 is the owner's, by hand; step 10 is later.

A person uses the app and the Scraper records what they do. Each recorded action is resolved to the
page and the control it hit. From that, the Scraper drafts a test: an xUnit UI test that calls page
objects, a `.feature.md` scenario that uses only catalog phrases, or both. The draft is built and run
once.

This is the "later" in [ScraperEx plan section 7](../ScraperEx/plan.md) and requirement 8 there. The
recording format was designed with it in mind: `recording.json` is already an ordered event list.

## 1. Where this fits

| Exists | What it gives this |
| --- | --- |
| **Running** (the gate, the IDEs' Run, AD-022) | Runs the drafted test or scenario once, as the last check |
| **Presenting** (the Presenter, AD-018) | A recorded scenario is an ordinary scenario, so the Presenter can show it |
| **Scraping** (AD-027) | The recording session, the browser and its profile, snapshots, masks, page keys, page objects with `// <scraped …/>` headers, `ProjectCode` (members and their locators), `SnapshotSelector` (a locator matched against a snapshot, no browser), the control brief, `check_code`, Verify |
| **The phrase catalog** (AD-011, AD-016, AD-017, AD-019) | Control verbs are already phrases on the core interfaces (`I tap {control}`, `I enter {value} into {control}`, `I select {value} from {control}`, `I check {control}`). `list_steps` and `validate_scenario` are MCP tools |
| **Narrated recordings** (AD-024) | Not reused. "Record" already means a video there, so this feature is named **Record a test** throughout |

Nothing in this proposal needs a new store or a model call. Resolving actions and drafting tests are
rules over files. As with controls, the agent improves the draft and the Scraper checks it.

## 2. The idea in one example

The person clicks **Record a test** on the Blazor sample's User Form, types a first name, ticks
Terms, chooses a country, clicks Submit, and clicks **Assert** on the confirmation text.

`recording.json` gets these events, with the nodes in the page capture taken before them:

```json
{ "number": 1, "kind": "page",   "page": "/userform", "snapshot": "001-user-form.nodes.jsonl" },
{ "number": 2, "kind": "input",  "page": "/userform", "node": 41, "value": "Ada" },
{ "number": 3, "kind": "check",  "page": "/userform", "node": 77, "checked": true },
{ "number": 4, "kind": "select", "page": "/userform", "node": 95, "value": "Netherlands" },
{ "number": 5, "kind": "click",  "page": "/userform", "node": 130 },
{ "number": 6, "kind": "assert", "page": "/userform", "node": 141, "check": "text", "value": "Thanks, Ada" }
```

**Resolving** a node uses the page objects' headers to find the page object for `/userform`. It
then uses that page object's members, through `ProjectCode` and `SnapshotSelector`, to find which
member's locator matches the node or its nearest ancestor:

| # | Member | Control | Verb | Phrase |
| --- | --- | --- | --- | --- |
| 2 | `UserFormPage.FirstName` | `TextInputControl` | `Enter` | `I enter "Ada" into First Name` |
| 3 | `UserFormPage.Terms` | `CheckBoxControl` | `Check` | `I check Terms` |
| 4 | `UserFormPage.Country` | `SelectControl` | `SelectByText` | `I select "Netherlands" from Country` |
| 5 | `UserFormPage.Submit` | `ButtonControl` | `Tap` | `I tap Submit` |
| 6 | none | | | **gap:** node 141 has no member |

**Drafting** produces two files. The first is an xUnit test:

```csharp
// <recorded session="2026-10-03-user-form" hash="…" />
[Fact]
public async Task Submitting_the_form_thanks_the_user()
{
    var page = await App.OpenAsync<UserFormPage>();
    page.FirstName.Enter("Ada");
    page.Terms.Check();
    page.Country.SelectByText("Netherlands");
    page.Submit.Tap();
    // gap: event 6 asserts "Thanks, Ada" on node 141, which no member covers
}
```

The second is a `.feature.md` scenario. It has the same steps in a `gherkin` block, and the gap is a
prose line outside that block (AD-023), so it does not bind.

The gap goes to the agent with a brief: add a `Confirmation` member to `UserFormPage`, then fill in
the step. The Scraper builds the result (`check_code`) and runs it once through the IDE's runner.

## 3. Decisions proposed

| # | Decision | Why |
| --- | --- | --- |
| R1 | **A test recording is a recording session** with actions in it. `recording.json` gains event kinds `click`, `input`, `select`, `check`, `key` (Enter, Escape, Tab only), `navigate`, `dialog` and `assert`. Page events stay as they are | One format, one folder, one index. The same session also feeds the Scraper: its pages can make page objects, so a person can record a test on an app that has no page objects yet |
| R2 | **An action points at a node in the most recent capture of its page.** The recorder script records the element's number from the capture's element list. If the element came after that capture, the host first takes a silent capture after the page settles (`settle.js`), then records the action. If even that fails, the event carries the element's path from the root (type, test id, role, name, classes per level) | Resolving is then offline and deterministic, from files only, so it can be tested without a browser and repeated after page objects change |
| R3 | **Resolution is a separate pass over a stored recording**, not done while recording. It goes node → page object (by page key and header) → the deepest member whose locator matches the node or an ancestor (`SnapshotSelector`) → the control's interface → the verb → the phrase (`list_steps`) | A recording made before the page objects exist can be resolved after they are made. Re-resolving after a rename costs nothing |
| R4 | **Rules draft, the agent improves, the Scraper checks**, as AD-027 already holds for page objects. The rules write every resolved step and mark every gap. Gaps are a node with no member, a member whose control has no verb for the action, or a verb with no phrase. The agent gets a brief per gap and fills it by adding the member, the control verb or the phrase. It never invents a scenario phrase (AD-017) | The same split that worked for controls, and the same budget per prompt (5 briefs) |
| R5 | **Assertions are recorded, not guessed.** In the recording browser, **Assert** puts the person in pick mode (`pick.js`). They click an element and choose a check: is visible, has the text, has the value, is checked, is on the page. The draft also adds a check on the page reached after each navigation (`I should be on the X page`), the one check that is always safe | A guessed assertion either checks nothing or checks text that changes. A test without its checks is not a test |
| R6 | **No waits in the draft** (AD-004). Each control already waits for its element to be ready, and a navigation becomes the page check from R5. The time between events is kept in `recording.json` only for a person reading it | Recorded pauses turned into sleeps are what make recorded tests flaky in other tools |
| R7 | **Typed values: an amendment to AD-027.** A capture still never reads `value`. Only the action events of a test recording keep what was typed or chosen, and the masks apply to them before they are written. These are never kept: `type=password` fields, fields that match a new `## Secrets` table in the config (by test id, name or label), and anything a mask matches. Those become a `{placeholder}` the person maps to a setting or test data (AD-012) | A test needs its inputs. A secret never lands in the repository |
| R8 | **Drafted tests carry a header**: `// <recorded session=… hash=… />` at the top of the test file, or in the scenario's `## Metadata`. This applies the rule that the code is the registry to tests as well | A teammate can see what a test came from, and the view can say "the recording changed since this test" |
| R9 | **A platform is a source.** `IScrapeSource` gets an `Acted` event (the event plus its node). Html (Playwright) comes first. MAUI and desktop come later, through UI Automation events (Invoke, ValueChanged, Toggle, SelectionItem), once the UI Automation scrape source is there (ScraperEx open item) | The engine stays platform-neutral (AD-001). The MAUI scrape source is a project of its own anyway |

## 4. The recorder (Html)

`recorder.js` is an embedded script beside `capture.js`. Playwright injects it with `exposeBinding`,
into every frame.

| Listens to | Records | Notes |
| --- | --- | --- |
| `click` (capture phase), on an element or its nearest clickable ancestor (`a`, `button`, `[role=button]`, `[onclick]`, `label`) | `click`, the node | A click on a `label` resolves to the input it labels |
| `change` on `input` and `textarea`, or `blur` after `input` events | `input`, the node, the final value | One event per field visit, not per key |
| `change` on `select`, or a click on a `role=option` inside a `role=listbox` | `select`, the node of the select, and the option's text | The owned-list rule from the control brief (step 7) maps the option back to its select |
| `change` on a checkbox or radio, or `aria-checked` flipping after a click | `check`, the node, the new state | |
| `keydown` Enter, Escape or Tab | `key` | Other keys are part of `input` |
| The source's `Transitioned`, and the auto capture | `navigate`, then a `page` | The page event already exists |
| `page.on('dialog')` | `dialog`, its text, accepted or dismissed | |

**Merging, in the engine:** a `click` followed by an `input` on the same node becomes one `input`.
Several `input` events on one field without another action between them become the last one. A
`click` on a select's own element, followed by its `select`, becomes the `select`.

## 5. Drafting

| Target | Where | What |
| --- | --- | --- |
| xUnit UI test | `Tests/Recorded/<Name>Tests.cs` in the scraped project, a `partial` class; the person's part is never touched | One `[Fact]` per recording. It starts by opening the first page (the page object's own way of opening), then makes one page-object call per resolved step, and puts a `// gap:` line per gap |
| Scenario | `Features/Recorded/<name>.feature.md` | `## Scenario:`, `Given I am on the <first> page`, one step per resolved action, `Then` per assert, and gaps as prose. Checked with `validate_scenario`. Bound only by catalog phrases |

Both targets come from the same resolved list. The default is a setting in the project's config:
`## Tests` → `Target: xunit | scenario | both`. `brinell scrape new` and the view's create write it
(`scenario` when the project has a `uat.config.md`, otherwise `xunit`), so it is always visible and the
person changes it in one place. When it is missing, the same rule applies. The person can override it
per recording in the view, and `draft-test --target` on the command line.

**Values.** A literal stays a literal. A value that also appears on a later page (an id, a number
the app made) becomes a check, not an input. A placeholder from R7 is filled from settings or test
data, and the draft says which.

## 6. Surfaces

| Where | What |
| --- | --- |
| Scraper view | A **Tests** step after Verify. It has **Record a test** (the browser opens on the start URL or the current page, with a small toolbar: **Assert**, **Stop**, a counter), the list of recorded tests with their resolved steps, each gap with **Fix with the agent**, **Draft** (xUnit, scenario or both), and **Run once** |
| Feature file | A CodeLens **Record steps here** on a scenario heading. It records from the page the scenario reaches and puts the resolved steps after the last step. Later step |
| MCP | `start_recording` gets `actions: true`. New tools: `list_recorded_actions` (a session's events), `resolve_actions` (the steps and gaps), `draft_test` (target, name), and `test_brief` (one gap as a brief). `check_code` and `validate_scenario` are unchanged |
| Command line | `brinell scrape resolve <session>` and `brinell scrape draft-test <session> --target both`, model-free (AD-028) |
| Agent | The `brinell-scraper` agent and the `scrape-pages` skill get a "tests from a recording" section. The view sends gap briefs as it sends control briefs (agent and model from the header) |

## 7. Steps

| # | Step | Check |
| --- | --- | --- |
| 1 | Event model: the new `RecordingEvent` kinds and fields (`node`, `path`, `value`, `checked`, `check`), read and written; old recordings still read | Unit tests on files |
| 2 | `recorder.js` and the source's `Acted` event; the silent capture before an action on a new element; merging | Browser tests (`Category=Browser`) on the test site and the Blazor sample's User Form |
| 3 | R7: the `## Secrets` table, password fields, masks on values, placeholders; amend AD-027 | Unit tests; a recording of a login form holds no password |
| 4 | Resolution: node → page object → member → verb → phrase; gaps with a reason | Unit tests on stored recordings: the User Form example resolves to section 2's table |
| 5 | Drafting both targets, the `recorded` header, `## Tests` in the config | `check_code` and `validate_scenario` pass on the drafted User Form test |
| 6 | Assert: the toolbar, pick, the five checks | Browser test |
| 7 | The view's Tests step, Run once through the IDE runner | Stand-in host; then in the IDEs |
| 8 | Gap briefs, MCP tools, the command line, the skill and the agent | An agent closes the User Form gap (`Confirmation`) and the test passes |
| 9 | Acceptance: Bouw7, one journey recorded (open a project, edit a field, save), drafted, the gaps fixed, the test run | By hand, in Visual Studio |
| 10 | Later: **Record steps here** in a feature file; the UI Automation source for MAUI and desktop | |

Steps 1 to 5 need no IDE and no model. They make the whole pipeline testable from files.

## 8. Open questions

| # | Question | Proposal |
| --- | --- | --- |
| Q1 | Is R7, keeping typed values in test recordings, acceptable as an amendment to AD-027? | Yes, with secrets as placeholders and masks applied first |
| Q2 | xUnit, scenario, or both by default? | **Decided: a setting**, `## Tests` → `Target` in `scraper.config.md` (section 5). It is written when the project is created: scenario when the project has a UAT config (`uat.config.md`), otherwise xUnit |
| Q3 | Does the Tests step belong in the Scraper view or the BDD preview? | Scraper view: it owns the browser and the recording. The feature file only gets the CodeLens (step 10) |
| Q4 | Should a recording on a page with no page object first make the page object (Draft), then resolve? | Yes, automatically: Draft by rules, then resolve; the remaining gaps go to the agent |
| Q5 | Can one recording become several tests (split at each navigation)? | No for now: one recording is one test. The person records again, or edits |
| Q6 | Should requests the app made be offered as checks (`the server received …`)? | Later. The request guard already sees them, but it needs the Backend subject (uat/complex) |
| Q7 | Should Construction (MAUI) come before Bouw7 (Html)? | Html first: the browser and every step up to Verify are built. MAUI waits for the UI Automation source |

## 9. Built

| Step | Commit | What differs from the plan |
| --- | --- | --- |
| 1 | Event model | Actions also carry `capture` (the page event their node is in) and `accepted` (a dialog); `CaptureOf` falls back to the latest capture of the page |
| 2 | `recorder.js`, `Acted` | An action's own capture is taken unsettled and kept only while still on its page; else the path (R2). Browser tests run on the test site's `pages/form.html`, not on the Blazor sample's User Form |
| 3 | R7 | A secret never leaves the browser: `recorder.js` sends its placeholder. `## Secrets` has `Field` and an optional `Placeholder` |
| 4 | Resolution | Verbs come from Brinell.Html's controls by reflection (Brinell.Blazor's share their names); the step-name rule moved to `Brinell.Core` (`StepNames`) |
| 5 | Drafting | `## Tests` also has `Base`, the class an xUnit draft's person's part derives from. A secret is an environment variable in the xUnit test and a gap in the scenario: a step cannot name a setting yet. Checked on the Blazor sample: built, ran green, `validate_scenario` valid |
| 6 | Assert | The toolbar is in a shadow root; Assert is also on the view's strip |
| 7 | The view's Tests step | **Run once runs in the server** (`dotnet test` with the class, or the scenario's address from `brinell/runTarget`), not the IDE's test UI: the view, MCP and the command line run a draft the same way. A suite whose scenario class is `[Theory]` cannot run one scenario; the view says so. Tried through a stand-in host; not inside VS Code or Visual Studio |
| 8 | Briefs, MCP, CLI, agent | Also `run_test` (MCP). Checked with Claude in the agent's place over MCP on the User Form (a contact radio with no member); not with Copilot or Claude Code as the agent |

Open: step 9 (Bouw7 in Visual Studio, by hand); the Tests step tried inside both IDEs; step 10.
