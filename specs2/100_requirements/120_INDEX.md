# Functional Requirements Index

## Overview

This document indexes all functional requirements (FR) for the Brinell UI Testing Framework.

**Last Updated:** January 6, 2026

---

## Requirements Summary

| ID | Title | Priority | Status |
|----|-------|----------|--------|
| FR-001 | [Multi-Platform Support](120_001_MultiPlatformSupport.spx.md) | High | Approved |
| FR-002 | [Control Object Pattern](120_002_ControlObjectPattern.spx.md) | High | Approved |
| FR-003 | [Page Object Pattern](120_003_PageObjectPattern.spx.md) | High | Approved |
| FR-004 | [State Verification](120_004_StateVerification.spx.md) | High | Approved |
| FR-005 | [Waiting and Synchronization](120_005_WaitingSynchronization.spx.md) | High | Approved |
| FR-006 | [Logging and Diagnostics](120_006_LoggingDiagnostics.spx.md) | High | Approved |
| FR-007 | [Platform Automation](120_007_PlatformAutomation.spx.md) | High | Approved |
| FR-008 | [Extensibility](120_008_Extensibility.spx.md) | Medium | Approved |
| FR-009 | [Test Isolation](120_009_TestIsolation.spx.md) | High | Approved |
| FR-010 | [Error Handling](120_010_ErrorHandling.spx.md) | High | Approved |
| FR-011 | [Dependency and Licensing](120_011_DependencyLicensing.spx.md) | Medium | Approved |
| FR-012 | [Container Pattern](120_012_ContainerPattern.spx.md) | High | Approved |
| FR-013 | [Async Pattern](120_013_AsyncPattern.spx.md) | High | Approved |

---

## New Requirements (REVIEW-003 Implementation)

| ID | Title | Priority | Status | Source |
|----|-------|----------|--------|--------|
| FR-014 | [Configuration and Settings](120_014_ConfigurationSettings.spx.md) | High | Draft | REVIEW-003 §1 |
| FR-015 | [Screenshot and Evidence](120_015_ScreenshotEvidence.spx.md) | High | Draft | REVIEW-003 §2 |
| FR-016 | [Test Context Lifecycle](120_016_TestContextLifecycle.spx.md) | High | Draft | REVIEW-003 §4 |
| FR-017 | [Accessibility Testing](120_017_AccessibilityTesting.spx.md) | Low | Draft | REVIEW-003 §8 |
| FR-018 | [Driver Adapter Pattern](120_018_DriverAdapterPattern.spx.md) | High | Draft | User Request |
| FR-019 | [Method Logging Pattern](120_019_MethodLoggingPattern.spx.md) | High | Draft | User Request |
| FR-020 | [Log File Management](120_020_LogFileManagement.spx.md) | High | Draft | User Request |
| FR-021 | [Nullable Parameter Handling](120_021_NullableParameterHandling.spx.md) | High | Draft | User Request |
| FR-022 | [Timeout Handling](120_022_TimeoutHandling.spx.md) | High | Draft | REVIEW-005 §1 |
| FR-023 | [Exception Strategy](120_023_ExceptionStrategy.spx.md) | High | Draft | REVIEW-005 §4 |

---

## Requirements by Category

### Core Patterns

| FR | Description |
|----|-------------|
| FR-002 | Control Object Pattern — UI element abstraction |
| FR-003 | Page Object Pattern — Page abstraction |
| FR-012 | Container Pattern — Element scoping |
| FR-018 | Driver Adapter Pattern — Technology abstraction |

### Verification and Synchronization

| FR | Description |
|----|-------------|
| FR-004 | State Verification — Is*/Wait*/Check*/Assert* |
| FR-005 | Waiting and Synchronization — Wait strategies |
| FR-022 | Timeout Handling — Unified timeout spec |
| FR-021 | Nullable Parameters — Early return on null |

### Logging and Diagnostics

| FR | Description |
|----|-------------|
| FR-006 | Logging and Diagnostics — Structured logging |
| FR-015 | Screenshot and Evidence — Evidence collection |
| FR-019 | Method Logging Pattern — PrepareLog pattern |
| FR-020 | Log File Management — Per-test/per-run logs |

### Error Handling

| FR | Description |
|----|-------------|
| FR-010 | Error Handling — Error messages, retry patterns |
| FR-023 | Exception Strategy — Exception hierarchy |

### Platform Support

| FR | Description |
|----|-------------|
| FR-001 | Multi-Platform Support — MAUI, Blazor, WPF |
| FR-007 | Platform Automation — Driver integration |
| FR-013 | Async Pattern — Blazor async support |

### Test Management

| FR | Description |
|----|-------------|
| FR-009 | Test Isolation — Independent tests |
| FR-014 | Configuration — Settings management |
| FR-016 | Test Context — Context lifecycle |

### Optional/Future

| FR | Description |
|----|-------------|
| FR-008 | Extensibility — Custom controls |
| FR-011 | Dependency and Licensing — MIT license |
| FR-017 | Accessibility Testing — A11y support |

---

## Updated Requirements (Expanded)

The following existing requirements were expanded:

| FR | Additions |
|----|-----------|
| FR-006 | Log levels (FR-006.1.1), Log file management reference (FR-006.5) |
| FR-007 | Mobile-specific (FR-007.2.2), Web-specific (FR-007.3.1), Driver adapter note (FR-007.6) |
| FR-009 | Parallel execution support (FR-009.5) |
| FR-010 | Retry patterns (FR-010.4) |

---

## Legacy References

- **SPEC-006-001-INTERFACES**: Original interface specifications (to be superseded)

---

## Related Documents

- [110_goal/](../110_goal/) — Goal documents (G-001 through G-008)
- [130_quality/](../130_quality/) — Quality requirements (NFR-REL, NFR-MAINT, NFR-COMPAT, etc.)
- [131_performance/](../131_performance/) — Performance requirements
- [132_security/](../132_security/) — Security requirements
- [133_usability/](../133_usability/) — Usability requirements
