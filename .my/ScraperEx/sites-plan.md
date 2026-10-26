# The Scraper on real sites: one page per technology, explored in a loop

Status: **built**, steps 1-5, 2026-10-01; what the sites found and is not fixed is in section 6. Builds on [auto-plan.md](auto-plan.md) (Explore, Draft, Verify, the
loop) and the test sites in [plan.md](plan.md) section 10.

## 1. What we want

For each technology in plan.md's section 10, one public site and one page; a UI test project per
technology under `Scraper.Sites/`; Explore run over it; and the loop extended so it runs these too.
Our own sites (the test site, the Blazor sample) are in the loop already.

## 2. The sites

One page each, chosen for what it tests and for being small enough to explore within the budget.
None needs a login (Explore never types, so a sign-in page would stop it).

| Project | Technology | Site and page | Why this page |
| --- | --- | --- | --- |
| `Bruinet.Html.Plain` | plain, jQuery | [the-internet `/tables`](https://the-internet.herokuapp.com/tables) | sortable headers (safe clicks), and `edit` / `delete` links per row: the guards must skip them |
| `Bruinet.Html.React` | React | [DemoQA `/tabs`](https://demoqa.com/tabs) | tabs (safe clicks), a disabled tab, ads from other hosts (the host guard) |
| `Bruinet.Html.Vue2` | Vue 2, Element UI | [Element UI `#/en-US/component/tabs`](https://element.eleme.io/#/en-US/component/tabs) | a hash route, Element's tabs, closable tabs (a `close` that is not a danger word) |
| `Bruinet.Html.Blazor` | Blazor WebAssembly, MudBlazor | [MudBlazor `/components/tabs`](https://mudblazor.com/components/tabs) | Blazor with no websocket (WebAssembly), per-render ids, a large docs page |

Not chosen: Sauce Demo, react-admin and vue-element-admin need a login; Radzen answers headless
Chromium with 403; Vuetify's pages are 7 MB; Fluent UI Blazor is a Blazor Server site (websockets) and
can follow once these pass.

## 3. The projects

```text
Scraper.Sites/                    at the Bruinet root: what the Scraper made of real sites, not tests
  Bruinet.Html.Plain/
    Bruinet.Html.Plain.csproj     references srcnew/Bruinet.Html (Blazor: srcnew/Bruinet.Blazor)
    scraper.config.md             StartUrl, the site's host in Explore hosts, the budget below
    Pages/                        drafted *.scraped.cs
    Recordings/                   Explore's sessions (git-ignored, Q1)
  Bruinet.Html.React/ …
  Bruinet.Html.Vue2/ …
  Bruinet.Html.Blazor/ …
```

The config for a public site is careful and light on the site: `ClickPolicy safe`, every danger
word on, `BlockMethods` as default, `MaxPages 5`, `MaxDepth 1` (the page and what one click on it
shows), `MaxMinutes 2`, `DelayMs 2000`, and the site's host in `Explore hosts` by name. About 5
requests for pages and 5 clicks a run, no faster than one every 2 seconds.

**One page:** Explore follows every link to the app's host, so on a docs site it would leave the
page at once. A new `## Explore` field, **`Within`**, keeps it there: a path (or hash-route) prefix a
link must stay under to be visited; links outside are skipped as "outside Within". Default empty
(the whole app).

## 4. The loop

`scrape-loop.ps1 -Target <name>` gets a target per project: `plain`, `react`, `vue2`, `blazor`, and
`sites` for all four in turn.

| Stage | For a public site |
| --- | --- |
| App | none: the site's URL; the stage checks it answers, and a site that does not is reported as **site down**, not a failure |
| Explore | headless, with the project's config; fails when nothing past the start page was captured or a guard stopped it |
| Guards | no counters on a public site: fails when an unsafe element was clicked (from `explore.md`: every click's judgement) or a non-GET request left the browser unblocked |
| Draft | as now; the drafts must build |
| Agent | as now (Copilot by default) |
| Verify | against the live site; a member not found is reported with the capture's date, since a public site changes |
| Report | as now, plus the site's answer time; `sites` writes one report with a row per site |

Runs work on a copy of the project under `TestResults/scrape-loop/`, as now, so a loop never
writes into `Scraper.Sites/`; **Explore** and **Draft** into `Scraper.Sites/` are run by hand, to update
the committed page objects.

## 5. Steps

One commit per step; each from step 3 on ends with a loop run, its report in the commit message.

| # | Step | Verify |
| --- | --- | --- |
| 1 | `Within` in `ExploreSettings`, the config reader and writer, the judge and Explorer (a link outside is skipped, with the reason) | **Done.** `ExploreSettings.Within`, read from `## Explore`; the judge skips a link outside it ("outside Within … (the config)"), a path or a hash route; the summary's `links` line; the server's config data and the settings tab's field. The account page explored `Within: /pages/account.html` stays on it and its states. 2 tests, 1 extended |
| 2 | The four projects under `Scraper.Sites/` (csproj, config), and `.gitignore` for their sessions | **Done.** Each references `Bruinet.Html` (MudBlazor's `Bruinet.Blazor` too, with platform `html`: a third-party app sets no Bruinet busy signal) and builds empty; a README with the commands; `Scraper.Sites/*/Recordings/` ignored |
| 3 | Explore over each, by hand (`scrape explore --headless --yes`), then `scrape draft` and `scrape verify` | **Done.** the-internet: 1 capture, 0 clicks, `edit`/`delete` left (same-page anchors), 6 of 6 found. DemoQA: 4 captures, 3 tabs clicked, the disabled tab left, 8 analytics POSTs blocked, 36 of 36. Element UI: 3 captures, 3 clicks (MaxMinutes), 81 of 81. MudBlazor: 5 captures, 4 clicks (MaxPages), 93 of 93. All drafts build. **Fixed:** `ByLinkText` (and `:has-text`) match part of a text, so a text is unique only when no other has it in part (DemoQA's "Links" found "Broken Links - Images"); `TabContainerControl` is a base to derive from, so a container is listed to make a control of, not drafted (MudBlazor's draft did not build); a page opens at `DOMContentLoaded` and the capture waits for `load` and the network within one `SettleLimit` (Element UI's `load` never fires: it waited 30 s and crashed); a browser that cannot open the app is exit 2 with a message, not a crash; only `HttpOnly` cookies are watched for a sign-out (Google's one-minute `_gat` stopped Element UI as "signed out"); a link or button text over 50 characters is content, not a locator (MudBlazor's rotating ad). 6 tests. The loop on the test site (careful, test tenant) and the Blazor sample passes as before |
| 4 | The loop's public-site targets and the `sites` target (section 4) | **Done** with `-Agent none`: `scrape-loop.ps1 -Target plain|react|vue2|blazor|sites`; a site that does not answer is "site down", not a failure (not yet seen); Explore passes on its guards and a minimum of captures (MaxPages and MaxMinutes are the budget, not a failure); Guards checks every click's label against danger words outside Explore's judge and lists what was blocked; `sites` writes one summary. `-Target sites -Agent none` passed: 6 of 6, 36 of 36, 81 of 81, 93 of 93; 0, 3, 2 and 4 clicks, none with a danger word. **With Copilot** (`-Target react -Agent copilot`, 77.65 AI credits, 3 m 36 s): it wrote `SideMenuGroup<TParent>` (the sidebar's groups, their links as a collection) and a part with the six groups and `Heading` for the draft's `Tabs2`; `check_code` passed. Found: the agent stage had never run, since `$agent` was the `-Agent` parameter (PowerShell names ignore case); and Verify read `new(this, "Elements")` of the project's own control as a selector: such a member is now not read (its control makes its locator), and the loop says how many. Re-verified: 36 of 42 found once, 6 not read |
| 5 | The guide's "The loop" section gets the sites; plan.md section 10 points here | **Done.** The guide's loop section (the targets, site down, the Guards check, Verify's unread members); plan.md section 10 points here. Links checked |

## 6. Found, not fixed

| Found | Where | Proposal |
| --- | --- | --- |
| A link outside `Within` is reported as "unsafe", which reads as alarming | DemoQA: 33 of 36 skipped | its own verdict, "out of scope", listed apart |
| A sort header with a script handler only (jQuery tablesorter: no role, no `aria-sort`, no `onclick`) is not seen as clickable, so neither clicked nor listed | the-internet `/tables` | a header in a table's `thead` with `cursor: pointer` (the capture would keep the computed cursor) is a sort header |
| A table's per-row links are listed one by one as positions | the-internet: 16 of 17 left out | a row control for the agent (Draft already says "make a control" for the repeated pattern) |
| Element UI takes about 40 s a step (a slow analytics script keeps it from settling, and each click replays from the page) | Element UI | `AllowRequests`-like `IgnoreRequests` for third-party hosts the settle need not wait for; or a shorter settle for public sites |
| A locale in the route names the page (`EnUsComponentTabsPage`) | Element UI | locale segments (`en-US`) left out of class names, as `pages` is |
| `ByLinkText` and `ByPartialLinkText` map to the same `a:has-text(...)` in `Bruinet.Html.Playwright`, so a "link text" is never exact | the framework | `ByLinkText` exact (a framework change: every Html test using it) |

## 7. Questions

| # | Question | Decided (2026-10-01) |
| --- | --- | --- |
| Q1 | Commit the public sites' recordings? plan.md section 10 said not: their content is not ours | Commit the projects, configs and drafted page objects; git-ignore `Scraper.Sites/*/Recordings/` |
| Q2 | Is exploring public sites acceptable? | Only lightly: 5 pages, depth 1, 2 minutes, 2 s between clicks (section 3); by hand or the loop, never on a schedule |
| Q3 | Where do the projects go? | `Scraper.Sites/` at the root: what the Scraper made of real sites, not tests |
