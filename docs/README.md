# Brinell Documentation

Brinell is a multi-platform .NET UI test automation framework. These docs are
the active, curated set: guides for someone *using* Brinell. The decisions and
invariants behind it - what must stay true when *changing* Brinell - are in
[`.docs`](../.docs/README.md).

## Start Here

- [Quick Start](getting-started/quick-start.md)
- [Framework Overview](getting-started/framework-overview.md)
- [Test Writing Guide](guides/test-writing.md)
- [Troubleshooting](guides/troubleshooting.md)

## Samples

- [Todo: one MAUI app, five test tiers](../samples/Todo/README.md): page objects and
  ControlObjects, WireMock, a seeded SQLite database, and a test pyramid traced to user journeys,
  on Windows and Android.
- [One Todo UAT, end to end](../samples/Todo/uat-walkthrough.md): a Markdown scenario from
  `Given` to the running app - phrases, page objects, config and fixture.

## Architecture

- [Architecture Overview](architecture/overview.md)
- [Codebase Structure](architecture/structure.md)
- [Technology Stack](architecture/stack.md)
- [Testing](architecture/testing.md)
- [Architectural Decisions, Invariants, Contracts](../.docs/README.md)

## Controls

- [Control Object Index](controls/index.md)
- [Core Interfaces](controls/interfaces.md)
- [CommunityToolkit.Maui Controls](controls/community-toolkit.md)

## Guides

- [Test Writing](guides/test-writing.md)
- [UAT Template Guide](guides/uat-template-guide.md) - project layout, `uat.config.md`,
  scenario file shape
- [UAT Phrases And Flows](guides/uat-phrases-and-flows.md) - the step vocabulary and how to
  extend it
- [Test Data](guides/test-data.md) - test sets, the `brinell-data` tool, `TestData` in UAT
  files, typed mock bodies (`BRINELL001`)
- [Test Settings](guides/settings.md)
- [Reporting And Artifacts](guides/reporting-artifacts.md)
- [Migration Notes](guides/migration.md)
- [Troubleshooting](guides/troubleshooting.md)

## Platform Guides

- [MAUI](platform-guides/maui.md)
- [Playwright](platform-guides/playwright.md)
- [WinForms](platform-guides/winforms.md)
- [WPF](platform-guides/wpf.md)
- [Stride](platform-guides/stride.md)
- [Native Android](platform-guides/native-android.md)
- [Presenter](platform-guides/presenter.md)

## Run Guides

- [Build And Test](run/build-and-test.md)
- [MAUI](run/MAUI.md)
- [MAUI Android](run/maui-android.md)
- [Playwright](run/Playwright.md)
- [HTML](run/Html.md)
- [WinForms](run/WinForms.md)
- [WPF](run/WPF.md)

## Specs

- [Spec Status](specs/README.md)

## Maintenance Rules

- Keep links relative and valid.
- Use `Brinell.*` namespaces only.
- Mark every command with its working directory.
- Keep guides here; keep decisions and invariants in `.docs/`.
- Do not add generated test artifacts under `docs/` or `.my/reports/`.
