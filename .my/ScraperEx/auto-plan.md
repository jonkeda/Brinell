# The Scraper, automatic: explore an app and draft its page objects

Status: **building**: steps 1-8 done (2026-09-30; step 8's agent stage built, not yet run with an agent). Decisions in section 8 and 9.
Builds on [plan.md](plan.md) (the Scraper as built, AD-027) and [next-plan.md](next-plan.md).

## 1. What we want

Today a person browses and the Scraper captures; an agent writes the page objects. This plan adds:

| | Started from | Does |
| --- | --- | --- |
| **Explore** | the view's **Explore** button, or a command line | opens the app, and visits its pages and states by itself: links, tabs, menus, expanders, dialogs. Each new page or state is captured as if a person had gone there |
| **Draft** | the view's **Draft page objects** button, the same command with `--draft`, or at the end of Explore | writes a page object per recorded page from the suggestions, without a model, and builds it |

And it must do no harm the project did not allow: by default no logout, no delete, no submit, no
payment, nothing that changes data. How careful it is, is the project's setting (section 3): on a
test tenant a delete or an update can be fine.

## 2. Exploring

```text
start URL ──► capture ──► find what can be clicked ──► judge each: safe / unsafe / unknown
                ▲                                             │
                └── the new page or state ◄── click one safe ─┘   (breadth first, within the budget)
```

| Decision | What holds |
| --- | --- |
| **The same engine and files** | Explore is a recording session that clicks by itself: `ScraperService`, the Html source, `Recordings/<date>-explore/`. Everything after (patterns, suggestions, diff, the view) works on it unchanged |
| **What it follows** | Links to the app's own hosts (the start URL's and the aliases'); then, on each page, the elements that show more without changing anything: tabs (`role=tab`), menu and dropdown toggles (`aria-haspopup`, `aria-expanded`), expanders (`summary`, `aria-controls`), pagination and sorting in a table (read-only), dialogs opened by a button that says so (`aria-haspopup=dialog`). A dialog is captured, then left by reloading the page (a state is reached again by opening its page and replaying the clicks that led there), never with its action. Only what shows is clicked; a hidden element is judged in the state that shows it. A link is visited even when hidden (a collapsed menu's) |
| **What makes a state new** | Its page key, its content hash (without boxes, `SnapshotFile.ContentHash`) and which nodes show (an expander or dialog opening changes no box-free content: built, Chrome lays a closed `details` out, so the capture now keeps `open` too). A state seen before is not captured or explored again; a list of 50 rows is one state, not 50: links are followed once per page key (`/orders/{id}`) |
| **The budget** | At most `MaxPages` captures (default 50), `MaxDepth` clicks from the start (default 4), `MaxMinutes` (default 10), and a pause of `DelayMs` between clicks (default 500) so the app is not hammered. Explore stops at the first limit and says which |
| **The person can take over** | **Stop** ends it; the recording so far stays. A capture the person makes during Explore is kept like any other |
| **The login is the person's** | Explore uses the project's browser profile. When it reaches a sign-in page (the config's `LoginUrl`; a page with a password field that the app sent it to instead of the one asked for, so a home page with a login box is not one; or a host that is not the app's), it stops, says so, and asks the person to sign in in the window and press **Continue**. It never types a credential |

## 3. The guards, as settings

Four guards. Each is a setting of the project, in `scraper.config.md` and editable in the view's
**Explore settings** (section 5). The defaults are the careful ones; a project loosens what it
knows is harmless for its app and its tenant.

**Guard 1: which clicks.** Before a click, the element is judged:

| Judged | When |
| --- | --- |
| **Safe** | a link (`a[href]`) to the app's own host that is a plain GET page (no `download`, no `javascript:`, not in `NeverVisit`); a tab, menu toggle, expander, sort header or pager (by role and ARIA state, section 2); an element in `AlwaysClick` |
| **Unsafe** | anything that submits (`type=submit`, a button in a `form` without `type=button`, `form[method=post]`); anything whose text, name, title, test id, class or icon (`aria-label`, `title`, an icon's name) matches a danger word of an **enabled** category or `NeverClick`; links whose URL matches a danger path of an enabled category (`/logout`, `/delete`, `?action=delete`) or `NeverVisit`; `mailto:`, `tel:`, downloads, other hosts |
| **Unknown** | any other button or clickable (`onclick`, `role=button`) |

The setting **`ClickPolicy`** says what is clicked:

| `ClickPolicy` | Clicks | For |
| --- | --- | --- |
| `links` | links only | a first look; an app whose clicks the other guards cannot see (guard 3) |
| `safe` (default) | safe | most apps: only what is known to show more |
| `not-unsafe` | safe and unknown | an app on a test tenant, when the report shows too much skipped as unknown |
| `all` | everything, submits too | a throwaway environment only: the view asks twice, and it is refused for a host that is not in `ExploreHosts` by name |

**Danger words** are in **categories**, each on or off (all on by default), matched as whole words,
case-insensitive, in English, Dutch, German and French to start with (the apps are Exact's):

| Category | Words (a sample) |
| --- | --- |
| `sign-out` | log out, logout, sign out, afmelden, uitloggen, abmelden, se déconnecter |
| `delete` | delete, remove, verwijderen, wissen, löschen, supprimer |
| `update` | save, submit, opslaan, speichern, enregistrer, update, bijwerken, edit |
| `send` | send, verzenden, senden, envoyer, invite, publish, share |
| `pay` | pay, betalen, bezahlen, payer, checkout, order, bestellen |
| `approve` | confirm, bevestigen, approve, goedkeuren, reject, afwijzen, archive, archiveren |
| `bulk` | import, upload, sync, reset, clear all, deactivate, close account |

So "deleting is fine here" is `delete` off. `sign-out` stays on unless a project turns it off by
name: a logout ends the run anyway. `NeverClick` adds words, texts or selectors; `AlwaysClick`
names elements that are clicked whatever the words say.

**Guard 2: which requests.** While exploring, the Html source routes every request. The setting
**`BlockMethods`** says which methods are aborted (default `POST, PUT, PATCH, DELETE`); `GET`,
`HEAD` and `OPTIONS` always go through. So "updates are fine" is `BlockMethods: POST, DELETE`, or
empty for none. **`AllowRequests`** lets single endpoints through anyway (a read-only GraphQL
endpoint, a search that posts). A blocked request is listed in the report; the click it belonged to
does nothing, and nothing new is captured.

**Guard 3: what the request guard cannot see.** Blazor Server, SignalR and other websockets carry a
click without an HTTP request, so guard 2 does not see them. The setting **`WebsocketApps`** says
what Explore does when the page opens one: `links` (default: `ClickPolicy` is lowered to `links`,
and the view says why), `warn` (the policy stands; the view warns before it starts), or `ignore`.

**Guard 4: where.** **`ExploreHosts`**: the hosts Explore may run against (default `localhost` and
`127.0.0.1`). Every other host is added by name. Before a run the view (or the command line) shows
the host and the guards as set, and asks to go on.

**Checked after every click:** still on the app's host; still signed in (the page is not a sign-in
page, and the cookies the start page had are still there); no dialog left open that was not ours;
no unexpected download. If any fails, Explore stops and reports what happened after which click.

**The report**, `Recordings/<session>/explore.md`: each capture and the click that led to it; each
element skipped, with why (unsafe: which word or rule; unknown); each request blocked; why it
stopped. From the report the view offers the same actions as from a node (section 5): never click
this, always click this, never visit this.

**Dry run** (`--dry-run`, and the view's default the first time): Explore visits only links, clicks
nothing, and lists what it *would* click and skip on each page. Nothing is captured beyond the pages.

## 4. Drafting page objects

The Scraper still calls no model (AD-027): a draft is made from the suggestions, deterministically.

| Decision | What holds |
| --- | --- |
| **One page object per recorded page** | Its name from the page's title or key (`/invoices/{id}/edit` → `InvoiceEditPage`), its members from `suggest_controls` (stable locators only; the unstable ones are listed in a comment block at the end, for a person), frame members on their frame's scope |
| **Generated and hand-written apart** | The draft is `Pages/<Name>.scraped.cs`, a `partial` class with the header line; a person's additions go in `Pages/<Name>.cs`, the other part, which Draft never touches. Drafting again replaces only the `.scraped.cs`. A page whose page object was made by hand (a header, not a `.scraped.cs`) is not drafted |
| **The gate** | After writing, `check_code` builds the project. A draft that does not build is reverted and reported with the error, never left half-written |
| **Removed members** | A member that a new draft no longer has, and that a test uses, breaks the build: that is the point. The report says which member went and which recording showed it last |
| **Controls** | Not drafted: a control needs a judgement (what is one part) that the patterns only suggest. Draft lists the patterns that repeat and says "make a control" for the agent (the `scrape-pages` skill) or a person |
| **The agent's pass, optional** | After Draft, the view's **Improve with the agent** opens the chat with `/brinell-scrape-pages` on the drafted files: better names, controls, members to drop |

AD-027 is amended: the Scraper drafts code from rules (no model); the agent improves it.

## 5. Started from the command line or a button

| | |
| --- | --- |
| **Command line** | `Brinell.Bdd.LanguageServer scrape explore --project <name> [--solution <folder>] [--dry-run] [--draft] [--max-pages N] [--max-depth N] [--max-minutes N] [--headless]` and `scrape draft --project <name> [--page <key>]`. The language server already has `--preview` and `--mcp` modes, so no new project (AD-026). Exit code 0 when it ran within its guards, 1 when a guard stopped it, 2 for an error. Headless is for our own apps in CI only; a login needs the window |
| **The view** | **Explore** (with the budget, the dry-run switch and the guard summary before it starts), a progress line (captures, clicks, skipped, blocked), **Stop**, **Continue** after a sign-in; **Draft page objects** on the Pages tab, for all pages or the one open |
| **Explore settings** | A settings panel in the view for everything in section 6: the budget, `ClickPolicy`, the danger categories as switches, `BlockMethods` as checkboxes, `WebsocketApps`, and the lists. It writes `scraper.config.md` |
| **Adding to the config from the view** | The extension edits the config, so a person never has to find the right table: on the recording browser's current page, **Add this host to Explore hosts**, **Add as an alias**, **Use as start URL**, **Never visit**; on a node or a report line, **Never click this**, **Always click this**; on a blocked request in the report, **Allow this request**. Each writes one row in its table (or one field in `## App`), keeps the rest of the file as it was, and says what it wrote. With no `scraper.config.md` yet, **Create config** writes one for the project, with the current URL as start URL |
| **MCP** | `explore_app` (asks first, dry run by default) and `draft_page_objects`: an agent can start them, a person approves. `update_scraper_config` (asks first) makes the same one-row changes as the view |
| **The language server** | `brinell/scraper/config` reads the config as data and `brinell/scraper/configEdit` applies one change (add a row, remove a row, set a field): a writer that keeps the Markdown's other text and order (`ScraperConfigWriter`, beside `ScraperConfig`) |

## 6. The config

New sections in `scraper.config.md`, all optional; each has the default shown:

```markdown
## Explore

| Field | Value |
| --- | --- |
| MaxPages | 50 |
| MaxDepth | 4 |
| MaxMinutes | 10 |
| DelayMs | 500 |
| AgentMinutes | 10 |
| LoginUrl | /login |
| ClickPolicy | safe |
| BlockMethods | POST, PUT, PATCH, DELETE |
| WebsocketApps | links |

## Danger words

| Category | On |
| --- | --- |
| sign-out | true |
| delete | true |
| update | true |
| send | true |
| pay | true |
| approve | true |
| bulk | true |

## Explore hosts

| Host |
| --- |
| localhost |
| 127.0.0.1 |

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

A test tenant where deleting and saving are fine, and websocket clicks too:

```markdown
| ClickPolicy | not-unsafe |
| BlockMethods | |
| WebsocketApps | warn |

| delete | false |
| update | false |
```

## 7. Steps

One commit per step, tests with each. From step 6 on, each step ends with a run of the loop
(section 9) on the test site, and its report goes in the commit message.

| # | Step | Verify |
| --- | --- | --- |
| 1 | The settings (section 6) read and written: `ScraperConfig` for the new sections, `ScraperConfigWriter` for one-row edits that keep the file's other text | **Done.** `ExploreSettings` (the budget, `ClickPolicy`, `BlockMethods`, `WebsocketApps`, danger categories, hosts, never/always click, never visit, allow requests) on `ScraperConfig.Explore`; `ScraperConfigWriter`: set a field, add or remove a row, a new config, keeping the file's other text and line endings. 14 tests |
| 2 | The judge (guard 1): safe, unsafe, unknown for a node; `ClickPolicy`; the danger categories on and off; `NeverClick`, `AlwaysClick`, `NeverVisit` | **Done.** `ClickJudge` (safe, unsafe, unknown; visit or click; the reason) and `ClickJudge.Allows` for the policy; `DangerWords` matched as whole words in text, names, test ids, classes, URLs and icons (`fa-trash`, `alt`, `data-icon`), camelCase and dashes split, accents ignored; "edit" and "checkout" added; `Never click` / `Always click` texts as phrases, selectors as the nodes the page matched (filled in step 5). The capture keeps `download`. 90 tests |
| 3 | The request guard (guard 2, `BlockMethods`, `AllowRequests`) and the host guard (guard 4) in the Html source | **Done.** `RequestGuard`: a blocked method aborted unless `AllowRequests` lets it (method, path or URL), a page navigation to a host outside the Explore hosts aborted, `RefuseHost` for the start (host, or host and port), what it stopped and the websockets opened kept for the report; `PlaywrightScrapeSource.GuardAsync`/`UnguardAsync` route every request of the browser through it. The test site logs its requests and answers `/api/…` for any method. 16 unit and 5 browser tests |
| 4 | The test site gets a dangerous page: logout link, delete buttons, a submit form, a POST that would delete, a websocket; each counted by the site, so a test can prove what happened | **Done.** `pages/account.html` (linked from home): a logout link, a menu with sign out, delete buttons (text, icon, a GET link), rename (PUT), send, pay, import, confirm in a dialog, a form that posts, and "Tidy up" (a POST behind no word); each hits `/api/counter/<category>`, counted by `TestSiteServer.Counter`. Safe parts: tabs, an expander, a menu, a dialog with close. `pages/live.html` clicks over a websocket, counted by `SocketMessages`. The tests found the judge missing a close button (its `aria-label` is the node's name): fixed. 4 browser tests |
| 5 | Explore: links, then clicks by policy; state dedup; budget; sign-in detection with Continue; the websocket guard; the report; dry run | **Done.** `Explorer` (breadth first; replay by element signature; checks after every step: a download, the host, sign-in, the start's cookies; page dialogs dismissed; websocket guard; budget; dry run), `ExploreReport` (`explore.md`), `ScraperService.ExploreAsync` (opens blank so nothing loads before the guards) with `StopExploring` and `ContinueExploring`; `IExploreSource` on the Html source. With the defaults the test site's pages and the account page's states are found and every counter stays 0 ("Checkout" is a pay word, so its link is left); with `not-unsafe`, `delete` off and DELETE let through, only the delete counter moves and the POST and PUT behind unknown buttons are blocked. 11 browser tests |
| 6 | The command line (`scrape explore`, `scrape draft`, `scrape verify`) and the MCP tools, `update_scraper_config` too | **Done** for explore; `scrape draft` and `scrape verify` come with step 7, `draft_page_objects` too. `ScrapeCommand` (in `Brinell.Scraper`, forwarded by the language server's `scrape` mode): the guard summary first, "Go on?" unless `--yes`, progress lines, `--json`; exit 0, 1 (a guard stopped it: sign-in, host refused), 2 (an error, or not started). MCP `explore_app` (dry run by default) and `update_scraper_config` ask first: without `confirm` they give the summary or what would be written. `ScraperService.EditConfig` and `ScraperConfigWriter.Apply` (`set`, `add`, `remove`; a danger category's row replaced) are what `brinell/scraper/configEdit` will call in step 9. 14 tests here, 3 MCP tests |
| 7 | Draft: `.scraped.cs` partials, names, unstable members as a comment, the gate with revert; **Verify**: every member's locator resolved against the running app (section 9, stage 6) | **Done.** `PageDrafter` (class name from the key, members from every state of the page in its newest session so a closed dialog's parts are there, never a base member's name, unstable ones as a comment), `DraftRun` (write all, build, put back the drafts the build fails on, or all when the errors are elsewhere; a page with a hand-made page object is left alone; removed members from the last draft's own file), `PageVerifier` (locators read from the source and looked up as `LocatorExtensions` does, in a frame for a frame member; a person's partial part is merged in by `ProjectCode`). `scrape draft`, `scrape verify`, MCP `draft_page_objects` and `verify_page_objects`. The test site's account page drafted, built against `Brinell.Html`, verified; drafting again changes nothing; a member a person's part uses and a new draft drops puts the draft back. 14 tests |
| 8 | **The loop** (section 9): the script, the targets, the report; the agent stage with Copilot CLI and with Claude Code | **Done**, the agent stage built but not yet run with Copilot or Claude (it costs credits: run it by name). `tools/Scripts/scrape-loop.ps1` (`-Target`, `-Agent`, `-Profile`, `-Repeat`, `-AgentMinutes`, `-TurnOff`), the test site moved to `testsnew/Brinell.Scraper.TestSite` (runs on its own, `/_counters`). With none: careful passes (9 captures, no counter moved, 4 drafts, 78 of 78 members found once), twice in a row; test-tenant passes (delete 4, update 1, nothing else); `-TurnOff delete` fails at Guards (the "Remove all" link is visited). The CLIs' flags checked with `--help` |
| 9 | The view: Explore with the guard summary, progress, Stop, Continue; Explore settings; the add-to-config actions and Create config; Draft page objects; Improve with the agent | the manual test plan's new checks (Claude's part scripted as before) |
| 10 | The Blazor sample in the loop (links, then `WebsocketApps: warn`) | its pages found; the drafts build and verify; `UserFormPage` compared with the hand-checked one |
| 11 | AD-027 amended (Explore and its guards as settings, drafts from rules, the agent from its own CLI); the guide's "Explore and draft" and "The loop" sections | links valid |

## 8. Decisions

| # | Question | Decided (2026-09-30) |
| --- | --- | --- |
| Q1 | Click only what is known safe, or everything not known unsafe? | A setting: `ClickPolicy` (`links`, `safe` by default, `not-unsafe`, `all`), with the danger categories and blocked methods as settings too. Sometimes a delete or an update is fine |
| Q2 | Type into fields to reach more states? | Not in the first version. Later, maybe with the agent: an agent-led Explore where the agent picks the next action and the values from the judge's candidates (MCP: the state, the candidates, click, fill), with the same guards |
| Q3 | Websocket apps (Blazor Server): links only? | A setting: `WebsocketApps` (`links` by default, `warn`, `ignore`) |
| Q4 | Which hosts by default? | `localhost` and `127.0.0.1`; every other host by name |
| Q5 | Draft controls too? | No: controls are the agent's, they need a judgement |
| Q6 | The `.scraped.cs` partial split? | Yes |
| Q7 | A nightly Explore of the samples? | Later |
| Q8 | Can the extension add a URL to the config? | Yes: section 5's add-to-config actions and Create config, through `brinell/scraper/configEdit` |
| Q9 | Does Brinell's command line use Copilot? | No, it stays model-free (AD-027). An agent's own command line (Copilot CLI, Claude Code) runs with the Brinell MCP server, as the loop's agent stage does |

## 9. The loop: explore, draft, improve, check, verify, report, unattended

One command runs everything end to end against an app, with no person in it, so that Claude (or CI,
or the owner) can run it after every change and read one report:

```text
scrape-loop.ps1 -Target test-site|blazor-sample|<project> [-Agent copilot|claude|none] [-Profile careful|test-tenant] [-Repeat N]
```

| Stage | Does | Fails the loop when |
| --- | --- | --- |
| 1. App | starts the target app (the test site with its dangerous page, or the Blazor sample) on a free port, into a copy of its project | it does not start |
| 2. Explore | `Brinell.Bdd.LanguageServer scrape explore --project … --headless` with the profile's settings (`careful`: the defaults; `test-tenant`: `delete` and `update` off, `BlockMethods` empty) | a guard stopped it for a reason the profile does not expect; fewer pages than the target's known minimum |
| 3. Guards | reads the dangerous page's counters from the test site | `careful`: any counter above 0. `test-tenant`: sign-out, pay or send above 0, or delete and update not reached |
| 4. Draft | `scrape draft --project …` | a draft does not build (it is reverted and named) |
| 5. Agent | the agent's own command line, non-interactive, with only the Brinell MCP server's tools allowed and its shell and file writes denied (code goes through `write_code`, the build through `check_code`), and the `/brinell-scrape-pages` prompt: controls for the repeated patterns, better names | `check_code` fails at the end; the agent wrote outside the project; the agent exceeded its budget (time, and credits where the CLI reports them) |
| 6. Verify | `scrape verify`: opens each recorded page in the running app and resolves every page object member's locator (read from the source, mapped as `Brinell.Html.Playwright` maps it, a frame member on its frame): found once, found several times, not found | a member not found; a member found several times where one is expected |
| 7. Report | `loop-report.md` and `loop-report.json` in the run's folder: each stage's result and time, pages, captures, skipped and blocked with reasons, counters, drafts, the agent's changes (a diff), verify per member, the agent's cost | never: the report is written even when a stage fails |

**How the agent stage is run** (checked 2026-09-30 on this machine, Copilot CLI 1.0.69 and Claude
Code 2.0.1 installed):

```text
copilot -p "<the prompt>" --additional-mcp-config '{"mcpServers":{"brinell-bdd":{"type":"local","command":"dotnet","args":["<server dll>","--mcp","--solution","<copy>"],"tools":["*"]}}}' --allow-tool brinell-bdd --deny-tool shell --deny-tool write
claude -p "<the prompt>" --mcp-config <file> --strict-mcp-config --allowed-tools "mcp__brinell-bdd" --output-format json
```

A first try with Copilot CLI called `list_scraped_projects` and `list_recorded_pages` on a scratch
project and answered from them: about 9 AI credits for that short run. An improve pass reads
many pages and writes several files, so it costs more; the report shows it, and `-Agent none` runs
the loop without one.

**Who runs it:**
- **Claude**, after each step of this plan and whenever the Scraper changes: the loop is how "test
  everything" is done without a person, and its report is the evidence in the commit. With
  `-Repeat N` (or the `/loop` command) it runs again and again to catch what fails only sometimes.
- **The owner**, with `-Agent copilot` to see what Copilot makes of an app.
- **CI**, later (Q7): the loop on the samples with `-Agent none` every night; `diff_page` against
  the last night says what changed in the UI.

**Where it lives:** `tools/Scripts/scrape-loop.ps1` (a script beside the others there), the test
site's dangerous page in `testsnew/Brinell.Scraper.Tests/TestSite`, and the runs in
`TestResults/scrape-loop/<date>-<target>/` (ignored by git), each on a copy of the target project
so a run never changes the repository.

| # | Question | Proposal |
| --- | --- | --- |
| Q10 | Which agent does the loop use by default? | **Decided:** `none` when Claude runs it after a step (free, deterministic); `copilot` in a run asked for by name. Both CLIs are supported |
| Q11 | A budget for the agent stage? | **Decided:** a setting, `AgentMinutes` in `## Explore` (default 10), overridden by the loop's `-AgentMinutes`; with the CLI's own limits. The loop stops the agent after that and fails the stage |
