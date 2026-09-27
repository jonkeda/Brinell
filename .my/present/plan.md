# Presenter: open work

Status: **open**, collected 2026-09-27 from the Presenter notes that were still in flight. What
Presenter does today is in [docs/platform-guides/presenter.md](../../docs/platform-guides/presenter.md);
the workspace decision is [AD-018](../../.docs/decisions/ad-018-the-presenter-workspace-is-the-uat-project.md).
The notes these items came from are at
[7162ac7](https://github.com/exact-mobile-apps/test-tooling/tree/7162ac7/.my/present).

| # | Item | Status | Done when |
| --- | --- | --- | --- |
| 1 | App icon (was WP4 of the UI refresh) | todo | the icon shows in the taskbar, Alt-Tab and the title bar |
| 2 | Gesture bridge in Presenter | proposed, not started | [gesture-bridge.md](gesture-bridge.md) is done |
| 3 | Todo workspace in Presenter | to verify | a Todo scenario runs from Presenter |
| 4 | Scenario parity across techs | todo | the baseline scenarios run on every tech, or each gap has a reason |
| 5 | Template doc for a new tech UAT project | todo | a new project can be made from the doc alone |
| 6 | User journeys | not run | every journey in [userjourney.md](userjourney.md) is checked |

## 1. App icon

- Add `Resources/AppIcon/appicon.svg` (base) and `appiconfg.svg` (glyph), and in
  `Brinell.Presenter.csproj`:

  ```xml
  <ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#2563EB" />
    <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#2563EB" BaseSize="128,128" />
  </ItemGroup>
  ```

- Design: one shape that reads at 16 px, no text; a play triangle over the Brinell mark, in the
  accent `#2563EB`. The splash is optional; today WinUI shows a blank window while it starts.
- The app is unpackaged (`WindowsPackageType=None`), so the taskbar, Alt-Tab and title-bar icons
  may not come from `<MauiIcon>`. Check all three. Fallbacks: `<ApplicationIcon>` with a
  generated `.ico`, or `AppWindow.SetIcon` in the `#if WINDOWS` block of `App.xaml.cs`.
- Touches only the csproj and new files, so it can run beside anything else.

## 2. Gesture bridge

Presenter is the one MAUI app in `srcnew/` that is also an app under test, and it has no gesture
bridge. The proposal is in [gesture-bridge.md](gesture-bridge.md).

## 3. Todo workspace

The three things that kept `samples/Todo/tests/Brinell.Samples.Todo.Uat` out of Presenter are
fixed in its `uat.config.md`: `Fixture` is `TodoUatFixture`, `Pages` is the Uat assembly, and
`AppPath` uses `win-x64`. Nobody has run it in Presenter since. Then switch the config to the
`Project` + `Fixture` form (AD-018) and drop the paths.

## 4. Scenario parity

Every tech has scenarios, but they are named and worded per tech, so a difference between
adapters does not show as the same scenario failing on one of them:

| Target | Scenarios today |
| --- | --- |
| MAUI | `main-page-greeting`, `main-page-validation`, `user-form-basic-input`, `main-page-missing-control` |
| STRIDE | `main-page-greeting` |
| WPF | `home-page-visible`, `login-valid-credentials` |
| WinForms | `login-basic-input` |
| Blazor | `counter-basic`, `login-invalid-credentials` |
| HTML | `counter-basic`, `form-controls-basic` |

Target: the same baseline set (`main-page-greeting`, `main-page-validation`,
`user-form-basic-input`, `missing-control-diagnostics`) on MAUI, WPF, WinForms, Blazor, HTML and
Stride where practical; a phrase a tech cannot support is listed as unsupported, with a reason.
The vocabulary is already shared (one catalog), so this is about the sample apps and scenarios.

## 5. Template doc

Every tech UAT project has the same shape (`uat.config.md`, `Scenarios/`, `Runtime/`,
`TestSettings/`, a theory from `UatScenarioTestBase`), but that is only discoverable by reading
one. Write the short "new tech UAT project" section, probably in
[docs/guides/uat-template-guide.md](../../docs/guides/uat-template-guide.md).
