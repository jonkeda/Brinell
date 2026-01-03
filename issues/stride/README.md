# Stride Platform Issues

This folder contains issue documentation for the Brinell.Stride platform.

---

## Issue Index

| Issue | Title | Status | Severity |
|-------|-------|--------|----------|
| [ISSUE-001](ISSUE-001-UI-Mode-Configuration.md) | UI Mode Configuration Causes Test Failures | Identified | High |
| [ISSUE-002](ISSUE-002-Windows-Menu-Popup.md) | Windows Menu Popup During Tests | Resolved | Medium |
| [ISSUE-003](ISSUE-003-Shift-Key-Lock.md) | Shift Key Gets Locked After Test Run | Resolved | High |
| [ISSUE-004](ISSUE-004-Keyboard-Input-Focus.md) | Keyboard Input Requires Window Focus | In Progress | High |
| [ISSUE-005](ISSUE-005-GetForegroundWindow-Unreliable.md) | GetForegroundWindow() Reports False Focus State | In Progress | High |
| [ISSUE-006](ISSUE-006-SetForegroundWindow-Restrictions.md) | SetForegroundWindow() Doesn't Grant Focus | In Progress | High |
| [ISSUE-007](ISSUE-007-Simulated-Input-Threading-Crash.md) | Simulated Input Causes Game Crash | Resolved | Critical |
| [ISSUE-008](ISSUE-008-Tests-Pass-Solo-Fail-Batch.md) | Tests Pass Solo But Fail in Batch | In Progress | High |
| [ISSUE-009](ISSUE-009-Click-To-Focus-Not-Triggering.md) | Click-to-Focus Not Being Executed | In Progress | High |

---

## Summary by Status

### Resolved (3)
- ISSUE-002: Windows Menu Popup
- ISSUE-003: Shift Key Lock
- ISSUE-007: Simulated Input Threading Crash

### In Progress (5)
- ISSUE-004: Keyboard Input Focus
- ISSUE-005: GetForegroundWindow Unreliable
- ISSUE-006: SetForegroundWindow Restrictions
- ISSUE-008: Tests Pass Solo Fail Batch
- ISSUE-009: Click-to-Focus Not Triggering

### Identified (1)
- ISSUE-001: UI Mode Configuration

---

## Test Status

| Metric | Count |
|--------|-------|
| Total Tests | 55 |
| Passing | 45 |
| Failing | 10 |

---

## Root Cause Categories

### Sample App Configuration
- ISSUE-001: UI mode flag controls which UI is available

### Input Simulation / Key Management
- ISSUE-002: Modifier keys triggering Windows menus
- ISSUE-003: Keys remaining pressed after test failure

### Windows Focus Management
- ISSUE-004: Missing focus check before keyboard input
- ISSUE-005: GetForegroundWindow lies about keyboard focus
- ISSUE-006: SetForegroundWindow restrictions prevent focus
- ISSUE-008: Focus unpredictability in batch execution
- ISSUE-009: Click-to-focus code not executing

### Game Engine Threading
- ISSUE-007: Simulated input from wrong thread

---

## Related Documentation

- [PLAN-008-Stride-Update](../plan/PLAN-008-Stride-Update.md)
- [PLAN-008b-Stride-Issues](../plan/PLAN-008b-Stride-Issues.md)
- [PLAN-008c-Input-Issues](../plan/PLAN-008c-Input-Issues.md)
- [PLAN-008d-Remaining-Fixes](../plan/PLAN-008d-Remaining-Fixes.md)
- [PLAN-008e-Simulated-Input-Issues](../plan/PLAN-008e-Simulated-Input-Issues.md)
- [PLAN-008f-Input-Simulation-Root-Cause](../plan/PLAN-008f-Input-Simulation-Root-Cause.md)
