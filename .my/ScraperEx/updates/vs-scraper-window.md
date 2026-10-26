# The Scraper window in Visual Studio: labels, the project list, restored windows, a new project

Status: **built**, 2026-10-01; what differs from the plan is in section 7. Four updates from using the Scraper window in Visual Studio. The page
is the language server's (`tools/Bruinet.Bdd.LanguageServer/ScraperPage.cs`), so updates 1, 2 and
4 reach VS Code too; update 3 is Visual Studio's own.

## 1. Labels above the bar's inputs

Today the bar is a row of bare controls: the project picker (no text, only an `aria-label`), and
two inputs whose placeholders ("Session name", "Start URL (config's)") vanish once something is typed.

```text
Project               Session                 Start URL
[Site.UITests  v] [+] [                    ]  [https://localhost:5001/   ]  Record  Capture  Pick  Stop
```

| Decision | What holds |
| --- | --- |
| A `<label>` above each | `Project`, `Session`, `Start URL`, each tied to its control (`for=`), so a screen reader and a UI test find them by name. The buttons keep their place on the inputs' line |
| The placeholders say what an empty field does | Session: "today's date"; Start URL: the config's start URL itself (`https://localhost:5001/`), not "(config's)" |
| Layout | Each label and its control a small column (`.sc-field`: `display: grid`); the bar stays one wrapping row, so a narrow window wraps whole fields, not a label from its input |
| AutomationIds | The controls keep their ids (`sc-project`, `sc-session`, `sc-url`) |

## 2. The project picker: the projects with a `scraper.config.md`

**Found:** the picker shows `Site.UITests`, a project the loop makes under
`TestResults/scrape-loop/<run>/`, not one of the repository's. `ScrapeProject.FindAll` walks the
whole solution folder and skips only `bin`, `obj`, `node_modules`, `Recordings` and dot-folders, so
every loop run adds copies (`Site.UITests`, `BlazorSample.UITests`, `Bruinet.Html.React`, …), the
same name many times. `Project(name)` then takes the first folder of that name it finds, which may
be a throwaway copy.

| Decision | What holds |
| --- | --- |
| The open solution's projects first | When the solution file is known (Visual Studio's open `.sln`/`.slnx`, VS Code's workspace `.sln`), a scraped project is a project **in the solution** whose folder has a `scraper.config.md`. That is what "a project" means in the IDE |
| Without a solution, the folder walk, with git's view | The walk also skips `TestResults` and `artifacts`, and any folder `git check-ignore` reports ignored (one call for the candidate folders), so a run's copy never shows |
| A name once | Two projects of the same name (two folders) are shown as `Name (relative folder)`; `Project(name)` refuses an ambiguous name and says which folders, rather than taking the first |
| What the picker shows | The project's name; the folder as its tooltip; a project whose config does not parse is listed with "config has errors" and opens the config on choosing it |
| No project yet | As today: "No project here has a scraper.config.md yet", and the new project window (update 4) instead of the old Create config form |

The language server gets the solution path where it already gets the solution folder
(`--solution`); the `.sln`/`.slnx` there is read as `BddWorkspace` already reads it (the folders of the
projects it lists), else the folder walk.

## 3. A tool window restored at start-up shows nothing

**Found:** when Visual Studio starts with the BDD Explorer (or the Scraper, or the Scenario
Preview) already open from the last session, the window is empty. Visual Studio rebuilds the window
from its saved layout by calling its constructor, which only creates an empty WebView2
(`BddExplorerWindow`, `ScraperWindow`, `ScenarioPreviewWindow`). The page is loaded and filled by
`PreviewController.AttachExplorerAsync` (`ScraperController.AttachAsync`, `AttachAsync`), and only
the menu commands (`ShowExplorer`, `ShowScraper`, `ShowPreview` in `BruinetBddPackage`) call those.
A restored window is never attached. The package may not even be loaded yet: it auto-loads on a
feature file or debugging, and a restored window loads it without running a command.

| Decision | What holds |
| --- | --- |
| The window attaches itself | Each window overrides `ToolWindowPane.OnToolWindowCreated` and asks the package to attach it (`BruinetBddPackage.AttachAsync(this)`), on the main thread, after the package's `InitializeAsync` has started the language server. The commands then only show the window: attaching twice is already harmless (`IsReady`) |
| The server may still be starting | The window shows "Starting the Bruinet language server…" until `_controller.StartAsync` is done; the attach waits for it rather than finding `_server` null |
| A solution opened later | The Explorer refreshes when the solution opens (`IVsSolutionEvents.OnAfterOpenSolution`): at start-up the window may be restored before any solution is loaded, and the suites come from the solution |
| All three windows | The same fix for the Explorer, the Scraper and the Preview (the Preview with the active feature file, or its empty state) |
| Checked by | Open each window, close Visual Studio, start it again: each shows its content without a command. With and without a feature file open; with the solution opened from the start page afterwards |

## 4. A new project window

A **+** button next to the project picker opens a form that makes a scraped UI test project: the
`.csproj` and its `scraper.config.md`, and adds it to the solution.

```text
New scraped project                                                   [x]

  Name          [MyApp.UITests                         ]
  Folder        [testsnew\MyApp.UITests                ]  (relative to the solution)
  Platform      (o) Html   ( ) Blazor
  Start URL     [https://localhost:5001/               ]
  Test ids      [data-testid, data-test                ]
  Bruinet       (o) Project references (this repository)   ( ) NuGet packages
  Add to solution [x] Bruinet.sln, folder [Scraped        v]

  Writes:  testsnew\MyApp.UITests\MyApp.UITests.csproj
           testsnew\MyApp.UITests\scraper.config.md
           testsnew\MyApp.UITests\Pages\

                                             [Cancel]  [Create project]
```

| Decision | What holds |
| --- | --- |
| Where | In the Scraper page, as a panel over the right-hand side (as Create config is today), so both IDEs have it; **+** has the label "New scraped project" and the AutomationId `sc-new-project` |
| What it writes | The `.csproj` (`net10.0`, `IsPackable` false; `Bruinet.Html`, and `Bruinet.Blazor` for Blazor), a `scraper.config.md` (`## App`: Platform, StartUrl, Namespace `<Name>.Pages`, Pages; `## Test ids` from the field; `## Explore` with the careful defaults and the start URL's host in `## Explore hosts` when it is not `localhost`), and an empty `Pages/`. The config is written by `ScraperConfigWriter`, as Create config does |
| Bruinet references | Inside this repository: project references to `srcnew/`, relative, as `Scraper.Sites` has them. Elsewhere: `PackageReference`s at the version of the running language server. The default is whichever the solution folder shows (a `srcnew/Bruinet.Html` beside it, or not) |
| Added to the solution | The server writes the files; the IDE adds the project: Visual Studio through `DTE.Solution.AddFromFile` into the chosen solution folder, VS Code with `dotnet sln add`. Unticked, the files are written and nothing else |
| Checks before writing | The name is a valid project name and not taken in the solution; the folder does not exist or is empty; the start URL is an absolute http(s) URL. Each problem shows under its field, and **Create project** stays off until there are none |
| Afterwards | The picker reloads and selects the new project; the Explore tab opens with its guard summary, so the next step is in view |
| An existing project | The form keeps Create config's other use: **Add a config to an existing project** (a link in the form) lists the solution's test projects without one and writes only the `scraper.config.md` |
| The server | `bruinet/scraper/newProject` (name, folder, platform, start URL, test ids, references) → the project's path, or the problems by field; `bruinet/scraper/projectDefaults` → the folder and references to propose. MCP gets `create_scraped_project` (asks first, as `update_scraper_config` does) |

## 5. Steps

| # | Step | Verify |
| --- | --- | --- |
| 1 | Labels on the bar (update 1) | The server's page test (`ScraperMcpTests` reads the host page): each control has its label; a screenshot in Visual Studio and VS Code |
| 2 | The project list from the solution, run copies left out, duplicate names shown apart (update 2) | Unit tests: a solution with two scraped projects and a `TestResults` copy lists two; a duplicate name is refused by `Project(name)`. In Visual Studio with this repository: `Bruinet.Blazor.UITests` and the four `Scraper.Sites` projects |
| 3 | Restored windows attach themselves (update 3) | The restart check in update 3, in Visual Studio, by hand (no VS UI test here) |
| 4 | `newProject` and `projectDefaults` in the server and MCP; the form and **+** (update 4) | Unit tests: the files written for Html and Blazor, inside and outside this repository; the checks. The new project builds, and Explore runs on it against the test site |
| 5 | The guide (`docs/guides/scraper.md`, "Make a project scrapeable") shows **+** and the form | Links valid |

## 6. Questions

| # | Question | Proposal |
| --- | --- | --- |
| Q1 | The default folder for a new project | `testsnew\<Name>` inside this repository; elsewhere the folder of the solution's other test projects, else beside the solution |
| Q2 | Should the solution file the language server reads be the root `Bruinet.sln` or `srcnew\Bruinet.sln`? | The one Visual Studio has open; the CLI's `--solution` accepts a file or a folder |

## 7. As built

| # | Built | Differs from the plan |
| --- | --- | --- |
| 1 | `Project`, `Session`, `Start URL` labels above the controls (`for=`), one height for the bar's controls; Session's placeholder is the session it falls back to (`recording`), Start URL's the project's start URL | Session falls back to `recording`, not today's date: the placeholder says what happens |
| 2 | `ScrapeProject.FindAll`: the projects the `.sln`/`.slnx` in the folder lists **and** those under it, skipping `TestResults` and `artifacts`; `ScraperService.Label` and `Project(name)` by name, label or relative folder, refusing an ambiguous name; `projects` sends `label`; the picker shows the label, its folder as tooltip; the server takes a solution file or its folder | Not solution-first: `Scraper.Sites` is not in `Bruinet.sln`, and solution-only would hide it. No `git check-ignore`: `TestResults` is skipped by name. A config that does not parse still fails the whole list (as before) |
| 3 | Each tool window's `OnToolWindowCreated` calls `BruinetBddPackage.Attach`; the commands go through the same `AttachAsync`. The Explorer already refreshed on a solution opening; the Scraper now gets `{type: "projects"}` then and lists its projects again | No "Starting the language server" text: the attach waits for the server. **Not run in Visual Studio**: the restart check is the owner's |
| 4 | `ProjectMaker` (defaults, problems by field, the files); `bruinet/scraper/projectDefaults`, `newProjectCheck`, `newProject`; MCP `create_scraped_project` (asks first); the form behind **+**, which also opens when no project has a config; `addProject` for the IDE: Visual Studio through DTE into a solution folder, VS Code with `dotnet sln add --solution-folder` | The form does not open the Explore tab's guard summary by itself: it selects the project and the Explore tab |
| 5 | The guide's "Make a project scrapeable" and its MCP table | |

Checked: Scraper tests 272, language server 89 (5 new tests: discovery with a solution and a loop
copy, duplicate names, the files for Html, Blazor and in this repository, problems by field, the
JSON-RPC methods and the MCP tool asking first); the VS extension builds; the VS Code extension
type-checks. The page through the stand-in host on this repository: six projects (the five
`Scraper.Sites` and `Bruinet.Blazor.UITests`, no `TestResults` copies), the labels aligned, the form
proposing `testsnew/MyApp.UITests` with project references, a missing start URL shown under its
field and **Create project** off until it is given. Not tried inside Visual Studio or VS Code.
