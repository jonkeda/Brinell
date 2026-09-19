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
- `Confirm(read, done, timeoutMs)` on `ViewBase` and `RootedScopeBase`: the one way a Core
  method waits for its action's effect. It never repeats the action, and returns a
  `Confirmation<T>` (`Confirmed`, `NotConfirmed`, `Replaced`) whose `Failure(...)` reports a
  replaced element as `StaleElementException`.
- MAUI's own interfaces: `IMauiElement` (with `InstanceKey`), `IMauiDriver`, `IMauiElementScope`,
  `IMauiPage`, `IMauiTestContext`, and `MauiElementExtensions` for the geometry and scope helpers
  (AD-010).
- MAUI exceptions: `StaleElementException`, `ElementNotReadyException` (`NotReadyReason`),
  `ScopeNotReadyException` (with `Readiness`), `AppUnavailableException`, raised by both drivers.
- Scope readiness: `ScopeReadiness` / `ScopeReadinessState`, `ProbeReadiness()` on every scope,
  `ProbeContentReadiness(root)` and `AsksParent` for containers.
- Collections: `ItemKey` on every row (`IMauiItemObject.Key`), `IItemStrategy.HasStableIds`,
  `timeoutMs` on `Item(int)`, `FindItem` and `ItemWhere`.
- Near-miss warnings: a call that passed after three or more replacements, or after using more
  than half its budget, logs `LogResult.Warning`. `BRINELL_CALL_LOG` (a folder) makes
  `MauiTestContext` write every call to CSV.
- `tools/Scripts/repeat-run.ps1`: runs a test project N times and summarizes failures by test.
- AD-009 (app bugs stay failures) and AD-010 (MAUI ahead of Core, on purpose).
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
  `CarouselView`, `Expander`, `RatingView`, `DrawingView` and `MediaPlayPauseButton` confirm their
  effect with `Confirm` instead of a nested poll; a timeout carries the last read's error as
  `InnerException`.
- **MAUI calls (AD-004, AD-009; `.my/stale-readiness/`):** every public call on a control,
  page, container, collection or row is one log entry/exit pair and one budget. Only the call's
  poll waits, and each attempt first asks the scope chain (row, collection, page), then finds
  the element again. Actions resolve by polling, then act once. A Core method that throws
  `ElementNotReadyException` has not acted, so it is asked again within the budget.
- MAUI no longer implements Core's `IElement`, `IDriver`, `IElementScope`, `IPageObject`,
  `ITestContext<T>`, `IContainerControl` or `IContainerObject`, and controls no longer derive
  from `ControlObjectBase`. Core itself is unchanged.
- Finds on MAUI elements, drivers and scopes make one attempt; the timeout overloads are gone.
- `IsLoaded()`, `GetTitle()` and `TakeScreenshot(string?)` lose their unused timeouts.
- `ItemContainerBase` is `ItemObjectBase`, and `IMauiItemContainer` is `IMauiItemObject`.
- MAUI throws `ScopeNotReadyException` where it threw `PageLoadException`, and
  `ElementNotReadyException` where a disabled or hidden control used to raise `TimeoutException`.
  The FlaUI driver reports a disabled toolbar or menu item, or one no bridge target answered
  for, as `ElementNotReadyException`.
- `Item(int)`, `this[int]`, `SelectItem` and `ItemWhere` wait within their budget; `TryItem`,
  `TrySelectItem` and `FindItem` answer about now. `ScrollToItem`, `ScrollToEnd`, `ScrollToTop`
  and `WaitForItems` run on one budget (`DefaultWait` unless given), and a long list needs a
  budget to match.
- A row is found again by its item key, and reports `ItemChanged` instead of answering for
  another item.
- `AppRoot` no longer scrolls to look for a control; Shell's flyout still does, within itself.
- `IsExists()` / `IsVisible()` on a control read the element only; they no longer consult the
  page.
- `IItemRootProvider`: `KeyOf(root, position)` and `TryGetItemRoot(ItemKey)`.
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

### Removed
- MAUI: `DriverRootScope` (use `AppRoot`), `Popup.Page => null`, `CanResolveElements`,
  `CreateScopeNotReadyException`, `IsParentReady` / `WaitParentReady`, `WaitContentReady` /
  `WaitContentReadyCore` (use `ProbeContentReadiness`), `IsReady(int?)` (now `IsReady()`), the
  page's `EnsureLoaded`, both `Until` implementations (use `Confirm`), `RunPoll`,
  `WaitVisibleCore`, `EnsureVisible(element, timeout)` and `doEnsureVisible`, and the virtual
  `ViewBase.FindElement()` (override `TryFindElement()` and `NotFound()`).
- `PageReadinessSnapshot` from the MAUI page contract.

### Fixed
- `Brinell.Maui.UITests`' `NavigationDemoPage` looked up by name through an XPath locator the
  FlaUI driver does not support, so its "unreachable by name" probes never looked.
- N/A (initial release)

## [0.1.0] - TBD

- Initial public release
