// Stepping through a scenario while a debugged run is paused at a step: the engine writes where it
// is paused (bdd-paused.json); this shows it (the editor at the step, the preview's bar) and makes
// the moves: a one-shot move in the breakpoints file, then the debugger goes on.
import * as fs from "fs";
import * as path from "path";
import * as vscode from "vscode";
import * as breakpoints from "./breakpoints";

/** What stepping asks of the preview. */
export interface SteppingView {
  paused(paused: breakpoints.Paused | undefined, source: string | undefined): void;
  readonly documentPath: string | undefined;
}

export type Move = "continue" | "over" | "into" | "out" | "stop";

export class BddStepping implements vscode.Disposable {
  private readonly watcher: fs.FSWatcher | undefined;
  private current: breakpoints.Paused | undefined;
  private settle: NodeJS.Timeout | undefined;

  constructor(private readonly view: SteppingView) {
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

  /** Makes a move from the paused step; nothing when no run is paused. */
  async move(move: Move): Promise<void> {
    const paused = this.current;
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
    const paused = breakpoints.readPaused();
    this.current = paused;
    await vscode.commands.executeCommand("setContext", "brinellBdd.paused", paused !== undefined);
    await vscode.commands.executeCommand("setContext", "brinellBdd.pausedAtCall", paused?.isCall === true);
    await vscode.commands.executeCommand("setContext", "brinellBdd.pausedInCall", (paused?.depth ?? 0) > 0);
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
  // Features folder of the workspace, the preview's own first.
  private async sourceOf(running: string): Promise<string | undefined> {
    const normalized = running.replace(/\\/g, "/");
    const at = normalized.toLowerCase().lastIndexOf("/features/");
    if (at < 0) {
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
