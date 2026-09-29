# Brinell BDD In VS Code

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
- else `brinellBdd.serverPath`;
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

Old results are deleted as in Visual Studio: **✕** beside a scenario's result, **Delete results**
in the preview's bar for the file, and **Brinell BDD: Delete All Results of a Suite** in the
Command Palette for a whole suite (VS Code has no BDD Explorer). Each asks first in a modal dialog,
naming how many runs and files, and deletes for good.

A video script (`*.video.md`) previews as the scenes it will record, with **Record** on its title;
a scenario heading has **Record** beside **Run**. Record builds the suite, runs the scenes with
`BRINELL_RECORD`, and offers **Watch** when the video is made; the preview lists the file's
recordings. Settings: `brinellBdd.recordingsFolder`, `brinellBdd.voice`, `brinellBdd.beat`. It
needs ffmpeg. See [Narrated Recordings](narrated-recordings.md).

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
Visual Studio's (`%LOCALAPPDATA%\Brinell\bdd-breakpoints.json`), so a breakpoint set in one IDE
stops a run started from the other.

## Step Through A Scenario

While a debugged run is paused at a step, the paused step's file opens at its line, and the
preview shows the paused bar with the step marked, as in Visual Studio. The moves:

| Move | Keys | Command |
| --- | --- | --- |
| Step over | Alt+Shift+F10 | **Brinell BDD: Step Over BDD Step** |
| Step into (a `## Step:` call) | Alt+Shift+F11 | **Brinell BDD: Step Into BDD Call** |
| Step out (of a call) | Ctrl+Shift+F11 | **Brinell BDD: Step Out of BDD Call** |
| Continue / Stop | F5 / Shift+F5 | the debugger's own |

The keys work only while a run is paused at a step (the context keys `brinellBdd.paused`,
`brinellBdd.pausedAtCall`, `brinellBdd.pausedInCall`), so they take no key from VS Code otherwise.
The bar's buttons do the same. F10 and F11 stay the C# debugger's.
