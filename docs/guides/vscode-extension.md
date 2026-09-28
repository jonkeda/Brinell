# Brinell UAT In VS Code

The VS Code extension (`tools/Brinell.Bdd.VsCode`) is the Visual Studio extension's twin. It
starts the same language server (`Brinell.Bdd.LanguageServer`) and shows the same Scenario Preview,
so the two IDEs say the same thing about a file. What each feature does is in the Visual Studio
guide ([visual-studio-preview.md](visual-studio-preview.md)). This page is only what differs in
VS Code.

It needs the .NET 10 runtime, and the C# extension (`ms-dotnettools.csharp`) to debug.

## Build And Run

From `tools/Brinell.Bdd.VsCode`:

```powershell
npm install
npm run compile
```

Open the folder in VS Code and press **F5** to start an Extension Development Host. In it, open a
folder with a suite, such as `samples/Todo`. The extension uses:
- the server in its own `server` folder when packaged;
- else `brinellUat.serverPath`;
- else the repository's build of `tools/Brinell.Bdd.LanguageServer` (build it first:
  `dotnet build tools\Brinell.Bdd.LanguageServer` from the Brinell root).

## In The Editor

Feature files, step files and the suite configs stay Markdown files. The language server adds
the gate's diagnostics, outline, folding, colours, completion, hover, go to definition, find
references, quick fixes and rename, as in Visual Studio.

## Scenario Preview

**Ctrl+K V** in a feature or step file, or the preview button in the editor's title bar, opens the
preview beside it. It follows the active file, the caret and the preview follow each other, and
**Run** and **Debug** on a heading run that scenario through Test Explorer.

## Test Explorer

The **Testing** view lists each suite, its feature files and their scenarios; an outline is one
test for all its rows. **Run** builds the suite's project, then runs
`dotnet test --no-build --filter BddScenario=<address>`, and marks each scenario from the report
its run wrote. A failed scenario points at its failed step. **Debug** runs the same way; the C#
debugger attaches to the testhost, and step breakpoints stop the run. The suite's test class must
use `[UatScenarioTheory]`; a suite that does not says so in the view. One run at a time, and
cancelling a run stops the test process and the app it started.

## Step Breakpoints

**Ctrl+K F9**, the editor's context menu, or a click on a step's keyword in the preview toggles a
step breakpoint. A red dot marks it in the gutter. The breakpoints are the same per-user file as
Visual Studio's (`%LOCALAPPDATA%\Brinell\uat-breakpoints.json`), so a breakpoint set in one IDE
stops a run started from the other.
