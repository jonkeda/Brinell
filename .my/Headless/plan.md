# Headless: MauiUnit and "in memory" renamed

MauiUnit becomes MauiHeadless, and every name or phrase that means "this head, without a device"
(`InMemory…`, "in memory") becomes Headless. 626 hits of MauiUnit in 115 files, 4 `InMemory` types,
63 "in memory" phrases in 43 files. Nothing is released yet (no tags), so no deprecation path; Tomorrow
references none of it.

## Why the name changes back

Commit 91211700 chose MauiUnit over Headless because "headless" is the browser's word. The owner now
wants Headless: it says what the head is (no device, no window) to someone who doesn't know bUnit, while
MauiUnit reads as a unit-test framework. AD-032 is amended with that line.

## Names, now and after

| Now | After |
| --- | --- |
| `src/MauiUnit/` (PackageId `MauiUnit`, `namespace MauiUnit.*`) | `src/MauiHeadless/`, `MauiHeadless`, `MauiHeadless.*` |
| `src/Bruinet.Maui.MauiUnit/` (PackageId) | `src/Bruinet.Maui.Headless/`  |
| `tests/MauiUnit.Tests`, `.MSTest`, `.NUnit` | `tests/MauiHeadless.Tests`, `.MSTest`, `.NUnit` |
| `tests/Bruinet.Maui.MauiUnit.Tests` | `tests/Bruinet.Maui.Headless.Tests` |
| `samples/MauiUnit.Samples.App` | `samples/MauiHeadless.Samples.App` |
| `samples/Bruinet.Samples.Maui.App.MauiUnit`, `…Maui.ShellApp.MauiUnit`, `samples/Todo/src/Bruinet.Samples.Todo.App.MauiUnit` | `….Headless` |
| `MauiUnitDriver`, `MauiUnitElement`, `MauiUnitAppBridge`, `MauiUnitHead`, `MauiUnitAssertionException`, `MauiUnitRefusals`, `MauiUnitState`, `MauiUnitLocators`, `MauiUnitDialog` | `MauiHeadless…` |
| `CreateMauiUnitOptions`, `IsMauiUnit`, `MauiPlatform.MauiUnit` | `CreateHeadlessOptions`, `IsHeadless`, `MauiPlatform.Headless` |
| `InMemoryPreferences`, `InMemorySecureStorage` (`MauiUnit/Hosting`) | `HeadlessPreferences`, `HeadlessSecureStorage` |
| `BRUINET_HEAD=mauiunit` | `BRUINET_HEAD=headless` |
| `tools/Scripts/test-maui-on-mauiunit.ps1` | `tools/Scripts/test-maui-headless.ps1` |
| `ci.yml`: job `mauiunit-macos`, steps "MAUI suites on MauiUnit" | `maui-headless-macos`, "MAUI suites headless" |
| `samples/Todo/tests/Bruinet.Samples.Todo.UITests/Recordings/mauiunit/`, keys in `reviewed.json` | `Recordings/headless/`, keys rewritten |
| `docs/guides/mauiunit.md` | `docs/guides/maui-headless.md` |
| Test names (`The_context_says_it_runs_on_MauiUnit`, `SamplePagesOnMauiUnitTests`, `Parse_NamesTheHead_TheDeviceUnlessMauiUnit`, …) | `…Headless…` |
| "in memory" where it means the head ("328 pass in memory", "the suites in memory in CI") | "headless" |

## Not renamed

- The web's bUnit head (`BRUINET_HEAD=bunit`, `Bruinet.Html.Bunit`): bUnit is the library's own name.
- The browser's headless option (`BrowserOptions.Headless`, `HtmlTestContextOptions.Headless`, `HEADLESS`):
  the `MauiHeadless` prefix keeps the two apart.
- "In memory" where it means data held in memory, not the head: `Bruinet.Mocking.Data`, the Scraper,
  Todo's unit-test fakes.
- History: past CHANGELOG entries, closed `.my` records, commit messages.

## Steps

Each step leaves the build green and its tests passing, and is one commit.

| # | Step | Check |
| --- | --- | --- |
| 1 | The framework: `src/MauiUnit` → `MauiHeadless` (folder, csproj, PackageId, namespaces, types, `HeadlessPreferences`, `HeadlessSecureStorage`), its three test projects and its sample app; `Bruinet.sln` entries | `dotnet build Bruinet.sln`; `dotnet test tests/MauiHeadless.Tests`, `.MSTest`, `.NUnit` |
| 2 | Bruinet's head: `Bruinet.Maui.MauiUnit` → `Bruinet.Maui.Headless`, its types, `MauiPlatform.Headless`, `IsHeadless`, `CreateHeadlessOptions`; its tests | `dotnet test tests/Bruinet.Maui.Headless.Tests` |
| 3 | The apps' headless builds: the three `*.App.MauiUnit` samples → `*.App.Headless`, and the UI test projects' references to them | `dotnet build` of the MAUI UI and UAT suites, Todo's included |
| 4 | Head selection: `BRUINET_HEAD=headless`, the head parser, skip reasons, `DeviceFact` and `@device` texts | `BRUINET_HEAD=headless dotnet test tests/Bruinet.Maui.UITests` |
| 5 | Scripts and CI: `test-maui-headless.ps1`, `ci.yml`'s jobs and steps, `pack.ps1` if it lists the projects | the script locally; CI green on Windows and macOS |
| 6 | Recordings: `Recordings/mauiunit` → `headless` and `reviewed.json`'s keys; `bruinet sql stale` and `reviewed` still pass | Todo's MAUI suite headless with replay (sql-replay step 8's T1 to T5) |
| 7 | Words: the guide renamed and rewritten, AD-032 amended, the `maui-ui-test` and `maui-control` skills, `AGENTS.md`, `.github/copilot-instructions.md`, `docs/`, a CHANGELOG entry; "in memory" phrases by the rule above | `grep -ri mauiunit` finds only history; links resolve |
| 8 | Whole check: every MAUI suite headless on Windows; the Windows device head once, to see nothing else moved | the suites' counts match before and after (328 pass, 33 skip headless) |

## Decided (the owner, 2026-10-27)

1. Bruinet's project is `Bruinet.Maui.Headless`, as `Bruinet.Html.Bunit` and `Bruinet.Maui.FlaUI` name
   the head after the stack.
2. Types take the prefix `MauiHeadless…` (`MauiHeadlessDriver`), so a search never mixes them with the
   browser's `Headless`.
3. The head's value is `headless`; the web keeps `bunit`; one value per head, no aliases.
4. Todo's `InMemoryTodoRepository` and `InMemoryUserRepository` stay: unit-test fakes, the usual name.
5. The packages `MauiUnit` and `Bruinet.Maui.MauiUnit` are renamed outright with a CHANGELOG line; no
   release shipped them.
6. AD-032's why is the line in "Why the name changes back".

Done 2026-10-29 in one commit: steps 1 to 7 and the headless checks of 8 (51+2+2+4 framework tests, 328 pass and 33 skip in the MAUI suite, Todo 45 pass and 3 skip). Not run: the Windows device head, CI.
