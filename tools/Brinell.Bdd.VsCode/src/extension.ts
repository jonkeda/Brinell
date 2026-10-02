// The Brinell BDD extension for VS Code: the same language server as Visual Studio's
// (brinell lsp, tools/Brinell.Cli) for diagnostics, completion and navigation, the same Scenario Preview,
// Test Explorer on the scenarios' addresses, step breakpoints shared with Visual Studio, and the
// Scraper view, which records an app's pages into a UI test project.
import * as fs from "fs";
import * as path from "path";
import * as vscode from "vscode";
import { LanguageClient, LanguageClientOptions, ServerOptions } from "vscode-languageclient/node";
import * as breakpoints from "./breakpoints";
import { deleteResults, ScenarioPreview } from "./preview";
import { ScraperView } from "./scraper";
import { BddStepping, Move } from "./stepping";
import { ScenarioTests } from "./tests";

const featureOrSteps = /\.(feature|steps)\.md$/i;
// A video script previews too, as the scenes it will record.
const previewable = /\.(feature|steps|video)\.md$/i;
let client: LanguageClient | undefined;

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const server = serverPath(context);
  if (!server) {
    void vscode.window.showErrorMessage("The Brinell BDD language server is not built. Build tools/Brinell.Cli, or set brinellBdd.serverPath.");
    return;
  }

  const serverOptions: ServerOptions = { command: "dotnet", args: [server, "lsp"] };
  const clientOptions: LanguageClientOptions = {
    documentSelector: [
      { scheme: "file", language: "markdown", pattern: "**/*.feature.md" },
      { scheme: "file", language: "markdown", pattern: "**/*.steps.md" },
      { scheme: "file", language: "markdown", pattern: "**/*.video.md" },
      { scheme: "file", language: "markdown", pattern: "**/uat.config.md" },
      { scheme: "file", language: "markdown", pattern: "**/bdd.config.md" },
    ],
    initializationOptions: { solutionDir: vscode.workspace.workspaceFolders?.[0]?.uri.fsPath },
    outputChannelName: "Brinell BDD Language Server",
  };
  client = new LanguageClient("brinellBdd", "Brinell BDD", serverOptions, clientOptions);
  await client.start();

  const tests = new ScenarioTests(client, (line, file) => preview.running(line, file));
  const preview = new ScenarioPreview(client, {
    run: (uri, line, debug) => void tests.runAt(uri, line, debug),
    record: (uri, line, suite) => void tests.record(uri, line, suite).then(() => preview.refresh()),
    cancel: () => tests.cancel(),
    move: (move) => void stepping.move(move as Move),
  });
  const stepping = new BddStepping(preview);
  const scraper = new ScraperView(client);
  const gutter = vscode.window.createTextEditorDecorationType({
    gutterIconPath: context.asAbsolutePath(path.join("media", "breakpoint.svg")),
    gutterIconSize: "70%",
  });
  const showBreakpoints = (editor: vscode.TextEditor | undefined) => {
    if (editor && featureOrSteps.test(editor.document.fileName)) {
      editor.setDecorations(gutter, breakpoints.linesOf(editor.document.uri.fsPath).map((line) => new vscode.Range(line - 1, 0, line - 1, 0)));
    }
  };

  context.subscriptions.push(
    client,
    tests,
    preview,
    stepping,
    scraper,
    gutter,
    vscode.commands.registerCommand("brinellBdd.openScraper", () => scraper.open()),
    vscode.commands.registerCommand("brinellBdd.stepOver", () => stepping.move("over")),
    vscode.commands.registerCommand("brinellBdd.stepInto", () => stepping.move("into")),
    vscode.commands.registerCommand("brinellBdd.stepOut", () => stepping.move("out")),
    vscode.commands.registerCommand("brinellBdd.openPreview", async () => {
      const document = vscode.window.activeTextEditor?.document;
      if (document && previewable.test(document.fileName)) {
        await preview.open(document);
      }
    }),
    vscode.commands.registerCommand("brinellBdd.toggleStepBreakpoint", () => {
      const editor = vscode.window.activeTextEditor;
      if (editor && featureOrSteps.test(editor.document.fileName)) {
        breakpoints.toggle(editor.document.uri.fsPath, editor.selection.active.line + 1);
      }
    }),
    vscode.commands.registerCommand("brinellBdd.refreshTests", () => tests.discover()),
    vscode.commands.registerCommand("brinellBdd.deleteSuiteResults", async () => {
      // The suites as the BDD Explorer lists them; the picked one loses every scenario result.
      const suites = (await client!.sendRequest<{ suites: string[] }>("brinell/explorer", { view: "results" })).suites;
      const suite = suites.length === 1 ? suites[0] : await vscode.window.showQuickPick(suites, { placeHolder: "The suite whose results to delete" });
      if (suite) {
        await deleteResults(client!, { scope: "suite", suite });
        await preview.refresh();
      }
    }),
    breakpoints.onDidChangeBreakpoints(() => vscode.window.visibleTextEditors.forEach(showBreakpoints)),
    vscode.window.onDidChangeActiveTextEditor(showBreakpoints),
    vscode.workspace.onDidChangeTextDocument((change) => {
      if (!featureOrSteps.test(change.document.fileName)) {
        return;
      }

      // Last change first: each one's line is then still where the file had it before.
      for (const edit of [...change.contentChanges].sort((a, b) => b.range.start.line - a.range.start.line)) {
        const delta = edit.text.split("\n").length - 1 - (edit.range.end.line - edit.range.start.line);
        breakpoints.shift(change.document.uri.fsPath, edit.range.start.line + 1, delta);
      }
    }),
    vscode.workspace.onDidSaveTextDocument((document) => {
      if (featureOrSteps.test(document.fileName)) {
        void tests.discover();
      }
    }),
  );

  vscode.window.visibleTextEditors.forEach(showBreakpoints);
}

export function deactivate(): Thenable<void> | undefined {
  return client?.stop();
}

// The server: the setting, else the one packed in the extension, else the repository's own build.
function serverPath(context: vscode.ExtensionContext): string | undefined {
  const configured = vscode.workspace.getConfiguration("brinellBdd").get<string>("serverPath");
  if (configured && fs.existsSync(configured)) {
    return configured;
  }

  const packed = context.asAbsolutePath(path.join("server", "brinell.dll"));
  if (fs.existsSync(packed)) {
    return packed;
  }

  for (const folder of vscode.workspace.workspaceFolders ?? []) {
    for (let at = folder.uri.fsPath; ; at = path.dirname(at)) {
      const built = path.join(at, "tools", "Brinell.Cli", "bin", "Debug", "net10.0", "brinell.dll");
      if (fs.existsSync(built)) {
        return built;
      }

      if (path.dirname(at) === at) {
        break;
      }
    }
  }

  return undefined;
}
