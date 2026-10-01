# The Scraper

The Scraper records an app's pages into a UI test project and helps an agent (Copilot, Claude) make
page objects and controls from them. Html and Blazor apps today. The decisions are in
[AD-027](../../.docs/decisions/ad-027-the-scraper-records-into-the-project-and-calls-no-model.md).

## Make a project scrapeable

Add `scraper.config.md` to the UI test project the page objects go into (one project per app, or
several for a large app; a project that references another scraped project sees its controls):

```markdown
# Scraper Config

## App

| Field | Value |
| --- | --- |
| Platform | blazor |
| StartUrl | http://localhost:5180/ |
| Namespace | MyApp.UITests.PageObjects |
| Pages | PageObjects |
| Controls | PageObjects/Controls |
| Screenshots | false |

## Aliases

| Url |
| --- |
| https://myapp.example.be/ |

## Test ids

| Attribute |
| --- |
| data-testid |
| data-automation-id |

## Masks

| Pattern | Replacement |
| --- | --- |
| `NL\d{2}[A-Z]{4}\d{10}` | `IBAN` |
```

- `Platform`: `html` or `blazor` (a Blazor project gets the `Brinell.Blazor` controls suggested).
- `Aliases`: other hosts of the same app; a page elsewhere (a sign-in provider) is not recorded.
- `Test ids`: the attributes that are test ids, preferred first.
- `Masks`: text replaced before anything is written; what a user types is never recorded.

`testsnew/Brinell.Blazor.UITests` is an example: its config, its `Recordings/`, and
`PageObjects/UserFormPage.cs`, made from them.

## Record

- **VS Code:** **Brinell BDD: Open Scraper** (also on a `scraper.config.md`'s title bar).
- **Visual Studio:** View, Other Windows, **Scraper**.
- **An agent:** the MCP tools below.

**Record** opens a Chromium window. Work in it: each page the app moves to is captured once it has
settled (a hash route and `pushState` count as moves). **Capture** takes the page again (a dialog
open, a tab chosen); **Pick** lets you click an element to find it in the capture; **Stop** closes
the window. The window keeps its own login per project, under your local application data.

Recordings go to `Recordings/<date>-<session>/` in the project: `recording.json` and one
`*.nodes.jsonl` per page. Commit them: they are what the page objects were made from. The browser
is Playwright's Chromium (`pwsh <an Html test project's output>\playwright.ps1 install chromium`).

## Explore and draft

**Explore** visits the app's pages and states by itself and captures each new one as if you had
gone there: links to the app's own hosts, then tabs, menus, expanders, sorting, paging and dialogs
that say they are one. It works breadth first and stops at the first limit of its budget. Its
recording is `Recordings/<date>-explore/`, and everything else (patterns, suggestions, changes)
works on it as on yours. **Draft page objects** then writes a page object per recorded page, with
no model.

- **The view:** the **Explore** tab shows the host and the guards as set, the budget, and a dry run
  (ticked the first time: links only, it lists what it would click). **Start**, **Stop**, and
  **Continue** once you have signed in. Afterwards it lists what was captured, skipped and blocked,
  each with **Never click**, **Always click** or **Allow this request**. **Explore settings** edits
  everything below. On the Pages tab: **Draft page objects**, **Verify**, and **Improve with the
  agent** (the `/brinell-scrape-pages` prompt on the drafts).
- **The command line** (from the solution folder):

  ```powershell
  dotnet <Brinell.Bdd.LanguageServer.dll> scrape explore --project MyApp.UITests [--dry-run] [--max-pages N] [--max-depth N] [--max-minutes N] [--headless] [--yes] [--json]
  dotnet <Brinell.Bdd.LanguageServer.dll> scrape draft --project MyApp.UITests [--page /orders/{id}]
  dotnet <Brinell.Bdd.LanguageServer.dll> scrape verify --project MyApp.UITests [--headless]
  ```

  `explore` shows the guards and asks "Go on?" unless `--yes`. It exits with 0 when it ran within
  its guards, 1 when a guard stopped it (a sign-in, a refused host), 2 for an error.
- **An agent:** `explore_app` (a dry run unless asked otherwise), `draft_page_objects`,
  `verify_page_objects` and `update_scraper_config`. Each asks you first.

**Signing in is yours.** Explore uses the project's browser profile. When it reaches a sign-in page
(the config's `LoginUrl`, a page with a password field it was sent to, or another host), it stops
and asks you to sign in in the window. It never types.

**The guards** are settings in `scraper.config.md`. The defaults are the careful ones; loosen what
you know is harmless for your app and tenant:

```markdown
## Explore

| Field | Value |
| --- | --- |
| MaxPages | 50 |
| MaxDepth | 4 |
| MaxMinutes | 10 |
| DelayMs | 500 |
| LoginUrl | /login |
| ClickPolicy | safe |
| BlockMethods | POST, PUT, PATCH, DELETE |
| WebsocketApps | links |

## Danger words

| Category | On |
| --- | --- |
| delete | false |

## Explore hosts

| Host |
| --- |
| localhost |
| test.myapp.example |

## Never click

| Match |
| --- |
| text: Empty cache |
| selector: [data-testid=danger-zone] button |

## Always click

| Match |
| --- |
| selector: [data-testid=filter-toggle] |

## Never visit

| Url |
| --- |
| /admin |

## Allow requests

| Method | Url |
| --- | --- |
| POST | /graphql |
```

| Setting | What it decides |
| --- | --- |
| `ClickPolicy` | `links`; `safe` (the default: links, tabs, menus, expanders, sorting, paging, dialogs); `not-unsafe` (also buttons it cannot place); `all` (submits too: a throwaway environment only, asked twice, and only on a host named in `Explore hosts`) |
| `Danger words` | categories of words that make an element unsafe, all on by default, in English, Dutch, German and French: `sign-out`, `delete`, `update`, `send`, `pay`, `approve`, `bulk`. Turn one off by name |
| `BlockMethods` | request methods aborted while exploring; `GET`, `HEAD` and `OPTIONS` always go through. `Allow requests` lets single endpoints through |
| `WebsocketApps` | a page that opens a websocket (Blazor Server, SignalR) clicks without a request the guard can see: `links` (the default) explores its links only, `warn` explores it anyway, `ignore` does not say so |
| `Explore hosts` | where Explore may run: `localhost` and `127.0.0.1`, and every other host by name |

After every click it checks it is still on the app's host, still signed in, with no dialog it did
not open and no download; otherwise it stops and says after which click. The report,
`explore.md` in the session, lists each capture and the click that led to it, each element skipped
and why, and each request blocked.

**A draft** is `Pages/<Name>.scraped.cs`, a `partial` class made from the page as it opens: a member
per element with a stable locator, named from its test id or text. What has no stable locator, and
what only a click shows, is listed in a comment at the end. Put your own members in the other part,
`Pages/<Name>.cs`. Drafting again replaces only the `.scraped.cs`. A page with a page object you
made by hand is not drafted. A draft that does not build is put back and reported, and so is one
that drops a member your part uses. Controls are not drafted: Draft lists the parts that repeat,
and **Improve with the agent** makes them. **Verify** opens each page in the running app and finds
every member's locator: once, several times, or not at all.

## Generate

The view's **Generate…**, **Generate page object** and **Make a control** open the agent's chat
with the `/brinell-scrape-pages` prompt (Visual Studio puts it on the clipboard). The agent follows
the [scrape-pages](../../.github/skills/scrape-pages/SKILL.md) skill: controls for the parts that
repeat, then a page object per page, then the build until it passes. Review the result in your
usual diff.

Register the MCP server as in [Copilot And Other Agents](visual-studio-preview.md#copilot-and-other-agents).
Its Scraper tools:

| Tool | Gives or does |
| --- | --- |
| `list_scraped_projects` | the projects with a `scraper.config.md` |
| `start_recording`, `capture_page`, `stop_recording` | a recording (one at a time per server) |
| `list_recorded_pages` | pages by key (path and hash route, ids as `{id}`), their captures, the page object made for each and whether the page changed since |
| `get_page_snapshot` | a capture as an outline with node numbers; a node's subtree for a large page |
| `find_similar_elements`, `search_recordings` | where a kind of node, or a text, occurs |
| `detect_patterns` | repeated parts (controls to make) and forms, tables, lists, navigation, dialogs, frames |
| `list_controls`, `match_controls` | the project's page objects and controls, and which occur on a page |
| `suggest_controls` | per element: a name, the control that fits, and a locator (test id, accessible name or text, stable id, stable class; a position last, marked unstable) |
| `scraped_header` | the first line of a generated file: what it was made from |
| `write_code`, `check_code` | write a file into the project (syntax checked first), then build it |
| `diff_page` | what changed on a page since its page object was made |
| `explore_app` | Explore, a dry run unless asked; asks first |
| `draft_page_objects`, `verify_page_objects` | Draft and Verify, for all pages or one |
| `update_scraper_config` | one row or field of `scraper.config.md` changed; asks first |

## When the app changes

Record the page again. The page list marks a page whose page object was made from an older
capture; **Changes** (or `diff_page`) lists what was added, removed and changed since, and
**Update page object** asks the agent to change only that.

## The loop

`tools/Scripts/scrape-loop.ps1` runs everything end to end, unattended, and writes one report. Run
it after changing the Scraper (from the Brinell root):

```powershell
pwsh tools/Scripts/scrape-loop.ps1 -Target test-site
pwsh tools/Scripts/scrape-loop.ps1 -Target test-site -Profile test-tenant -Repeat 3
pwsh tools/Scripts/scrape-loop.ps1 -Target blazor-sample
pwsh tools/Scripts/scrape-loop.ps1 -Target test-site -Agent copilot
```

| Stage | Does | Fails when |
| --- | --- | --- |
| App | starts the target on a free port: the Scraper's test site (`testsnew/Brinell.Scraper.TestSite`, with a page of dangerous controls that count each hit) or the Blazor sample | it does not answer |
| Explore | `scrape explore --headless --yes` with the profile's guards | a guard stopped it unexpectedly, or it found fewer pages than the target has |
| Guards | reads the test site's counters | `careful`: any counter moved. `test-tenant`: anything but delete and update moved, or those were not reached |
| Draft | `scrape draft` | a draft does not build |
| Agent | Copilot CLI or Claude Code with only the Brinell MCP server's tools, to make controls and better names; Copilot CLI by default; skipped for `-Agent none` | `check_code` fails, or it ran past `-AgentMinutes` |
| Verify | `scrape verify` | a member is not found, or found more than once |

Each run works on a new project under `TestResults/scrape-loop/<date>-<target>/`, never on the
repository, and writes `loop-report.md` and `loop-report.json` there even when a stage fails.
`-WebsocketApps` overrides the Blazor sample's `warn`. `-TurnOff <category>` turns a danger word off
while the guard check still expects the profile, to prove the loop catches a broken guard. The
agent stage costs the CLI's credits; `-Agent none` runs the loop without it.
