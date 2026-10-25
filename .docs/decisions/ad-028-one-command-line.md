# AD-028: One Command Line

Bruinell has one command, `bruinell` (`tools/Bruinell.Cli`, packed as the dotnet tool `Bruinell.Cli`).
Everything Bruinell runs from a command line is a command of it:

| Decision | What holds |
| --- | --- |
| **One executable** | `bruinell scrape …`, `bruinell data …` (test data and scenario checks), `bruinell scenario …` (preview, deleting results), `bruinell controls generate`, and the servers other programs start: `bruinell lsp` (the IDE extensions) and `bruinell mcp` (agents). The language server, the data validator and the control generator are folders of the one project (`Lsp/`, `Data/`, `Generate/`), tested by one test project. |
| **Groups are nouns, commands verbs** | `bruinell <group> <command> [argument] [options]`. A group on its own, and `--help` anywhere, shows what comes next. |
| **The same words everywhere** | `--solution` (default: the nearest folder up with a `.sln`/`.slnx`), `--project` (default: the project the current folder is in), `--json` (one JSON document on stdout), `--yes`, `--headless`. Results go to stdout; progress, questions and problems to stderr. |
| **Exit codes mean the same** | 0 fine; 1 problems found (data problems, members verify did not find, a guard that stopped explore); 2 it could not run (a wrong call, a missing project, no browser). |
| **No one to ask, no question** | With stdin redirected, a command that would ask does not start without `--yes`, and no browser window opens. |
| **Thin** | A command parses, calls the service the IDEs and MCP call (`ScraperService`, `BddLanguageServer`, `DataValidatorApp`, the generator), and prints. Parsing is System.CommandLine. |

**Why:** there were four entry points (`bruinell-data`, the language server with no arguments,
`--mcp`, `--preview` and `scrape`, and `Bruinell.Generator`), each parsing its own way, two of them
reached by a path into `bin/Debug`. One name is one thing to install and to find with `--help`.
Merging them adds no project (AD-026, R2: one process, one project). Evidence:
[the design](https://github.com/exact-mobile-apps/test-tooling/blob/0c519980/.my/cli/design.md).

**Ruled out:**
- a second tool beside `bruinell` (`bruinell-data`), or an alias for it;
- a `Bruinell.Cli` that starts the language server as another process;
- `bruinell` running UI tests (`dotnet test` with a filter does) or wrapping `dotnet new`;
- the Presenter as a command: it is an app; the scenario runner (`--pipe`, AD-025) stays a process a host starts.

**Broken when:**
- a Bruinell executable with a command line of its own is added beside `bruinell`;
- a guide tells a person to run a Bruinell command by a path into `bin` (scripts that build it first may);
- a command asks a question with no one at a terminal, or prints anything but its result on stdout under `--json`.
