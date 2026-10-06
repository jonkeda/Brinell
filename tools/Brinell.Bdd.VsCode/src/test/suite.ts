// The tests run.ts starts inside VS Code: Step through Todo's TOD.01.1 as a user does, through the
// extension's commands (the keys F10, F11 and F5 are bound to them), with a breakpoint at line 24.
import * as assert from "assert";
import * as fs from "fs";
import * as path from "path";
import * as vscode from "vscode";
import type { BrinellBddApi, StepEvent } from "../extension";
import type { StepPaused } from "../stepping";

const workspace = process.env.BRINELL_TODO!;
const feature = path.join(workspace, "tests", "Brinell.Samples.Todo.Uat", "Features", "TOD.01-see-my-todos.feature.md");
const heading = 15;

// Named waits: the first stop comes after a build, the runner's start and the app's.
const firstStopMs = 5 * 60_000;
const nextStopMs = 60_000;
const breakpointsFile = path.join(process.env.LOCALAPPDATA!, "Brinell", "bdd-breakpoints.json");

export async function run(): Promise<void> {
  const extension = vscode.extensions.getExtension<BrinellBddApi>("brinell.brinell-bdd");
  assert.ok(extension, "The extension is not loaded.");
  const api = await extension.activate();
  assert.ok(api, "The extension did not start: is tools/Brinell.Cli built?");

  const events: StepEvent[] = [];
  let arrived: (() => void) | undefined;
  api.onStep((event) => {
    events.push(event);
    arrived?.();
  });
  const next = async (withinMs: number): Promise<StepEvent> => {
    const deadline = Date.now() + withinMs;
    while (events.length === 0) {
      const left = deadline - Date.now();
      assert.ok(left > 0, `No step event within ${withinMs} ms.`);
      await new Promise<void>((resolve) => {
        arrived = resolve;
        setTimeout(resolve, left);
      });
    }

    return events.shift()!;
  };
  const pausedAt = async (line: number, withinMs = nextStopMs): Promise<StepPaused> => {
    const event = await next(withinMs);
    assert.strictEqual(event.kind, "paused", `Expected a stop at line ${line}, got ${JSON.stringify(event)}.`);
    assert.strictEqual(event.kind === "paused" && event.paused.line, line);
    const editor = vscode.window.activeTextEditor;
    assert.strictEqual(editor?.document.uri.fsPath.toLowerCase(), feature.toLowerCase(), "The feature file is not in front.");
    assert.strictEqual(editor.selection.active.line + 1, line, "The caret is not at the stop.");
    const paused = (event as Extract<StepEvent, { kind: "paused" }>).paused;
    console.log(`paused at line ${line}` + (paused.app ? ` app ${paused.app}` : "") + (paused.page ? ` page ${paused.page}` : "") + (paused.arguments ? ` ${JSON.stringify(paused.arguments)}` : ""));
    return paused;
  };

  fs.mkdirSync(path.dirname(breakpointsFile), { recursive: true });
  fs.writeFileSync(breakpointsFile, JSON.stringify({ files: { [feature]: [24] } }));
  await vscode.window.showTextDocument(vscode.Uri.file(feature));

  const refused = await vscode.commands.executeCommand<string | undefined>("brinellBdd.stepThrough", vscode.Uri.file(feature), heading);
  assert.strictEqual(refused, undefined, `The Step-through run did not start: ${refused}`);
  await pausedAt(18, firstStopMs);

  await vscode.commands.executeCommand("brinellBdd.stepOver");
  await pausedAt(19);

  // F11 at a step that is no call: the next step, as Step into is in a debugger.
  await vscode.commands.executeCommand("brinellBdd.stepInto");
  await pausedAt(23);

  // F5: on to the breakpoint.
  await vscode.commands.executeCommand("brinellBdd.continue");
  // The stop says what the step works with (D5): the page the step before left, the arguments as read.
  const atBreakpoint = await pausedAt(24);
  assert.strictEqual(atBreakpoint.page, "Todo List");
  assert.deepStrictEqual(atBreakpoint.arguments, { control: "Sync" });

  for (const line of [25, 26, 27]) {
    await vscode.commands.executeCommand("brinellBdd.stepOver");
    await pausedAt(line);
  }

  await vscode.commands.executeCommand("brinellBdd.stepOver");
  const ended = await next(nextStopMs);
  assert.ok(ended.kind === "ended" && ended.status === "passed", `The run did not pass: ${JSON.stringify(ended)}.`);
  console.log("ended: passed");
}
