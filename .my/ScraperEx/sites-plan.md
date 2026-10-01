# The Scraper on real sites: one page per technology, explored in a loop

Status: **plan**, 2026-10-01. Builds on [auto-plan.md](auto-plan.md) (Explore, Draft, Verify, the
loop) and the test sites in [plan.md](plan.md) section 10.

## 1. What we want

For each technology in plan.md's section 10, one public site and one page; a UI test project per
technology under `Recordings/`; Explore run over it; and the loop extended so it runs these too.
Our own sites (the test site, the Blazor sample) are in the loop already.

## 2. The sites

One page each, chosen for what it tests and for being small enough to explore within the budget.
None needs a login (Explore never types, so a sign-in page would stop it).

| Project | Technology | Site and page | Why this page |
| --- | --- | --- | --- |
| `Brinell.Html.Plain` | plain, jQuery | [the-internet `/tables`](https://the-internet.herokuapp.com/tables) | sortable headers (safe clicks), and `edit` / `delete` links per row: the guards must skip them |
| `Brinell.Html.React` | React | [DemoQA `/tabs`](https://demoqa.com/tabs) | tabs (safe clicks), a disabled tab, ads from other hosts (the host guard) |
| `Brinell.Html.Vue2` | Vue 2, Element UI | [Element UI `#/en-US/component/tabs`](https://element.eleme.io/#/en-US/component/tabs) | a hash route, Element's tabs, closable tabs (a `close` that is not a danger word) |
| `Brinell.Html.Blazor` | Blazor WebAssembly, MudBlazor | [MudBlazor `/components/tabs`](https://mudblazor.com/components/tabs) | Blazor with no websocket (WebAssembly), per-render ids, a large docs page |

Not chosen: Sauce Demo, react-admin and vue-element-admin need a login; Radzen answers headless
Chromium with 403; Vuetify's pages are 7 MB; Fluent UI Blazor is a Blazor Server site (websockets) and
can follow once these pass.

## 3. The projects

```text
Recordings/                       at the Brinell root
  Brinell.Html.Plain/
    Brinell.Html.Plain.csproj     references srcnew/Brinell.Html (Blazor: srcnew/Brinell.Blazor)
    scraper.config.md             StartUrl, the site's host in Explore hosts, the budget below
    Pages/                        drafted *.scraped.cs
    Recordings/                   Explore's sessions (git-ignored, Q1)
  Brinell.Html.React/ …
  Brinell.Html.Vue2/ …
  Brinell.Html.Blazor/ …
```

The config for a public site is careful and small: `ClickPolicy safe`, every danger word on,
`BlockMethods` as default, `MaxPages 10`, `MaxDepth 2`, `MaxMinutes 3`, `DelayMs 1000`, and the
site's host in `Explore hosts` by name.

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
writes into `Recordings/`; **Explore** and **Draft** into `Recordings/` are run by hand, to update
the committed page objects.

## 5. Steps

One commit per step; each from step 3 on ends with a loop run, its report in the commit message.

| # | Step | Verify |
| --- | --- | --- |
| 1 | `Within` in `ExploreSettings`, the config reader and writer, the judge and Explorer (a link outside is skipped, with the reason) | unit tests; the test site explored `Within: /pages/` captures only those pages |
| 2 | The four projects under `Recordings/` (csproj, config), and `.gitignore` for their sessions | each builds empty |
| 3 | Explore over each, by hand (`scrape explore --headless --yes`), then `scrape draft` and `scrape verify` | per site: captures, clicks, skipped (the-internet's `delete` / `edit` among them), blocked; the drafts build; members found once. Fixes found go in this step |
| 4 | The loop's public-site targets and the `sites` target (section 4) | `-Target sites -Agent none` passes; then once with Copilot |
| 5 | The guide's "The loop" section gets the sites; plan.md section 10 points here | links valid |

## 6. Questions

| # | Question | Proposal |
| --- | --- | --- |
| Q1 | Commit the public sites' recordings? plan.md section 10 said not: their content is not ours | Commit the projects, configs and drafted page objects; git-ignore `Recordings/*/Recordings/` |
| Q2 | Is exploring public sites acceptable? | Yes at this budget (10 pages, 1 s between clicks), run by hand or the loop, never in CI on a schedule |
| Q3 | Folder name `Recordings/` at the root, beside `srcnew/` and `testsnew/`? | As asked. `testsnew/Scraper.Sites/` would keep test projects together |
