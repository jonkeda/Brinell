# Architecture Overview

Brinell is organized around stable core abstractions and replaceable platform
drivers.

## Core Contracts

`srcnew/Brinell.Core` owns:

- locators;
- control and page interfaces;
- test context contracts;
- timeout and wait utilities;
- screenshot and artifact services;
- shared test attributes and exceptions.

Core should not know about Appium, FlaUI, Playwright, Selenium, Stride, or UI
framework-specific element types.

## Settings

`srcnew/Brinell.Core/Settings` owns JSON-backed test settings. Settings are
loaded from a configurable root, default file, local override file, scenario
convention file, and explicit files. UAT scenarios attach resolved settings to
the execution context.

## Platform Implementations

Platform packages translate core concepts into driver-specific behavior:

- MAUI controls and page objects in `srcnew/Brinell.Maui`;
- WPF and WinForms through FlaUI;
- HTML and Blazor through Selenium or Playwright;
- Stride through automation hooks and named-pipe style integration;
- native Android through Android-specific driver helpers.

## Tests

Tests live under `testsnew/` and follow three broad styles:

- unit tests: no external app or device required;
- UI tests: require a sample app, browser, Appium server, or desktop UI;
- UAT tests: load `uat.config.md` plus `.uat.md` scenario files.

## Artifacts

Brinell-owned test artifacts should flow through the shared artifact provider:

```text
TestResults/
  <run-id>/
    manifest.json
    summary.md
    suites/
      <suite-name>/
        runner/
        logs/
        screenshots/
        uat/
        traces/
```

See [Testing](testing.md) for run and artifact guidance.
See [Reporting And Artifacts](../guides/reporting-artifacts.md) for the current
folder model and manifest behavior.
