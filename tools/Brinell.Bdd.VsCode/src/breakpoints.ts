// The step breakpoints (R2): the per-user file Brinell.Bdd's BddBreakpoints reads while a debugger is
// attached, shared with the Visual Studio extension. Read on each call, so neither IDE overwrites
// the other's with a stale copy; written whole and then moved into place.
import * as fs from "fs";
import * as os from "os";
import * as path from "path";
import * as vscode from "vscode";

const file = path.join(process.env.LOCALAPPDATA ?? path.join(os.homedir(), ".local", "share"), "Brinell", "bdd-breakpoints.json");

type Store = { files?: Record<string, number[]>; [other: string]: unknown };

const changed = new vscode.EventEmitter<string>();

/** A file's breakpoints changed (its path). */
export const onDidChangeBreakpoints = changed.event;

function read(): Store {
  try {
    return JSON.parse(fs.readFileSync(file, "utf8")) as Store;
  } catch {
    // No file yet, or one another writer has half written: no breakpoints.
    return {};
  }
}

function key(store: Store, filePath: string): string | undefined {
  return Object.keys(store.files ?? {}).find((name) => name.toLowerCase() === filePath.toLowerCase());
}

/** The lines with a breakpoint in a file, ascending. */
export function linesOf(filePath: string): number[] {
  const store = read();
  const found = key(store, filePath);
  return found ? [...(store.files![found] ?? [])].filter((line) => Number.isInteger(line)).sort((a, b) => a - b) : [];
}

/** Sets or clears the breakpoint at a 1-based line; true when it is set now. */
export function toggle(filePath: string, line: number): boolean {
  const store = read();
  const lines = linesOf(filePath);
  const at = lines.indexOf(line);
  const set = at < 0;
  if (set) {
    lines.push(line);
  } else {
    lines.splice(at, 1);
  }

  write(store, filePath, lines);
  return set;
}

/** Keeps breakpoints on their steps: lines after `afterLine` move by `delta`, lines a deletion removed are cleared. */
export function shift(filePath: string, afterLine: number, delta: number): void {
  if (delta === 0) {
    return;
  }

  const lines = linesOf(filePath);
  if (!lines.some((line) => line > afterLine)) {
    return;
  }

  const moved = lines
    .filter((line) => line <= afterLine || line > afterLine - delta)
    .map((line) => (line > afterLine ? line + delta : line));
  write(read(), filePath, [...new Set(moved)]);
}

function write(store: Store, filePath: string, lines: number[]): void {
  store.files = store.files ?? {};
  const found = key(store, filePath);
  if (found) {
    delete store.files[found];
  }

  if (lines.length > 0) {
    store.files[filePath] = [...lines].sort((a, b) => a - b);
  }

  save(store);
  changed.fire(filePath);
}

// The engine reads the file at every step while debugging, so the move into place may meet it
// open: it is tried again, briefly.
function save(store: Store): void {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const temporary = `${file}.${process.pid}.tmp`;
  fs.writeFileSync(temporary, JSON.stringify(store, null, 2));
  for (let attempt = 1; ; attempt++) {
    try {
      fs.renameSync(temporary, file);
      return;
    } catch (error) {
      if (attempt === saveAttempts) {
        throw error;
      }

      Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, saveRetryPauseMs);
    }
  }
}

const saveAttempts = 10;
const saveRetryPauseMs = 20;

/** The file the engine writes while a debugged run is paused at a step, beside the breakpoints. */
export const pausedFile = path.join(path.dirname(file), "bdd-paused.json");

/** Where a debugged run is paused, as the engine wrote it. */
export type Paused = {
  process: number;
  file: string;
  line: number;
  depth: number;
  isCall: boolean;
  text: string;
  calls: string[];
  failed: boolean;
  message?: string;
  arguments?: Record<string, string>;
  app?: string;
  page?: string;
  /** A Step-through run's stop (the runner can take a screenshot), not a debugged run's. */
  stepping?: boolean;
};

/** Where a debugged run is paused; undefined when none is. */
export function readPaused(): Paused | undefined {
  try {
    return JSON.parse(fs.readFileSync(pausedFile, "utf8")) as Paused;
  } catch {
    // No run paused, or the engine is writing or removing the file just now.
    return undefined;
  }
}

/** A one-shot move from the paused step (over, into, out) for the test process that paused. */
export function setMove(mode: "over" | "into" | "out", depth: number, processId: number): void {
  const store = read();
  store.next = { mode, depth, process: processId };
  save(store);
}

/** Removes a move no run took: a Continue, or a run that ended. */
export function clearMove(): void {
  const store = read();
  if ("next" in store) {
    delete store.next;
    save(store);
  }
}
