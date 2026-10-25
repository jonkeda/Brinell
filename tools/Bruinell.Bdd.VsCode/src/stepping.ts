// Stepping through a scenario, two ways. Step through: the language server runs the scenario in the
// suite's runner and says where it pauses (bruinell/step/paused); the moves go back to it. Debug: the
// engine writes where a debugged run is paused (bdd-paused.json); a move is a one-shot move in the
// breakpoints file, then the debugger goes on. Either way this shows the pause: the editor at the
// step, and the preview's bar.
import * as fs from "fs";
import * as path from "path";
import * as vscode from "vscode";
import { LanguageClient } from "vscode-languageclient/node";
import * as breakpoints from "./breakpoints";

/** What stepping asks of the preview. */
export interface SteppingView {
  paused(paused: breakpoints.Paused | undefined, source: string | undefined): void;
  readonly documentPath: string | undefined;
}

export type Move = "continue" | "over" | "into" | "out" | "stop";

/** Where a Step-through run paused, as the server says it. */
export type StepPaused = {
  uri: string;
  scenario: string;
  at?: string;
  line: number;
  text: string;
  depth: number;
  isCall: boolean;
  calls: { text: string; file?: string; line: number; depth: number }[];
  failed: boolean;
  message?: string;
  arguments?: Record<string, string>;
  app?: string;
  page?: string;
};

export class BddStepping implements vscode.Disposable {
  private readonly watcher: fs.FSWatcher | undefined;
  private debugged: breakpoints.Paused | undefined;
  private stepped: breakpoints.Paused | undefined;
  private stepping = false;
  private settle: NodeJS.Timeout | undefined;

  constructor(private readonly client: LanguageClient, private readonly view: SteppingView, private readonly moving: () => void = () => undefined) {
    const folder = path.dirname(breakpoints.pausedFile);
    fs.mkdirSync(folder, { recursive: true });
    this.watcher = fs.watch(folder, (_, name) => {
      if (name === path.basename(breakpoints.pausedFile)) {
        // The engine writes beside and moves into place: several events for one change.
        clearTimeout(this.settle);
        this.settle = setTimeout(() => void this.onPausedChanged(), 50);
      }
    });
    void this.onPausedChanged();
  }

  /** Whether a Step-through run is going, paused or not. */
  get isStepping(): boolean {
    return this.stepping;
  }

  /** A Step-through run started: it pauses before its first step. */
  started(): void {
    this.stepping = true;
  }

  /** The server says the Step-through run paused. */
  async onStepPaused(paused: StepPaused): Promise<void> {
    this.stepped = {
      process: 0,
      file: paused.at ? vscode.Uri.parse(paused.at).fsPath : "",
      line: paused.line,
      depth: paused.depth,
      isCall: paused.isCall,
      text: paused.text,
      calls: paused.calls.map((call) => `${call.text} at ${path.basename(call.file ?? "")}:${call.line}`),
      failed: paused.failed,
      message: paused.message,
      arguments: paused.arguments,
      app: paused.app,
      page: paused.page,
      stepping: true,
    };
    await this.show(this.stepped);
  }

  /** The server says the Step-through run ended. */
  async onStepEnded(): Promise<void> {
    this.stepping = false;
    this.stepped = undefined;
    await this.show(this.debugged);
  }

  /**
   * Debug from here (AD-022): the C# debugger attaches to the paused Step-through
   * run's runner, then Step over runs the paused step, which breaks in it. Why not, when it did not.
   */
  async debugFromHere(): Promise<string | undefined> {
    if (!this.stepped) {
      return "No Step-through run is paused.";
    }

    const debug = await this.client.sendRequest<{ processId?: number; reason?: string }>("bruinell/step/debug", {});
    if (!debug.processId) {
      return debug.reason ?? "The runner could not be debugged.";
    }

    const attached = await vscode.debug.startDebugging(vscode.workspace.workspaceFolders?.[0], {
      name: `Bruinell BDD runner ${debug.processId}`,
      type: "coreclr",
      request: "attach",
      processId: String(debug.processId),
    });
    if (!attached) {
      return `The debugger could not attach to the runner (process ${debug.processId}).`;
    }

    await this.move("over");
    return undefined;
  }

  /** Makes a move from the paused step; nothing when no run is paused. */
  async move(move: Move): Promise<void> {
    if (this.stepped || (this.stepping && move === "stop")) {
      // The bar goes while the run moves; the server says where it pauses next.
      this.moving();
      this.stepped = undefined;
      await this.show(undefined);
      const taken = await this.client.sendRequest<{ taken: boolean; reason?: string }>("bruinell/step/move", { move });
      if (!taken.taken) {
        void vscode.window.showWarningMessage(`The Step-through move did not go: ${taken.reason}`);
      }

      return;
    }

    const paused = this.debugged;
    if (!paused) {
      return;
    }

    if (move === "over" || move === "into" || move === "out") {
      breakpoints.setMove(move, paused.depth, paused.process);
    } else {
      breakpoints.clearMove();
    }

    await vscode.commands.executeCommand(move === "stop" ? "workbench.action.debug.stop" : "workbench.action.debug.continue");
  }

  dispose(): void {
    clearTimeout(this.settle);
    this.watcher?.close();
  }

  private async onPausedChanged(): Promise<void> {
    this.debugged = breakpoints.readPaused();
    if (!this.stepping) {
      await this.show(this.debugged);
    }
  }

  // The context keys for the step keys, the editor at the step, and the preview's bar.
  private async show(paused: breakpoints.Paused | undefined): Promise<void> {
    await vscode.commands.executeCommand("setContext", "bruinellBdd.paused", paused !== undefined);
    await vscode.commands.executeCommand("setContext", "bruinellBdd.stepPaused", paused !== undefined && paused === this.stepped);
    await vscode.commands.executeCommand("setContext", "bruinellBdd.pausedAtCall", paused?.isCall === true);
    await vscode.commands.executeCommand("setContext", "bruinellBdd.pausedInCall", (paused?.depth ?? 0) > 0);
    if (!paused) {
      this.view.paused(undefined, undefined);
      return;
    }

    const source = await this.sourceOf(paused.file);
    if (source) {
      const editor = await vscode.window.showTextDocument(vscode.Uri.file(source), { preview: false, preserveFocus: false });
      const position = new vscode.Position(Math.max(0, paused.line - 1), 0);
      editor.selection = new vscode.Selection(position, position);
      editor.revealRange(new vscode.Range(position, position), vscode.TextEditorRevealType.InCenter);
    }

    this.view.paused(paused, source);
  }

  // The run's file is the build output's copy: the source is the file at the same path under a
  // Features folder of the workspace, the preview's own first. A Step-through run's is the source.
  private async sourceOf(running: string): Promise<string | undefined> {
    const normalized = running.replace(/\\/g, "/");
    const at = normalized.toLowerCase().lastIndexOf("/features/");
    if (at < 0 || !/\/bin\//i.test(normalized)) {
      return fs.existsSync(running) ? running : undefined;
    }

    const relative = normalized.substring(at + "/features/".length);
    const mine = this.view.documentPath?.replace(/\\/g, "/");
    const root = mine ? mine.toLowerCase().lastIndexOf("/features/") : -1;
    if (mine && root >= 0) {
      const candidate = path.join(mine.substring(0, root + "/features/".length), relative);
      if (fs.existsSync(candidate)) {
        return candidate;
      }
    }

    const found = await vscode.workspace.findFiles(`**/Features/${relative}`, "**/{bin,obj}/**", 1);
    return found[0]?.fsPath ?? (fs.existsSync(running) ? running : undefined);
  }
}
