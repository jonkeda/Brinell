# Codebase Structure

Working directory for paths in this document: Brinell root.

## Top Level

| Path | Purpose |
| --- | --- |
| `srcnew/` | Active Brinell source projects |
| `testsnew/` | Active unit, UI, and UAT test projects |
| `samples/` | Sample applications used by UI tests and demos |
| `tools/` | Developer tools such as the scraper |
| `docs/` | Active curated documentation |
| `docs2/` | Preserved previous documentation tree |
| `.my/reports/` | Planning and research notes |
| `Brinell.sln` | Top-level solution, including tools and a selected source/test slice |
| `srcnew/Brinell.sln` | Broad compile solution for many source, sample, and test projects |

Neither solution is a complete inventory. Use the directory lists below when
checking whether a project exists.

## Source Projects

`srcnew/` contains package-producing projects:

- `Brinell.Automation`
- `Brinell.Blazor`
- `Brinell.Core`
- `Brinell.Html`
- `Brinell.Html.Playwright`
- `Brinell.Maui`
- `Brinell.Maui.Appium`
- `Brinell.Maui.CommunityToolkit`
- `Brinell.Maui.FlaUI`
- `Brinell.Mocking`
- `Brinell.NativeAndroid`
- `Brinell.Presenter`
- `Brinell.Stride`
- `Brinell.Uat`
- `Brinell.WinForms`
- `Brinell.Wpf`

## Test Projects

`testsnew/` follows naming by capability:

- `Brinell.*.Tests` for unit and integration-style checks;
- `Brinell.*.UITests` for live UI automation;
- `Brinell.*.Uat.Tests` for markdown scenario execution.

## Samples

`samples/` contains sample apps for:

- Blazor;
- MAUI;
- Stride;
- WinForms;
- WPF;
- shared sample models and helpers.

## Adding Code

- Add shared contracts to `srcnew/Brinell.Core`.
- Add platform behavior to the matching `srcnew/Brinell.*` project.
- Add tests to the matching `testsnew/Brinell.*.Tests` project.
- Add UI coverage to `testsnew/Brinell.*.UITests` only when a live app or
  platform driver is required.
- Add UAT scenario examples under the matching `testsnew/Brinell.*.Uat.Tests`
  project.
- Add test settings code under `srcnew/Brinell.Core/Settings`.
- Add artifact/reporting code under `srcnew/Brinell.Core/Artifacts` unless the
  behavior is platform-specific.
