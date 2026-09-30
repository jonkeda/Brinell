# The Scraper, next: Playwright in the extensions, a test plan, a help guide

Status: **plan**, 2026-09-30. Nothing is built. Questions in section 5 wait for the owner.
Follows [plan.md](plan.md), whose open items (section 13) this takes up: Q8, the IDE runs, and
Pick.

## 1. Q8: Playwright ships in the extensions, for now

**Decided (2026-09-30):** the extensions carry Playwright's driver, about 100 MB more. It is the
easier way now; an updater of its own is for later, when Playwright's pace makes releases a burden.

What that means:

| | |
| --- | --- |
| Where the driver is | In the language server's output (`.playwright`), and so in both extensions: the VSIX builds it in (`PublishLanguageServer`), the VS Code package gets it from `dotnet publish` in `release.yml`. Nothing to change |
| Its platform | The release job runs on `windows-latest`, so the driver is win-x64. The VS Code extension on macOS or Linux would have the wrong one; it already needs .NET on Windows for the rest (the narrator's recorders), so this is a known limit, said in the guide |
| A Playwright update | A version bump of `Microsoft.Playwright` in `srcnew/Directory.Packages.props` and an extension release |
| The browser | Not in the extensions: Chromium is installed per Playwright version into `%LOCALAPPDATA%\ms-playwright`. Today Record fails with "run `playwright.ps1 install chromium`". **Step 3** makes it a button: the server runs Playwright's own installer (`Microsoft.Playwright.Program.Main(["install", "chromium"])`), with progress in the view, and an MCP tool `install_browser` that asks first |
| Later | The updater designed on 2026-09-30 (the Html source as a process of its own, installed per version under `%LOCALAPPDATA%\Brinell\scrape-hosts`, with `status`, `install`, `update`, `prune`) is in git at a3763989 (`.my/ScraperEx/next-plan.md`), should the size or the pace start to hurt |

## 2. The test plan

**What exists (all automated):**

| Suite | Tests | Covers |
| --- | --- | --- |
| `testsnew/Brinell.Scraper.Tests` | 78 (13 with `Category=Browser`, headless Chromium) | snapshot file, config, store, masks, page keys, index; capture on the test site (frames, cross-origin, shadow roots, late rendering, transitions, dialogs, typed text, highlight, pick); the service; outline, signatures, patterns, suggestions, diff; code reading, writing and the build gate |
| `testsnew/Brinell.Bdd.LanguageServer.Tests` | 4 Scraper tests of 83 | the MCP tools and the `brinell/scraper/*` methods over a recording made beforehand |
| `testsnew/Brinell.Blazor.UITests` | 2 of 35 | a page object made by the Scraper, driven against the Blazor sample |

**What is missing:** nothing has run the Scraper view inside VS Code or Visual Studio; Pick has
never had a person's click; no headed recording has been checked by a person.

**The plan:** a manual test plan, `.my/ScraperEx/manual-test-plan.md`, in the form of
[the Visual Studio one](../uat/manual/visual-studio-test-plan.md) (an ID, what to do, what you
should see, a result per check), about an hour:

| Section | Checks |
| --- | --- |
| Setup | the extensions built and installed (the VSIX about 100 MB larger: Playwright's driver); a scratch project with `scraper.config.md`; the Blazor sample running |
| Record | Record opens the browser at the start URL; moving between pages captures each once; a hash route and a `pushState` move are captures; an unchanged page is not captured twice; Capture with a dialog open; Stop |
| Login | a site with a login keeps it between sessions (the profile); a sign-in provider's page is not captured |
| Pick and highlight | Pick, click an element: the view shows it; Escape cancels; a line clicked in a live capture is outlined in the browser, also inside a frame |
| Pages, patterns, code | the lists fill; Open file, Compare (the IDE's diff) |
| Generate | Generate… opens the agent's chat with the prompt (VS Code) or puts it on the clipboard (Visual Studio); the agent makes a page object that `check_code` passes |
| Refresh | record a changed page: the page list says "changed", Changes lists the difference |
| Masks | a masked value never reaches `Recordings/` |
| Theme | light, dark and high contrast in both IDEs |
| The browser | Record with no Chromium installed offers Install browser; the install shows progress and Record works after it |

Each section is run once in VS Code and once in Visual Studio. The public sites of step 10 are the
extra run for a release.

Automated addition: a missing browser is reported as "not installed" (a Playwright install folder
pointed at an empty temp folder), not as Playwright's raw error.

## 3. The help guide

**What exists:** `docs/guides/scraper.md`: the config, recording in each IDE, the MCP tools, the
refresh. It is a reference for someone who knows what the Scraper is.

**The plan:**

| Piece | What | Where |
| --- | --- | --- |
| A walkthrough | From nothing to a building page object on the Blazor sample, in about 15 minutes: install the browser, add `scraper.config.md`, record three pages, generate, check, and change a page and refresh. Screenshots of the view at each step | `docs/guides/scraper.md`, a "First page object" section at the top |
| Troubleshooting | No browser (Install browser); VS Code on macOS or Linux (the driver is Windows'); a page captured too early; a login that does not last; a locator that is only a position (ask for test ids); the build gate fails; a Blazor app without the busy signal; a VSIX that finds no project | the same guide, and a line in `docs/guides/troubleshooting.md` |
| Help in the view | A **?** button: what each button does, what the Pages, Patterns and Code tabs mean, what "changed" means, with a link to the guide. Empty states that say the next step ("No project here has a `scraper.config.md`: add one", "Nothing recorded yet: Record") | the server's page (`ScraperPage`), so both IDEs get it |
| The agent's side | The `scrape-pages` skill already is the agent's guide; the MCP server's instructions point to it | no change |
| The extensions' READMEs | One paragraph each on the Scraper, linking the guide | `tools/Brinell.Bdd.VsCode/README.md`, the VSIX description |

## 4. Steps

One commit per step.

| # | Step | Verify |
| --- | --- | --- |
| 1 | The manual test plan written (section 2) | a reviewer can follow it cold |
| 2 | The help in the view and the guide's walkthrough and troubleshooting (section 3) | screenshots of the view's help in both themes; the walkthrough followed once from scratch |
| 3 | Install browser: the view's button and `install_browser`, when Record finds no Chromium | the automated check above; the manual check in the test plan |
| 4 | The manual test plan run in VS Code and Visual Studio | the results recorded in it |
| 5 | AD-027 amended (Playwright in the extensions; the browser installed on demand); the guide's install section | links valid |

## 5. Decisions

| # | Question | Decided (2026-09-30) |
| --- | --- | --- |
| Q1 | Which browsers? | Chromium only |
| Q2 | Who runs the manual test plan? | Split. Claude runs what needs no IDE window: the recording browser (driven by a script as a person would), the view's page through a stand-in host, MCP, masks, refresh, Install browser. The owner runs what needs Visual Studio or VS Code on screen: the commands, the tool window and webview, Open file, Compare, Generate, themes, and Pick with a real click. The test plan marks each check with who runs it. Before each extension release, the owner's part again |
