# Manual test plan: the Scraper

Status: **ready**; Claude's part run 2026-09-30 (section 11), the owner's not yet.
For: the owner at a Windows machine with Visual Studio and VS Code (about 40 minutes), and Claude
without an IDE window (scripted). From [next-plan.md](next-plan.md), section 2.

What each feature should do is in the user guide, [docs/guides/scraper.md](../../docs/guides/scraper.md).
When this plan and the guide disagree, note it as a finding.

## How to use this plan

- **Who** says who runs a check: **O** the owner, in the IDE; **C** Claude, scripted (the
  recording browser driven as a person would, the view's page through a stand-in host that relays
  to the real language server, MCP); **O/C** both.
- Each check has an ID, what to do, and what you should see. Mark it **Pass**, **Fail** or
  **Blocked**, with a note for anything but a plain pass.
- When something fails, carry on if you can. Take a screenshot, and copy the language server's
  output (VS Code: Output, "Brinell BDD Language Server"; Visual Studio: Output, "Brinell BDD").
- Checks marked *(after step N)* wait for that step of the next plan.

## 1. What you need

| | |
| --- | --- |
| Visual Studio | 2022 17.10 or later, or 2026, with the **Visual Studio extension development** workload |
| VS Code | 1.95 or later, Node.js 20 |
| .NET | the .NET 10 SDK |
| Repository | Brinell, branch `move`, at or after this plan's commit |
| Chromium | Playwright's: from the Brinell root, `dotnet build testsnew\Brinell.Html.UITests`, then `pwsh testsnew\Brinell.Html.UITests\bin\Debug\net10.0\playwright.ps1 install chromium` |

The project used is `testsnew\Brinell.Blazor.UITests` (it has a `scraper.config.md`); the app is the
Blazor sample. A recording adds folders under its `Recordings\`: delete the ones you made when
you are done (`git clean -fd testsnew\Brinell.Blazor.UITests\Recordings`, after `git status`).

## 2. Setup

| ID | Who | Do | You should see | Result |
| --- | --- | --- | --- | --- |
| S-1 | O/C | From the Brinell root: `dotnet build tools\Brinell.Bdd.LanguageServer`. | It builds. | ☐ |
| S-2 | O/C | Start the Blazor sample: `dotnet run --project samples\Brinell.Samples.Blazor.App --urls http://localhost:5180`. | http://localhost:5180 shows the sample's home page. | ☐ |
| S-3 | O | VS Code: open `tools\Brinell.Bdd.VsCode`, `npm ci`, `npm run compile`, press **F5**, and in the new window open the Brinell root folder. | A window titled "[Extension Development Host]" with the Brinell folder. | ☐ |
| S-4 | O | Visual Studio: open `tools\Brinell.Bdd.VisualStudio\Brinell.Bdd.VisualStudio.slnx`, press **F5**; in the experimental instance open `srcnew\Brinell.sln`. | The experimental instance with the solution. | ☐ |

## 3. Opening the view

| ID | Who | Do | You should see | Result |
| --- | --- | --- | --- | --- |
| V-1 | O | VS Code: Command Palette, **Brinell BDD: Open Scraper**. Then open `testsnew\Brinell.Blazor.UITests\scraper.config.md`. | The Scraper opens in an editor tab. The config's title bar has the Scraper's button, which shows the same view. | ☐ |
| V-2 | O | Visual Studio: **View > Other Windows > Scraper**. | The Scraper opens in a document tab. | ☐ |
| V-3 | O/C | Look at the view. | The project picker shows `Brinell.Blazor.UITests`. The Pages tab lists the sample's recorded pages (13), each with its captures. `/userform` says `UserFormPage`. | C: Pass; O: ☐ |
| V-4 | C | The view on a solution with no `scraper.config.md`. | A line says no project here has a `scraper.config.md` and to add one. | Pass (run 1) |

## 4. Recording

| ID | Who | Do | You should see | Result |
| --- | --- | --- | --- | --- |
| R-1 | O/C | Session name `manual`, Start URL empty, **Record**. | A Chromium window opens at http://localhost:5180/. Within a few seconds the status says "Recording …-manual: 1 page(s)". Capture, Pick and Stop are enabled, Record is not. | C: Pass; O: ☐ |
| R-2 | O/C | In the browser, click **Counter** in the menu, then **Login**. | Each page is captured once it has settled: the status counts 2, then 3. The Pages list gains `/counter` and `/login` captures. | C: Pass; O: ☐ |
| R-3 | O/C | Go back to **Counter** without changing anything. | Not captured again: the count stays. | C: Pass; O: ☐ |
| R-4 | O/C | On Counter, click the increment button, then **Capture** in the view. | A new capture (the count changed on the page). | C: Pass; O: ☐ |
| R-5 | C | On the test site's single-page app, follow a hash link and the `pushState` button. | Each is a capture, as `/pages/spa.html#/orders/{id}` and `/pages/settings`. | Pass (run 1) |
| R-6 | C | On the test site's order page, open the dialog, **Capture**. | The capture has the dialog (`role=dialog`) at the end of body. | Pass (run 1) |
| R-7 | C | Go to a page on another host (the test site on `127.0.0.1`). | Not captured: it is not the app's. | Pass (run 1) |
| R-8 | O/C | **Stop**. | The browser closes; Record is enabled again. `Recordings\<date>-manual\` has `recording.json` and one `.nodes.jsonl` per capture. | C: Pass; O: ☐ |
| R-9 | C | A second **Record** in a session with a cookie set in the first. | The cookie is still there: the profile keeps a login between sessions. | Pass (run 1) |

## 5. Pick and highlight

| ID | Who | Do | You should see | Result |
| --- | --- | --- | --- | --- |
| P-1 | O | Record again. **Pick**, then click Counter's **Click me** button (`increment-btn`) in the browser. | While picking, the element under the mouse is outlined in blue. After the click the view shows that page's capture with the button's line selected, and its details: the control (`ButtonControl`), a locator, how often its kind occurs. The click did not reach the page (the count did not change). | ☐ |
| P-2 | O | **Pick**, then press **Escape** in the browser. | Picking ends; nothing is selected. | ☐ |
| P-3 | O/C | With the newest capture of the live session open in the view, click a line. | The element is outlined in red in the browser. | C: Pass; O: ☐ |
| P-4 | C | The same inside a frame (the test site's checkout). | The outline is drawn in the frame. | Pass (run 1) |

## 6. Pages, patterns and code

| ID | Who | Do | You should see | Result |
| --- | --- | --- | --- | --- |
| G-1 | O/C | Pages: click `/userform`, then a line of it. | The outline, and the node's details with a suggested member, e.g. `TextInputControl<…> FirstNameInput => new(this, Locator.ByCss("[data-automation-id='FirstNameInput']"));`. | C: Pass; O: ☐ |
| G-2 | O | **Open file** on a capture. | The `.nodes.jsonl` opens in the editor beside the view. | ☐ |
| G-3 | O | On a page with two captures (Counter, after R-4), **Compare**. | The IDE's diff opens with the two captures, the count's line changed. | ☐ |
| G-4 | O/C | **Patterns** tab; click one. | Patterns by kind with their counts and an example each; the selected one's examples open their page at the node. | C: Pass; O: ☐ |
| G-5 | O/C | **Code** tab; click `UserFormPage`. | The project's page objects (Counter, Home, Login, UserForm, the shell); the click opens the file (O) or asks to (C). | C: Pass; O: ☐ |

## 7. Generating

| ID | Who | Do | You should see | Result |
| --- | --- | --- | --- | --- |
| A-1 | O | VS Code: **Generate page object** on `/counter`. | Copilot Chat opens with `/brinell-scrape-pages Make or update the page object for the recorded page /counter …`, not sent yet. | ☐ |
| A-2 | O | Visual Studio: the same. | The status bar says the prompt is on the clipboard; pasting it in Copilot Chat gives the same text. | ☐ |
| A-3 | C | As an agent over MCP: a page object for a recorded page, from `suggest_controls` and `scraped_header`, then `write_code` and `check_code`. | It builds; `match_controls` names it as the page's page object, not changed. | Pass (run 1) |
| A-4 | O | Optional: send A-1's prompt in agent mode with the `brinell-bdd` MCP server registered. | The agent follows the skill: controls, a page object, `check_code` until it passes. | ☐ |

## 8. Refresh and masks

| ID | Who | Do | You should see | Result |
| --- | --- | --- | --- | --- |
| F-1 | C | Change a recorded page that has a page object (the test site, served from a copy), record it again. | The Pages list says the page object and "changed"; **Changes** lists what was added, removed and changed. | Pass (run 1) |
| M-1 | C | A mask in the config (a product code); record a page that shows it. | The code is not in the `.nodes.jsonl`; its replacement is. | Pass (run 1) |
| M-2 | C | Type into a field, then capture. | What was typed is in no file. | Pass (run 1) |

## 9. Themes

| ID | Who | Do | You should see | Result |
| --- | --- | --- | --- | --- |
| T-1 | O | VS Code: switch between a dark, a light and a high-contrast theme. | The view follows without a reload and stays readable. | ☐ |
| T-2 | O | Visual Studio: **Tools > Theme**, Dark, Light, Blue; then Windows high contrast. | The same. | ☐ |

## 10. Later checks

| ID | Who | Do | You should see | Result |
| --- | --- | --- | --- | --- |
| H-1 | O/C | *(after step 2)* The view's **?**. | What each button and tab does, and a link to the guide. | ☐ |
| B-1 | C | *(after step 3)* **Record** with no Chromium installed (Playwright's browsers folder pointed at an empty one). | The view offers **Install browser** and says why. | ☐ |
| B-2 | O/C | *(after step 3)* **Install browser**. | Progress in the view; Record works after it. | ☐ |

## 11. Results

| Run | By | Build | Result |
| --- | --- | --- | --- |
| 1 | Claude, 2026-09-30 | `move` after `b0caaea3`, with the fixes below | the 20 C and O/C checks that exist today pass (A-3 and F-1 by hand over MCP, the rest by the script); H-1, B-1, B-2 wait for steps 2 and 3 |
| 2 | owner | | |

How run 1 was done: [manual-run/](manual-run/Program.cs). `bridge.py` stands in for an IDE (it serves
the view's page and relays its requests to a real language server); the script drives the view's
page in a headless Chromium and the recording browser over DevTools, which the server opens when
`BRINELL_SCRAPER_DEBUG_PORT` is set. A second run after the fixes passed all 18 scripted checks,
and F-1 in its own run.

What run 1 found, fixed before it passed:

| Check | Found | Fix |
| --- | --- | --- |
| G-4 | A pattern's example opened the right page, then the page open before took its place again: the Pages list reopened its last capture after the example's | Going to an example (or a picked node) forgets the open capture first |
| G-4 | An example that is a wrapper (`div.nav-item` around a link) was not in the outline, so nothing was selected | The outline shows the node asked for (`focus`), wrapper or not |
| R-9 (flaky) | The status could stay at "0 page(s)": the server's notices can arrive out of order, and the start's notice came after the first capture's | The view's recording state only moves forward; the start's answer sets it too |
| A-3 | The first page object did not build: two members named `Element` (the counter's `-` and `+`), and the frame's inputs declared on the page | Suggested names are unique on a page and fall back to a `data-*` value (`Minus`, `Plus`); a node in a frame says its frame member (`scope`), and the view and the skill use it |
