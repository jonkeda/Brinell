// The Scenario Preview in a webview: the same host page and HTML as Visual Studio's, from the same
// server (brinell/previewHost, brinell/preview).
import * as vscode from "vscode";
import { LanguageClient } from "vscode-languageclient/node";
import * as breakpoints from "./breakpoints";
import { Paused } from "./breakpoints";

type Preview = { html: string; suites: string[]; suite?: string; bindState: string; reason?: string; resultsRoot?: string; resultsHost?: string };

/** What the preview asks of the rest of the extension. */
export interface PreviewHost {
  /** Runs as a test (dotnet test), under the debugger with debug. */
  run(uri: vscode.Uri, line: number | undefined, debug: boolean): void;
  /** Runs on the suite's runner (AD-022): the preview's Run. */
  runOnRunner(uri: vscode.Uri, line: number | undefined): void;
  /** Steps through the scenario at a heading's line, in the suite's runner. */
  step(uri: vscode.Uri, line: number): void;
  /** Records a video script, or the scenario at a heading's line, on video. */
  record(uri: vscode.Uri, line: number | undefined, suite: string | undefined): void;
  cancel(): void;
  /** A move from the paused bar: continue, over, into, out or stop. */
  move(move: string): void;
}

const previewable = /\.(feature|steps|video)\.md$/i;

export class ScenarioPreview implements vscode.Disposable {
  private panel: vscode.WebviewPanel | undefined;
  private document: vscode.TextDocument | undefined;
  private suite = new Map<string, string>();
  private typing: NodeJS.Timeout | undefined;
  private readonly subscriptions: vscode.Disposable[] = [];

  constructor(private readonly client: LanguageClient, private readonly host: PreviewHost) {
    this.subscriptions.push(
      vscode.window.onDidChangeActiveTextEditor((editor) => editor && this.follow(editor.document)),
      vscode.workspace.onDidChangeTextDocument((change) => {
        if (change.document === this.document) {
          clearTimeout(this.typing);
          this.typing = setTimeout(() => void this.refresh(), 300);
        }
      }),
      vscode.window.onDidChangeTextEditorSelection((change) => {
        if (change.textEditor.document === this.document) {
          void this.panel?.webview.postMessage({ type: "scrollTo", line: change.selections[0].active.line + 1 });
        }
      }),
      breakpoints.onDidChangeBreakpoints((path) => {
        if (this.document && path.toLowerCase() === this.document.uri.fsPath.toLowerCase()) {
          void this.panel?.webview.postMessage({ type: "breaks", lines: breakpoints.linesOf(path) });
        }
      }),
      client.onNotification("brinell/resultsChanged", (message: { uri: string }) => {
        if (this.document && vscode.Uri.parse(message.uri).fsPath.toLowerCase() === this.document.uri.fsPath.toLowerCase()) {
          void this.refresh();
        }
      }),
    );
  }

  /** Opens the preview beside the editor for a feature or step file. */
  async open(document: vscode.TextDocument): Promise<void> {
    if (!this.panel) {
      this.panel = vscode.window.createWebviewPanel("brinellBdd.preview", "Scenario Preview", { viewColumn: vscode.ViewColumn.Beside, preserveFocus: true }, {
        enableScripts: true,
        retainContextWhenHidden: true,
        localResourceRoots: [],
      });
      this.panel.onDidDispose(() => (this.panel = undefined));
      this.panel.webview.onDidReceiveMessage((message) => this.onMessage(message));
      const host = await this.client.sendRequest<{ html: string }>("brinell/previewHost", {});
      this.panel.webview.html = host.html;
    }

    this.document = document;
    await this.refresh();
  }

  /** Renders the preview again: after a run, or when the suite was built. */
  async refresh(): Promise<void> {
    if (!this.panel || !this.document) {
      return;
    }

    const uri = this.document.uri;
    const recordingsFolder = vscode.workspace.getConfiguration("brinellBdd").get<string>("recordingsFolder") || undefined;
    const preview = await this.client.sendRequest<Preview>("brinell/preview", { uri: uri.toString(), suite: this.suite.get(uri.fsPath), recordingsFolder });
    let html = preview.html;

    // Screenshots: the server's virtual host is the results folder, which a VS Code webview reaches through its own URIs.
    if (preview.resultsRoot && preview.resultsHost) {
      const root = vscode.Uri.file(preview.resultsRoot);
      this.panel.webview.options = { ...this.panel.webview.options, localResourceRoots: [root] };
      html = html.split(`https://${preview.resultsHost}/`).join(`${this.panel.webview.asWebviewUri(root).toString()}/`);
    }

    this.panel.title = `Preview ${uri.path.split("/").pop()}`;
    await this.panel.webview.postMessage({ ...preview, html, type: "update" });
    await this.panel.webview.postMessage({ type: "breaks", lines: breakpoints.linesOf(uri.fsPath) });
    await this.panel.webview.postMessage(theme());
  }

  /** The file the preview shows. */
  get documentPath(): string | undefined {
    return this.document?.uri.fsPath;
  }

  /** Tells the page where a debugged run is paused, or that it went on (nothing). */
  paused(paused: Paused | undefined, source: string | undefined): void {
    void this.panel?.webview.postMessage(paused
      ? {
          type: "paused",
          text: paused.text,
          line: paused.line,
          isCall: paused.isCall,
          depth: paused.depth,
          calls: paused.calls,
          failed: paused.failed,
          message: paused.message,
          arguments: paused.arguments,
          app: paused.app,
          page: paused.page,
          stepping: paused.stepping === true,
          here: source !== undefined && source.toLowerCase() === this.document?.uri.fsPath.toLowerCase(),
        }
      : { type: "paused" });
  }

  /** Tells the page a run started (a line, or the whole file) or ended (nothing), and whether it is paused. */
  running(line: number | undefined, file: boolean, paused = false): void {
    void this.panel?.webview.postMessage(line !== undefined || file ? { type: "running", line, file, paused } : { type: "running" });
  }

  dispose(): void {
    this.subscriptions.forEach((subscription) => subscription.dispose());
    this.panel?.dispose();
  }

  private follow(document: vscode.TextDocument): void {
    if (this.panel && previewable.test(document.fileName) && document !== this.document) {
      this.document = document;
      void this.refresh();
    }
  }

  // A Step-through run's app as it is now, taken by the runner while paused, opened beside.
  private async screenshot(): Promise<void> {
    const shot = await this.client.sendRequest<{ path?: string; reason?: string }>("brinell/step/screenshot", {});
    if (shot.path) {
      await vscode.commands.executeCommand("vscode.open", vscode.Uri.file(shot.path), { viewColumn: vscode.ViewColumn.Beside, preview: true });
    } else {
      void vscode.window.showWarningMessage(`No screenshot: ${shot.reason}`);
    }
  }

  private onMessage(message: { type: string; line?: number; suite?: string; path?: string; debug?: boolean; test?: boolean; scope?: string; move?: string }): void {
    if (message.type === "debug" && message.move) {
      this.host.move(message.move);
      return;
    }

    if (message.type === "screenshot") {
      void this.screenshot();
      return;
    }

    if (message.type === "debugHere") {
      void vscode.commands.executeCommand("brinellBdd.debugFromHere");
      return;
    }

    const document = this.document;
    if (!document) {
      return;
    }

    switch (message.type) {
      case "reveal": {
        const editor = vscode.window.visibleTextEditors.find((candidate) => candidate.document === document);
        if (editor && message.line) {
          const position = new vscode.Position(message.line - 1, 0);
          editor.selection = new vscode.Selection(position, position);
          editor.revealRange(new vscode.Range(position, position), vscode.TextEditorRevealType.InCenter);
        }

        break;
      }
      case "pickSuite":
        if (message.suite) {
          this.suite.set(document.uri.fsPath, message.suite);
          void this.refresh();
        }

        break;
      case "openScreenshot":
        if (message.path) {
          void vscode.commands.executeCommand("vscode.open", vscode.Uri.file(message.path));
        }

        break;
      case "run":
        if (message.test === true || message.debug === true) {
          this.host.run(document.uri, message.line, message.debug === true);
        } else {
          this.host.runOnRunner(document.uri, message.line);
        }

        break;
      case "step":
        if (message.line) {
          this.host.step(document.uri, message.line);
        }

        break;
      case "runFile":
        if (message.test === true || message.debug === true) {
          this.host.run(document.uri, undefined, message.debug === true);
        } else {
          this.host.runOnRunner(document.uri, undefined);
        }

        break;
      case "record":
        this.host.record(document.uri, message.line, this.suite.get(document.uri.fsPath));
        break;
      case "watch":
        if (message.path) {
          void vscode.env.openExternal(vscode.Uri.file(message.path));
        }

        break;
      case "cancel":
        this.host.cancel();
        break;
      case "toggleBreak":
        if (message.line) {
          breakpoints.toggle(document.uri.fsPath, message.line);
        }

        break;
      case "deleteResults":
        if (message.scope === "scenario" || message.scope === "file") {
          void deleteResults(this.client, { scope: message.scope, uri: document.uri.toString(), line: message.line, suite: this.suite.get(document.uri.fsPath) }).then(() => this.refresh());
        }

        break;
    }
  }
}

type DeleteResults = { done: boolean; reason?: string; what?: string; suite?: string; runsChanged: number; runsDeleted: number; filesDeleted: number; skipped: { run: string; reason: string }[] };

/**
 * Old results, for good (brinell/deleteResults): the server counts first, the user confirms with
 * the counts, then it deletes. Also the BDD Explorer's and a command's way to it.
 */
export async function deleteResults(client: LanguageClient, target: { scope: "scenario" | "file" | "suite"; uri?: string; line?: number; suite?: string }): Promise<void> {
  const count = await client.sendRequest<DeleteResults>("brinell/deleteResults", { ...target, dryRun: true });
  if (!count.done) {
    void vscode.window.showInformationMessage(`No results deleted: ${count.reason ?? "nothing to delete"}.`);
    return;
  }

  const runs = count.runsChanged + count.runsDeleted;
  if (runs === 0) {
    void vscode.window.showInformationMessage(`${count.what} has no results to delete in ${count.suite}.`);
    return;
  }

  const answer = await vscode.window.showWarningMessage(
    `Delete the results of ${count.what} in ${runs} run(s) of ${count.suite}: ${count.filesDeleted} file(s)?`,
    { modal: true, detail: "They are deleted for good." },
    "Delete",
  );
  if (answer !== "Delete") {
    return;
  }

  const done = await client.sendRequest<DeleteResults>("brinell/deleteResults", { ...target, dryRun: false });
  const skipped = done.skipped.length ? ` ${done.skipped.length} run(s) left alone: ${done.skipped.map((run) => `${run.run} (${run.reason})`).join("; ")}.` : "";
  void vscode.window.showInformationMessage(`Deleted the results of ${done.what}: ${done.filesDeleted} file(s); ${done.runsDeleted} run(s) removed, ${done.runsChanged} kept with other results.${skipped}`);
}

// VS Code's own theme colours, as the preview's variables.
export function theme(): { type: string; kind: string; vars: Record<string, string> } {
  const kind = vscode.window.activeColorTheme.kind;
  return {
    type: "theme",
    kind: kind === vscode.ColorThemeKind.HighContrast || kind === vscode.ColorThemeKind.HighContrastLight ? "high-contrast" : kind === vscode.ColorThemeKind.Dark ? "dark" : "light",
    vars: {
      "--uat-bg": "var(--vscode-editor-background)",
      "--uat-fg": "var(--vscode-editor-foreground)",
      "--uat-muted": "var(--vscode-descriptionForeground)",
      "--uat-border": "var(--vscode-panel-border)",
      "--uat-surface": "var(--vscode-editorWidget-background)",
      "--uat-accent": "var(--vscode-textLink-foreground)",
      "--uat-font": "var(--vscode-font-family)",
      "--uat-mono": "var(--vscode-editor-font-family)",
    },
  };
}
