// The Scraper view: the server's page (brinell/scraper/host) in a webview. The page does the work;
// this only relays its brinell/scraper/* requests and the server's brinell/scraper/changed, and opens
// files, diffs and the agent chat for it.
import * as vscode from "vscode";
import { LanguageClient } from "vscode-languageclient/node";
import { theme } from "./preview";

type PageMessage = { type: string; id?: number; method?: string; params?: unknown; path?: string; left?: string; right?: string; title?: string; prompt?: string };

export class ScraperView implements vscode.Disposable {
  private panel: vscode.WebviewPanel | undefined;
  private readonly subscriptions: vscode.Disposable[] = [];

  constructor(private readonly client: LanguageClient) {
    this.subscriptions.push(
      client.onNotification("brinell/scraper/changed", (change: object) => void this.panel?.webview.postMessage({ ...change, type: "changed" })),
      vscode.window.onDidChangeActiveColorTheme(() => void this.panel?.webview.postMessage(theme())),
    );
  }

  /** Opens the view, or shows it when it is open. */
  async open(): Promise<void> {
    if (this.panel) {
      this.panel.reveal();
      return;
    }

    this.panel = vscode.window.createWebviewPanel("brinellBdd.scraper", "Scraper", vscode.ViewColumn.Active, { enableScripts: true, retainContextWhenHidden: true, localResourceRoots: [] });
    this.panel.onDidDispose(() => (this.panel = undefined));
    this.panel.webview.onDidReceiveMessage((message: PageMessage) => void this.onMessage(message));
    this.panel.webview.html = (await this.client.sendRequest<{ html: string }>("brinell/scraper/host", {})).html;
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
        if (message.id !== undefined && message.method?.startsWith("brinell/scraper/")) {
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
          await openChat(message.prompt);
        }

        break;
    }
  }
}

// The agent's chat with the prompt filled in; where that command is missing, the prompt on the clipboard.
async function openChat(prompt: string): Promise<void> {
  try {
    await vscode.commands.executeCommand("workbench.action.chat.open", { query: prompt, isPartialQuery: true });
  } catch {
    await vscode.env.clipboard.writeText(prompt);
    void vscode.window.showInformationMessage("The prompt is on the clipboard: paste it into Copilot Chat in agent mode.");
  }
}
