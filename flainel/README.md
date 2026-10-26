# Flainel

Flainel is for running software as a dark factory: agents write, test and review the code, people
design the line and judge what comes out. Its own part is the specs and the checks on them. A
loop can only run dark when its spec is exact and something other than a person tells it whether
the work meets it. Flainel lives in Bruinet's repository for now and may become its own later.

## What it does

- A spec format in small layers: intent, rules, examples, acceptance, each line with an id.
- Checks on a spec before anything is built: is every rule there, exact, and given an example.
- Checks on the work against the spec: every spec id reached by a test, every test pointing back.
- Bruinet runs the tests; Flainel says which spec lines they prove and which they leave open.

## The idea behind it

Every loop turns plan, build, deploy and feedback, and the build of one loop is many turns of the
loop inside it: tasks in a capability, capabilities in a module. A loop goes dark when its
feedback is an exact check. The specs loop runs across all of them, feeding each plan, so the
spec is where the factory's light stays on longest. The loops are written out in
[.my/loops/](.my/loops/02_loops_generic.md); the research behind the specs is in [.my/specs/](.my/specs/).

## How it grows

It starts very small, on Bruinet's Todo sample, which already has journeys, tiers and a coverage
report. Once Todo's spec and its checks are understood, Flainel moves to Tomorrow, one module at a
time. Nothing is built ahead of a sample that needs it. The plan is [.my/plan.md](.my/plan.md).
