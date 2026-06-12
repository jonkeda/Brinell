# Architectural Decisions

This page records active decisions that should guide new Brinell work.

## AD-001: Core Stays Platform-Neutral

`Brinell.Core` owns contracts and shared utilities. Platform element types stay
in platform projects.

## AD-002: Page Objects Own Structure

Tests should describe user intent. Page objects expose meaningful operations and
controls; they do not leak locator plumbing into test methods.

## AD-003: Controls Own Repeated Interaction Behavior

If the same interaction pattern appears in multiple tests or pages, move it into
a Brinell control or shared platform helper.

## AD-004: Wait For State

Do not fix tests by adding arbitrary sleeps or longer delays. Wait for concrete
UI state, navigation completion, busy sentinel changes, text, visibility,
enabled state, request observation, or another observable condition.

## AD-005: Pointer Input Is Opt-In

Routine actions should use semantic control APIs and UI automation patterns.
Pointer input is only for gesture-only surfaces and stays gated by
`BRINELL_ALLOW_POINTER_INPUT`.

## AD-006: xUnit Assert Only

Use xUnit `Assert`. Do not add FluentAssertions.

## AD-007: Shared Artifact Layout

Screenshots, logs, traces, UAT output, and runner reports should use the shared
`TestResults/<run-id>/suites/<suite>/` layout.
