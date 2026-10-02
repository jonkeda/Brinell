# One command line: `brinell`

**Status:** built (section 10); Q1-Q6 decided as recommended. **Updated:** 2026-10-02.

Brinell has a single command, `brinell`. It is a `dotnet tool` that a person, the scrape loop, CI
and the IDE extensions all use. Each of today's separate entry points becomes a subcommand of it.

## 1. Today

| Entry point | How it is run | What it does |
| --- | --- | --- |
| `brinell-data` (`tools/Brinell.DataValidator`) | `dotnet run --project …/Brinell.DataValidator -- validate`. It is packable as a tool but not shipped (an open item in `.my/README.md`) | Checks test data and scenarios with `validate`, `phrases`, `references`, `snippet`, `describe`, `list` and `schema` |
| `Brinell.Bdd.LanguageServer` with no arguments | Run by the IDE extensions as `dotnet <dll>` | The language server, over stdio |
| `… scrape explore\|draft\|verify` | `dotnet tools/…/bin/Debug/net10.0/Brinell.Bdd.LanguageServer.dll scrape …`, from `scrape-loop.ps1` | Runs the Scraper without a view |
| `… --mcp [--solution]` | The `mcp.json` of Copilot and Claude, and the loop | The MCP server |
| `… --preview <file>` | By hand | Writes one scenario file's preview as a page |
| `Brinell.Generator` | `tools/Scripts/CreateHtml.Bat` and `CreateMaui.Bat` | Turns `.tpl.cs` into `.gen.cs` |
| `Brinell.Uat` runner (`--pipe`) | Started by a host (the Presenter) | Runs a suite in its own process (AD-025). **Not a command for people** |
| `Brinell.Presenter` | A MAUI app | The GUI. **Not a command line** |

This has several problems:

- There are two names. `brinell-data` is the only command people know about, and the Scraper has no name at all.
- To run the Scraper, a person has to type a path into `bin/Debug`.
- Each tool has its own way of handling flags: `--x` for one, `-x` for another, and the position of an argument matters in some.
- Help text exists for some commands and is missing for others.
- The scripts call into build output folders, so they break after a Release build or a clean.

## 2. Goals

1. **One name.** People install `brinell` once, and every command a person or a script runs starts with it.
2. **Find by `--help`.** `brinell`, `brinell <group>` and `brinell <group> <command>` each print what comes next.
3. **The same rules everywhere.**
   - Options are the same everywhere: `--solution`, `--project`, `--json`, `--yes`.
   - Exit codes are the same everywhere: 0 means fine, 1 means problems found or a guard stopped the run, 2 means the command could not run. `brinell-data` and `scrape` already follow these codes.
4. **Thin.** A command parses its arguments, calls the service that the IDEs and MCP already call (`ScraperService`, `DataValidatorApp`, `Brinell.Bdd.Workspace`), and prints the result. It holds no logic of its own (AD-022).
5. **Scripts and CI** call `dotnet brinell …` from a tool manifest. They no longer use a path into `bin`.

**Not goals:**
- The CLI does not run UI tests. `dotnet test` with a filter stays the way to run them.
- It does not replace the Presenter.
- It does not wrap `dotnet new`.

## 3. Commands

The form is `brinell <group> <command> [argument] [options]`. Groups are nouns and commands are verbs.

```
brinell
├─ scrape                         the Scraper (docs/guides/scraper.md)
│   ├─ new <Name> --url <start>   a UI test project with a scraper.config.md   (today: MCP create_scraped_project)
│   ├─ list                       the projects with a scraper.config.md        (MCP list_scraped_projects)
│   ├─ status                     the seven steps and what to do next          (MCP get_scraper_progress)
│   ├─ check                      can the Scraper reach the app, signed in     (the view's step 2)
│   ├─ explore                    explore the app by itself under its guards   (today: scrape explore)
│   ├─ record                     record by hand in a visible browser, until Enter
│   ├─ draft [--page <key>…]      draft page objects                           (today: scrape draft)
│   ├─ verify                     look the page objects' members up in the app (today: scrape verify)
│   └─ config get|set|add|remove  read or change scraper.config.md             (MCP update_scraper_config)
├─ data                           test data and scenarios (docs/guides/test-data.md; today: brinell-data)
│   ├─ validate [file] [--fix]
│   ├─ phrases [file] [--keyword When]
│   ├─ references <namespace>
│   ├─ snippet <file>:<line>
│   ├─ describe <Type>
│   ├─ list
│   └─ schema [--check]
├─ scenario                       feature files
│   ├─ preview <file> [--suite] [--out page.html] [--host]   (today: --preview)
│   └─ results delete [file] [--line N | --suite <name>]      (MCP delete_results; .my/uat/tools/bdd-delete-runs.md)
├─ controls
│   └─ generate [--input <paths>] [--members all|actions|state]   (today: Brinell.Generator)
├─ mcp [--solution]               the MCP server over stdio, for agents        (today: --mcp)
├─ lsp                            the language server over stdio, for the IDEs (today: no arguments)
└─ version
```

Notes:

- `mcp` and `lsp` are servers for other programs. Help lists them under **For tools**, after the commands for people.
- `scrape status`, `check`, `record`, `config` and `results delete` are new as commands, but each one calls an existing service method. Only the `scrape` commands that exist today (`explore`, `draft`, `verify`) are needed for the first version (section 8).
- `data validate`'s `--format json` becomes `--json`, the same as everywhere else. `--format json` keeps working as an alias.
- Commands that are not in the tree:
  - The Uat runner's `--pipe` stays a process that a host starts (AD-025).
  - The Presenter stays an app.
  - `scrape-loop.ps1` stays a script: it starts agents, which is not Brinell's job. Its Brinell steps call `dotnet brinell scrape …`.

## 4. Shared options and behaviour

| Option | Meaning | Default |
| --- | --- | --- |
| `--solution <folder>` | Where projects are searched for | The nearest folder upwards from the current one that has a `.sln`/`.slnx`, otherwise the current folder |
| `--project <name>` | Which project to use (`scrape`, `data`) | The project whose folder holds the current folder. If that does not apply and only one project exists, that one; with several, exit 2 and list them |
| `--json` | Print the result as JSON, for scripts and models, with the same shape MCP returns | Text for a person |
| `--yes` | Answer yes to the question asked before a run (explore's budget, `--fix`) | Ask. When stdin is not a terminal and `--yes` is missing, exit 2 instead of hanging |
| `--headless` | No visible browser (`scrape`) | Visible when stdin is a terminal, headless otherwise |
| `-h`, `--help` | Help for the current level | |

Other rules:

- **Output.** Results go to stdout, and progress and logs go to stderr. With `--json`, stdout holds exactly one JSON document, so the output can be piped.
- **Exit codes.** 0 fine. 1 means problems were found: data problems, members that verify found missing, or a guard that stopped explore. 2 means the command could not run: a bad argument, a missing project, or no browser.
- **Ctrl+C.** Stops cleanly. Explore saves what it has captured so far, just as **Stop** does in the view.
- **Interactive only when on a terminal.** The sign-in pause during explore ("press Enter when signed in") happens only when stdin is a terminal. Without a terminal, a sign-in stops the run with exit 1.

## 5. Where it lives (AD-026)

**Recommended:** rename `tools/Brinell.Bdd.LanguageServer` to **`tools/Brinell.Cli`**. Its assembly becomes `brinell` and it is packed as a tool with `ToolCommandName` `brinell`. `Brinell.DataValidator` and `Brinell.Generator` move into it as folders (`Data/`, `Generate/`), and their projects are removed.

- This adds no project. It merges three executables into one, because under R2 one process is reason enough for one project.
- The language server code stays in the project as the `lsp` command. Its namespace stays `Brinell.Bdd.LanguageServer` in a `Lsp/` folder, so the code does not change, only where it sits.
- The IDE extensions ship `brinell.dll` and start it with `lsp`. This is one argument added in `extension.ts` and in the VSIX's server start.
- Generator's Roslyn (`Microsoft.CodeAnalysis.CSharp.Workspaces`) comes along into the tool. Under R3 this is acceptable: the tool is an end product and no library depends on it. The cost is package size (section 7).

**Ruled out:**
- A new `Brinell.Cli` that runs the language server as a second process. That is two executables again, and the CLI would be nothing but a launcher.
- Keeping `brinell-data` beside `brinell` as a second tool. It contradicts the goal of one name.

The decision is recorded once, as **AD-028 "One command line"**. AD-026's Examples list gains `brinell`, and `brinell-data` is removed from `docs/guides/test-data.md`.

## 6. Parsing

**Recommended: System.CommandLine 2.0.** It has been stable since .NET 10. It gives:

- help at every level;
- suggestions for typos;
- tab completion through `dotnet-suggest`;
- `--` handling;
- typed options.

The same parsing written by hand already exists three times in slightly different forms. Each command is a small class that declares its options and a handler. The handler calls the service and returns an exit code. The existing `Run(string[] …)` methods (`DataValidatorApp.Run`, `ScrapeCommand.RunAsync`, `PreviewCommand.Run`) become those handlers, and their own parsers are deleted.

Tests:
- One test checks that the command tree parses every documented example in this design and in the guides.
- The existing tests of `DataValidatorApp` and `ScrapeCommand` keep running through the handler. They pass `TextWriter`s in and read the exit code back.

## 7. Install and packaging

People install it globally:

```powershell
dotnet tool install -g Brinell.Cli
brinell --help
```

The repository and CI use a local tool:

```powershell
# working directory: the repo root, once
dotnet new tool-manifest
dotnet tool install Brinell.Cli
dotnet brinell scrape explore --project Brinell.Html.React --solution Scraper.Sites --headless --yes
```

Inside the Brinell repo itself, the scripts run it from source with `dotnet run --project tools/Brinell.Cli -- …`. A small `tools/Scripts/brinell.ps1` wrapper keeps that short, so nothing points into `bin`.

- **Package name.** The package is `Brinell.Cli` and the command is `brinell`. `pack.ps1` gains it, packed beside the libraries with the same version.
- **Playwright.** The Scraper needs Playwright's driver (about 100 MB, ScraperEx Q8) and Chromium.
  - The tool carries the driver.
  - Chromium is installed on first use, with a prompt.
  - A `brinell scrape install-browser` command installs it on purpose. It is the same as the view's browser-install button from `next-plan.md`.
- **Windows only.** `Brinell.Narrator.Windows` stays outside the tool (Windows-only, R1). `record_video` remains MCP-only until there is a reason to add it.

## 8. Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | AD-028 written, and this design's questions answered | |
| 2 | Rename the LanguageServer project to `tools/Brinell.Cli`. Its assembly becomes `brinell`, it gets `PackAsTool`, and the solution, extensions, `scrape-loop.ps1` and docs are repointed | The same tests pass identically. The IDE extensions start with `lsp` |
| 3 | Add System.CommandLine and the command tree. `lsp`, `mcp`, `scenario preview`, `scrape explore\|draft\|verify` move onto it | The old argument forms are gone. Help is shown at every level |
| 4 | Move DataValidator in as `brinell data …`. Delete the project, repoint `test-data.md`, and remove the "ship `brinell-data`" item from `.my/README.md` | `DataValidator` tests pass as `Brinell.Cli.Tests` |
| 5 | Move Generator in as `brinell controls generate`. `CreateHtml.Bat` and `CreateMaui.Bat` call it | `.gen.cs` output is byte-identical |
| 6 | New commands: `scrape new\|list\|status\|check\|record\|config`, `scenario results delete`, `version` | Each one has a test through its handler |
| 7 | Package it: `pack.ps1`, the tool manifest, and the Chromium install command. A guide `docs/run/cli.md` lists every command, and `docs/README.md` links to it | `dotnet tool install` from `artifacts` works on a clean machine |

Steps 2 to 5 are mechanical moves and are verified by the same tests passing. Step 6 is the only new behaviour.

## 9. Questions

| # | Question | Recommendation |
| --- | --- | --- |
| Q1 | Should the language server project become `Brinell.Cli` (one executable), or should `Brinell.Cli` be a new project beside it? | **Decided:** renamed (section 5) |
| Q2 | Should parsing use System.CommandLine 2.0, or stay hand-written? | **Decided:** System.CommandLine 2.0.12 |
| Q3 | Should Generator join, along with its Roslyn? It is used for Brinell's own controls, not by Brinell's users | **Decided:** yes, `controls generate` |
| Q4 | Should `brinell-data` stay for a release as an alias that prints "use `brinell data`"? | **Decided:** no |
| Q5 | Should the package be `Brinell.Cli` or `Brinell.Tool`? Where is it published (the same feed as the libraries)? | **Decided:** `Brinell.Cli`, packed by `pack.ps1` with the libraries |
| Q6 | Should there be a `brinell present` command that starts the Presenter app? | **Decided:** no |

## 10. As built

Steps 1-7 are done. AD-028 holds the decision, and `docs/run/cli.md` is the guide.

- **Code.** `tools/Brinell.Cli` (assembly `brinell`, tool `Brinell.Cli`) has `BrinellCommand`
  (the tree, exit codes, help for a bare group), `CommonOptions`, `Scrape/`, `Data/`, `Scenario/`,
  `Generate/` and `Lsp/` (the language server, the MCP server, `ServerCommands`). The moved code
  keeps its namespaces (`Brinell.Bdd.LanguageServer`, `Brinell.DataValidator`,
  `Brinell.Generator`), so only where it sits changed. `ScrapeCommand` moved out of
  `Brinell.Scraper` into `Scrape/ScrapeCommands.cs`: printing is the command line's job.
- **Tests.** `testsnew/Brinell.Cli.Tests` holds the three old test projects (`Lsp/`, `Data/`,
  `Generate/`), the scrape command tests (`Scrape/`) and `BrinellCommandTests` (help, wrong calls,
  every documented example parsing, status, list, config, new, version, generate): 308 pass.
  `Brinell.Scraper.Tests` 280 (it gave 7 to `Scrape/`), `Brinell.Bdd.Tests`, `Brinell.Uat.Tests`
  and `Brinell.TestData.Tests` pass. `Brinell.Bdd.Workspace.Tests` (69) passes too: its one test
  that read `srcnew/Brinell.sln` (removed in 2fd5fa57) now makes a solution of its own, and CI,
  AGENTS.md and the docs build `Brinell.sln`.
- **Checked by hand.** `Brinell.sln` builds. The VS Code extension compiles. `scrape-loop.ps1
  -Target test-site -Agent none` passes (explore, draft, verify through `brinell`). `brinell mcp`
  and `brinell lsp` answer `initialize` over stdio. `dotnet pack` makes a 58 MB tool that
  `dotnet tool install` puts in a manifest and runs. `controls generate` writes what the old
  generator wrote, byte for byte. Three committed `.gen.cs` were stale against both (a missing
  `using Brinell.Core.Testing;`) and are regenerated; `Brinell.Maui.Tests` passes.
- **Not checked.** The Visual Studio VSIX (it needs Visual Studio's MSBuild) and both IDEs
  starting `brinell.dll lsp` for real.
- **Left out of the design.** `--verbose`: the services have no log to show. `results delete`
  takes a file, `--line` or `--suite`; the service has no age filter, so no `--older-than`. No tool
  manifest in this repository: `Brinell.Cli` is on no feed yet, so `tools/Scripts/brinell.ps1` runs
  it from source.
