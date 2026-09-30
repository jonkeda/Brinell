# AD-027: The Scraper Records Into The Project And Calls No Model

The Scraper turns an app's pages into page objects and controls. It is a service with no UI and
no model of its own:

| Decision | What holds |
| --- | --- |
| **One engine behind the language server** | `Brinell.Scraper` records, detects patterns, suggests controls and locators, diffs, writes and builds. `Brinell.Bdd.LanguageServer` serves it to both IDEs (`brinell/scraper/*`) and to agents (MCP). An IDE shows the server's one page (`brinell/scraper/host`) and only relays its requests. |
| **The agent writes; the Scraper checks** | The Scraper calls no model. An agent reaches it through MCP tools; everything the tools do is deterministic and testable without tokens. Generated code counts once `check_code`, the project's own build with its analyzers, passes. |
| **Recordings are files in the UI test project** | A project is scraped when it has a `scraper.config.md`. Its recordings are `Recordings/<session>/` in the project: `recording.json`, an ordered list of events, and one `*.nodes.jsonl` per captured page (one node per line, fields in a fixed order), committed with the code. The index is built in memory; the browser login is under the user's profile, never in the repository. |
| **The code is the registry** | A generated file starts with `// <scraped page=… from=… hash=… />` or `control=…`. What exists, what it was made from, and whether the page changed since are read from the source, not from a store. |
| **Nothing typed, nothing unmasked** | A capture never reads what a user typed (`value`). The config's masks are applied before a snapshot is written or shown. |
| **A platform is a source** | `IScrapeSource` and the snapshot model (`ScrapeSnapshot`, `ScrapeNode`) are in `Brinell.Core.Scraping`, with no platform type. Html (Playwright, with Blazor's controls preferred for a Blazor project) is the first source; it is in the engine until a second one needs a project of its own (AD-026, R3). |

**Why:** the WPF Scraper (a window with its own browser, code preview, log viewer and approval UI,
calling the Copilot SDK itself, with a per-user SQLite corpus) duplicated what an IDE and its
agent already do, and nothing it generated could be reproduced by a teammate. Evidence (at
c4f5a864): [the plan, as built](https://github.com/exact-mobile-apps/test-tooling/blob/c4f5a864/.my/ScraperEx/plan.md),
[the capture spike](https://github.com/exact-mobile-apps/test-tooling/blob/c4f5a864/.my/ScraperEx/spike0/Program.cs).

**Ruled out:**
- a Scraper window or app of its own, and an IDE view that holds logic the other IDE's lacks;
- a model call, a prompt sent, or a model SDK in Brinell's Scraper;
- a corpus database, or recordings outside the project;
- a registry of generated code beside the code;
- recording input values; writing a snapshot before its masks are applied;
- `write_code` outside the project, or into `Recordings/`, `bin/` or `obj/`.

**Broken when:**
- a Scraper feature works in one IDE only, or needs a model to be tested;
- `Brinell.Scraper` references a model SDK, a database or a UI framework;
- a teammate cannot see from the repository what a page object was made from;
- a `*.nodes.jsonl` holds a typed value or an unmasked match of a mask.
