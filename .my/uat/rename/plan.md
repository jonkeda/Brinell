# Rename `Uat*` to `Bdd*`: Bdd is the main, Uat a specialization

Status: **plan**, decided 2026-09-28, not started.

## 1. Goal

**Bdd is the main; Uat is a specialization of it.** `Brinell.Bdd` is the feature-file engine, and
everything built around feature files (the Presenter, the workspace, the preview, the language
server, the IDE extensions) is Bdd. `Brinell.Uat` adds the UI to it: pages, controls and page
scope. So only `Brinell.Uat` and its tests keep `Uat` names; everything else takes `Bdd` names,
types and projects alike. `UatBinder` becomes `BddBinder`, `IUatClock` becomes `IBddClock`, and
`Brinell.Uat.LanguageServer` becomes `Brinell.Bdd.LanguageServer`.

A Bdd tool may use `Brinell.Uat` (the workspace builds a UI suite's catalogs through it), as a
general tool handles the specialization too. The name says what the thing is about, not what it
references.

This reverses AD-021's "Type names did not move with the code", and extends it to the projects
AD-022 named. Both are amended in the same work (section 7).

## 2. Scope

Counted on `move` at 4c4955e (type declarations, not files):

| Project | `Uat*` types | Decision |
| --- | --- | --- |
| `srcnew/Brinell.Bdd` | 100 (93 public, 7 internal) | **rename** |
| `testsnew/Brinell.Bdd.Tests` | 10 | **rename**, test classes with their subject (`UatBinderTests` to `BddBinderTests`) |
| `srcnew/Brinell.Core` | 1: `UatEffectiveStepKeyword` | **rename** |
| `srcnew/Brinell.Presenter` | 24 | **rename** |
| `testsnew/Brinell.Presenter.Uat.Tests` | 3 | **rename** the types; the project's name is Q1 |
| `samples/Todo` | 1: `UatScenarioOrderer` | **rename** |
| `tools/Brinell.Uat.Workspace` and its tests | 9 + 3 | **rename the project and its types** (section 3) |
| `tools/Brinell.Uat.Preview` and its tests | 13 + 4 | **rename the project and its types** |
| `tools/Brinell.Uat.LanguageServer` and its tests | 20 + helpers | **rename the project and its types** |
| `tools/Brinell.Uat.VisualStudio`, `tools/Brinell.Uat.VisualStudio.CodeLens` | 15 | **rename the projects and their types**, and the extension's user-facing names (section 4) |
| `tools/Brinell.Uat.VsCode` | none (TypeScript) | **rename the folder**, and the extension's user-facing names (section 4) |
| `srcnew/Brinell.Maui` | 1: `UatPlatformsAttribute` | **keep** (decided) |
| `srcnew/Brinell.Uat`, `testsnew/Brinell.Uat.Tests` | 58 | **keep**: the specialization |

The checks found:
- **No clashes:** no `Bdd*` type exists with any of the new names. The existing ones are the
  attributes, `BddHook*`, `BddSets` and `BddStepDefinitionsBase`.
- **No consumers outside this repository:** nothing under `MainConstruction2` outside Brinell uses
  these types or tools, so no `[Obsolete]` aliases are needed.

Not in scope:
- diagnostic codes (`UAT0xx`, `UATB0xx`, `UATA0xx`, `UATC0xx`, `UATG0xx`);
- the file formats (`uat.config.md` for a UI suite, `bdd.config.md` for a service suite), which
  already follow the rule;
- the `UatScenario` trait (Q2);
- member names that only contain "Uat";
- the namespaces of `Brinell.Bdd` and `Brinell.Presenter`, which are already right.

## 3. The tool projects

| Now | Becomes |
| --- | --- |
| `tools/Brinell.Uat.Workspace` (+ `.Tests`) | `tools/Brinell.Bdd.Workspace` (+ `.Tests`) |
| `tools/Brinell.Uat.Preview` (+ `.Tests`) | `tools/Brinell.Bdd.Preview` (+ `.Tests`) |
| `tools/Brinell.Uat.LanguageServer` (+ `.Tests`) | `tools/Brinell.Bdd.LanguageServer` (+ `.Tests`) |
| `tools/Brinell.Uat.VisualStudio` (+ its `.slnx`) | `tools/Brinell.Bdd.VisualStudio` |
| `tools/Brinell.Uat.VisualStudio.CodeLens` | `tools/Brinell.Bdd.VisualStudio.CodeLens` |
| `tools/Brinell.Uat.VsCode` | `tools/Brinell.Bdd.VsCode` |

The namespaces follow the folders (`Brinell.Uat.Workspace` becomes `Brinell.Bdd.Workspace`). The
types follow the rule: `UatSuite` → `BddSuite`, `UatFileAnalysis` → `BddFileAnalysis`,
`UatLanguageServer` → `BddLanguageServer`, `UatMcpServer` → `BddMcpServer`, `UatRunController` →
`BddRunController`, and so on.

Things to watch:
- **The Preview's embedded resources** (`preview.css`, `preview-host.js`) are read by their
  namespace-qualified name, so check them after the move. So are the snapshot files of its tests.
- **The server's dll** becomes `Brinell.Bdd.LanguageServer.dll`. It is named in:
  - the VSIX csproj (`ServerProject`, the publish target), its server path and its language
    client;
  - the VS Code client's `serverPath()`;
  - `release.yml` and `ci.yml`.
- **`brinell-data` and the Presenter** reference the Workspace: update their project references.
- **`InternalsVisibleTo`**, the test projects' references, the `.slnx`, and the paths in both
  workflows.

## 4. The extensions' user-facing names

Bdd is the main, so the extensions are "Brinell BDD". This changes what a user sees and has
stored:

| Now | Becomes | Note |
| --- | --- | --- |
| Display name "Brinell UAT" (both extensions) | "Brinell BDD" | |
| VSIX identity `Brinell.Uat.VisualStudio.<guid>` | `Brinell.Bdd.VisualStudio.<guid>` | a new identity: uninstall the old extension first |
| VS Code package `brinell-uat`, publisher-qualified id | `brinell-bdd` | a new extension id |
| VS Code settings and commands `brinellUat.*` | `brinellBdd.*` | a user's `brinellUat.serverPath` setting is not carried over |
| Content type `brinell-uat` | `brinell-bdd` | |
| Command names `Brinell.UatExplorer`, the window **UAT Explorer**, **Toggle UAT Step Breakpoint** | `Brinell.BddExplorer`, **BDD Explorer**, **Toggle BDD Step Breakpoint** | key bindings unchanged |
| Output pane **Brinell UAT**, Test Explorer controller "Brinell UAT", CodeLens "used in N UAT steps" | **Brinell BDD**, "used in N BDD steps" | |
| MCP server `brinell-uat`, prompt file tools `['brinell-uat']` | `brinell-bdd` | update the `.mcp.json` example |
| Diagnostic source "Brinell UAT" | "Brinell BDD" | |
| Breakpoints file `%LOCALAPPDATA%\Brinell\uat-breakpoints.json` | `bdd-breakpoints.json` | read by `Brinell.Bdd` (`BddBreakpoints`) and written by both IDEs: all three change in one commit; old breakpoints are lost |

## 5. How

Mechanical, one commit per step, each verified by the same tests passing identically:

1. **Rename list.** Generate the old-to-new type list from the declarations of every renamed row,
   longest name first. Check that no new name exists already, and that none is the name of a kept
   type.
2. **Brinell.Core and Brinell.Bdd, with every user:** Brinell.Uat, the tools, tests, samples and
   docs.
   - The rename is a whole-word replace (`\bUatBinder\b` to `BddBinder`) over `*.cs`, `*.md`,
     `*.xaml`, `*.tpl.cs` and `*.ts`, and **only** for names on the list. `UatScenario` never
     touches the Brinell.Uat type `UatScenarioTestBase`.
   - `git mv` each file whose name is its type (`UatBinder.cs` to `BddBinder.cs`), and each test
     class file with it.
   - The breakpoints file name changes here too (section 4), in Brinell.Bdd and in both IDEs.
3. **The Presenter** and its tests' types, and `UatScenarioOrderer` in the Todo sample.
4. **The tool projects** (section 3): `git mv` the folders and project files, change their
   namespaces, rename their types, and update what names them.
5. **The extensions' user-facing names** (section 4).
6. **Docs.** Amend AD-021 and AD-022 (section 7), and update the invariants and `.docs`
   contracts. Update the `docs/` guides (rename `visual-studio-preview.md` to fit, and fix the
   links in `docs/README.md`), the skills (`.github/skills/*`) and the prompt file. Update the
   `.my/uat/tools` plans and the manual test plan. Add a CHANGELOG entry under **Changed**: a
   breaking rename of public types, tool projects and extension identities.

After each commit, from the Brinell root:
- build `srcnew\Brinell.sln`;
- run the unit suites CI runs, plus `Brinell.Bdd.Tests`, the Workspace, Preview and
  LanguageServer tests, and the Presenter's;
- build the VSIX with Visual Studio's MSBuild, and compile the VS Code client (`npm run compile`);
- run tier 1 of `Brinell.Maui.UITests`, and check the Todo UAT suites' counts are unchanged (MAUI
  43, web 35, mixed 4, service 3).

At the end:
- a search for `Uat*` type declarations finds them only in `Brinell.Uat`, its tests and
  `UatPlatformsAttribute`;
- a search for `Brinell.Uat.` followed by `Workspace`, `Preview`, `LanguageServer`,
  `VisualStudio` or `VsCode` finds nothing outside `docs2/`.

## 6. Open questions

| # | Question | Default if not answered |
| --- | --- | --- |
| Q1 | `testsnew/Brinell.Presenter.Uat.Tests` tests the Presenter's UAT running. Rename the project to `Brinell.Presenter.Bdd.Tests`? | Rename, by the same rule. |
| Q2 | The run address trait is `UatScenario=<file>#<slug>` (`[UatScenarioTheory]` in Brinell.Uat sets it; `UatScenarioAddress` in Brinell.Bdd builds it). The address type is renamed; should the trait's text become `BddScenario`? It is in every filter the IDEs and scripts use. | Keep the trait text: it is a stored format, like the diagnostic codes. |

## 7. The AD amendments

**AD-021:** replace "Type names did not move with the code" with:
- Bdd is the main and Uat a specialization of it;
- `Uat` names are for `Brinell.Uat` and its tests only (and `UatPlatformsAttribute` for now);
- everything else takes `Bdd` names, types and projects alike.

Add to **Broken when**: a new `Uat*` type, or a `Brinell.Uat.*` project, outside Brinell.Uat.

**AD-022:** its table and text name the `Brinell.Uat.*` tool projects; replace them with the
`Brinell.Bdd.*` names.
