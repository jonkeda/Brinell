# SQL log: results of step 1

Step 1 of [plan.md](plan.md) is built and its five checks pass. The log records every command a
test sends, without values, from three sources: an EF interceptor, an ADO.NET wrapper that also
loads in a .NET Framework app, and SQL Server's Extended Events for an app left unchanged.
`bruinet sql` writes the schema snapshot, runs the capture and writes the report. The legacy
system can now be measured with four commands, as section 1.7 of the plan describes.

## What was built

| Part | Where |
| --- | --- |
| The log, the ADO.NET wrapper, statements, schema snapshot, Extended Events | `srcnew/Bruinet.Mocking.Data` (netstandard2.0, net8.0 to net10.0) |
| The EF interceptor, `UseSqlLog` | `srcnew/Bruinet.Mocking.Data.EntityFrameworkCore` (net10.0: EF Core 10 runs on .NET 10 only) |
| `bruinet sql schema`, `capture start`, `stop`, `read`, `report` | `tools/Bruinet.Cli/Sql/` |
| A clock row in every timings file, a `test` span for every test | `Bruinet.Core.Logging.Timing`, `Bruinet.Uat.TestScopeAttribute` |
| Tests | `Bruinet.Mocking.Data.Tests` (26, SQL Server ones included), `Bruinet.Cli.Tests/Sql` (5), `Bruinet.Mocking.Data.Net48.Tests` (1) |

Todo's two repositories now use plain ADO.NET types, and `TodoDatabase.Wrap` lets a test wrap its
connections; the spike suite logs through `UseSqlLog` and its own `CommandLog` is gone.

## The checks

| # | Check | Result |
| --- | --- | --- |
| L1 | Wrapper and interceptor | Pass: the spike on both heads in parallel, Todo.Web on bUnit (166 commands, every test named) |
| L2 | Nothing changes | Pass: the spike on both heads with the log on, off and in replay; Todo.Web on and off |
| L3 | Extended Events against in-process | Pass: 6 runs, 3 a head; every test the same distinct commands from both |
| L4 | The report | Partly: 6.1% for the tests' own steps (spike 4.9%); 17.7% with the arrange, not 37%; the fan-out right |
| L5 | .NET Framework | Pass: a net48 process over the framework's `System.Data.SqlClient` |

Todo.Web's browser head isn't logged: its host is its own process on SQLite, which has no
Extended Events. That is the out-of-process case the capture is for, on SQL Server.

## What L4 says about the spike's numbers

The spike's two shares were measured wrongly, in opposite directions. Its 4.9% used EF's own
reader duration, which doesn't run from execution to disposal; timed from execution, the
in-process share of a test's own steps is 10% to 16%. Its 37% counted the whole client-side time
of the reset (EF's migration check, building the model, opening connections) as database time;
the server's own time for the reset puts the share with the arrange at 11% to 18%. Which number
matters depends on the question: the client's time is what replay would save; the server's time
is what Extended Events can see on a legacy system.

## What building it found

- **EF's reader duration is not the reader's time.** The interceptor now times each reader from
  its execution itself.
- **The two clocks needed lining up by the start, not the end.** A reset's last write can end
  within a millisecond of a test's window opening. Commands now go to the test they started in,
  and the marker query aligns the server's start times with when this machine sent them.
- **An earlier capture's files stay on the server.** Each capture now writes files of its own
  name, or a later read takes the old events too.
- **SqlClient's `sp_reset_connection`** shows in Extended Events on every pooled connection; it
  is housekeeping, and left out.
- **Objects need the schema.** Without it, aliases (`[p]` in EF's `DELETE FROM [p]`) and system
  procedures counted as objects; with a snapshot, only its objects count.
- **Fan-out comes from a test's own steps.** Counting the arrange made every table the reset
  clears reach every test; on a system whose fixture resets everything, that hides the answer.
- **Literals are values.** A batch sent as text, and constants EF inlines, carry values; the log
  replaces every literal with `?`.
- **Code typed to `SqlConnection` can't be wrapped.** Todo needed its repositories moved to plain
  ADO.NET types; on a legacy system the capture is the route that needs nothing changed.

## Next

The owner measures the legacy system with the four commands of section 1.7 and writes the
numbers to `legacy-measure.md`. Step 2, the package's replay, is decided from them.
