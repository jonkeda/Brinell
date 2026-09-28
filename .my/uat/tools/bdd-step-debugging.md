# Design: stepping through a scenario (shortcuts and buttons)

Status: **steps 1 (engine) and 2 (Visual Studio) built** 2026-09-28, step 2 not yet seen in Visual
Studio (manual B-16 to B-20); steps 3 (VS Code) and 4 open. Date: 2026-09-28.

Visual Studio (step 2): the commands **Step Over BDD Step**, **Step Into BDD Call** and **Step Out
of BDD Call** on the Debug menu with their chords, enabled from `bdd-paused.json`; the controller
watches that file, opens the paused step's source (the build output's path mapped back under the
`Features` folder) at its line, and sends the preview a `paused` message; the preview's bar sends
`debug {move}`. A move writes `next` (`BddBreakpointStore.SetMove`) and `Debugger.Go`; Continue
clears it and goes; Stop is `Debugger.Stop`. The package also loads when debugging starts, so a
run debugged from Test Explorer can be stepped. The bar was checked in a browser against TOD.01's
rendered preview (colours, which buttons are enabled, the posted message, hiding on resume).

What the engine does (`BddBreakpoints`, tests in `BddBreakpointsTests`):
- **A call is a position of its own.** A `## Step:` call has no step of its own; the run stops
  *at the call* (its line in the feature file, `isCall: true`) before its first step. Step over
  from there runs the call whole; Step into goes inside without running anything (the engine stops
  again at the same moment, one level deeper). A breakpoint on a call's line now stops at the call,
  not at its first inner step.
- **The moves**, from a pause at depth *d*: over stops at a step at depth ≤ *d* or at a call that
  starts at level *d* + 1; out of *d* is over at *d* − 1; into stops at the next position. A move
  for another test process is ignored and left in the file.
- **`bdd-paused.json`** beside the breakpoints file, while paused: `process`, `file`, `line`,
  `depth`, `isCall`, `text`, `calls`, `failed`. The IDE writes the move with that `depth` and
  `process`.
- **Never fails a step.** The files are read and written with a short retry (another process, or a
  virus scan, may hold them for a moment), then given up: a write that failed once made a step
  fail with "Access to the path is denied" (found by the tests, 3 runs in 8).
- The breakpoints file is read at every step while debugging (it was cached by its write time,
  which could miss a move written in the same clock tick).

## Problem

A step breakpoint stops the debugger before a step
([BddExecution.cs](../../../srcnew/Brinell.Bdd/BddExecution.cs), `RunNextAsync`). From there only
C# stepping exists: F10 runs the whole step and then walks the engine's async code, and F5 runs
to the next breakpoint. There is no way to go **one BDD step** at a time, and the preview shows
nothing of where the run is paused.

## Goal

While a debugged run is paused at a step, a tester moves through the scenario in its own terms:
the next step, into a `## Step:` call's steps, out of it, or on to the end. The preview marks
the paused step and offers the same moves as buttons.

## The moves

| Move | Shortcut (VS / VS Code) | Stops before |
| --- | --- | --- |
| **Step over** | Alt+Shift+F10 | the next step at the same call depth: a `## Step:` call runs whole |
| **Step into** | Alt+Shift+F11 | the next step, inside a `## Step:` call's steps when the paused step is a call |
| **Step out** | Ctrl+Shift+F11 | the first step after the current `## Step:` call |
| **Continue** | F5 (the debugger's own) | the next step breakpoint |
| **Stop** | Shift+F5 (the debugger's own) | the run ends; the app is closed |
| **Toggle breakpoint** | Ctrl+K, F9 (exists) | - |

Single chords, one press each. They were free in every scope of Visual Studio 2026 (18.11,
checked through DTE on 2026-09-28); the nearer ones are Visual Studio's own and stay so: Ctrl+F10
is Run To Cursor, Ctrl+F11 Toggle Disassembly, Ctrl+Shift+F10 Set Next Statement, Alt+F10 Apply
Code Changes. Step out cannot be Alt+Shift+F11 (that is Step into), so it takes Ctrl+Shift+F11.
The same chords in VS Code, checked against its default keybindings before they ship. They are
enabled only while a BDD debug run is paused at a step, so outside one they do nothing.

## The buttons

A bar at the top of the preview, shown only while a debugged run is paused:

```text
⏸ Paused before: When I tap Sync  (TOD.01.3, line 54 · in Step: the synced list has todos)
[ ▶ Continue ] [ ⤼ Step over ] [ ↘ Step into ] [ ↗ Step out ] [ ■ Stop ]
```

- The paused step is highlighted in the preview, and the editor's caret goes to its line (the
  step file's line for a call's step).
- The buttons send the same moves as the shortcuts; Step into is disabled when the step is not a
  call, Step out when it is not inside one.
- Stepping into a call opens its step file's preview at the call's first step.

## How it works

- **The engine** reads one more entry in `bdd-breakpoints.json`: a one-shot move,
  `"next": { "mode": "over" | "into" | "out", "depth": 2, "run": "<run id>" }`, written by the IDE.
  `BddBreakpoints.Before` breaks at the next step that fits the move (`over`: depth ≤ the paused
  depth; `into`: any; `out`: depth < the paused depth) and removes the entry. The run id keeps a
  stale move from breaking another run.
- **The IDE** writes the move, then continues the debugger (DTE `Debugger.Go` in VS; the debug
  session's `continue` in VS Code). No new channel into the test process: the file is already
  read while a debugger is attached.
- **The paused step** is known to the IDE from the break: the engine writes it next to the
  breakpoints (`bdd-paused.json`: file, line, call depth, calls), and clears it when the step
  runs. The preview reads it for the bar and the highlight.

## Steps

| # | Step | Done when |
| --- | --- | --- |
| 1 | Engine: the one-shot move and the paused file in `BddBreakpoints`. | Unit tests: over, into and out across a nested call; a stale run id is ignored; the paused file is written and cleared. |
| 2 | Visual Studio: the commands, their chords, the preview's bar and highlight. | Manual checks B-16 to B-20 (new) on TOD.01.3 and TOD.01.2's `## Step:` call. |
| 3 | VS Code: the same commands and bar. | The same checks in VS Code. |
| 4 | The guides and AD-022's list of what the IDEs do. | |

## Open questions

| # | Question | Default |
| --- | --- | --- |
| Q1 | Bare F10/F11, or Ctrl+F10/F11, instead of the single chords above? | Decided 2026-09-28: single chords (Alt+Shift+F10, Alt+Shift+F11, Ctrl+Shift+F11). F10/F11 stay C#'s; Ctrl+F10/F11 are Visual Studio's. |
| Q2 | Should Step over also stop after a failed step (before the next), or only with "break on failure"? | Only with "break on failure", as today. |
