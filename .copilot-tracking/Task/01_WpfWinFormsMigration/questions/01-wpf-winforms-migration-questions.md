<!-- markdownlint-disable-file -->

# Questions: Migrating Brinell.Wpf and Brinell.WinForms from src to srcnew

## Round 1

### 🎯 Research Scope

**What is the main goal of this migration?**

- [X] Port all WPF and WinForms control implementations from `src/` to `srcnew/` following the new generic `TScope` architecture *(Both `srcnew/Brinell.Wpf` and `srcnew/Brinell.WinForms` already exist as shells with placeholder files — the goal is to fill them)*
- [ ] Create new WPF/WinForms implementations from scratch, ignoring old code
- [ ] Create a minimal subset of controls initially, with others deferred
- [ ] Other:

**What does a successful migration outcome look like?**

- [X] Fully functional WPF and WinForms projects in `srcnew/` that build against `Brinell.Core` with FlaUI integration, matching the MAUI migration pattern *(This aligns with how `srcnew/Brinell.Maui` was structured — 71+ files, fully self-contained)*
- [ ] Placeholder projects that compile with partial implementations
- [ ] Just the architecture scaffolding (interfaces/bases) without concrete controls
- [ ] Other:

### 📋 Scope Boundaries

**Which aspects should the migration cover?**

- [X] WPF controls — 13 controls (Button, CheckBox, ComboBox, Label, ListBox, MessageBoxDialog, PasswordBox, ProgressBar, ScrollView, Slider, TabItem, TextBox, TreeView)
- [X] WinForms controls — 16 controls (Button, CheckBox, ComboBox, DataGridView, DateTimePicker, GroupBox, Label, ListBox, NumericUpDown, PasswordBox, ProgressBar, RadioButton, RichTextBox, TabControl, TextBox, TrackBar)
- [X] Context classes — `WpfTestContext`, `WinFormsTestContext` implementing `ITestContext`
- [X] Page base classes — `WpfPageBase`, `WinFormsPageBase` implementing `IPageObject`
- [X] Testing base classes — port `WpfUITestBase`, `WinFormsUITestBase`
- [ ] WPF VisualValidation — `ScreenshotCapture.cs`, `ValidationReport.cs` (180+230 lines)
- [X] Unit tests for the framework code itself
- [X] UI tests against sample apps
- [X] Sample app updates to reference srcnew
- [ ] Other:

**What should the migration explicitly skip?**

- [X] Blazor migration — `srcnew/Brinell.Blazor` also has placeholders but uses Playwright, not FlaUI *(Different driver stack, separate migration)*
- [ ] WPF VisualValidation — defer to a later task
- [ ] Sample app updates
- [ ] No exclusions — comprehensive coverage
- [ ] Other:

### 🔍 Technical Context

**Which FlaUI integration pattern should the new WPF/WinForms projects follow?**

- [X] Reuse `Brinell.Maui.FlaUI` driver infrastructure — `FlaUIMauiDriver` and `FlaUIMauiElement` already implement FlaUI wrapping with UIA pattern support *(These types already exist and handle the FlaUI ↔ new architecture bridge)*
- [X] Create separate `Brinell.Wpf.FlaUI` and `Brinell.WinForms.FlaUI` driver projects specific to each platform
- [ ] Inline FlaUI base classes directly into each platform project (duplicating code)
- [ ] Create a new shared `srcnew/Brinell.FlaUI` library matching the old `src/Brinell.FlaUI`
- [ ] Other:

**How should the element/scope type hierarchy work?**

- [X] Create `IWpfScope<TScope>` / `IWinFormsScope<TScope>` mirroring `IMauiScope<TScope>`, with platform-specific element types wrapping `IElement` *(Follows the established MAUI pattern for consistency)*
- [ ] Reuse `IMauiScope<TScope>` directly for WPF/WinForms (rename to platform-neutral name later)
- [ ] Use `IElement` directly without platform-specific scope interfaces
- [ ] Other:

**Should WPF and WinForms share a common FlaUI base, or be fully independent?**

- [ ] Share a common FlaUI base layer — both use `FlaUI.UIA3` for Windows desktop automation, the control patterns are identical *(WPF and WinForms use the same UIA patterns: Value, Toggle, RangeValue, ExpandCollapse, etc.)*
- [X] Keep fully independent — each platform project self-contained even if it means some duplication
- [ ] Depends on the control — share where patterns match, diverge where they differ
- [ ] Other:

### 🧩 Topic Decomposition

**Which sub-topics should the research cover before implementation?**

- [X] FlaUI driver reuse strategy — Can `FlaUIMauiElement` and `FlaUIMauiDriver` be leveraged, or do WPF/WinForms need their own? *(Critical architectural decision — determines all downstream work)*
- [X] Control base class hierarchy — How to adapt the generic `TScope` pattern for WPF/WinForms controls *(The old controls return `void`, new must return `TScope` for fluent chaining)*
- [X] Context and page lifecycle — How WPF/WinForms apps are launched, attached to, and navigated compared to MAUI *(WPF uses `Application.Launch()` + window attach; WinForms similar but different control tree)*
- [ ] VisualValidation porting strategy — How to handle WPF's `ScreenshotCapture`/`ValidationReport` in the new architecture
- [ ] Test infrastructure — How to set up test fixtures for desktop apps (process management, window discovery)
- [ ] Sample app compatibility — Whether existing sample apps can reference srcnew without changes
- [ ] Other:

### 💡 Assumptions

*List constraints, defaults, or scope decisions inferred from user input and the codebase. The user should correct any that are wrong.*

- [X] The `srcnew/` shell projects (`Brinell.Wpf.csproj`, `Brinell.WinForms.csproj`) already have correct TFMs (`net8.0-windows;net9.0-windows;net10.0-windows`), references to `Brinell.Core`, and FlaUI package references *(Verified from csproj analysis)*
- [X] The `srcnew/Brinell.sln` already includes all WPF/WinForms source and test projects with correct build configurations *(Solution file was read and confirmed)*
- [X] Test shell projects exist in `testsnew/` (`Brinell.Wpf.Tests`, `Brinell.Wpf.UITests`, `Brinell.WinForms.Tests`, `Brinell.WinForms.UITests`) with `GlobalUsings.cs` already importing expected namespaces *(Verified — shells only, no test code)*
- [X] The old `src/Brinell.FlaUI` shared library (13 files) is the primary dependency being replaced — it provides `ControlBase`, `TextControlBase`, `ToggleControlBase`, `SelectorControlBase`, `RangeControlBase`, `PageBase`, `FlaUITestContext`, `FlaUIDriverAdapter` *(All WPF/WinForms controls inherit from these)*
- [X] `srcnew/Brinell.Maui.FlaUI` already has a working FlaUI integration (`FlaUIMauiDriver`, `FlaUIMauiElement` with UIA pattern support for Value, Toggle, RangeValue, ExpandCollapse, ScrollItem) *(5 files, fully functional)*
- [X] After migration, the old `src/Brinell.Wpf`, `src/Brinell.WinForms`, and `src/Brinell.FlaUI` can be removed *(User explicitly asked about this)*
- [ ] None of these — remove all assumptions

### ⚠️ Risks and Concerns

**Are there known risks, past failures, or sensitive areas the research should address?**

- [X] The old controls use non-generic interfaces (`IControlObject`, `IToggleControl`) while new Core uses generic `TScope` versions — this is a fundamental API redesign, not a simple port *(Every control signature changes)*
- [X] The `FlaUIMauiElement` is named and namespaced for MAUI — reusing it for WPF/WinForms may create confusing naming, or it may need to be extracted into a platform-neutral shared project *(Naming matters for API consumers)*
- [ ] FlaUI version compatibility — old and new may reference different FlaUI versions
- [ ] WinForms-specific control patterns (e.g., `DataGridView`, `NumericUpDown`, `DateTimePicker`) may require UIA patterns not yet exposed by `FlaUIMauiElement`
- [ ] Process lifecycle for desktop apps differs from MAUI (no Appium, no device session)
- [ ] No known risks
- [ ] Other:

### 🔎 Suggestions

*Codebase-informed insights discovered during analysis. Check the ones to carry into the research brief.*

**`FlaUIMauiElement` already covers most UIA patterns needed by WPF/WinForms** — see [srcnew/Brinell.Maui.FlaUI/](srcnew/Brinell.Maui.FlaUI/)

- [X] Consider extracting `FlaUIMauiElement` → shared `FlaUIElement` in a platform-neutral `Brinell.Desktop.FlaUI` or similar project, then having both Maui, Wpf, and WinForms reference it *(Avoids duplication while addressing the naming concern)*
- [ ] Keep `FlaUIMauiElement` as MAUI-specific and create parallel `FlaUIWpfElement` / `FlaUIWinFormsElement` that wrap the same FlaUI patterns
- [ ] Dismiss — not relevant to this research
- [ ] Other:

**MAUI control base hierarchy (`ControlBase<TScope>`, `ToggleControlBase<TScope>`, etc.) is platform-agnostic** — see [srcnew/Brinell.Maui/Controls/](srcnew/Brinell.Maui/Controls/)

- [X] Investigate whether the MAUI control base classes (which work against `IMauiElement`) can be generalized or shared across desktop platforms *(If the element interface is unified, controls could be shared too)*
- [ ] Create parallel WPF/WinForms control base hierarchies from scratch
- [ ] Dismiss — MAUI controls are too MAUI-specific to share
- [ ] Other:

**Old `WpfUITestBase` and `WinFormsUITestBase` handle app launch/attach lifecycle** — see [src/Brinell.Wpf/Testing/WpfUITestBase.cs](src/Brinell.Wpf/Testing/WpfUITestBase.cs)

- [X] Review the old test base classes to understand the app-launch lifecycle patterns needed for WPF/WinForms testing *(Desktop apps need process management, window attach, cleanup — different from mobile/web)*
- [ ] Dismiss — test infrastructure is out of scope
- [ ] Other:

**Both old projects share identical FlaUI patterns through `src/Brinell.FlaUI`** — see [src/Brinell.FlaUI/](src/Brinell.FlaUI/)

- [X] The fact that WPF and WinForms share a common FlaUI base in the old architecture strongly suggests they should share a common base in the new one too *(13 shared base class files used by both)*
- [ ] Dismiss — the new architecture should diverge
- [ ] Other:

## Round 2

### ⚡ Conflicts

The following checked answers appear to contradict each other. Please resolve before proceeding.

**Conflict 1: FlaUI sharing vs. independence**

In **Technical Context → "Should WPF and WinForms share a common FlaUI base?"**, you checked:

> - [X] Keep fully independent — each platform project self-contained even if it means some duplication

But in **Suggestions**, you also checked:

> - [X] Consider extracting `FlaUIMauiElement` → shared `FlaUIElement` in a platform-neutral project
> - [X] The fact that WPF and WinForms share a common FlaUI base in the old architecture strongly suggests they should share a common base in the new one too

**How should this be resolved?**

- [X] **Fully independent projects, no shared FlaUI layer** — `Brinell.Wpf.FlaUI` and `Brinell.WinForms.FlaUI` each have their own `FlaUIElement`, `FlaUIDriver` implementations even if code is duplicated. `Brinell.Maui.FlaUI` stays separate. *(Maximum isolation, simplest project graph, but duplicated FlaUI wrapping code)*
- [ ] **Platform-neutral shared FlaUI project** — Extract a `Brinell.FlaUI` (or `Brinell.Desktop.FlaUI`) that provides shared `FlaUIElement` + `FlaUIDriver`, then Maui/Wpf/WinForms all reference it. Each platform project only has thin platform-specific wiring. *(Minimum duplication, but introduces a shared dependency)*
- [ ] **WPF+WinForms share, MAUI stays separate** — Create a shared `Brinell.Desktop.FlaUI` for desktop platforms only. `Brinell.Maui.FlaUI` stays as-is since MAUI also supports mobile (Appium). *(Compromise: desktop platforms share, MAUI retains its own dual-driver story)*
- [ ] Other:

**Conflict 2: Reuse Maui.FlaUI vs. Create separate driver projects**

In **Technical Context → FlaUI integration pattern**, you checked both:

> - [X] Reuse `Brinell.Maui.FlaUI` driver infrastructure
> - [X] Create separate `Brinell.Wpf.FlaUI` and `Brinell.WinForms.FlaUI` driver projects

These are mutually exclusive. Did you mean:

- [X] **Reuse the code patterns from Maui.FlaUI** but place them in separate per-platform driver projects (`Brinell.Wpf.FlaUI`, `Brinell.WinForms.FlaUI`) *(Learn from Maui.FlaUI, build fresh per-platform)*
- [ ] **Literally reference Brinell.Maui.FlaUI** from Wpf/WinForms projects and use `FlaUIMauiElement` directly *(No new driver projects, but MAUI naming leaks into WPF/WinForms)*
- [ ] **Extract shared code from Maui.FlaUI** into a neutral project, then have per-platform thin wrappers *(Best of both — shared driver code, clean per-platform naming)*
- [ ] Other:

### 📐 Follow-up: Expanded Scope

You added Unit tests, UI tests, and Sample app updates to scope. A few clarifying questions:

**What is the priority order for implementation phases?**

- [X] Framework first (Context → Pages → Controls → Testing), then tests, then samples *(Build foundation first, validate later)*
- [ ] Framework + UI tests together (port a control, immediately write a test for it) *(Incremental validation)*
- [ ] Framework only in this task — tests and samples as separate follow-up tasks *(Keep this task focused)*
- [ ] Other:

**For UI tests, should we port the existing sample test suites or write new ones?**

- [X] Port existing tests from `samples/Brinell.Samples.Wpf.UITests/` and `samples/Brinell.Samples.WinForms.UITests/` to `testsnew/` *(3 WPF test classes, 5 WinForms test classes already exist)*
- [ ] Write new focused tests in `testsnew/Brinell.Wpf.UITests/` and `testsnew/Brinell.WinForms.UITests/` that exercise each control type *(Fresh test design following the MAUI test pattern)*
- [ ] Both — port existing and add new coverage for controls not covered by sample tests
- [ ] Other:

**For sample apps, what changes are expected?**

- [X] Update sample UI test projects to reference `srcnew/` instead of `src/` *(Minimal — just change project references, update namespace imports)*
- [ ] Rewrite sample test code to use the new fluent `TScope` API *(The API signatures change fundamentally — existing tests won't compile as-is)*
- [ ] Defer sample updates until the framework is fully validated *(Reduce risk by not changing too many things at once)*
- [ ] Other:
