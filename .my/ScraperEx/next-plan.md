# The Scraper, next: a Playwright updater, a test plan, a help guide

Status: **plan**, 2026-09-30. Nothing is built. Questions in section 5 wait for the owner.
Follows [plan.md](plan.md), whose open items (section 13) this takes up: Q8, the IDE runs, and
Pick.

## 1. Q8: Playwright gets an updater of its own

**The problem.** Playwright releases about once a month. Its browser build and its driver change
with every release, and old browser builds stop matching what sites expect. Today the Scraper's
Playwright is fixed at build time:

- the `Microsoft.Playwright` package (1.62) is referenced by `Brinell.Scraper`, so the language
  server, and so both IDE extensions, carry its driver (`.playwright`, 102 MB for win-x64);
- the .NET client and the driver must be the **same version**: the driver speaks a protocol that
  changes between releases, so the driver cannot be swapped under a built server;
- the Chromium build is installed per Playwright version into `%LOCALAPPDATA%\ms-playwright`,
  by hand (`playwright.ps1 install chromium`).

So a Playwright update today means a new extension release, and each release is 100 MB heavier.

**The proposal: the Html source is a process of its own, updated on its own.**

```text
Brinell.Bdd.LanguageServer (in the extension, small)
  +-- Brinell.Scraper (engine: store, patterns, suggestions, code; no Playwright)
        +-- starts, over a named pipe --> Brinell.Scraper.Html (the "scrape host")
                                           Microsoft.Playwright + its driver + a Chromium build
                                           in %LOCALAPPDATA%\Brinell\scrape-hosts\<version>\
```

| Decision | What holds |
| --- | --- |
| **The host is its own executable** | `Brinell.Scraper.Html` holds `PlaywrightScrapeSource` and nothing else. The engine talks to it through `IScrapeSource` over a named pipe, as Presenter talks to a suite's runner (AD-025): versioned JSON messages, one per line. This is the project split Q2 foresaw (AD-026: R2, another process; R3, a dependency the server should not carry). |
| **Installed per version, beside each other** | `%LOCALAPPDATA%\Brinell\scrape-hosts\<version>\`: the host, its driver, and a marker saying which Chromium it installed. A new version is installed next to the old one; the old one is removed once nothing runs it. An update never breaks a recording in progress. |
| **The updater** | `brinell-scrape-host` (a small command, and the same code in the server): `status` (installed, newest available, Chromium present), `install [version]`, `update`, `prune`. It downloads the host package, unpacks it, and runs Playwright's `install chromium` for that version. |
| **Where the package comes from** | A NuGet package of the host, `Brinell.Scraper.Html.Host`, one per Playwright release, built by CI (Q1). NuGet gives versioning, signing and a feed the team already has. |
| **When it runs** | Never on its own during a recording. The view shows "Playwright 1.62 installed, 1.64 available: Update"; **Record** with no host installed offers the install. An agent gets the same as a tool (`scrape_host_status`, `update_scrape_host`) that asks first. |
| **The protocol is versioned** | The host says its protocol version first (as the runner does). A host too new or too old for the server says which side to update. |
| **Tests** | The engine's tests use a fake `IScrapeSource`; the host's tests start Chromium as today (`Category=Browser`). |

What it costs: one more project and process, a package per Playwright release, and the pipe. What
it gains: the extensions drop about 100 MB, a Playwright update is an install, not a release, and a
broken Playwright release can be skipped (`install <older version>`).

**Alternatives looked at:**

| Alternative | Why not |
| --- | --- |
| Ship the driver in the VSIX and release often | Every Playwright release is a Brinell release; 150 MB extensions |
| Keep the client in the server, point it at a newer driver (`PLAYWRIGHT_DRIVER_SEARCH_PATH`) | Client and driver must be one version: a newer driver breaks the older client |
| Use the driver in a Brinell Html test project's output (the earlier Q8 proposal) | Ties the Scraper to a built test project with the same Playwright version as the server; a project on another version breaks it |
| Drive the user's own Chrome or Edge over CDP, no Playwright | Loses Playwright's frame handling (cross-origin frames were the hardest part, spike 0), its waits and its profiles; a second browser layer to maintain |

## 2. The test plan

**What exists (all automated):**

| Suite | Tests | Covers |
| --- | --- | --- |
| `testsnew/Brinell.Scraper.Tests` | 78 (13 with `Category=Browser`, headless Chromium) | snapshot file, config, store, masks, page keys, index; capture on the test site (frames, cross-origin, shadow roots, late rendering, transitions, dialogs, typed text, highlight, pick); the service; outline, signatures, patterns, suggestions, diff; code reading, writing and the build gate |
| `testsnew/Brinell.Bdd.LanguageServer.Tests` | 4 Scraper tests of 83 | the MCP tools and the `brinell/scraper/*` methods over a recording made beforehand |
| `testsnew/Brinell.Blazor.UITests` | 2 of 35 | a page object made by the Scraper, driven against the Blazor sample |

**What is missing:** nothing has run the Scraper view inside VS Code or Visual Studio; Pick has
never had a person's click; no headed recording has been checked by a person; the updater (section
1) has no tests yet because it does not exist.

**The plan:** a manual test plan, `.my/ScraperEx/manual-test-plan.md`, in the form of
[the Visual Studio one](../uat/manual/visual-studio-test-plan.md) (an ID, what to do, what you
should see, a result per check), about an hour:

| Section | Checks |
| --- | --- |
| Setup | the extensions built and installed; a scratch project with `scraper.config.md`; the Blazor sample running |
| Record | Record opens the browser at the start URL; moving between pages captures each once; a hash route and a `pushState` move are captures; an unchanged page is not captured twice; Capture with a dialog open; Stop |
| Login | a site with a login keeps it between sessions (the profile); a sign-in provider's page is not captured |
| Pick and highlight | Pick, click an element: the view shows it; Escape cancels; a line clicked in a live capture is outlined in the browser, also inside a frame |
| Pages, patterns, code | the lists fill; Open file, Compare (the IDE's diff) |
| Generate | Generate… opens the agent's chat with the prompt (VS Code) or puts it on the clipboard (Visual Studio); the agent makes a page object that `check_code` passes |
| Refresh | record a changed page: the page list says "changed", Changes lists the difference |
| Masks | a masked value never reaches `Recordings/` |
| Theme | light, dark and high contrast in both IDEs |
| The updater | status, install, update, prune; Record with no host installed; a host of another protocol version (after section 1) |

Each section is run once in VS Code and once in Visual Studio. The public sites of step 10 are the
extra run for a release.

Automated additions with the updater: the host protocol (a fake host), install and prune on a temp
folder with a local package feed, and the version mismatch message.

## 3. The help guide

**What exists:** `docs/guides/scraper.md`: the config, recording in each IDE, the MCP tools, the
refresh. It is a reference for someone who knows what the Scraper is.

**The plan:**

| Piece | What | Where |
| --- | --- | --- |
| A walkthrough | From nothing to a building page object on the Blazor sample, in about 15 minutes: install the host, add `scraper.config.md`, record three pages, generate, check, and change a page and refresh. Screenshots of the view at each step | `docs/guides/scraper.md`, a "First page object" section at the top |
| Troubleshooting | No browser (host not installed); a page captured too early; a login that does not last; a locator that is only a position (ask for test ids); the build gate fails; a Blazor app without the busy signal; a VSIX that finds no project | the same guide, and a line in `docs/guides/troubleshooting.md` |
| Help in the view | A **?** button: what each button does, what the Pages, Patterns and Code tabs mean, what "changed" means, with a link to the guide. Empty states that say the next step ("No project here has a `scraper.config.md`: add one", "Nothing recorded yet: Record") | the server's page (`ScraperPage`), so both IDEs get it |
| The agent's side | The `scrape-pages` skill already is the agent's guide; the MCP server's instructions point to it | no change |
| The extensions' READMEs | One paragraph each on the Scraper, linking the guide | `tools/Brinell.Bdd.VsCode/README.md`, the VSIX description |

## 4. Steps

One commit per step.

| # | Step | Verify |
| --- | --- | --- |
| 1 | The manual test plan written (section 2), without the updater's checks | a reviewer can follow it cold |
| 2 | The help in the view and the guide's walkthrough and troubleshooting (section 3) | screenshots of the view's help in both themes; the walkthrough followed once from scratch |
| 3 | Spike: the host as a process, `IScrapeSource` over a pipe, the test site captured through it | the 13 browser tests pass through the pipe |
| 4 | `Brinell.Scraper.Html` host project; the engine without Playwright; the server and the extensions lose `.playwright` | extension size measured before and after |
| 5 | The host package and the updater (`status`, `install`, `update`, `prune`), in the view and as MCP tools | install from a local feed into a temp folder; two versions side by side |
| 6 | The updater's checks added to the manual test plan; the plan run in VS Code and Visual Studio | the results recorded in the test plan |
| 7 | AD-027 amended (the host process, the updater); the guide's install section | links valid |

Steps 1 and 2 do not wait for the rest: the test plan and the guide can be used on today's build.

## 5. Questions

| # | Question | Proposal |
| --- | --- | --- |
| Q1 | Where does the host package come from? | The team's NuGet feed, built by CI on each Playwright release (a scheduled job opens a pull request with the new version, as Dependabot does) |
| Q2 | Which browsers? | Chromium only, as now. Firefox or WebKit only if a recording needs them |
| Q3 | Check for updates by itself? | When the view opens, at most once a day, and only show it; never install without a click |
| Q4 | The Html UI tests (`Brinell.Html.Playwright`) also pin Playwright: one version for both? | No: the tests follow their own package reference. The host is only the Scraper's |
| Q5 | Keep old host versions? | The newest two; `prune` removes the rest |
| Q6 | Who runs the manual test plan, and when? | Once now for both IDEs, then before each extension release |
