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
type StepStart = { started: boolean; reason?: string; scenario?: string; suite?: string };
type RecordTarget = { canRecord: boolean; reason?: string; project?: string; filter?: string; environment: Record<string, string>; video?: string; folder?: string };
type Recording = { path: string; name: string; written: string; captions?: string };

export class ScenarioTests implements vscode.Disposable {
  readonly controller = vscode.tests.createTestController("bruinetBdd", "Bruinet BDD");
  private readonly data = new WeakMap<vscode.TestItem, Data>();
  private readonly output = vscode.window.createOutputChannel("Bruinet BDD");
  private process: ChildProcess | undefined;
  private stepRun: { run: vscode.TestRun; scenario: vscode.TestItem; started: number } | undefined;
  private runnerRun: { run: vscode.TestRun; scenarios: vscode.TestItem[]; started: number } | undefined;

  // A file changed outside build output since, or a project built cleanly when: a Run with nothing
  // changed skips dotnet build, which takes about 10 s even for a project that is up to date. A
  // Step-through run reads the scenario files from the source (I1), so only other files count for it.
  private lastChange = Date.now();
  private lastCodeChange = Date.now();
  private readonly buildEvents = new vscode.EventEmitter<{ project: string; built: boolean }>();

  /** Whether a run built its project or found it up to date: for the extension's own tests. */
  readonly onBuild = this.buildEvents.event;
  private readonly lastBuild = new Map<string, number>();
  private readonly watcher = vscode.workspace.createFileSystemWatcher("**/*");

  constructor(
    private readonly client: LanguageClient,
    private readonly running: (line: number | undefined, file: boolean, paused?: boolean) => void,
    private readonly stepping: { started(): void; readonly isStepping: boolean },
  ) {
    const changed = (uri: vscode.Uri) => {
      if (!/[\\/](bin|obj|TestResults|node_modules|\.git|\.vs)[\\/]/i.test(uri.fsPath)) {
        this.lastChange = Date.now();
        if (!readFromSource.test(uri.fsPath)) {
          this.lastCodeChange = this.lastChange;
        }
      }
    };
    this.watcher.onDidChange(changed);
    this.watcher.onDidCreate(changed);
    this.watcher.onDidDelete(changed);
    this.controller.resolveHandler = async () => this.discover();
    this.controller.refreshHandler = async () => this.discover();
    this.controller.createRunProfile("Run", vscode.TestRunProfileKind.Run, (request, token) => this.run(request, token, false), true);
    this.controller.createRunProfile("Debug", vscode.TestRunProfileKind.Debug, (request, token) => this.run(request, token, true), true);
    this.controller.createRunProfile("Step through", vscode.TestRunProfileKind.Run, async (request, token) => {
      await this.step(request, token);
    }, false);
    this.controller.createRunProfile("Run on the runner", vscode.TestRunProfileKind.Run, async (request, token) => {
      await this.runOnRunner(request, token);
    }, false);
  }

  /** Reads every suite's scenarios from the server. */
  async discover(): Promise<void> {
    const suites = await this.client.sendRequest<SuiteScenarios[]>("bruinet/scenarios", {});
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
    const matches = await this.itemsAt(uri, line);
    if (matches.length === 0) {
      void vscode.window.showWarningMessage("No suite runs this scenario by its address. Build the suite, and give its test class [UatScenarioTheory].");
      return;
    }

    // A file linked into several suites runs in its own.
    const request = new vscode.TestRunRequest([this.own(matches)]);
    const source = new vscode.CancellationTokenSource();
    await this.run(request, source.token, debug);
  }

  /**
   * Runs the scenario at a heading's line, or a whole file, on the suite's runner (I4 of the debugger
   * plan): from the source, with no build when only scenario files changed. Why it did not start, when it did not.
   */
  async runOnRunnerAt(uri: vscode.Uri, line: number | undefined): Promise<string | undefined> {
    const matches = await this.itemsAt(uri, line);
    return matches.length === 0
      ? this.refuse("No suite holds this scenario. Build the suite first.")
      : await this.runOnRunner(new vscode.TestRunRequest([this.own(matches)]), new vscode.CancellationTokenSource().token);
  }

  /** Steps through the scenario at a heading's line, from the preview; why it did not start, when it did not. */
  async stepAt(uri: vscode.Uri, line: number): Promise<string | undefined> {
    const matches = await this.itemsAt(uri, line);
    return matches.length === 0
      ? this.refuse("No suite holds this scenario. Build the suite first.")
      : await this.step(new vscode.TestRunRequest([this.own(matches)]), new vscode.CancellationTokenSource().token);
  }

  // Of a file's items in the suites that hold or link it, the one of the suite whose project holds it.
  private own(matches: vscode.TestItem[]): vscode.TestItem {
    const holds = (item: vscode.TestItem) => {
      let suite: vscode.TestItem = item;
      while (suite.parent) {
        suite = suite.parent;
      }

      const data = this.data.get(suite);
      const folder = data?.kind === "suite" && data.project ? path.dirname(data.project).toLowerCase() + path.sep : undefined;
      return folder !== undefined && item.uri!.fsPath.toLowerCase().startsWith(folder);
    };
    return matches.find(holds) ?? matches[0];
  }

  private refuse(reason: string): string {
    this.output.appendLine(reason);
    void vscode.window.showWarningMessage(reason);
    return reason;
  }

  /** The server says a run on the runner ended: each scenario's result. */
  runnerEnded(ended: { status: string; message?: string; results: { scenario: string; status: string; message?: string }[] }): void {
    const going = this.runnerRun;
    if (!going) {
      return;
    }

    this.runnerRun = undefined;
    const duration = Date.now() - going.started;
    for (const scenario of going.scenarios) {
      // An outline's rows are its name with [n]; the scenario failed when a row did.
      const name = (this.data.get(scenario) as { name: string }).name;
      const results = ended.results.filter((result) => result.scenario === name || result.scenario.startsWith(`${name} [`));
      const failed = results.find((result) => result.status !== "passed");
      if (results.length === 0) {
        going.run.skipped(scenario);
      } else if (failed?.status === "failed") {
        going.run.failed(scenario, new vscode.TestMessage(failed.message ?? `${failed.scenario} failed.`), duration);
      } else if (failed) {
        going.run.errored(scenario, new vscode.TestMessage(failed.message ?? `${failed.scenario} could not run.`), duration);
      } else {
        going.run.passed(scenario, duration);
      }
    }

    this.output.appendLine(`The run on the runner ended: ${ended.status}${ended.message ? `: ${ended.message}` : ""}`);
    going.run.end();
    this.running(undefined, false);
  }

  /** The server says the Step-through run paused, or went on. */
  stepPaused(paused: boolean): void {
    if (this.stepRun) {
      this.running(this.stepRun.scenario.range!.start.line + 1, false, paused);
    }
  }

  /** The server says the Step-through run ended: its test's result. */
  stepEnded(status: string, message: string | undefined): void {
    const step = this.stepRun;
    if (!step) {
      return;
    }

    this.stepRun = undefined;
    const duration = Date.now() - step.started;
    if (status === "passed") {
      step.run.passed(step.scenario, duration);
    } else if (status === "failed") {
      step.run.failed(step.scenario, new vscode.TestMessage(message ?? "A step failed."), duration);
    } else if (status === "stopped") {
      step.run.skipped(step.scenario);
    } else {
      step.run.errored(step.scenario, new vscode.TestMessage(message ?? "The Step-through run failed."), duration);
    }

    this.output.appendLine(`The Step-through run ended: ${status}${message ? `: ${message}` : ""}`);
    step.run.end();
    this.running(undefined, false);
  }

  // The scenario at a heading's line, or the file when there is none; in every suite that holds it.
  private async itemsAt(uri: vscode.Uri, line: number | undefined): Promise<vscode.TestItem[]> {
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
    return matches;
  }

  // One scenario, or a file's, on the suite's runner; the test run ends when the server says the run ended.
  private async runOnRunner(request: vscode.TestRunRequest, token: vscode.CancellationToken): Promise<string | undefined> {
    if (this.process || this.stepRun || this.runnerRun || this.stepping.isStepping) {
      return this.refuse("A BDD run is going already. Wait for it, or cancel it, before starting another.");
    }

    const picked = request.include ?? [];
    const item = picked.length === 1 ? picked[0] : undefined;
    const kind = item ? this.data.get(item)?.kind : undefined;
    if (!item || (kind !== "scenario" && kind !== "file")) {
      return this.refuse("A run on the runner runs one scenario or one file: pick one.");
    }

    const fileItem = kind === "scenario" ? item.parent! : item;
    const suite = fileItem.parent!;
    const data = this.data.get(suite);
    const scenarios: vscode.TestItem[] = [];
    if (kind === "scenario") {
      scenarios.push(item);
    } else {
      item.children.forEach((scenario) => scenarios.push(scenario));
    }

    const line = kind === "scenario" ? item.range!.start.line + 1 : undefined;
    const run = this.controller.createTestRun(new vscode.TestRunRequest(scenarios));
    scenarios.forEach((scenario) => run.started(scenario));
    this.running(line, line === undefined);
    const fail = (reason: string): string => {
      scenarios.forEach((scenario) => run.errored(scenario, new vscode.TestMessage(reason)));
      run.end();
      this.running(undefined, false);
      return this.refuse(reason);
    };
    if (data?.kind === "suite" && data.project && !await this.build(run, data.project, true)) {
      return fail("The build failed, so the run did not start.");
    }

    const started = await this.client.sendRequest<{ started: boolean; reason?: string }>("bruinet/run/start", { uri: fileItem.uri!.toString(), line, suite: suite.label });
    if (!started.started) {
      return fail(`This cannot be run on the runner: ${started.reason}`);
    }

    this.runnerRun = { run, scenarios, started: Date.now() };
    token.onCancellationRequested(() => this.cancel());
    return undefined;
  }

  // Step through one scenario (AD-022): its suite's project is built, then the
  // server runs it in the suite's runner, paused before its first step; its test run ends when the
  // server says the run ended.
  private async step(request: vscode.TestRunRequest, token: vscode.CancellationToken): Promise<string | undefined> {
    if (this.process || this.stepRun || this.stepping.isStepping) {
      return this.refuse("A BDD run is going already. Wait for it, or cancel it, before starting another.");
    }

    const scenarios: vscode.TestItem[] = [];
    const add = (item: vscode.TestItem): void => {
      if (this.data.get(item)?.kind === "scenario") {
        scenarios.push(item);
      } else {
        item.children.forEach(add);
      }
    };
    (request.include ?? this.all()).forEach(add);
    if (scenarios.length !== 1) {
      return this.refuse("Step through runs one scenario: pick a scenario, not a file or a suite.");
    }

    const scenario = scenarios[0];
    const suite = scenario.parent!.parent!;
    const data = this.data.get(suite);
    const line = scenario.range!.start.line + 1;
    const run = this.controller.createTestRun(new vscode.TestRunRequest([scenario]));
    run.started(scenario);
    this.running(line, false);
    this.output.show(true);
    if (data?.kind === "suite" && data.project && !await this.build(run, data.project, true)) {
      run.errored(scenario, new vscode.TestMessage("The build failed, so the Step-through run did not start."));
      run.end();
      this.running(undefined, false);
      return this.refuse("The build failed, so the Step-through run did not start.");
    }

    const started = await this.client.sendRequest<StepStart>("bruinet/step/start", { uri: scenario.uri!.toString(), line, suite: suite.label });
    if (!started.started) {
      run.errored(scenario, new vscode.TestMessage(`This cannot be stepped through: ${started.reason}`));
      run.end();
      this.running(undefined, false);
      return this.refuse(`This cannot be stepped through: ${started.reason}`);
    }

    this.output.appendLine(`Stepping through '${started.scenario}' in ${started.suite}: F10 over, F11 into, Shift+F11 out, F5 on to a breakpoint, Shift+F5 stop.`);
    this.stepRun = { run, scenario, started: Date.now() };
    this.stepping.started();
    token.onCancellationRequested(() => void this.client.sendRequest("bruinet/step/move", { move: "stop" }));
    return undefined;
  }

  /**
   * Records a video script, or the scenario at a heading's line (the narrator): the gate run with
   * BRUINET_RECORD, then the video it made, to watch. The settings give the Recordings folder, the
   * voice, the beat and the touch marks' colour.
   */
  async record(uri: vscode.Uri, line: number | undefined, suite: string | undefined): Promise<void> {
    if (this.process) {
      void vscode.window.showWarningMessage("A BDD run is going already. Wait for it, or cancel it, before recording.");
      return;
    }

    const settings = vscode.workspace.getConfiguration("bruinetBdd");
    const recordingsFolder = settings.get<string>("recordingsFolder") || undefined;
    const target = await this.client.sendRequest<RecordTarget>("bruinet/recordTarget", {
      uri: uri.toString(), line, suite, recordingsFolder, voice: settings.get<string>("voice") || undefined, beat: settings.get<number>("beat") || undefined,
      touchColor: settings.get<string>("touchColor") || undefined,
    });
    if (!target.canRecord || !target.project || !target.filter) {
      void vscode.window.showWarningMessage(`This cannot be recorded: ${target.reason ?? "no suite runs it."}`);
      return;
    }

    this.output.show(true);
    this.running(line, line === undefined);
    try {
      this.output.appendLine(`Recording into ${target.folder}...`);
      if (!await this.build(undefined, target.project)) {
        void vscode.window.showErrorMessage("The build failed, so the recording did not start.");
        return;
      }

      const code = await this.exec(undefined, "dotnet", ["test", target.project, "--no-build", "--filter", target.filter, "--logger", "console;verbosity=normal"], false, target.environment);
      const videos = await this.client.sendRequest<Recording[]>("bruinet/recordings", { uri: uri.toString(), suite, recordingsFolder });
      const video = target.video ? videos.find((candidate) => candidate.name === target.video) : videos[0];
      if (!video) {
        void vscode.window.showWarningMessage("The run made no video: see the Bruinet BDD output.");
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

  /** Stops the run: the test process and everything it started, the app included; a Step-through run through the server. */
  cancel(): void {
    if (this.runnerRun) {
      this.output.appendLine("Cancelling the run on the runner...");
      void this.client.sendRequest("bruinet/run/cancel", {});
      return;
    }

    if (this.stepRun) {
      this.output.appendLine("Stopping the Step-through run...");
      void this.client.sendRequest("bruinet/step/move", { move: "stop" });
      return;
    }

    if (this.process?.pid && this.process.exitCode === null) {
      this.output.appendLine("Cancelling the BDD run...");
      spawn("taskkill", ["/PID", String(this.process.pid), "/T", "/F"], { windowsHide: true });
    }
  }

  dispose(): void {
    this.watcher.dispose();
    this.buildEvents.dispose();
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
        if (!await this.build(run, data.project)) {
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
        byFile.set(uri.toString(), await this.client.sendRequest<ScenarioResult[]>("bruinet/lastRuns", { uri: uri.toString() }));
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

  // While the run goes: each app the testhost announces (Bruinet.Core's DebuggedApps) is attached
  // too, so a breakpoint in the app's own code stops.
  private attachApps(folder: vscode.WorkspaceFolder | undefined, testhost: number, child: ChildProcess, run: vscode.TestRun): void {
    const announced = path.join(os.tmpdir(), "Bruinet", "debugged-apps");
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
        void vscode.debug.startDebugging(folder, { name: `Bruinet BDD app ${app}`, type: "coreclr", request: "attach", processId: String(app) }).then((started) =>
          run.appendOutput(started ? `The debugger is attached to the app too (process ${app}).\r\n` : `The debugger could not attach to the app (process ${app}); the run goes on.\r\n`));
        fs.rmSync(path.join(announced, name), { force: true });
      }
    }, 500);
  }

  // Builds a project, unless nothing outside build output changed since its last clean build; for a
  // run on the suite's runner, unless nothing but scenario files did.
  private async build(run: vscode.TestRun | undefined, project: string, runner = false): Promise<boolean> {
    const started = Date.now();
    if ((this.lastBuild.get(project) ?? -1) > (runner ? this.lastCodeChange : this.lastChange)) {
      this.output.appendLine(runner
        ? `Nothing but scenario files changed since ${project} was last built: not built again (the runner reads them from the source).`
        : `Nothing changed since ${project} was last built: not built again.`);
      this.buildEvents.fire({ project, built: false });
      return true;
    }

    if (await this.exec(run, "dotnet", ["build", project, "-v:minimal", "-nologo"], false) !== 0) {
      return false;
    }

    // From when it started: a file saved during the build makes the next Run build again. The server
    // loads the new output, and starts the suite's warm runner from it.
    this.lastBuild.set(project, started);
    void this.client.sendNotification("bruinet/buildDone", {});
    this.buildEvents.fire({ project, built: true });
    return true;
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
          void vscode.debug.startDebugging(folder, { name: "Bruinet BDD testhost", type: "coreclr", request: "attach", processId: attach[1] }).then((started) => {
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

// What a run on the suite's runner reads from the source folder, not the build's copies (AD-022).
const readFromSource = /(\.feature\.md|\.steps\.md|\.video\.md|[\\/](uat|bdd)\.config\.md)$/i;
