# The Scraper view as a workflow: proposal

Status: **proposal**, 2026-10-01; revised 2026-10-02 after review (section 10). A clickable mockup: [workflow-mockup.html](workflow-mockup.html)
(open it in a browser; the data in it is made up). Builds on the view as built
([plan.md](../plan.md) steps 7-9, [auto-plan.md](../auto-plan.md) step 9,
[updates/vs-scraper-window.md](../updates/vs-scraper-window.md)).

## 1. Why

The view today is a toolbox: a bar of seven controls (project, session, start URL, Record,
Capture, Pick, Stop, plus Continue and This page… when they apply) and six tabs (Pages, Patterns,
Code, Explore, Explore settings, Generate…). Everything is there, nothing says what comes first.

| What a person meets | Why it is hard |
| --- | --- |
| The first thing in view is **Record**, with a session name and a start URL | Recording by hand is the slow way; **Explore**, the fast way, is the fourth tab |
| **Draft page objects**, **Verify** and **Improve with the agent** are buttons on the Pages tab | They are the last steps of the work, shown beside its first (the recorded pages) |
| Patterns are a tab of their own; making a control is a button on a node | The order the skill wants (controls first, then pages) is not visible |
| Explore and its settings are two tabs | The settings are needed before the first run and rarely after |
| Nothing shows what is done | Whether a project has recordings, drafts that build, members that verify, or pages that changed since, is found by clicking around |

## 2. Principles

1. **One path, always a next step.** The view knows where the project is and says what to do next,
   with one button for it.
2. **Show the state, not the tools.** Each step says what it found ("9 pages, 22 captures", "36 of
   42 members found") before what can be done.
3. **One primary action per step**; the rest under **More**.
4. **Steps can be skipped and revisited**, never locked: the path guides, it does not block. A step
   that needs an earlier one says which, and links to it.
5. **Nothing is lost.** Every action of today's view has a place (section 7).
6. **One page for both IDEs**, as today: the logic stays in the server's page; the IDEs relay.

## 3. The workflow

```text
 1 Project ──► 2 Connect ──► 3 Collect pages ──► 4 Review ──► 5 Controls ──► 6 Page objects ──► 7 Verify
                                   ▲                                              │                │
                                   └──────────── Keep current: the app changed ◄──┴────────────────┘
```

| # | Step | Is about | Primary action | Done when |
| --- | --- | --- | --- | --- |
| 1 | **Project** | Which UI test project the page objects go into | **Choose** a project, or **New project** (the form behind today's **+**) | A project with a `scraper.config.md` that reads |
| 2 | **Connect** | Can the Scraper reach the app, as whom, and where may it go | **Check the app**: opens the start URL, says whether it answered, whether a sign-in page came, and whether its host may be explored. A sign-in: **Open the browser**, sign in, **Continue** | The start page was reached signed in (kept per project until the profile is cleared) |
| 3 | **Collect pages** | Getting the app's pages and states into `Recordings/` | **Explore** (a dry run the first time, then for real), with the guard summary above the button. **Record by hand** under More | At least one page recorded |
| 4 | **Review** | What was recorded: pages, their captures, and what Explore skipped or blocked | none needed: **Next** goes on. Per page: the outline, highlight, Compare; per skipped element: Never click / Always click | Never blocks; shows a count of pages and anything blocked |
| 5 | **Controls** | The parts that repeat (Draft's "make a control" list) | **Make controls with the agent** (one prompt for all), or per pattern **Make a control** | No pattern left that asks for a control, or the person skipped them |
| 6 | **Page objects** | A page object per page | **Draft page objects** (rules, no model), then **Improve with the agent** | Every recorded page has a page object that builds |
| 7 | **Verify** | Do the page objects find the app's elements | **Verify** against the running app | Every member found once (unread members do not fail it) |

**Keep current** is not a step: when a page changed since its page object was made, or Verify
finds a member missing, steps 3, 6 and 7 get a badge ("2 pages changed") and the overview's next
action becomes **Re-record the changed pages**, then **Update page objects**.

## 4. The screen

```text
┌ Scraper ───────────────────────────────────────────────────────────────────────────────────┐
│ Project [Brinell.Html.React v] [+]          Next: Make the 3 controls Draft asked for  [Go]│
├──────────────────────┬─────────────────────────────────────────────────────────────────────┤
│ ✓ 1 Project          │  5  Controls                                                        │
│ ✓ 2 Connect          │  Parts that repeat become a control first, so page objects use it.  │
│ ✓ 3 Collect pages    │                                                                     │
│     4 pages, 9 caps  │  To make                                     Control name           │
│ ✓ 4 Review           │   ┌────────────────────────────────────────────────────────────┐   │
│ ● 5 Controls      3  │   │[x] div.element-group   6× /tabs   sidebar group [SideMenuGroup]│
│ ○ 6 Page objects     │   │[x] li.btn.btn-light   32× /tabs   menu item     [SideMenuItem] │
│ ○ 7 Verify           │   │[ ] tr                  8× /tables table row     [OrderRow]     │
│                      │   └────────────────────────────────────────────────────────────┘   │
│ ─────────────────    │                                                                     │
│ Code                 │  [ Make 2 controls with the agent ]   More ▾                       │
│                      │                                                                     │
│                      │  Skip: page objects without controls                    Next: 6 ▸  │
└──────────────────────┴─────────────────────────────────────────────────────────────────────┘
```

| Part | What holds |
| --- | --- |
| **Header** | The project picker and **+**, and the **next action** for the whole project (the first step not done, or a Keep current item), with **Go** |
| **Step list** (left) | The seven steps, each with its state: ✓ done, ● the next one, ○ to do, ! needs attention (a guard stopped Explore, a draft did not build, members missing), and a count where one helps |
| **Step panel** (right) | One sentence on what the step is for; what is there now; the primary action as the one filled button; the rest under **More ▾**; at the bottom **Skip** (where a step can be) and **Next ▸** |
| **Code** (below the steps) | Not a step: the project's page objects and controls (today's Code tab), reachable from anywhere. Explore's settings are in step 3, where they are used |
| **Running strip** | While a recording or an Explore runs, a strip under the header on every step: what runs, its progress ("captured 4, clicked 3, skipped 33, blocked 5"), **Capture**, **Pick**, **Stop**, **Continue** after a sign-in, **This page…**. Hidden when nothing runs |
| **Narrow window** | A Visual Studio tool window docked at the side: the step list becomes a row of numbered chips under the header (`1 ✓ 2 ✓ 3 ✓ 4 ✓ 5● 6 7`); the panel takes the width |
| **First time** | A project with no recordings opens on step 2 with, once, above it: **Three steps to page objects**, as a numbered list (1 Connect to the app, 2 Collect its pages, 3 Draft page objects), and "Each step says what to do next." |

### The steps, panel by panel

| Step | Shows | Primary | More |
| --- | --- | --- | --- |
| 1 Project | The project's folder, platform, start URL, namespace, and its config file | **Choose** (the picker) / **New project** | Open `scraper.config.md`; Add a config to an existing project |
| 2 Connect | The start URL; the result of the last check: answered or not, signed in or a sign-in page, the host allowed or refused (and why); the guard summary in one line | **Check the app** (then **Open the browser** / **Continue** for a sign-in; **Allow this host** when refused) | Change the start URL; aliases; clear the browser profile |
| 3 Collect pages | **Guards and budget as a form**: max pages, max depth, max minutes, pause between clicks, Within (number and text fields); Clicks (ClickPolicy) and Websocket apps (dropdowns); blocked request methods and danger-word categories (checkboxes); Explore hosts (a list with add and remove). A changed field is outlined, the line under the form says which changed and that they are not saved; **Save** writes only those rows to `scraper.config.md` (the rest of the file kept, as `configEdit` does), **Undo changes** puts the form back. Below: the last runs (captures, clicks, skipped, blocked, why it stopped) | **Explore** (dry run ticked until a dry run has passed once); with unsaved changes it asks to save first | **Record by hand** (session name, start URL; then the running strip); Never click, Always click, Never visit, Allow requests (the lists); open `scraper.config.md`, `explore.md` |
| 4 Review | The pages, each with its captures and whether it changed; what Explore skipped and blocked, grouped by reason | **Next** | Per page: outline, highlight in the browser, Compare captures, Changes since the page object; per skipped element: Never click, Always click; per blocked request: Allow |
| 5 Controls | **To make**: the patterns that ask for a control, each with a tick box (and one for all), where it occurs, what it looks like, and the control's name (proposed, editable). **Made**: the controls the agent made, each with its file, the pattern it came from, what uses it, whether it builds, and how often its parts are found in the recordings; **Review** (or **Review and fix** when a part is not found everywhere) | **Make *n* controls with the agent**: one prompt for the ticked patterns, with their names; each shows under Made as it is written | Show all patterns (forms, tables, navigation); open the Controls folder |
| 5 › a control | **Review**: the control's code; its parts (each member and the root) with their locator and in how many of the pattern's occurrences in the recordings each is found once (`show the 2` opens the captures with the misses highlighted); what uses it; a note for the agent, proposed from what is not found | **Fix with the agent**: the control, its parts' results and the note go to the agent; the change comes back as a diff and the control is checked again | **Accept** (as it is; remembered), **Check again**, rename the control, open the file to fix by hand, delete it |
| 6 Page objects | Per page, with a tick box (and one for all): the page, its **class name** (editable), its file, members and unstable ones, and drafted, hand-made, not drafted, or changed since | **Draft *n* page objects**: only the ticked ones; then **Improve with the agent** for the ticked ones | Draft all pages; Generate page object (the agent); open the Pages folder. **Renaming** a class renames it in its `.scraped.cs`, the person's part (`.cs`) and the files' names; a draft keeps the name after (it is read from the header's page, as today) |
| 7 Verify | Per page object: found once, several, not found, not read; when it ran, against which URL | **Verify** | Open a member's page object; re-record a page (step 3 for that page) |

## 5. Where the state comes from

One request, `brinell/scraper/progress` (project) → per step its state, its count and its next
action; computed from what the server already knows, so it is the same in both IDEs and over MCP:

| Step | Done | Attention | Count |
| --- | --- | --- | --- |
| 1 | the config reads | the config has errors | — |
| 2 | a capture of the start page exists, or the last check passed | the last check: not answered, a sign-in, host refused | — |
| 3 | `RecordingIndex.Pages` not empty | the last Explore stopped for a guard | pages, captures |
| 4 | step 3 done (Review never blocks) | blocked requests in the last run | pages, blocked |
| 5 | `DraftRun.ControlsToMake` is empty and every made control is checked or accepted, or the step is skipped (kept in the config: `## Workflow`, `SkipControls`) | a made control has a part not found in every occurrence and is not accepted | patterns asking; controls to fix |
| 6 | every page has a page object (`ProjectCode` headers) | a draft put back; a page changed since its page object | pages without one, changed |
| 7 | the last verify passed and nothing was drafted since | members not found or found several times | members found / all |

The last check (step 2) and the last verify (step 7) are written to `Recordings/.scraper-state.json`
(git-ignored), so a reopened view knows them; everything else is read from the files as today.

## 6. What changes for the code

| Part | Change |
| --- | --- |
| `ScraperPage` | The tabs become the step list and panels; the bar becomes the header and the running strip. The existing functions (`loadPages`, `loadPatterns`, `loadExplore`, `loadSettings`, `loadCode`, `showNewProject`) are the panels' bodies, rearranged, not rewritten |
| Server | `brinell/scraper/progress`; `brinell/scraper/check` (step 2: open the start URL headless with the project's profile, report answer, sign-in, host); the state file. Step 3: `config` and `configEdit` as today, several edits in one `configEdit` call (one Save, one write). Step 5: `controlCheck` (a control's parts resolved in each of its pattern's occurrences in the recordings, as Verify resolves a page's members, but against the snapshots, so no browser), `makeControls` (the prompt for the ticked patterns and names), `acceptControl` (kept in the state file). Step 6: `draft` with a list of page keys (today one page or all), `renamePageObject` (class, files, the person's part) |
| MCP | `get_scraper_progress`: an agent sees the same next step |
| IDEs | Nothing new: they relay as today |
| AutomationIds | The controls keep their ids; steps get `sc-step-<n>`, the primary action `sc-primary` |

## 7. Every action of today's view, and where it goes

| Today | In the workflow |
| --- | --- |
| Project picker, **+** | Header (and step 1) |
| Session, Start URL, **Record** | Step 3, More: Record by hand |
| **Capture**, **Pick**, **Stop**, **Continue**, **This page…** | The running strip |
| Pages tab: pages, captures, outline, node details, highlight, Compare, Changes | Step 4 |
| Node: Make a control | Step 5, ticked (and from a node in step 4) |
| Node: Never click / Always click | Step 4 |
| Pages tab: **Draft page objects**, **Improve with the agent** | Step 6, for the ticked pages |
| Pages tab: **Verify** | Step 7 |
| Generate page object, Update page object | Step 6, per page |
| Patterns tab | Step 5 (those asking for a control) and Review (the rest) |
| Code tab | **Code**, below the steps |
| Explore tab | Step 3 (and its results in step 4) |
| Explore settings tab | Step 3's **Guards and budget** form, with **Save** |
| Create config | Step 1, More |
| Generate… (the agent's chat) | Steps 5 and 6's agent actions |

## 8. Build steps

| # | Step | Verify |
| --- | --- | --- |
| 1 | `progress` and the state file; MCP `get_scraper_progress` | Unit tests: each step's state for a project at each point (new, recorded, drafted, verified, changed) |
| 2 | The layout: header with next action, step list, panels from today's tabs (the Explore settings in step 3), Code below; the running strip | The page through the stand-in host: every action of section 7 reachable; screenshots wide and narrow, light and dark |
| 3 | Step 2's **Check the app** (`brinell/scraper/check`); step 3's settings form with Save (several edits in one write) | Against the test site: answered, a sign-in (its `LoginUrl`), a refused host. The form: each field round-trips, Save writes only what changed |
| 3b | Step 5: tick and make in one go, Made, `controlCheck`, the review panel (fix with the agent, accept) | Unit tests: a control's parts found in every occurrence, and one that misses two; the review through the stand-in host |
| 3c | Step 6: tick and draft those, rename (`renamePageObject`) | Unit tests: drafting two of three pages; a rename keeps the person's part and survives a draft |
| 4 | Next action and badges for Keep current | A page re-recorded with a change: steps 3, 6, 7 badged, the header says re-record then update |
| 5 | The guide's Scraper chapter rewritten around the steps | Links valid |

## 9. Questions

| # | Question | Proposal |
| --- | --- | --- |
| Q1 | Are the steps a left list or a row across the top? | A list when the view is wide (an editor tab), chips when narrow (a docked tool window), by the view's width |
| Q2 | Is Review a step? | Yes, but never blocking: it is where Explore's results and the recordings are looked at; without it they have no home |
| Q3 | Should Controls come before Page objects even though Draft does not need them? | Yes, as the skill says: a page object that uses a control is shorter; skipping is one click and remembered |
| Q4 | A wizard (one step at a time, Back/Next) instead of a list? | No: the list shows the state of all steps at once, and people jump back (re-record, verify again) |
| Q5 | Is a control's check against the recordings enough, or also against the running app? | The recordings first (no browser, every occurrence Explore captured); Verify (step 7) checks the page objects that use it in the app |
| Q6 | Renaming a page object that tests already use | The rename changes the class and its files only; the IDE's rename (Roslyn) is the way to change its users too, so the view offers it as "Rename in the IDE" when the class is used |

## 10. Revised after review (2026-10-02)

| Asked | Done |
| --- | --- |
| Remove the narrow checkbox and the theme dropdown from the mockup | Removed; the mockup follows the system's light or dark |
| The first-time line as a numbered list | **Three steps to page objects**: 1 Connect to the app, 2 Collect its pages, 3 Draft page objects; "Each step says what to do next." |
| Collect pages: the settings editable, with Save | The guards and budget as a form in step 3 (fields, dropdowns, checkboxes, the hosts list), changed fields outlined, **Save** and **Undo changes**; the separate Settings entry is gone |
| Controls: select several and make them in one go; how made controls look; review and fix them | Tick boxes and a name per pattern, **Make *n* controls with the agent**; a **Made** list with build and found-in-recordings results; a review panel per control (code, parts found, a note, **Fix with the agent**, **Accept**). In the mockup: state "Controls made, to review" |
| Page objects: select, draft only those, change the name | Tick boxes, **Draft *n* page objects**, the class name editable in the row |
| Page objects, a fourth point | Left empty in the review: still open |
