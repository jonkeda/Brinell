# The Scraper, automatic: explore an app and draft its page objects

Status: **plan**, 2026-09-30. Nothing is built. Questions in section 8 wait for the owner.
Builds on [plan.md](plan.md) (the Scraper as built, AD-027) and [next-plan.md](next-plan.md).

## 1. What we want

Today a person browses and the Scraper captures; an agent writes the page objects. This plan adds:

| | Started from | Does |
| --- | --- | --- |
| **Explore** | the view's **Explore** button, or a command line | opens the app, and visits its pages and states by itself: links, tabs, menus, expanders, dialogs. Each new page or state is captured as if a person had gone there |
| **Draft** | the view's **Draft page objects** button, the same command with `--draft`, or at the end of Explore | writes a page object per recorded page from the suggestions, without a model, and builds it |

And it must never do harm: no logout, no delete, no submit, no payment, nothing that changes data.

## 2. Exploring

```text
start URL ──► capture ──► find what can be clicked ──► judge each: safe / unsafe / unknown
                ▲                                             │
                └── the new page or state ◄── click one safe ─┘   (breadth first, within the budget)
```

| Decision | What holds |
| --- | --- |
| **The same engine and files** | Explore is a recording session that clicks by itself: `ScraperService`, the Html source, `Recordings/<date>-explore/`. Everything after (patterns, suggestions, diff, the view) works on it unchanged |
| **What it follows** | Links to the app's own hosts (the start URL's and the aliases'); then, on each page, the elements that show more without changing anything: tabs (`role=tab`), menu and dropdown toggles (`aria-haspopup`, `aria-expanded`), expanders (`summary`, `aria-controls`), pagination and sorting in a table (read-only), dialogs opened by a button that says so (`aria-haspopup=dialog`). A dialog is captured, then closed with Escape or its close button, never with its action |
| **What makes a state new** | Its page key and its content hash (without boxes, `SnapshotFile.ContentHash`). A state seen before is not captured or explored again; a list of 50 rows is one state, not 50 |
| **The budget** | At most `MaxPages` captures (default 50), `MaxDepth` clicks from the start (default 4), `MaxMinutes` (default 10), and a pause of `DelayMs` between clicks (default 500) so the app is not hammered. Explore stops at the first limit and says which |
| **The person can take over** | **Stop** ends it; the recording so far stays. A capture the person makes during Explore is kept like any other |
| **The login is the person's** | Explore uses the project's browser profile. When it reaches a sign-in page (the config's `LoginUrl`, a page with a password field, or a host that is not the app's), it stops, says so, and asks the person to sign in in the window and press **Continue**. It never types a credential |

## 3. Never doing harm

Four guards, each on its own enough to stop most harm; together they are the rule.

**Guard 1: judge every click before it happens.** An element is clicked only when it is judged
safe. Everything not known to be safe is skipped and listed in the report.

| Judged | When |
| --- | --- |
| **Safe** | a link (`a[href]`) to the app's own host that is a plain GET page (no `download`, no `javascript:`, not in `NeverVisit`); a tab, menu toggle, expander, sort header or pager (by role and ARIA state, above); an element in the config's `AlwaysClick` |
| **Unsafe, never clicked** | anything that submits (`type=submit`, a button inside a `form` without `type=button`, `form[method=post]`); anything whose text, name, title, test id, class or icon (`aria-label`, `title`, an icon's name) matches a danger word (below) or `NeverClick`; links whose URL matches a danger path (`/logout`, `/signout`, `/delete`, `/remove`, `?action=delete`) or `NeverVisit`; `mailto:`, `tel:`, downloads, other hosts |
| **Unknown, not clicked** | any other button or clickable (`onclick`, `role=button`) that is neither: an explorer that only clicks what it understands |

**Danger words**, in English, Dutch, German and French to start with (the apps are Exact's), matched
as whole words, case-insensitive: *log out, logout, sign out, afmelden, uitloggen, abmelden, se
déconnecter; delete, remove, verwijderen, wissen, löschen, supprimer; submit, save, opslaan,
speichern, enregistrer, send, verzenden, senden, envoyer; pay, betalen, bezahlen, payer; confirm,
bevestigen, bestätigen, confirmer; approve, goedkeuren, reject, afwijzen; archive, archiveren;
cancel order, annuleren; reset, clear all; import, upload, sync, publish, invite, deactivate,
close account*. The list is in the engine, the config adds to it (`NeverClick`) and can take a word
out for one app (`AlwaysClick` wins over a danger word only for the exact element it names).

**Guard 2: no request that changes data leaves the browser.** While exploring, the Html source
routes every request: `GET`, `HEAD` and `OPTIONS` go through; `POST`, `PUT`, `PATCH` and `DELETE`
are **blocked** (aborted, listed in the report) unless the config's `AllowRequests` names them
(a GraphQL endpoint that only reads, a search that posts). A click that needs a blocked request
simply does nothing, and the state is not captured as new.

**Guard 3: the connection that the request guard cannot see.** Blazor Server, SignalR and other
websockets carry a click to the server without an HTTP request, so guard 2 does not see them. For
such an app (the config's `Platform: blazor` with a server circuit, or a websocket opened on the
page) Explore says so before it starts, and runs **links only** (no tabs, menus or dialogs) unless
the config says `ExploreClicks: true`. Guard 1 still applies.

**Guard 4: where it may run at all.** Explore runs only against a host listed in the config's
`ExploreHosts` (default: `localhost` and `127.0.0.1` only). A test tenant is added there by name;
a production host is never the default. Before it starts, the view (or the command line) shows the
host and the guards and asks to go on.

**Checked after every click:** still on the app's host; still signed in (the page is not a sign-in
page, and the cookies the start page had are still there); no dialog left open that was not ours;
no unexpected download. If any fails, Explore stops and reports what happened after which click.

**The report**, `Recordings/<session>/explore.md`: each capture and the click that led to it; each
element skipped, with why (unsafe: which word or rule; unknown); each request blocked; why it
stopped. A person reads it to add `AlwaysClick` or `NeverClick` entries and run again.

**Dry run** (`--dry-run`, and the view's default the first time): Explore visits only links, clicks
nothing, and lists what it *would* click and skip on each page. Nothing is captured beyond the pages.

## 4. Drafting page objects

The Scraper still calls no model (AD-027): a draft is made from the suggestions, deterministically.

| Decision | What holds |
| --- | --- |
| **One page object per recorded page** | Its name from the page's title or key (`/invoices/{id}/edit` → `InvoiceEditPage`), its members from `suggest_controls` (stable locators only; the unstable ones are listed in a comment block at the end, for a person), frame members on their frame's scope |
| **Generated and hand-written apart** | The draft is `Pages/<Name>.scraped.cs`, a `partial` class with the header line; a person's additions go in `Pages/<Name>.cs`, the other part, which Draft never touches. Drafting again replaces only the `.scraped.cs` |
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
| **MCP** | `explore_app` (asks first, dry run by default) and `draft_page_objects`: an agent can start them, a person approves |

## 6. The config

New sections in `scraper.config.md`, all optional:

```markdown
## Explore

| Field | Value |
| --- | --- |
| MaxPages | 50 |
| MaxDepth | 4 |
| MaxMinutes | 10 |
| DelayMs | 500 |
| LoginUrl | /login |
| ExploreClicks | false |

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

## 7. Steps

One commit per step, tests with each.

| # | Step | Verify |
| --- | --- | --- |
| 1 | The judge (guard 1): safe, unsafe, unknown for a node, the danger words, the config's lists | unit tests on snapshots: every danger word in four languages, forms, submit buttons, icons, `AlwaysClick` winning only for its element |
| 2 | The request guard (guard 2) and the host guard (guard 4) in the Html source | browser tests on the test site: a `fetch` POST blocked, a GET through, `AllowRequests` honoured, a host outside `ExploreHosts` refused |
| 3 | The test site gets a dangerous page: logout link, delete buttons, a submit form, a POST that would delete, a websocket | the site itself; each is counted by the page (a server-side counter) so a test can prove nothing happened |
| 4 | Explore: breadth first over links, then safe clicks; state dedup; budget; sign-in detection with Continue; the report; dry run | browser tests on the test site: all pages found, the dangerous page's counters stay 0, the report lists every skip with its reason |
| 5 | The command line (`scrape explore`, `scrape draft`) and the MCP tools | a run on the test site from the command line, exit codes |
| 6 | Draft: `.scraped.cs` partials, names, unstable members as a comment, the gate with revert | unit tests; the test site drafted and building; a second draft after a change replaces only the `.scraped.cs` |
| 7 | The view: Explore with the guard summary, progress, Stop, Continue; Draft page objects; Improve with the agent | the manual test plan's new checks (Claude's part scripted as before) |
| 8 | The Blazor sample explored and drafted, links only (guard 3), and with `ExploreClicks: true` | its pages found; the drafts build; `UserFormPage` compared with the hand-checked one |
| 9 | AD-027 amended; the guide's "Explore and draft" section | links valid |

## 8. Questions

| # | Question | Proposal |
| --- | --- | --- |
| Q1 | Only click what is known safe, or click everything not known unsafe? | Only what is known safe (section 3). Missing a menu is cheap; one delete is not |
| Q2 | Ever type into a field (a search box, a filter) to reach more states? | Not in the first version. Later: only fields the config names, with values from the config |
| Q3 | Blazor Server and other websocket apps: links only by default? | Yes (guard 3); `ExploreClicks: true` per app when a person has checked the danger words cover it |
| Q4 | Which hosts by default? | `localhost` and `127.0.0.1` only; every other host by name in `ExploreHosts` |
| Q5 | Draft: page objects only, or controls too? | Page objects only; controls stay with the agent or a person |
| Q6 | The `.scraped.cs` partial split: agreed? | Yes; it is how the generator's `.gen.cs` already works for controls |
| Q7 | Run Explore in CI against our samples, to notice when a page changes? | Later: a nightly Explore of the samples with a `diff_page` report is a cheap UI change detector |
