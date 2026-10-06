# Codebase Structure

Working directory for paths in this document: Brinell root.

## Top Level

| Path | Purpose |
| --- | --- |
| `srcnew/` | Active Brinell source projects |
| `testsnew/` | Active unit, UI, and UAT test projects |
| `samples/` | Sample applications used by UI tests and demos |
| `tools/` | Developer tools: `brinell`, the command line (`tools/Brinell.Cli`: the Scraper, test data, control generation, the language and MCP servers), and the IDE extensions |
| `docs/` | Active curated documentation |
| `docs2/` | Preserved previous documentation tree |
| `.my/` | Plans for work in flight, indexed in `.my/README.md` |
| `Brinell.sln` | The solution: every project, in folders by area, each test project beside the one it is named after. Tests and the Presenter also find the repository root by it |

The two Visual Studio extension projects are in the solution but not built by it; they build from
their own `.slnx` with Visual Studio's MSBuild.

## Source Projects

`srcnew/` contains package-producing projects:

- `Brinell.Stride.Automation`
- `Brinell.Bdd` - feature files without a UI: parse, bind, values, tables, callable steps, namespaces, run
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
- `Brinell.Uat` - UAT on top of `Brinell.Bdd`: pages, controls, page scope, apps, the UAT test base
- `Brinell.WinForms`
- `Brinell.Wpf`

## Test Projects

`testsnew/` follows naming by capability:

- `Brinell.*.Tests` for unit and integration-style checks;
- `Brinell.*.UITests` for live UI automation: the UI tests, the page objects, and the platform's
  UAT suite (`uat.config.md`, `Features/`, and `Uat/` with its fixture and scenario test class),
  which drives the same page objects. A UAT run is the filter `Category=UAT`.

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
- Add UAT scenario examples under `Features/` of the matching
  `testsnew/Brinell.*.UITests` project.
- Add test settings code under `srcnew/Brinell.Core/Settings`.
- Add artifact/reporting code under `srcnew/Brinell.Core/Artifacts` unless the
  behavior is platform-specific.
