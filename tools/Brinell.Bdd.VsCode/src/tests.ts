// Test Explorer on the scenarios' addresses (R1): each suite, its feature files and their scenarios,
// run with `dotnet test --filter BddScenario=<address>` after a build, and debugged by attaching the
// C# debugger to the testhost. The UI test rules hold (R4): one run at a time, and Cancel stops the
// test process and the app it started.
import { spawn, ChildProcess } from "child_process";
import * as fs from "fs";
import * as os from "os";
import * as path from "path";
import * as vscode from "vscode";
import { LanguageClient } from "vscode-languageclient/node";

type ScenarioTest = { name: string; line: number; filter: string };
type FileScenarios = { uri: string; relative: string; scenarios: ScenarioTest[] };
type SuiteScenarios = { suite: string; project?: string; runnable: boolean; reason?: string; files: FileScenarios[] };
type ScenarioResult = { scenario: string; status: string; durationMs: number; message?: string; line?: number };

type Data = { kind: "suite"; project?: string; runnable: boolean; reason?: string } | { kind: "file" } | { kind: "scenario"; filter: string; name: string };
type RecordTarget = { canRecord: boolean; reason?: string; project?: string; filter?: string; environment: Record<string, string>; video?: string; folder?: string };
type Recording = { path: string; name: string; written: string; captions?: string };

export class ScenarioTests implements vscode.Disposable {
  readonly controller = vscode.tests.createTestController("brinellBdd", "Brinell BDD");
  private readonly data = new WeakMap<vscode.TestItem, Data>();
  private readonly output = vscode.window.createOutputChannel("Brinell BDD");
  private process: ChildProcess | undefined;

  constructor(private readonly client: LanguageClient, private readonly running: (line: number | undefined, file: boolean) => void) {
    this.controller.resolveHandler = async () => this.discover();
    this.controller.refreshHandler = async () => this.discover();
    this.controller.createRunProfile("Run", vscode.TestRunProfileKind.Run, (request, token) => this.run(request, token, false), true);
    this.controller.createRunProfile("Debug", vscode.TestRunProfileKind.Debug, (request, token) => this.run(request, token, true), true);
  }

  /** Reads every suite's scenarios from the server. */
  async discover(): Promise<void> {
    const suites = await this.client.sendRequest<SuiteScenarios[]>("brinell/scenarios", {});
    this.controller.items.replace(suites.map((suite) => {
      const item = this.controller.createTestItem(suite.suite, suite.suite);
      item.description = suite.runnable ? undefined : suite.reason;
      this.data.set(item, { kind: "suite", project: suite.project, runnable: suite.runnable, reason: suite.reason });
      item.children.replace(suite.files.map((file) => {
        const uri = vscode.Uri.parse(file.uri);
        const fileItem = this.controller.createTestItem(`${suite.suite}|${file.uri}`, file.relative, uri);
        this.data.set(fileItem, { kind: "file" });
        fileItem.children.replace(file.scenarios.map((scenario) => {
          const test = this.controller.createTestItem(`${suite.suite}|${file.uri}|${scenario.line}`, scenario.name, uri);
          test.range = new vscode.Range(scenario.line - 1, 0, scenario.line - 1, 0);
          this.data.set(test, { kind: "scenario", filter: scenario.filter, name: scenario.name });
          return test;
        }));
        return fileItem;
      }));
      return item;
    }));
  }

  /** Runs the scenario at a heading's line, or a whole file, from the preview. */
  async runAt(uri: vscode.Uri, line: number | undefined, debug: boolean): Promise<void> {
    if (this.controller.items.size === 0) {
      await this.discover();
    }

    const matches: vscode.TestItem[] = [];
    this.controller.items.forEach((suite) => suite.children.forEach((file) => {
      if (file.uri?.fsPath.toLowerCase() !== uri.fsPath.toLowerCase()) {
        return;
      }

      if (line === undefined) {
        matches.push(file);
      } else {
        file.children.forEach((scenario) => scenario.range?.start.line === line - 1 && matches.push(scenario));
      }
    }));

    if (matches.length === 0) {
      void vscode.window.showWarningMessage("No suite runs this scenario by its address. Build the suite, and give its test class [UatScenarioTheory].");
      return;
    }

    // A file linked into several suites runs in the first, its own.
    const request = new vscode.TestRunRequest([matches[0]]);
    const source = new vscode.CancellationTokenSource();
    await this.run(request, source.token, debug);
  }

  /**
   * Records a video script, or the scenario at a heading's line (the narrator): the gate run with
   * BRINELL_RECORD, then the video it made, to watch. The settings give the Recordings folder, the
   * voice and the beat.
   */
  async record(uri: vscode.Uri, line: number | undefined, suite: string | undefined): Promise<void> {
    if (this.process) {
      void vscode.window.showWarningMessage("A BDD run is going already. Wait for it, or cancel it, before recording.");
      return;
    }

    const settings = vscode.workspace.getConfiguration("brinellBdd");
    const recordingsFolder = settings.get<string>("recordingsFolder") || undefined;
    const target = await this.client.sendRequest<RecordTarget>("brinell/recordTarget", {
      uri: uri.toString(), line, suite, recordingsFolder, voice: settings.get<string>("voice") || undefined, beat: settings.get<number>("beat") || undefined,
    });
    if (!target.canRecord || !target.project || !target.filter) {
      void vscode.window.showWarningMessage(`This cannot be recorded: ${target.reason ?? "no suite runs it."}`);
      return;
    }

    this.output.show(true);
    this.running(line, line === undefined);
    try {
      this.output.appendLine(`Recording into ${target.folder}...`);
      if (await this.exec(undefined, "dotnet", ["build", target.project, "-v:minimal", "-nologo"], false) !== 0) {
        void vscode.window.showErrorMessage("The build failed, so the recording did not start.");
        return;
      }

      const code = await this.exec(undefined, "dotnet", ["test", target.project, "--no-build", "--filter", target.filter, "--logger", "console;verbosity=normal"], false, target.environment);
      const videos = await this.client.sendRequest<Recording[]>("brinell/recordings", { uri: uri.toString(), suite, recordingsFolder });
      const video = target.video ? videos.find((candidate) => candidate.name === target.video) : videos[0];
      if (!video) {
        void vscode.window.showWarningMessage("The run made no video: see the Brinell BDD output.");
        return;
      }

      const failed = code !== 0 ? " A scenario failed: the video shows where." : "";
      const choice = await vscode.window.showInformationMessage(`Recorded ${video.name}.${failed}`, "Watch", "Open folder");
      if (choice === "Watch") {
        void vscode.env.openExternal(vscode.Uri.file(video.path));
      } else if (choice === "Open folder") {
        void vscode.commands.executeCommand("revealFileInOS", vscode.Uri.file(video.path));
      }
    } finally {
      this.running(undefined, false);
    }
  }

  /** Stops the run: the test process and everything it started, the app included. */
  cancel(): void {
    if (this.process?.pid && this.process.exitCode === null) {
      this.output.appendLine("Cancelling the BDD run...");
      spawn("taskkill", ["/PID", String(this.process.pid), "/T", "/F"], { windowsHide: true });
    }
  }

  dispose(): void {
    this.controller.dispose();
    this.output.dispose();
  }

  private async run(request: vscode.TestRunRequest, token: vscode.CancellationToken, debug: boolean): Promise<void> {
    if (this.process) {
      void vscode.window.showWarningMessage("A BDD run is going already. Wait for it, or cancel it, before starting another.");
      return;
    }

    const run = this.controller.createTestRun(request);
    token.onCancellationRequested(() => this.cancel());
    try {
      // The scenarios asked for, by suite.
      const bySuite = new Map<vscode.TestItem, vscode.TestItem[]>();
      const add = (item: vscode.TestItem) => {
        const data = this.data.get(item);
        if (data?.kind === "scenario") {
          const suite = item.parent!.parent!;
          bySuite.set(suite, [...(bySuite.get(suite) ?? []), item]);
        } else {
          item.children.forEach(add);
        }
      };
      (request.include ?? this.all()).forEach(add);

      for (const [suite, scenarios] of bySuite) {
        const data = this.data.get(suite);
        if (data?.kind !== "suite" || !data.runnable || !data.project) {
          scenarios.forEach((scenario) => run.skipped(scenario));
          run.appendOutput(`${suite.label}: ${data?.kind === "suite" ? data.reason ?? "cannot run" : "cannot run"}\r\n`);
          continue;
        }

        scenarios.forEach((scenario) => run.started(scenario));
        this.running(scenarios.length === 1 ? scenarios[0].range!.start.line + 1 : undefined, scenarios.length > 1);

        // Build first, so a stale app never runs; then the scenarios by their addresses.
        if (await this.exec(run, "dotnet", ["build", data.project, "-v:minimal", "-nologo"], false) !== 0) {
          scenarios.forEach((scenario) => run.errored(scenario, new vscode.TestMessage("The build failed, so the BDD run did not start.")));
          continue;
        }

        const filter = scenarios.map((scenario) => (this.data.get(scenario) as { filter: string }).filter).join("|");
        await this.exec(run, "dotnet", ["test", data.project, "--no-build", "--filter", filter, "--logger", "console;verbosity=normal"], debug);
        await this.report(run, scenarios);
      }
    } finally {
      this.running(undefined, false);
      run.end();
    }
  }

  private all(): vscode.TestItem[] {
    const items: vscode.TestItem[] = [];
    this.controller.items.forEach((item) => items.push(item));
    return items;
  }

  // Each scenario's result, from the report its run wrote.
  private async report(run: vscode.TestRun, scenarios: vscode.TestItem[]): Promise<void> {
    const byFile = new Map<string, ScenarioResult[]>();
    for (const scenario of scenarios) {
      const uri = scenario.uri!;
      if (!byFile.has(uri.toString())) {
        byFile.set(uri.toString(), await this.client.sendRequest<ScenarioResult[]>("brinell/lastRuns", { uri: uri.toString() }));
      }

      const name = (this.data.get(scenario) as { name: string }).name;

      // An outline's rows are its name with [n]; the scenario failed when a row did.
      const results = byFile.get(uri.toString())!.filter((result) => result.scenario === name || result.scenario.startsWith(`${name} [`));
      const failed = results.find((result) => result.status === "failed");
      const duration = results.reduce((total, result) => total + result.durationMs, 0);
      if (results.length === 0) {
        run.skipped(scenario);
      } else if (failed) {
        const message = new vscode.TestMessage(failed.message ?? `${failed.scenario} failed.`);
        if (failed.line) {
          message.location = new vscode.Location(scenario.uri!, new vscode.Position(failed.line - 1, 0));
        }

        run.failed(scenario, message, duration);
      } else if (results.every((result) => result.status === "passed")) {
        run.passed(scenario, duration);
      } else {
        run.skipped(scenario);
      }
    }
  }

  // While the run goes: each app the testhost announces (Brinell.Core's DebuggedApps) is attached
  // too, so a breakpoint in the app's own code stops.
  private attachApps(folder: vscode.WorkspaceFolder | undefined, testhost: number, child: ChildProcess, run: vscode.TestRun): void {
    const announced = path.join(os.tmpdir(), "Brinell", "debugged-apps");
    const attached = new Set<number>();
    const timer = setInterval(() => {
      if (child.exitCode !== null) {
        clearInterval(timer);
        return;
      }

      let files: string[] = [];
      try {
        files = fs.readdirSync(announced).filter((name) => name.startsWith(`${testhost}-`) && name.endsWith(".json"));
      } catch {
        // No app announced yet.
      }

      for (const name of files) {
        const app = Number(name.slice(0, -".json".length).split("-").pop());
        if (!Number.isInteger(app) || attached.has(app)) {
          continue;
        }

        attached.add(app);
        void vscode.debug.startDebugging(folder, { name: `Brinell BDD app ${app}`, type: "coreclr", request: "attach", processId: String(app) }).then((started) =>
          run.appendOutput(started ? `The debugger is attached to the app too (process ${app}).\r\n` : `The debugger could not attach to the app (process ${app}); the run goes on.\r\n`));
        fs.rmSync(path.join(announced, name), { force: true });
      }
    }, 500);
  }

  // Runs a command with its output in the run and the Output channel; with debug, the testhost waits
  // for a debugger (VSTEST_HOST_DEBUG), and the C# debugger attaches to the process id it prints.
  private exec(run: vscode.TestRun | undefined, command: string, args: string[], debug: boolean, variables: Record<string, string> = {}): Promise<number> {
    const folder = vscode.workspace.workspaceFolders?.[0];
    this.output.appendLine(`${command} ${args.join(" ")}`);
    const env = { ...process.env, ...variables, ...(debug ? { VSTEST_HOST_DEBUG: "1", VSTEST_DEBUG_NOBP: "1" } : {}) };
    return new Promise((resolve) => {
      const child = spawn(command, args, { cwd: folder?.uri.fsPath, env, windowsHide: true });
      this.process = child;
      // The testhost prints its "Process Id" line twice; a second attach would fail and cancel the run.
      const attached = new Set<string>();
      const line = (data: Buffer) => {
        const text = data.toString();
        this.output.append(text);
        run?.appendOutput(text.replace(/\r?\n/g, "\r\n"));
        const attach = debug ? /Process Id:\s*(\d+)/.exec(text) : null;
        if (attach && run && !attached.has(attach[1])) {
          attached.add(attach[1]);
          void vscode.debug.startDebugging(folder, { name: "Brinell BDD testhost", type: "coreclr", request: "attach", processId: attach[1] }).then((started) => {
            if (!started) {
              run.appendOutput("The C# debugger did not attach; the run is cancelled. Install the C# extension.\r\n");
              this.cancel();
            } else {
              this.attachApps(folder, Number(attach[1]), child, run);
            }
          });
        }
      };
      child.stdout.on("data", line);
      child.stderr.on("data", line);
      child.on("close", (code) => {
        this.process = undefined;
        resolve(code ?? 1);
      });
    });
  }
}
