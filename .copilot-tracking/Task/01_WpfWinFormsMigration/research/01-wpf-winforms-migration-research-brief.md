<!-- markdownlint-disable-file -->
# Research Brief: Migrating Brinell.Wpf and Brinell.WinForms from src to srcnew

**Source:** [01-wpf-winforms-migration-questions.md](../questions/01-wpf-winforms-migration-questions.md)
**Date:** February 22, 2026

## Validated Research Questions

1. **FlaUI Driver Architecture** — How should each platform's FlaUI driver project be structured? Reuse code patterns from `Brinell.Maui.FlaUI` but create fully independent `Brinell.Wpf.FlaUI` and `Brinell.WinForms.FlaUI` projects. No shared FlaUI layer. What element types, driver classes, and locator extensions are needed per platform?

2. **Control Base Class Hierarchy** — How to adapt the generic `TScope` pattern for WPF/WinForms controls? The old controls use non-generic interfaces and return `void`. The new must implement `IControlObject<TScope>`, `IToggleControlObject<TScope>`, etc. with fluent chaining. What base classes are needed (`ControlBase<TScope>`, `ToggleControlBase<TScope>`, `EditableTextControlBase<TScope>`, etc.)?

3. **Context and Page Lifecycle** — How should `WpfTestContext`/`WinFormsTestContext` and `WpfPageBase`/`WinFormsPageBase` be structured? Desktop apps use `Application.Launch()` + window attach. How does this map to the new `ITestContext<TElement>` and `IPageObject` interfaces?

## Agreed Scope

### In Scope
- **WPF:** 13 controls, Context, Pages, Testing base — fully independent `Brinell.Wpf` + `Brinell.Wpf.FlaUI`
- **WinForms:** 16 controls, Context, Pages, Testing base — fully independent `Brinell.WinForms` + `Brinell.WinForms.FlaUI`
- **Platform-specific interfaces:** `IWpfScope<TScope>`, `IWinFormsScope<TScope>`, platform-specific element types
- **Unit tests** for framework code
- **UI tests** ported from existing `samples/Brinell.Samples.*.UITests/`
- **Sample app updates** to reference `srcnew/` (project reference + namespace import changes)

### Out of Scope
- Blazor migration (different driver — Playwright)
- WPF VisualValidation (`ScreenshotCapture.cs`, `ValidationReport.cs`)
- Any changes to `Brinell.Maui.FlaUI` (stays as-is)

## Locked Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| FlaUI sharing | Fully independent per platform | Maximum isolation, simplest project graph |
| Driver pattern | Reuse Maui.FlaUI code patterns in separate projects | Learn from proven implementation, clean per-platform naming |
| Scope hierarchy | Platform-specific `IWpfScope<TScope>` / `IWinFormsScope<TScope>` | Matches established MAUI pattern |
| Implementation order | Framework first → tests → samples | Build foundation, validate later |
| UI test strategy | Port existing sample test suites | 3 WPF + 5 WinForms test classes already exist |
| Sample app changes | Update references + namespace imports | Minimal change approach |

## Priority Order

1. Research: Deep-dive into MAUI architecture, FlaUI driver patterns, old control implementations
2. Plan: Design per-platform driver projects, control hierarchy, context/page classes
3. Implement WPF: `Brinell.Wpf.FlaUI` → `Brinell.Wpf` (Context → Pages → Controls → Testing)
4. Implement WinForms: `Brinell.WinForms.FlaUI` → `Brinell.WinForms` (Context → Pages → Controls → Testing)
5. Tests: Port UI tests from samples
6. Samples: Update project references
7. Cleanup: Remove old `src/Brinell.Wpf`, `src/Brinell.WinForms`, `src/Brinell.FlaUI`

## Key Constraints

- New projects must follow `srcnew/Brinell.Maui` architecture pattern (71+ files, self-contained)
- All controls implement new Core generic interfaces (`IControlObject<TScope>`, etc.)
- FlaUI integration via pattern interfaces (`IRangePatternElement`, `IExpandCollapsePatternElement`, etc.)
- Windows-only TFMs: `net8.0-windows;net9.0-windows;net10.0-windows`
- Shell projects, solution file, and test project shells already exist — fill them, don't recreate
