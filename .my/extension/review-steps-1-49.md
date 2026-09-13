# Review — steps 1 to 49

A review of the whole programme in [steps.md](steps.md) and
[plan-the-quiet-run.md](plan-the-quiet-run.md): what was delivered, whether it holds up when
checked today, where the record overstates it, and what is left.

**Every number here was measured for this review on 2026-09-13**, not copied from the step
write-ups. Where a step's own record disagrees with what was measured, the measurement wins and
the disagreement is listed in section 4.

---

## 1. Verdict

**The goal holds.** A MAUI suite driven through FlaUI now runs without taking the keyboard, the
pointer or the foreground, and does so by default.

| Measured today | Result |
|---|---|
| MAUI UI suite, nothing set | **297 tests: 281 passed, 16 skipped, 0 failed**, 3 m 10 s |
| Shell area | 15 of 15, three consecutive runs |
| Stress walk (150 page changes, bridge busy throughout) | completes, 0 root re-attachments |
| Root `Brinell.sln` | builds, 0 errors |
| Unit suites: Core, Maui, Generator, Uia, Wpf, WinForms, Automation | **349 passed, 1 skipped, 0 failed** |
| Mobile heads | hub app Android and iOS, Shell app Android: build (the Shell app does not target iOS) |

**But it is not finished, and it is not safe yet.** Eighty-five files of work - 61 modified with
3,371 lines added, 24 new - are uncommitted on top of a commit named `step 24`. The public
documentation describes a switch that no code reads. And one behaviour the fix for the worst
failure relies on is measured but not understood.

---

## 2. The goal, claim by claim

| Claim | State | Evidence |
|---|---|---|
| Never synthesizes a keystroke | **Holds** | Every site funnels through `PhysicalInput.Used`, refused by default for this stack; zero refusals across full runs |
| Never synthesizes pointer input | **Holds** | Same mechanism |
| Never takes the foreground | **Holds, with one known exception** | `WS_EX_NOACTIVATE` took button clicks from two foreground grabs each to zero; `ForegroundWatchdogTests` fails if navigation grabs it. **Launch still grabs once per collection** - Windows gives a new process the foreground - and the watchdog puts it back within milliseconds |
| Stays behind the user's windows | **Holds** | Watchdog keyed on the process from the moment of launch |
| Is the default | **Holds for the MAUI FlaUI stack** | Module initializer; `BRINELL_BACKGROUND_MODE=0` opts out. WPF and WinForms deliberately still default to allowed |
| Verified on a real desktop by a person | **Not since the fix** | The report "the screen still pops over the editor" came before `WS_EX_NOACTIVATE`. Nobody has watched a run since |

---

## 3. Stage by stage

### Stage 0 — groundwork (1-3)

Occluded screenshots, off-screen placement, and the `PhysicalInput` guard with its inventory.
**Solid, and the guard turned out to be the backbone of everything after it.** The inventory is
what made "zero refusals" a measurement rather than a belief.

Worth knowing: step 2's off-screen placement was later measured to break visibility checks -
UI Automation's `IsOffscreen` counts the monitor - so it stays an opt-in and must not become a
default.

### Stage A — the bridge spikes and contract (4-12)

The design question that could have killed the programme - can a custom provider on a child
window join a MAUI app's tree without collapsing it - was answered yes, early and with tests.
**The strongest stage.** `ContractTests` pins GUIDs, the method table and verb numbers by
literal assertion, and `EveryVerb_IsClassifiedByKind` now forces a decision for every new verb.

### Stage B — background execution (13-16)

Focus and text verbs, background mode, parallel collections. **Delivered, but step 15 is still
recorded as "partly"**, blocked on step 36 and on a human check. Both blockers are gone: 36 was
root-caused, and the human check was replaced by the foreground watchdog. The row should close.

### Stage C — gestures (17-19)

Full gesture vocabulary, bound at publish time. **Step 19 is genuinely unfinished**: the
id-addressed gesture control object needs generator work and has not had it.

### Stage D — semantic verbs (20-26)

Dates, scrolling, navigation, state reads, pickers, dialogs, menus. **Broad and well tested, and
it produced the two most important findings of the programme**: MAUI's Picker live-lock (39,
upstream, guarded) and `S_FALSE` not surviving UI Automation (40).

Two design decisions from this stage have held up and should be kept on purpose:

- **Read-back over HRESULT.** Every verb that reports what landed was immune to the `S_FALSE`
  loss; every verb that trusted the HRESULT alone was harmed.
- **A refusal that explains itself returns `S_OK` with the reason in the payload.** Step 43 later
  measured that a failing `Exchange` loses its payload entirely, which made `InvokeMenuItem`'s
  design load-bearing rather than incidental.

### Stage E — hardening (27-30)

Security gating, lifetime, the accessibility audit. **Substantively done, with real corrections
made to the design along the way**: section 8's claim that no string is interpreted as a command,
and no information disclosed, was false and was rewritten.

**Step 30, documentation and AD-008, has not been started**, and section 5 shows that the
documentation it would update is now actively wrong.

### Stage G — the parked list (31-42)

| Step | State today |
|---|---|
| 31 Stepper | **Parked, correctly.** A peer for `MauiStepper` was built and crashed the app; WinUI cannot apply MAUI's template to a subclass. Fully reverted; a different route is written down |
| 32 Shell app | **Closed.** Scope root that nothing carried, plus a flyout driven by chrome guesses |
| 33 Navigation stall | **Closed** |
| 34 Actions do not Try | **Closed** - no `Try` action remains on either interface, checked |
| 35 Timings | **Closed** - a report, deliberately not a gate |
| 36 ReturnToHub flake | **Closed** - a detached page answered `NavigationDepth` for a stack it had left |
| 37, 38, 39, 40 | **Closed** |
| 41 Run-time gate on the app | **Open** |
| 42 Cached bridge vs closed window | **Open** |

### Stage H — untie the knot (43-45)

`BRINELL_E_DECLINED`, the Try removal, and the seven step-36 skips gone. **The outcome is right
and the record overstates one part of it** - see section 4.

### Stage I — the quiet run (46-49)

**Delivered, and the stage where measurement mattered most.** Three plausible fixes were measured
out before the right ones were kept: off-screen placement (broke visibility), a two-way split of
verbs for the audit (half the backlog was noise), and "the app froze" (it had not).

### Stage J — clearing the board

The freeze RCA, Shell, timings, and a long tail of readiness races. **Most of the programme's
remaining flakiness was readiness, not logic**: a page arriving later than a click returns, a
scroll landing later than its verb returns, a window element retired by UI Automation.

---

## 4. Where the record overstates or has drifted

| Row | What it says | What is true |
|---|---|---|
| **15** | "partly - blocked on the flaky step 36, and the human check is not done" | Both blockers are resolved; should be **done** |
| **45** | "`InvokeAnywhere` stops guessing" | `InvokeAnywhere` was not rewritten. The fix was on the other side - pages now answer honestly, so the walk's existing rule works. The outcome stands; the title describes work that did not happen |
| **49** | "Full suite with nothing set: 266 passed" | 281 passed after steps 32 and 35 |
| **1** | "the test became order-dependent in stage B" | It passed in every full run measured today |
| **plan-the-quiet-run §1** | "11 failed", "33 skipped" | Historical baseline, correctly labelled as such - not wrong, but a reader skimming could take it as current |

---

## 5. Defects found by this review

**1. The documented switch does nothing.** `BRINELL_ALLOW_POINTER_INPUT` appears in AD-005, in
`AGENTS.md`, in `docs/architecture/stack.md` and in `docs/guides/troubleshooting.md`. **No code
reads it.** The real switch is `BRINELL_BACKGROUND_MODE`, and since step 49 its absence means
quiet for MAUI. Someone following the troubleshooting guide sets a variable with no effect.

**2. The AppSupport README documents a deleted API.** It still shows
`driver.TryPerformGesture(...)`, removed at step 34. It also does not mention that withdrawn
targets are now retired rather than disconnected.

**3. AD-005 is stale and AD-008 does not exist.** AD-005 describes pointer input as opt-in through
the dead variable; the bridge that now carries most interaction has no decision record at all.

**4. Line endings.** The editing scripts used in the later stages wrote LF into files the
repository keeps as CRLF. `core.autocrlf=true` normalises them on commit, so history is not
affected, but working-copy diffs and some editors will show whole-file churn until they are
committed.

**5. Dead branch.** `BrinellVerbFailure.Describe` still has an `S_FALSE` case that can never
arrive at the client.

---

## 6. Risks and debt

**Uncommitted work is the largest risk.** Everything from step 25 onwards - the security gate,
lifetime fixes, the audit, `BRINELL_E_DECLINED`, the quiet default, the freeze fix - exists only in
the working copy. One bad checkout loses it.

**The freeze fix relies on behaviour nobody understands.** Removing `UiaDisconnectProvider` from
page unload took window-element invalidations from 7-8 to 0, and that is measured. *Why* a single
provider disconnect retires the whole window's element is not known. If a Windows or WinUI
update changes it, the self-healing root will keep runs alive, and `NavigationStressTests`
(opt-in, `BRINELL_STRESS=1`) is the thing that will notice.

**The WPF and WinForms UI suites were not run.** Their unit suites pass, and the quiet default is
scoped so it should not reach them - it is declared by the MAUI FlaUI assembly's module
initializer - but a process that loads both stacks would be quiet for both. Not measured.

**The launch grab remains.** Once per collection, for milliseconds. The watchdog makes it short;
nothing makes it impossible from outside the app.

**`MauiFixture.Open` now carries readiness logic.** It waits for the app to name its top page and
for that page's root to appear. That was necessary, and it is exactly the kind of helper the user
earlier asked not to grow - worth a look before more is added there.

**Timing baseline is one run old,** taken on one machine. Flags against it are indicative until it
has been refreshed from a few runs.

---

## 7. What still needs to be done

In the order it is worth doing.

### Now

1. **Commit.** 85 files, in reviewable pieces if possible: the contract and provider changes
   (43, retire-not-disconnect), the driver (quiet default, watchdog, `NOACTIVATE`, self-healing
   root, readiness), the sample apps, the tests, then the documents.
2. **Watch one run on a real desktop.** The foreground claim was last checked by a person *before*
   the fix that made it true. Start a full run, keep typing in the editor, and confirm nothing
   comes forward except the launch flash.

### Documentation - step 30, now overdue

3. **Remove `BRINELL_ALLOW_POINTER_INPUT`** from AD-005, `AGENTS.md`, `stack.md` and the
   troubleshooting guide, and document `BRINELL_BACKGROUND_MODE` and the quiet default in its place.
4. **Write AD-008** - gestures and semantic actions go through UI Automation - with the three
   admission tests, so the bridge does not become the app's primary automation surface.
5. **Fix the AppSupport README** (`TryPerformGesture`, retire-not-disconnect,
   `UseBrinellGestureBridge` being required in every app - the Shell app went silent without it).
6. **Add a MAUI guide section** on background execution: the quiet default, how to opt out, the
   watchdog, and why off-screen placement is not recommended.

### Correct the record

7. Close row **15**; retitle row **45** to what was done; update row **49**'s count; confirm row
   **1** and close its caveat.

### Remaining steps

8. **Step 41** - measure the run-time gate on the app, not only the test host. The audit's walk
   against an app launched without `BRINELL_UIA_BRIDGE=1` should find nothing.
9. **Step 42** - a second MAUI window in the sample that can be closed on demand, so the
   stale-bridge guard is measured.
10. **Step 31** - the Stepper, by the route written down: resolve through `…Plus` / `…Minus` and a
    bridge `GetState("Value")`, and leave WinUI's styling alone.
11. **Step 19** - the id-addressed gesture control object and its generator work.

### Worth doing, not blocking

12. **Explain the disconnect invalidation** with a minimal repro in `Brinell.Uia.TestHost`, so the
    freeze fix rests on understanding rather than on a measurement alone.
13. **Run the WPF and WinForms UI suites** once, to confirm the quiet default did not reach them.
14. **Refresh the timing baseline** from three or four runs.
15. **Remove the unreachable `S_FALSE` branch** in `BrinellVerbFailure.Describe`.
16. **Upstream reports**: MAUI Picker live-lock (39), and `MauiStepper` rejecting its template in a
    subclass (31).

---

## 8. What the programme did well, and should keep doing

- **Measure before fixing.** Off-screen placement, the two-way verb split and the "freeze" were all
  plausible and all wrong, and each was caught by a measurement before it shipped.
- **Make tests able to go red.** Step 27's first gating test passed for the wrong reason and was
  caught by forcing the failure; the same check exposed a harness that manufactured the leak it was
  looking for at step 28.
- **Park with a name.** Stage G turned "flaky" into numbered, reasoned entries, most of which have
  since closed with a specific cause.

And what to do less of:

- **Declaring done ahead of the evidence.** Row 45 claims a rewrite that did not happen, and it was
  marked done before its five-run bar was met - the bar was met afterwards, but the order was wrong.
- **Interfering with the measurement.** Several runs this session were invalidated by a build
  overwriting binaries mid-run, killed processes, or a log file overwritten by test output. Each
  cost a rerun and one nearly cost a wrong conclusion.
