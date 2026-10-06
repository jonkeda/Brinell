# The `brinell` Command Line

`brinell` is Brinell's one command line (AD-028): the Scraper, test data and scenario checks,
feature-file previews, control generation, and the servers the IDE extensions and agents start.
`brinell --help`, and `--help` after any command, shows what comes next.

## Install

Anywhere:

```powershell
dotnet tool install -g Brinell.Cli
brinell --help
```

For a repository's scripts and CI, as a local tool (working directory: the repository root):

```powershell
dotnet new tool-manifest
dotnet tool install Brinell.Cli
dotnet brinell --help
```

Inside Brinell's own repository, run it from source (working directory: the Brinell root):

```powershell
pwsh tools/Scripts/brinell.ps1 --help
```

The Scraper drives Playwright's Chromium. Install it once per machine with
`brinell scrape install-browser`.

## Commands

| Command | Does | Guide |
| --- | --- | --- |
| `brinell scrape new <Name> --url <start>` | Makes a UI test project with a `scraper.config.md` and an empty `Pages` folder (`--platform`, `--folder`, `--test-id`, `--references`) | [Scraper](../guides/scraper.md) |
| `brinell scrape list` | The projects with a `scraper.config.md` | |
| `brinell scrape status` | The eight steps of a project and what to do next | |
| `brinell scrape check [--visible]` | Opens the start URL: does the app answer, signed in, on a host Explore may go to | |
| `brinell scrape explore` | Explores the app by itself under the project's guards (`--dry-run`, `--max-pages`, `--max-depth`, `--max-minutes`, `--delay-ms`, `--url`) | |
| `brinell scrape record` | Records by hand in a browser window; `c` and Enter captures, Enter stops (`--by-hand`, `--session`, `--url`) | |
| `brinell scrape draft [--page <key>]…` | Drafts page objects from the recorded pages | |
| `brinell scrape verify` | Looks the page objects' members up in the running app | |
| `brinell scrape resolve <session>` | Resolves a test recording's actions to members, verbs and catalog phrases, and lists its gaps; exit 1 with gaps | |
| `brinell scrape draft-test <session>` | Drafts a test recording as an xUnit test, a scenario or both (`--target`, `--name`); exit 1 when gaps are marked | |
| `brinell scrape config get\|set\|add\|remove` | Reads or changes `scraper.config.md`: `set Explore MaxPages 20`, `add "Explore hosts" shop.example` | |
| `brinell scrape install-browser` | Installs Chromium for the Scraper | |
| `brinell data validate [file] [--fix]` | Checks every test set and scenario, or one file | [Test Data](../guides/test-data.md) |
| `brinell data phrases [file] [--keyword When]` | The phrases steps can use, with argument types | |
| `brinell data references <namespace>` | The config and files that use a namespace | |
| `brinell data snippet <file>:<line>` | A method for a step that binds to nothing | |
| `brinell data describe <Type>` | The fields of a test-data type | |
| `brinell data list` | The test sets and what uses them | |
| `brinell data schema [--check]` | Writes (or checks) `testdata.schema.json` | |
| `brinell scenario preview <file> [--out page.html]` | A feature file's preview as a page (`--suite`, `--host`) | [Visual Studio Preview](../guides/visual-studio-preview.md) |
| `brinell scenario results delete [file] [--line N \| --suite <name>]` | Deletes old results of a scenario, a file or a suite from every run | |
| `brinell controls generate --input <paths>` | Each `.tpl.cs` to its `.gen.cs` (`--members all\|actions\|state`) | [generator contract](../../.github/skills/maui-control/references/generator-contract.md) |
| `brinell version` | The version | |

For tools, not people:

| Command | Started by |
| --- | --- |
| `brinell lsp` | The Visual Studio and VS Code extensions: the language server, over stdio |
| `brinell mcp [--solution <folder>]` | Copilot, Claude and other agents: the MCP server, over stdio (`.mcp.json`, `.vscode/mcp.json`) |

## Options Every Command Spells The Same

| Option | Meaning | Default |
| --- | --- | --- |
| `--solution <folder>` | Where projects are found | The nearest folder up with a `.sln` or `.slnx`, else the current one |
| `-p`, `--project <name>` | The project: its name or its folder relative to the solution | The project the current folder is in, else the only one |
| `--json` | The result as one JSON document on stdout | Text |
| `-y`, `--yes` | Yes to the question before it starts | Ask; with no one at a terminal, do not start |
| `--headless` | No browser window | A window when someone is at a terminal |

`data` commands find their `uat.config.md` (or `bdd.config.md`) in the current folder, or take
`--config` and `--assembly`.

Results go to stdout; progress, questions and problems to stderr.

## Exit Codes

| Code | Means |
| --- | --- |
| 0 | Done, nothing wrong |
| 1 | Ran, and found problems: data problems, members verify did not find, a guard that stopped explore, a draft put back |
| 2 | Could not run: a wrong call (it says where `--help` is), no such project, no browser, or not confirmed |

## In A Script

From a scraped project's folder (working directory: the UI test project):

```powershell
brinell scrape explore --headless --yes --json > explore.json
if ($LASTEXITCODE -eq 2) { throw 'explore could not run' }
brinell scrape draft
brinell scrape verify --headless
```

`tools/Scripts/scrape-loop.ps1` runs explore, draft, an agent and verify this way, unattended.
