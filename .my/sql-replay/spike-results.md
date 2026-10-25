# SQL replay spike: results

The spike of [plan.md](plan.md), step 0. In progress: 0.3 and 0.4 are done on Todo.Web; the spike
app (0.1, 0.2), churn (0.5) and the other pass lines are not started.

## Todo.Web on the bUnit head (0.3)

Todo.Web's web suite now runs on either head (`BRINELL_HEAD=bunit`). On bUnit the app's own
composition is the fallback of the context, so its singletons outlive page loads as they do in the
host. The ten journey tests pass on both heads: about 1 s on bUnit, 12 s in the browser.

Two gaps in the bUnit head showed up and are fixed in `Brinell.Html.Bunit`:

- **The page's own `history.back()`.** bUnit's loose mode answered it with nothing, so an app's
  back button stayed put. The context now keeps the circuit's history and goes back in it.
- **An app's own busy script.** Todo.Web says it's busy through `brinellTodo.setBusy`, not
  Brinell's `brinellBusy.set`. `BunitTestContextOptions.BusyScript` names it.

No test needed `@browser`. The two-device scenarios live in other suites and weren't touched.

## The database's share on Todo.Web (0.4)

Measured with timing decorators over the two repositories, three runs one test at a time, the
median per test.

| What | Value |
| --- | --- |
| Median share per test | 6.0% |
| All tests together | 100 ms of database time in 1,681 ms |
| Highest | 50% (`Next_CyclesOpen_InProgress_Done_Open`, 25 ms of 49 ms) |
| Seeding per fixture | 14 ms (empty), 40 ms (three todos) |

So on Todo.Web, P1 fails: replay would save about 6% of the bUnit run. That's because Todo's
database is a local SQLite file, where a call takes about 1 ms. The decision now rests on the
spike app, on SQL Server, where each call crosses a connection.

One finding beside the database: three tests take about 470 ms on bUnit where the others take
10 to 50 ms (the sync, the discard dialog, delete). Most of that is neither the database nor
rendering; it looks like a wait in the page objects or the view models, and is worth a look for
the bUnit head's speed on its own.
