# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `Brinell.Maui.CommunityToolkit` control objects for CommunityToolkit.Maui: `Expander`,
  `AvatarView`, `RatingView`, `DrawingView`, `Popup`, `StateContainer` and `MediaElement`, each
  with a sample page section and Windows UI tests. Routes and platform gaps are in
  `docs/controls/community-toolkit.md`.
- Sample app `ToolkitVerbSink`: answers bridge verbs for toolkit views that AppSupport cannot know.
- Generator shortcuts: a component declares `protected bool? IsPlayingShortcut() =>
  PlayPauseButton.IsPlaying();` and gets the public trio as single calls on the part, with no
  second unit of work around it.
- Ordered comparisons `GreaterThan`, `AtLeast`, `LessThan`, `AtMost` for
  `[GenerateComparisons]`; `RangeControlBase.GetValue` declares all four
  (`WaitValueGreaterThan`, ...).
- `MediaPlayPauseButton` and `MediaTimeLabel` part controls; `MediaElement.Stop`,
  `WaitOpened`, `GetElapsed`, `GetRemaining` and their comparison variants.
- `Until(...)` on `ViewBase` and `RootedScopeBase`: the one way a Core method waits. It polls a
  read of the element the method holds, with no second readiness check or log entry, and keeps
  the last read's exception (`WaitHelper.WaitFor(..., out lastError)`).
- Generator: `[AbsenceTolerant]` on a `Get*Core` reads once with the element resolved
  optionally (`RunGetWithOptionalElement`); `Is*Core` forwards extra parameters like `Get*Core`;
  state shortcuts may take parameters. The CLI prints a warning for a Core method that calls a
  `Run*` helper or a part's public member.
- `AvatarInitials` and `AvatarImage` part controls; `ContentDialog` gains `WaitTitle`,
  `AssertTitle`, `AssertButtonTexts` (sequence), `...HasItem`, `...Count`, `WaitMessage`,
  `AssertMessage`.
- Initial release of Brinell UI testing framework
- `Brinell.Core` - Core abstractions and interfaces
  - `IDriverAdapter` and `IElementAdapter` for platform abstraction
  - `ITestContext` with platform identification
  - Control interfaces: `IButton`, `ICheckBox`, `ITextBox`, etc.
  - Test attributes: `[UITest]`, `[SmokeTest]`, `[Platform]`, `[Priority]`
  - Exception types for common UI test failures
  - Screenshot service abstractions
  - CSV test logging support
  
- `Brinell.Wpf` - WPF application automation
  - FlaUI-based driver and element adapters
  - Control implementations: Button, CheckBox, ComboBox, ListBox, etc.
  - Page Object base class with control discovery
  - Visual validation support
  
- `Brinell.Html` - Web application automation
  - Selenium WebDriver-based implementation
  - HTML control wrappers
  - Support for Chrome, Firefox, Edge browsers
  
- `Brinell.Maui` - Mobile/MAUI automation
  - Appium-based driver implementation
  - MAUI control wrappers
  - Android and iOS support
  
- `Brinell.Mocking` - API mocking
  - WireMock.Net integration
  - Fluent API stub builder
  - Request/response recording

### Changed
- `CommunityToolkit.Maui` 9.0.0 -> 15.0.1 and `Microsoft.Maui.Controls` -> 10.0.90 (required by the
  toolkit). Added `CommunityToolkit.Maui.MediaElement` 10.0.0.
- `Brinell.Maui.CommunityToolkit` no longer references CommunityToolkit.Maui, is centrally
  versioned, and is in `srcnew/Brinell.sln`. `CreateMaui.Bat` generates its controls too.
- `MediaElement<TScope>` moved from `Brinell.Maui.Controls.Media` to
  `Brinell.Maui.CommunityToolkit.Controls.Media`.
- `MediaElement` forwards to its parts instead of polling around them: `SetPlaying` returns
  the media element rather than the page; `WaitProgressPasses` is replaced by
  `WaitProgressGreaterThan`, and `WaitDurationKnown` by `WaitOpened`.
- Every control now does one unit of work per call. `ToggleControlBase`, `Stepper`,
  `Expander`, `RatingView`, `DrawingView` wait with `Until` instead of a nested poll; a timeout
  carries the last read's error as `InnerException`.
- `AvatarView`: the `Text` part is now `Initials` (`AvatarInitials`), and `GetText` /
  `WaitText` / `AssertText` are `GetInitials` / `WaitInitials` / `AssertInitials`. `Image` is an
  `AvatarImage`.
- `StateContainer`: `IsShowing` / `WaitShowing` / `AssertShowing` take a view id only (the
  `Locator` overloads are gone) and are generated; `WaitShowing`'s `expected` is `bool?`.
- `ContentDialog` is a generated template; `GetTitle` and friends take an optional `timeoutMs`,
  and `GetButtonTexts` returns `IReadOnlyList<string>?`.
- `Popup.CloseWith` is hand-written as two calls in sequence; same signature.
- `WebView.GetUrl` / `GetPageTitle` lose an `[AbsenceTolerant]` that had no effect, so their
  behaviour stays as it was now that the attribute works on getters.
- Migrated from Oravey.UITestFramework namespace

### Fixed
- N/A (initial release)

## [0.1.0] - TBD

- Initial public release
