# Codebase Structure

Working directory for paths in this document: Bruinell root.

## Top Level

| Path | Purpose |
| --- | --- |
| `srcnew/` | Active Bruinell source projects |
| `testsnew/` | Active unit, UI, and UAT test projects |
| `samples/` | Sample applications used by UI tests and demos |
| `tools/` | Developer tools: `bruinell`, the command line (`tools/Bruinell.Cli`: the Scraper, test data, control generation, the language and MCP servers), and the IDE extensions |
| `docs/` | Active curated documentation |
| `docs2/` | Preserved previous documentation tree |
| `.my/` | Plans for work in flight, indexed in `.my/README.md` |
| `Bruinell.sln` | The solution: every project, in folders by area, each test project beside the one it is named after. Tests and the Presenter also find the repository root by it |

The two Visual Studio extension projects are in the solution but not built by it; they build from
their own `.slnx` with Visual Studio's MSBuild.

## Source Projects

`srcnew/` contains package-producing projects:

- `Bruinell.Appium` - what the Appium stacks share: Appium's error mapping, `AndroidHost`, `AndroidRecorder`
- `Bruinell.Stride.Automation`
- `Bruinell.Bdd` - feature files without a UI: parse, bind, values, tables, callable steps, namespaces, run
- `Bruinell.Blazor`
- `Bruinell.Core`
- `Bruinell.Html`
- `Bruinell.Html.Playwright`
- `Bruinell.Maui`
- `Bruinell.Maui.Appium`
- `Bruinell.Maui.CommunityToolkit`
- `Bruinell.Maui.FlaUI`
- `Bruinell.Mocking`
- `Bruinell.NativeAndroid` - native Android apps (Views, AndroidX, Material) through Appium, in MAUI's and Html's shape
- `Bruinell.Presenter`
- `Bruinell.Stride`
- `Bruinell.Uat` - UAT on top of `Bruinell.Bdd`: pages, controls, page scope, apps, the UAT test base
- `Bruinell.WinForms`
- `Bruinell.Wpf`

## Test Projects

`testsnew/` follows naming by capability:

- `Bruinell.*.Tests` for unit and integration-style checks;
- `Bruinell.*.UITests` for live UI automation: the UI tests, the page objects, and the platform's
  UAT suite (`uat.config.md`, `Features/`, and `Uat/` with its fixture and scenario test class),
  which drives the same page objects. A UAT run is the filter `Category=UAT`.

## Samples

`samples/` contains sample apps for:

- Blazor;
- MAUI;
- native Android (`Bruinell.Samples.NativeAndroid.App`, a .NET for Android control gallery);
- Stride;
- WinForms;
- WPF;
- shared sample models and helpers.

## Adding Code

- Add shared contracts to `srcnew/Bruinell.Core`.
- Add platform behavior to the matching `srcnew/Bruinell.*` project.
- Add tests to the matching `testsnew/Bruinell.*.Tests` project.
- Add UI coverage to `testsnew/Bruinell.*.UITests` only when a live app or
  platform driver is required.
- Add UAT scenario examples under `Features/` of the matching
  `testsnew/Bruinell.*.UITests` project.
- Add test settings code under `srcnew/Bruinell.Core/Settings`.
- Add artifact/reporting code under `srcnew/Bruinell.Core/Artifacts` unless the
  behavior is platform-specific.
