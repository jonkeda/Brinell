// The Scraper view: the server's page (bruinet/scraper/host) in a webview. The page does the work;
// this only relays its bruinet/scraper/* requests and the server's bruinet/scraper/changed, and opens
// files, diffs and the agent chat for it.
import * as vscode from "vscode";
import { LanguageClient } from "vscode-languageclient/node";
import { theme } from "./preview";

type PageMessage = { type: string; id?: number; method?: string; params?: unknown; path?: string; left?: string; right?: string; title?: string; prompt?: string; agent?: string | null; model?: string | null; solutionFolder?: string | null };

export class ScraperView implements vscode.Disposable {
  private panel: vscode.WebviewPanel | undefined;
  private readonly subscriptions: vscode.Disposable[] = [];

  constructor(private readonly client: LanguageClient) {
    this.subscriptions.push(
      client.onNotification("bruinet/scraper/changed", (change: object) => void this.panel?.webview.postMessage({ ...change, type: "changed" })),
      vscode.window.onDidChangeActiveColorTheme(() => void this.panel?.webview.postMessage(theme())),
    );
  }

  /** Opens the view, or shows it when it is open. */
  async open(): Promise<void> {
    if (this.panel) {
      this.panel.reveal();
      return;
    }

    this.panel = vscode.window.createWebviewPanel("bruinetBdd.scraper", "Scraper", vscode.ViewColumn.Active, { enableScripts: true, retainContextWhenHidden: true, localResourceRoots: [] });
    this.panel.onDidDispose(() => (this.panel = undefined));
    this.panel.webview.onDidReceiveMessage((message: PageMessage) => void this.onMessage(message));
    this.panel.webview.html = (await this.client.sendRequest<{ html: string }>("bruinet/scraper/host", {})).html;
  }

  dispose(): void {
    this.subscriptions.forEach((subscription) => subscription.dispose());
    this.panel?.dispose();
  }

  private async onMessage(message: PageMessage): Promise<void> {
    switch (message.type) {
      case "ready":
        await this.panel?.webview.postMessage(theme());
        break;
      case "call":
        // Only the Scraper's own requests: the page is not a way to the rest of the server.
        if (message.id !== undefined && message.method?.startsWith("bruinet/scraper/")) {
          const value = await this.client.sendRequest(message.method, message.params ?? {}).then((result) => result, (error: Error) => ({ problem: error.message }));
          await this.panel?.webview.postMessage({ type: "result", id: message.id, value });
        }

        break;
      case "open":
        if (message.path) {
          await vscode.window.showTextDocument(vscode.Uri.file(message.path), { preview: true, viewColumn: vscode.ViewColumn.Beside });
        }

        break;
      case "diff":
        if (message.left && message.right) {
          await vscode.commands.executeCommand("vscode.diff", vscode.Uri.file(message.left), vscode.Uri.file(message.right), message.title);
        }

        break;
      case "chat":
        if (message.prompt) {
          const via = await openChat(message.prompt, message.agent ?? undefined, message.model ?? undefined, () => void this.panel?.webview.postMessage({ type: "chatDone" }));
          void this.panel?.webview.postMessage({ type: "chatted", via, opened: via !== "clipboard", model: message.model ?? null });
        }

        break;
      case "agents":
        void this.panel?.webview.postMessage({ type: "agents", models: await chatModels(), agents: true });
        break;
      case "addProject":
        if (message.path) {
          await addToSolution(message.path, message.solutionFolder ?? undefined);
        }

        break;
    }
  }
}

// A new scraped project into the workspace's solution, with dotnet sln add, in a solution folder when one is named.
async function addToSolution(project: string, solutionFolder: string | undefined): Promise<void> {
  const solutions = await vscode.workspace.findFiles("*.{sln,slnx}", undefined, 2);
  if (solutions.length !== 1) {
    void vscode.window.showInformationMessage(`Add ${project} to a solution by hand: the workspace has ${solutions.length === 0 ? "no solution" : "several"} at its root.`);
    return;
  }

  const args = ["sln", solutions[0].fsPath, "add", project, ...(solutionFolder ? ["--solution-folder", solutionFolder] : [])];
  const { execFile } = await import("node:child_process");
  execFile("dotnet", args, (error, _stdout, stderr) => {
    if (error) {
      void vscode.window.showWarningMessage(`dotnet sln add failed: ${stderr || error.message}`);
    } else {
      void vscode.window.setStatusBarMessage(`${project} is in the solution.`, 5000);
    }
  });
}

// The agent's chat with the prompt filled in; where that command is missing, the prompt on the clipboard.
// The models the chat can use, for the page's model choice.
async function chatModels(): Promise<{ id: string; name: string }[]> {
  try {
    const models = await vscode.lm.selectChatModels();
    const seen = new Set<string>();
    return models.filter(m => !seen.has(m.id) && seen.add(m.id)).map(m => ({ id: m.id, name: m.name }));
  } catch {
    return [];
  }
}

// How long the chat may take to refuse a request (an unknown model) before it counts as sent; an answer
// that comes back sooner means this VS Code does not wait for the answer (no blockOnResponse).
const ACCEPTED_MS = 2000;

// The prompt sent in the chosen agent (a custom agent's name, or agent / ask) with the chosen model; when the
// chat refuses those, sent without them; where there is no chat, on the clipboard. Says which. done() runs
// when the answer is complete, where VS Code can tell.
async function openChat(prompt: string, agent: string | undefined, model: string | undefined, done: () => void): Promise<"sent" | "clipboard"> {
  const open = (options: object) => Promise.resolve(vscode.commands.executeCommand("workbench.action.chat.open", { query: prompt, isPartialQuery: false, ...options }));
  try {
    const run = open({ mode: agent, modelSelector: model ? { id: model } : undefined, blockOnResponse: true });
    const first = await Promise.race([run.then(() => "answered"), new Promise<string>(resolve => setTimeout(() => resolve("running"), ACCEPTED_MS))]);
    if (first === "running") {
      void run.then(done, done);
    }
    return "sent";
  } catch {
    try {
      await open({});
      return "sent";
    } catch {
      await vscode.env.clipboard.writeText(prompt);
      void vscode.window.showInformationMessage("The prompt is on the clipboard: paste it into Copilot Chat in agent mode.");
      return "clipboard";
    }
  }
}
