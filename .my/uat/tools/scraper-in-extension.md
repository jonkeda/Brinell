# The Scraper in the IDE extensions, with GitHub Copilot

Status: **proposal**, drafted 2026-09-27. Nothing is built.
Builds on: [vscode-extension.md](vscode-extension.md), [visualstudio-extension.md](visualstudio-extension.md),
and the Scraper as it stands in `tools/Brinell.Scraper` (roadmap and notes in
[.my/Scraper/](../../Scraper/roadmap.md), dormant since 2026-05).

## 1. What the Scraper is today

A WPF app (.NET 10) with an embedded WebView2. The pipeline:

1. **Record.** Browse a site, capture a DOM snapshot on each page transition, and store the
   snapshots in a per-site SQLite corpus.
2. **Analyze.** Detect patterns that repeat across pages, and propose custom ControlObjects with a
   confidence score. The user approves or rejects each one.
3. **Generate.** Write the approved ControlObjects first, then PageObjects that use them.
4. **Check.** Roslyn syntax check, then write the files into a standalone Pages project, then
   `dotnet build`.

The LLM side uses the **GitHub Copilot SDK** (`GitHub.Copilot.SDK`), an agent runtime that wraps
the Copilot CLI:

- an analyzer session and a generator session, each on its own model;
- `SKILL.md` files for the Brinell conventions and each site's patterns;
- five corpus tools the model can call: `list_recorded_pages`, `get_page_snapshot`,
  `find_similar_elements`, `get_generated_controls`, `search_corpus`.

What works well and should be kept:

- recording is kept separate from generation;
- analysis looks across the whole corpus, not one page at a time;
- controls are generated before pages;
- the model reaches data through tools instead of one giant prompt;
- there is a compile gate.

What doesn't fit the IDE: the whole thing is a Windows-only WPF window, with its own code
preview (AvalonEdit), its own log viewer and its own approval UI. That duplicates what an IDE and
Copilot Chat already do better.

## 2. The idea

**Keep the Scraper's brain, drop its body.** The corpus, the capture script, the pattern
detection, the matcher and the compile gate become a **headless service**. The IDE gives the
editor, diff and approval. **Copilot is the only LLM**, reached the way each IDE offers it.

```text
VS Code / Visual Studio
 +-- Copilot Chat (agent mode) ---- MCP ----+
 +-- Brinell extension --------------------+-- Brinell.Scraper.Mcp (.NET, headless)
 |     Record view, corpus tree,              corpus (SQLite), capture, pattern detection,
 |     "Generate page object" commands        matcher, code output, Roslyn + dotnet build gate
 |                                                 |
 +-- a recording browser ------- CDP ----------+
       Chromium through Playwright (headed), one per recording session
```

Three changes from today:

| Today | In the extension |
| --- | --- |
| WebView2 inside a WPF window | a **headed Chromium started through Playwright**. Brinell.Html already depends on Playwright. The capture script is injected over CDP, as today over WebView2. It runs on any OS. |
| The app calls Copilot through the Copilot SDK, and the app owns the conversation | **Copilot Chat owns the conversation.** The Scraper's corpus tools become **MCP tools**. The IDE's Copilot calls them in agent mode, and the user sees and approves each step in chat. (Roadmap item 11.6 already suggested extracting an MCP server.) |
| Conventions as the Scraper's own `SKILL.md` files | the repo's **existing** skills (`.github/skills/html-control`, `html-ui-test`) and `copilot-instructions.md`, which Copilot already reads. Per-site patterns become generated instruction files in the target Pages project. |

## 3. Why MCP, and not the extension API or the Copilot SDK

| Route | Works in | For |
| --- | --- | --- |
| **MCP server** | VS Code, Visual Studio, Copilot CLI, and any MCP client | **the main route**: one implementation for both IDEs |
| VS Code Language Model Tools API (`vscode.lm.registerTool`) | VS Code only | not needed if MCP is there; maybe for a tool that must touch VS Code UI |
| VS Code Language Model API (`vscode.lm.selectChatModels`) | VS Code only | **batch** steps with no chat, e.g. "analyze all 40 recorded pages overnight", on the user's Copilot subscription |
| Copilot SDK (today) | anywhere the Copilot CLI runs | keep it for the **headless CLI** mode (section 7). It's the only route with no IDE. |

The MCP server doesn't call a model. It holds data and does the deterministic work, and Copilot
does the thinking. That keeps the service testable without a model and without tokens.

## 4. The MCP tools

The five corpus tools stay as they are. The new ones cover what the WPF app did around them:

| Tool | Does | Deterministic? |
| --- | --- | --- |
| `start_recording(site, url)` | opens the recording browser on a site's corpus | yes |
| `capture_page(name?)` | snapshots the current page into the corpus (automatic on transitions while recording) | yes |
| `list_recorded_pages`, `get_page_snapshot`, `find_similar_elements`, `get_generated_controls`, `search_corpus` | today's corpus tools | yes |
| `detect_patterns(site)` | the Scraper's `ControlGroupDetector` and `CssSignatureParser`: candidate groups with counts, *before* any model runs | yes |
| `propose_control(name, signature, properties)` | records a proposal in the corpus, marked "proposed" | yes |
| `match_controls(page)` | `ControlObjectMatcher`: which approved controls occur on a page | yes |
| `write_code(path, code)` | writes a ControlObject or PageObject into the target Pages project, in its folder convention | yes |
| `check_code(project)` | Roslyn syntax check, the Brinell analyzers, `dotnet build`; returns the diagnostics | yes |
| `diff_page(page, since)` | `DomDiffService`: what changed since the page objects were generated | yes |

A generation turn in chat then reads like this:

1. The user types "@brinell generate page objects for the invoices area".
2. Copilot calls `list_recorded_pages`, `detect_patterns` and `get_page_snapshot`.
3. It proposes three controls. The user approves two in chat.
4. It writes the controls, then the pages, with `write_code`.
5. It runs `check_code` and fixes what fails.
6. The user reviews the result in the IDE's normal diff (Source Control), not in a special preview.

## 5. What the extension adds on top of chat

Chat does the generating. The extension gives the things chat can't:

- **Record view.** Start and stop recording, name the current page, and see the pages captured so
  far. Clicking an element in the browser selects it in a **DOM tree** view (the Scraper's
  highlight service, over CDP).
- **Corpus tree.** Sites → pages → snapshots, with each page's status: new, generated, or changed
  since generation (from `diff_page`). *Compare* opens two snapshots in the IDE's diff editor as
  formatted JSON.
- **Commands.** Commands on a recorded page, *Generate page object* and *Regenerate one
  control*, open a Copilot chat with the right prompt and context already filled in. VS Code does
  this through a chat participant (`@brinell`) with slash commands (`/record`, `/analyze`,
  `/generate`, `/refresh`). Visual Studio does it with a prompt file.
- **Prompt files.** `.github/prompts/brinell-generate-pages.prompt.md` and
  `brinell-analyze-site.prompt.md`: the Scraper's `PromptBuilder` templates, turned into prompt
  files. So the pipeline also works in plain Copilot Chat without the extension.

## 6. Beyond page objects: the UAT link

The extension proposals already give Copilot the phrase catalog (`list_phrases`,
`validate_scenario`). With the Scraper in the same MCP server, one recording can feed two
outputs:

- **Page objects** from the DOM, as above.
- **A draft scenario** from the *actions* recorded between snapshots (click Save, type into
  Title). Copilot turns them into `.feature.md` steps using **only phrases from the catalog**, then
  checks the draft with `validate_scenario` before proposing it. A step with no phrase is left as
  a marked gap for a developer, never an invented phrase.

This is the "record a journey, get a scenario" feature testers ask for. It stays inside AD-017:
the draft is a sequence of steps, not a recorded script to replay.

## 7. Headless mode

The same service with a small CLI, `brinell-scrape`:

- `record`: opens the browser and records until closed;
- `analyze` and `generate`: run the pipeline with the Copilot SDK, as the WPF app does today.

This is for bulk regeneration of a big site, overnight or in CI. There's no chat and no IDE, and
the user approves the proposals afterwards, from a report.

## 8. MAUI later

The Scraper only knows HTML. The corpus schema (snapshots of elements with attributes, roles,
text and bounds) fits a UI Automation tree just as well, and roadmap item 11.5 already expects it.
A MAUI capture would take the tree from FlaUI (Windows) or Appium (Android), the same trees
Brinell's drivers use. The Presenter's Discovery already walks them. Generation would then use
the `maui-control` and `maui-ui-test` skills. Not in the first phases.

## 9. What happens to the WPF app

It stays as it is, dormant, while the service is extracted from its `Services/` folder. Once the
extension covers recording, the corpus and generation, the WPF shell is deleted: its views are
what the IDE replaces. The unit tests in `tools/Brinell.Scraper.Tests` move with the services.

## 10. Phases

| # | Phase | Delivers |
| --- | --- | --- |
| 0 | Extract | `Brinell.Scraper.Core` (corpus, detector, matcher, diff, output, validator) with no WPF or WebView2 dependency; the WPF app runs on it unchanged |
| 1 | MCP server | the corpus tools plus `detect_patterns`, `match_controls`, `write_code`, `check_code`; prompt files; works from Copilot agent mode in both IDEs against an **existing** corpus |
| 2 | Recording | the Playwright recording browser; `start_recording`, `capture_page`; the Record view and corpus tree in VS Code |
| 3 | Chat participant | `@brinell` with slash commands in VS Code; the same prompts in VS |
| 4 | Refresh | `diff_page` and "changed since generation" in the corpus tree, regenerating only what changed |
| 5 | Scenarios from recordings | action capture, drafts checked against the catalog |
| later | Headless CLI, MAUI capture, deleting the WPF shell | sections 7-9 |

Phase 1 already pays off: a corpus recorded with today's WPF app can be turned into page objects
from Copilot Chat in either IDE.

## 11. Risks

- **Model quality varies.** Copilot picks from the models the user's plan allows. The compile gate
  and the Brinell analyzers are what make the output trustworthy, not the model. Keep
  `check_code` mandatory in the prompt files.
- **Logged-in sites.** The recording browser needs the user's session. Use a persistent
  Playwright profile per site, as the Scraper keeps WebView2 cookies today. Never ask Copilot to
  sign in.
- **Data in prompts.** Snapshots of Exact Online or Construction pages can contain customer data.
  Capture from test tenants. Add a redaction step, covering input values and visible text, before
  anything reaches a tool result. Check what the capture script records today; nothing in it
  redacts yet.
- **MCP trust.** The server writes files and runs `dotnet build`. It runs only for the repo that
  configures it (`.vscode/mcp.json`, `.mcp.json`), and each write is shown in chat before it
  happens.

## 12. Open questions

| # | Question | Notes |
| --- | --- | --- |
| Q1 | Keep the Copilot SDK at all? | Only the headless CLI needs it. If bulk generation isn't needed, drop it and have one route (MCP). |
| Q2 | Where does the corpus live? | Today it's `%LOCALAPPDATA%`, per user. For a team, the corpus next to the Pages project (or exported JSON, roadmap 11.4) makes regenerating reproducible. |
| Q3 | Does the recording browser need to be headed in CI? | No: headless is fine for `capture_page` over a list of URLs. |
| Q4 | Owner | The Scraper is dormant and its owner hasn't decided on it (cleanup plan). This proposal gives it a direction; it needs a yes before phase 0. |
