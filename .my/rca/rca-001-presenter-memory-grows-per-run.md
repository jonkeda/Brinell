# RCA-001: Presenter's memory grows with every run of the Todo suite

Status: **analysed, not fixed**, 2026-09-30. Decided: **option 2**, a process of its own
([plan](rca-001-plan-runner-process.md)), over the option 1 recommended below. Found while building the Narrator mode
([design](../narrator/presenter/design.md), AD-025): after recording the eleven scenes of the Todo
video, Presenter "held about 850 MB".

## Answer

**Yes, it is a leak, but a much smaller one than 850 MB suggests.** Every run of a Todo scenario
leaves its whole load context behind: 102 assemblies that are never unloaded. What that costs:

| Per run of a Todo scenario | Grows by | What it is |
| --- | --- | --- |
| Private memory (what only Presenter can free) | **about 4 MB** | The leaked context's own data, and the 11 assemblies read into memory |
| Working set (the 850 MB figure) | **about 30 MB** | Mostly the same 91 DLL files mapped again: 25 MB of files a run. Backed by the files, so Windows can take it back under pressure |
| Assemblies loaded | 102 | 91 mapped from disk (26 of them ASP.NET Core), 11 read from memory |
| Handles | about 15 | |

The small sample suite (no mock server, no database) does not leak: its context unloads and
every number stays flat. So the leak is in what the Todo suite starts, not in Presenter's run loop.

The 850 MB was the **working set**, which counts the re-mapped files each time. The number that
says "leak" is private memory, and it went from 217 MB to 246 MB over eight runs.

## Measurements

Presenter, eight runs of one scenario each, Speed Fast, no recording (2026-09-30):

| | Todo suite (TOD.01.1) | | Sample suite (Greeting) | |
| --- | --- | --- | --- | --- |
| | Private MB | Working set MB | Private MB | Working set MB |
| Start | 195 | 311 | 170 | 287 |
| Run 1 | 217 | 443 | 178 | 306 |
| Run 2 | 226 | 477 | 180 | 309 |
| Run 4 | 242 | 539 | 183 | 314 |
| Run 6 | 242 | 586 | 181 | 311 |
| Run 8 | 246 | 635 | 182 | 312 |
| 20 s later | 246 | 634 | 182 | 311 |

The first Todo run costs more (about 130 MB of working set): ASP.NET Core, WireMock and SQLite
load for the first time. After that it is a steady 25 to 35 MB of working set a run.

The same session loop without Presenter's window (a test that creates a session, runs it and
disposes it; collected four times after each run):

| Run | Assemblies loaded | Of the sessions: from memory | mapped | files mapped, MB | Managed heap MB | Private MB | Working set MB |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 75 | 0 | 0 | 0 | 2 | 28 | 72 |
| 1 | 277 | 11 | 91 | 25 | 4 | 68 | 253 |
| 2 | 379 | 22 | 182 | 50 | 5 | 72 | 281 |
| 3 | 481 | 33 | 273 | 75 | 7 | 76 | 308 |

Same loop on the sample suite: 88 assemblies after every run, 2 MB managed, flat.

## Cause

A session loads the suite into a **collectible load context** (`BddSessionLoadContext`) and unloads
it when the run ends. A collectible context is only freed when nothing outside it still points into
it. The Todo suite starts two libraries that register themselves with the process and never take
that back:

| Holds the context | Who | Found by |
| --- | --- | --- |
| A timer that prunes the connection pool, one per context | `Microsoft.Data.Sqlite` (`SqliteConnectionFactory`) | The runtime's timer queue after a run |
| `AppDomain.ProcessExit` and `DomainUnload` handlers | `Microsoft.Data.Sqlite` | The events' handler lists after a run |
| Two event sources, held by the runtime while they are registered | ASP.NET Core (`Microsoft-AspNetCore-Server-Kestrel`, `Microsoft.AspNetCore.Hosting`), started by WireMock | `EventSource.GetSources()` after a run |
| **Something more, not found yet** | The mock server (WireMock on Kestrel) | Removing the three above lets a SQLite-only context unload, but not one that started the mock server |

Each run adds one timer, two handlers and two event sources (measured: 1/2/2 after run 1,
6/12/12 after run 6).

Two things make the working set grow faster than the private memory:

1. Every session gets a **new** context, and each maps the suite's third-party DLLs and the 26
   ASP.NET Core assemblies it needs again. The same files, mapped once more per run.
2. Nothing is ever unmapped, because the context never unloads.

**What this is not:** not the run loop, not the recorder and not the tree (the sample suite is
flat through the same code). Not the "read into memory" loading either: that is 11 assemblies a
run, part of the 4 MB, and it is what lets the suite be rebuilt while Presenter is open.

## What we can do

| # | Option | Effect | Cost, risk |
| --- | --- | --- | --- |
| 1 | **Reuse one context per build of the suite.** Keep the session's load context after a run, and use it again for the next run as long as the suite's own assemblies have not changed on disk; make a new one only after a rebuild. | Growth per run goes to about zero. A leak is left only per rebuild (one context, about 30 MB of working set). Runs also start faster: nothing is loaded again. | Medium. The suite's statics then live across runs, as they do in one `dotnet test` process, so it is the same as the gate. Needs a check for "has the suite changed" (the write times of the assemblies read into memory) and a test that a rebuild is picked up. **Expected, not measured yet.** |
| 2 | **Run the session in a process of its own.** Presenter starts a small runner per workspace and drives it; closing it frees everything. | No leak at all, and the whole of AD-025 goes away: `AppContext.BaseDirectory`, native libraries, ASP.NET Core and test data would work as under `dotnet test`. | Large. Stepping, the pacer, the recording target and the results have to cross a process boundary. A design of its own. |
| 3 | **Find and remove what else holds the context**, then clean up after each run (dispose the suite's timers, handlers and event sources). | The context unloads; no leak. | High, and fragile. It needs a heap tool to find the rest (`dotnet-dump` or `dotnet-gcdump`, not installed here), reaches into private fields of .NET, and ASP.NET Core is not built to be unloaded: the next version can add a new holder. The clean-up of the three known holders alone was measured: it changes nothing for the Todo suite. |
| 4 | **Leave it, and say so.** | None. | None. 100 runs cost about 400 MB of private memory and 3 GB of working set that Windows can trim. Restart Presenter in a long session. |

**Recommended: option 1.** It removes the growth per run, which is what a recording session of
many scenes meets, for a contained change in `BddExecutionService` and
`BddRuntimeAssemblyResolver`. Option 2 is the better end state if running a suite inside Presenter
keeps costing fixes like AD-025's; it should not be started for this leak alone. Option 3 is not
worth its risk. Until option 1 is built, option 4 holds: the numbers are small enough that
recording the Todo video (eleven runs) is not a problem.

## Also to correct

The 620 MB and 850 MB figures in the Narrator design, the manual test plan (M16), AD-025 and the
Presenter guide are working set. They overstate the leak by about seven times; this RCA has the
numbers to quote.

## How it was measured

- Presenter: `Get-Process Brinell.Presenter`, private memory and working set after each run,
  eight runs per suite, driven through UI Automation.
- The session loop: a scratch xUnit test over `BddExecutionService.CreateSessionAsync`, counting
  `AppDomain.GetAssemblies()`, the runtime's timer queue, the `AppContext` and `AppDomain` event
  handlers and `EventSource.GetSources()` that belong to a session context. Not committed.
- Which library holds what: the same checks on one library at a time in a collectible context
  (load only; SQLite open and close; mock server start and stop).
