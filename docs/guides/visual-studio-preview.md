# Scenario Preview In Visual Studio

The Brinell UAT extension shows a feature or step file as readable steps, beside the editor: what
each step binds to and on which page, the gate's diagnostic on a step that binds to nothing, a
callable call unfolded into its body, and each Scenario Outline row. It is the same check the
xUnit gate and `brinell-data validate` run, on the text as you type it.

Visual Studio 2022 17.10 or later (2026 too), and the .NET 10 runtime, which a Brinell suite needs
anyway.

## Build And Install

The extension needs Visual Studio's MSBuild, so it has a solution of its own. From the Brinell root:

```powershell
& "<Visual Studio>\MSBuild\Current\Bin\MSBuild.exe" tools\Brinell.Uat.VisualStudio\Brinell.Uat.VisualStudio.slnx -restore
```

The build publishes `Brinell.Uat.LanguageServer` into the VSIX and deploys the extension to the
experimental instance (`devenv /rootsuffix Exp`); pass `/p:DeployExtension=false` to only build it.
To install it for everyday use, open `tools\Brinell.Uat.VisualStudio\bin\Debug\net48\Brinell.Uat.VisualStudio.vsix`.

## Use

- Open a `.feature.md` or `.steps.md` and press **Ctrl+K, V** (or right-click, **Open Scenario
  Preview**; or **View > Other Windows > Scenario Preview**).
- The preview follows the active feature or step file, and renders again when you pause typing and
  after each build.
- The caret and the preview follow each other: move the caret and its step is highlighted; click a
  step and the caret goes there.
- The bar at the top says whether the file is **bound** or **parsed only**, and why. Until the
  suite is built, the preview parses only; build the solution and it binds.
- A file linked into several suites (AD-014) has a suite picker in the bar.
- The colours follow Visual Studio's theme, including high contrast.

The extension starts `dotnet Server\Brinell.Uat.LanguageServer.dll` from its own folder; its log is
the **Brinell UAT** pane of the Output window.

## Without Visual Studio

The server renders one file as a page, for a look or a bug report (from the Brinell root, after
`dotnet build tools\Brinell.Uat.LanguageServer`):

```powershell
tools\Brinell.Uat.LanguageServer\bin\Debug\net10.0\Brinell.Uat.LanguageServer.exe --preview samples\Todo\tests\Brinell.Samples.Todo.Uat\Features\TOD.01-see-my-todos.feature.md --solution samples\Todo --out TestResults\preview.html
```

`--host` writes the page the IDE's webview loads, with the preview posted into it.
