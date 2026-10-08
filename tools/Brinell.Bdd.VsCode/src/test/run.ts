// The extension in a real VS Code window. Starts the installed VS Code
// (BRINELL_VSCODE, else the per-user install) with a profile of its own, this extension loaded from
// source, and the tests of suite.ts, on the Todo sample. The developer's step breakpoints are set
// aside for the run and put back after it. Run: npm run test:ide (needs tools/Brinell.Cli built).
import { spawn } from "child_process";
import * as fs from "fs";
import * as os from "os";
import * as path from "path";

const extension = path.resolve(__dirname, "..", "..");
const root = path.resolve(extension, "..", "..");
const workspace = path.join(root, "samples", "Todo");
const code = process.env.BRINELL_VSCODE ?? path.join(process.env.LOCALAPPDATA ?? "", "Programs", "Microsoft VS Code", "Code.exe");
const profile = path.join(os.tmpdir(), "Brinell", "vscode-tests");
const breakpoints = path.join(process.env.LOCALAPPDATA ?? path.join(os.homedir(), ".local", "share"), "Brinell", "bdd-breakpoints.json");

async function main(): Promise<number> {
  if (!fs.existsSync(code)) {
    console.error(`No VS Code at ${code}: set BRINELL_VSCODE to its Code.exe.`);
    return 1;
  }

  const kept = fs.existsSync(breakpoints) ? fs.readFileSync(breakpoints, "utf8") : undefined;
  try {
    const env: NodeJS.ProcessEnv = { ...process.env, BRINELL_TODO: workspace };

    // From a VS Code terminal, this would make Code.exe a plain node.
    delete env.ELECTRON_RUN_AS_NODE;
    const child = spawn(code, [
      workspace,
      `--extensionDevelopmentPath=${extension}`,
      `--extensionTestsPath=${path.join(__dirname, "suite.js")}`,
      `--user-data-dir=${path.join(profile, "user")}`,
      `--extensions-dir=${path.join(profile, "extensions")}`,
      "--disable-workspace-trust",
      "--skip-welcome",
      "--skip-release-notes",
      "--disable-telemetry",
      "--new-window",
    ], { env, stdio: "inherit" });
    return await new Promise<number>((resolve) => child.on("close", (exit) => resolve(exit ?? 1)));
  } finally {
    if (kept === undefined) {
      fs.rmSync(breakpoints, { force: true });
    } else {
      fs.writeFileSync(breakpoints, kept);
    }
  }
}

void main().then((exit) => {
  console.log(exit === 0 ? "VS Code tests passed." : `VS Code tests failed (exit ${exit}).`);
  process.exit(exit);
});
