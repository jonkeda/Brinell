# Brinell Documentation

Brinell is a multi-platform .NET UI test automation framework. These docs are
the active, curated set. The previous documentation tree is preserved in
[`docs2`](../docs2/README.md) for historical reference while the new docs are
rebuilt.

## Start Here

- [Quick Start](getting-started/quick-start.md)
- [Framework Overview](getting-started/framework-overview.md)
- [Test Writing Guide](guides/test-writing.md)
- [Troubleshooting](guides/troubleshooting.md)

## Architecture

- [Architecture Overview](architecture/overview.md)
- [Codebase Structure](architecture/structure.md)
- [Technology Stack](architecture/stack.md)
- [Testing](architecture/testing.md)
- [Architectural Decisions](architecture/decisions.md)

## Controls

- [Control Object Index](controls/index.md)
- [Core Interfaces](controls/interfaces.md)

## Guides

- [Test Writing](guides/test-writing.md)
- [UAT Template Guide](guides/uat-template-guide.md)
- [UAT Phrases And Flows](guides/uat-phrases-and-flows.md)
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
- Keep current docs in `docs/`; keep historical material in `docs2/`.
- Do not add generated test artifacts under `docs/` or `.my/reports/`.
