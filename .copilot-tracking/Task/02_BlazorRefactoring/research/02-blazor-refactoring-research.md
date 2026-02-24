<!-- markdownlint-disable-file -->
# Task Research: Blazor Refactoring from src/tests to srcnew/testsnew

Migrate `src/Brinell.Blazor/ControlObject6/` and `tests/Brinell.Blazor.Tests.ControlObject6/` to the new srcnew/testsnew architecture, following the `srcnew/Brinell.Html` layered control pattern.

## Task Implementation Requests

* Update `srcnew/Brinell.Blazor/Brinell.Blazor.csproj` to reference `Brinell.Html` + `Brinell.Html.Playwright`
* Implement `BlazorTestContext` in `srcnew/Brinell.Blazor/Context/` implementing `IHtmlTestContext`
* Implement `BlazorPageObjectBase<TSelf>` in `srcnew/Brinell.Blazor/Pages/`
* Implement `BlazorTestFixtureBase` in `srcnew/Brinell.Blazor/Testing/`
* Migrate 14 controls that have direct Html equivalents as thin inheritors/re-exports
* Implement 5 Blazor-only controls (Audio, Video, Image, IFrame, NavMenu) extending Html base classes
* Create `MockHtmlFactory` in `testsnew/Brinell.Blazor.Tests/` for `IHtmlElement`/`IHtmlScope` mocking
* Migrate 20 unit test files from old async/FluentAssertions to new sync/xunit Assert pattern
* Remove Placeholder.cs files from `srcnew/Brinell.Blazor/` subdirectories

## Scope and Success Criteria

* Scope: `srcnew/Brinell.Blazor/` (source) and `testsnew/Brinell.Blazor.Tests/` (tests). Excludes changes to `Brinell.Html`, `Brinell.Html.Playwright`, `Brinell.Core`, and `testsnew/Brinell.Blazor.UITests/`.
* Assumptions:
  1. `srcnew/Brinell.Blazor/` is greenfield — all subdirectories contain only `Placeholder.cs`
  2. `testsnew/Brinell.Blazor.Tests/` is scaffolded (csproj + GlobalUsings only)
  3. Sync-first architecture — no async APIs; future async migration is a separate task
  4. `PlaywrightHtmlElement` sync-over-async bridge is the accepted pattern
* Success Criteria:
  * All 22 controls implemented in `srcnew/Brinell.Blazor/Controls/`
  * Context, page base, and test fixture implemented
  * Unit tests in `testsnew/Brinell.Blazor.Tests/` cover all controls
  * Project builds and tests pass
  * No Placeholder.cs files remain

## Decisions

Pre-selected recommendations for each genuine fork in the implementation. Accept defaults and proceed to `/task-plan`, or invoke `/task-decide` to review and change selections.

### D1: Where should Blazor-only controls (Audio, Video, Image, IFrame, NavMenu) live?

The subagent research found these are standard HTML elements (`<audio>`, `<video>`, `<img>`, `<iframe>`, `<nav>`), not Blazor-specific. Audio and Video share an identical method pattern (15-18 JS-evaluation methods for HTML5 media API).

- [x] Place in `srcnew/Brinell.Blazor/Controls/` as Blazor-specific controls extending `Brinell.Html` base classes *(per agreed scope — the brief explicitly excludes changes to `Brinell.Html`. These controls can be promoted to `Brinell.Html` later if desired.)*
- [ ] Place in `srcnew/Brinell.Html/Controls/` since they're standard HTML *(requires expanding scope to modify `Brinell.Html`)*
- [ ] Other:

> Evidence: [Control Migration Map](#control-migration-plan)

### D2: How should Blazor-only controls handle JS evaluation (play/pause/seek/volume)?

The old controls use `GetLocator().EvaluateAsync("el => el.play()")` directly on Playwright `ILocator`. The new `IHtmlElement` interface has `GetDomProperty()` (for reading) but no `Evaluate()` or setter method. Reading properties works; **actions and property writes don't**.

- [x] Use `GetDomProperty()` for reads, and add `Evaluate<T>()` + `Evaluate()` to `IHtmlElement` for write/invoke operations *(minimal scope expansion — `Evaluate` is already used internally in 6 places by `PlaywrightHtmlElement`; making it public is a natural extension. Affects 2 files: `IHtmlElement.cs` + `PlaywrightHtmlElement.cs`.)*
- [ ] Defer action methods (play/pause/seek/volume set) — only implement read-only properties for now *(avoids `Brinell.Html` changes but delivers incomplete controls)*
- [ ] Cast `IHtmlElement` to `PlaywrightHtmlElement` in Blazor to access internal `ILocator` *(breaks abstraction, defeats the point of the architecture)*
- [ ] Other:

> Evidence: [JS Evaluation Gap Analysis](#blazor-only-controls-js-evaluation-gap)

### D3: Should Audio/Video share a `MediaControlBase`?

Audio (15 methods) and Video (18 methods) share identical patterns: Play, Pause, Seek, Volume, Mute, Duration, CurrentTime, IsPlaying, IsPaused, IsEnded, Source. The only difference is Video adds `GetPoster()`.

- [x] Create `MediaControlBase<TScope>` in `srcnew/Brinell.Blazor/Controls/` with shared media methods; `AudioControl` and `VideoControl` inherit from it *(eliminates ~30 duplicated methods; follows DRY)*
- [ ] Keep Audio and Video as separate, independent classes *(simpler structure but significant duplication)*
- [ ] Other:

> Evidence: [Blazor-Only Control Methods](#blazor-only-control-methods)

### D4: How should the `testsnew/Brinell.Blazor.Tests/` project reference chain work?

The tests need to mock `IHtmlElement` and `IHtmlScope<TScope>` which are defined in `Brinell.Html`. The current csproj only references `Brinell.Core` + `Brinell.Blazor`.

- [x] Add `Brinell.Html` reference to the test project *(test code needs to mock `IHtmlElement`/`IHtmlScope`/`IHtmlTestContext` — these are the abstraction boundaries. `Brinell.Blazor` depends on `Brinell.Html` so this is already a transitive dependency, making it explicit is cleaner.)*
- [ ] Reference only `Brinell.Blazor` and rely on transitive exposure of `IHtmlElement` *(may work for types but not for test helper construction)*
- [ ] Other:

> Evidence: [Test Migration Patterns](#test-migration-plan)

## Outline

### 1. Source Architecture
### 2. Context and Page Implementation
### 3. Control Migration Plan
### 4. Blazor-Only Controls
### 5. Test Migration Plan
### 6. File Tree Changes

### Potential Next Research

* **Framework-wide async migration** — Separate task for converting all of `srcnew/` from sync to async
  * Reasoning: User is considering this; the `RunWithElement` pattern is already async-conversion-ready
  * Reference: Round 3 of questions document
* **Promote Blazor-only controls to Brinell.Html** — Audio, Video, Image, IFrame, NavMenu are standard HTML elements
  * Reasoning: Subagent research confirmed these use standard HTML5 APIs, not Blazor-specific features
  * Reference: [Control Migration Map](subagent/03-old-controls-migration-map.md)

## Research Executed

### File Analysis

* [srcnew/Brinell.Html/Controls/ControlBase.cs](srcnew/Brinell.Html/Controls/ControlBase.cs) — 247 lines, base class with `RunWithElement` pattern, `TScope` fluent chaining, polling, assertions
* [srcnew/Brinell.Html/Controls/Control.cs](srcnew/Brinell.Html/Controls/Control.cs) — Adds `Click()`, `SendKeys()`, `Clear()`, `ScrollIntoView()`
* [srcnew/Brinell.Html/Controls/ClickableControlBase.cs](srcnew/Brinell.Html/Controls/ClickableControlBase.cs) — Adds `DoubleClick()`, `RightClick()`, `Hover()`
* [srcnew/Brinell.Html/ObjectBase.cs](srcnew/Brinell.Html/ObjectBase.cs) — 47 lines, `Poll()` helper, `DefaultTimeoutMs`/`PollingIntervalMs`
* [srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs](srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs) — 180+ lines, sync-over-async bridge, 30+ method implementations
* [srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs](srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs) — Factory pattern (`CreateAsync`/`ForPage`), lifecycle management, navigation
* [srcnew/Brinell.Html.Playwright/LocatorExtensions.cs](srcnew/Brinell.Html.Playwright/LocatorExtensions.cs) — 14+ strategy mappings, recursive parent resolution
* [srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs](srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs) — Page object base with `Self` CRTP, `IsLoaded`, title/screenshot
* [srcnew/Brinell.Html/Testing/HtmlTestFixtureBase.cs](srcnew/Brinell.Html/Testing/HtmlTestFixtureBase.cs) — `IAsyncLifetime`, `CreateContextAsync` pattern
* [src/Brinell.Blazor/ControlObject6/Controls/AsyncControlObjectBase.cs](src/Brinell.Blazor/ControlObject6/Controls/AsyncControlObjectBase.cs) — 422 lines, old async base with direct Playwright access
* [src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs](src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs) — Old context: `IPage` wrapper, flat timeouts, Console logging
* All 22 old control files in `src/Brinell.Blazor/ControlObject6/Controls/`
* All 3 old interface files in `src/Brinell.Blazor/ControlObject6/Interfaces/`
* [tests/Brinell.Blazor.Tests.ControlObject6/Mocks/MockPlaywrightFactory.cs](tests/Brinell.Blazor.Tests.ControlObject6/Mocks/MockPlaywrightFactory.cs) — 97 lines, Playwright mock factory
* Representative test files: ButtonControlTests, CheckBoxControlTests, AudioControlTests, ImageControlTests

### Code Search Results

* `IHtmlElement` interface members — 17 Html-specific methods + inherited `IElement<IHtmlElement>` (30+ total)
* `RunWithElement` pattern in `ControlBase<TScope>` — confirmed async-ready at [ControlBase.cs L36-48](srcnew/Brinell.Html/Controls/ControlBase.cs#L36)
* `GetDomProperty` implementation in `PlaywrightHtmlElement` — uses `EvaluateAsync("(el, prop) => el[prop]", prop)`, can read media state
* No `Evaluate` method on `IHtmlElement` — gap for media control actions

### Project Conventions

* Standards referenced: Copilot instructions (no Thread.Sleep, no empty catch, sync-first)
* Constructor pattern: `(IHtmlScope<TScope> scope, Locator locator)` or `(IHtmlScope<TScope> scope, string selectorOrId)`
* Fluent chaining: all action/assert methods return `TScope`
* Test conventions: xunit Assert, Moq, no FluentAssertions in testsnew/

## Key Discoveries

### Project Structure

**Layering architecture confirmed:** `Brinell.Core` → `Brinell.Html` → `Brinell.Html.Playwright` forms a three-layer stack. `Brinell.Blazor` should sit alongside `Brinell.Html.Playwright`, depending on both `Brinell.Html` (for base classes/interfaces) and `Brinell.Html.Playwright` (for `PlaywrightTestContext` reuse):

```
Brinell.Core (abstractions)
    ↑
Brinell.Html (Html controls, IHtmlElement, IHtmlTestContext)
    ↑                    ↑
Brinell.Html.Playwright  Brinell.Blazor
(PlaywrightHtmlElement,  (Blazor controls extending Html,
 PlaywrightTestContext)   BlazorTestContext wrapping Playwright)
```

### Implementation Patterns

**The CRTP scope pattern is the central design mechanism.** Every control is generic on `TScope where TScope : IHtmlScope<TScope>`. The scope provides `Self`, `Context`, and `FindElement`. Controls *never* hold elements — they always find through the scope chain. This means:
- Blazor controls that simply inherit Html controls need **zero additional code** for the 17 matching controls
- Blazor-only controls inherit `Control<TScope>` or `ClickableControlBase<TScope>` and add methods via `RunWithElement`
- The entire pattern is async-conversion-ready

**`PlaywrightTestContext` can be reused or wrapped for Blazor.** It already handles full lifecycle (`CreateAsync`), external page wrapping (`ForPage`), iframe scoping (`ForFrame`), navigation, screenshots, and element finding. A `BlazorTestContext` could either wrap it (adding Blazor-specific behavior like SSR detection, SignalR readiness) or delegate to it directly.

### Blazor-Only Controls: JS Evaluation Gap

**Critical finding:** The old Audio/Video/Image controls rely on JS evaluation (`EvaluateAsync`) for actions like `play()`, `pause()`, `seek()`, and property setting (`currentTime`, `volume`). The new `IHtmlElement` interface exposes `GetDomProperty()` for reads but has **no** `Evaluate()` or property setter method.

**Gap scope:**
| Need | Supported by `IHtmlElement`? |
|------|-----|
| Read `el.currentTime`, `el.paused`, `el.duration` | **Yes** — `GetDomProperty()` |
| Read `el.src`, `el.poster`, `el.alt` | **Yes** — `GetDomAttribute()` or `GetDomProperty()` |
| Call `el.play()`, `el.pause()`, `el.load()` | **No** |
| Set `el.currentTime = 30`, `el.volume = 0.5` | **No** |
| Check `el.complete && el.naturalWidth > 0` (image loaded) | **No** (compound expression) |

**Impact:** Adding `Evaluate<T>()` + `Evaluate()` to `IHtmlElement` is a 2-file change (interface + `PlaywrightHtmlElement` implementation). `PlaywrightHtmlElement` already uses `EvaluateAsync` internally in 6 places — making it public is natural. This is flagged as Decision D2.

### Blazor-Only Control Methods

**Audio (15 methods):** Play, Pause, IsPlaying, IsPaused, IsEnded, GetCurrentTime, Seek, GetDuration, GetVolume, SetVolume, IsMuted, Mute, Unmute, GetSource, AssertPlaying/Paused

**Video (18 methods):** Same as Audio + GetPoster, 3 additional assertion variants

**Image (9 methods):** GetSource, GetAltText, IsLoaded, WaitLoaded, GetNaturalWidth/Height, AssertSource/Contains/AltText

**IFrame (11 methods):** GetSource, GetTitle, GetName, GetFrameLocator, ClickInside, FillInside, GetTextInside, ElementExistsInside, WaitForElementInside, AssertSource/Contains/ElementExists

**NavMenu (12 methods):** GetItemCount, GetItems, GetActiveItem, IsActive, NavigateTo, NavigateToIndex, GetItemHref, HasItem, AssertActiveItem/HasItem/ItemCount

### Complete Examples

**New control inheriting Html base (typical for 14 of 17 matching controls):**

```csharp
namespace Brinell.Blazor.Controls;

// Most controls need zero additional code — they ARE the Html controls
// Re-exported from the Blazor namespace for discoverability
public class ButtonControl<TScope> : Html.Controls.Buttons.ButtonControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ButtonControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ButtonControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
```

**Blazor-only control (MediaControlBase example):**

```csharp
namespace Brinell.Blazor.Controls;

public abstract class MediaControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected MediaControlBase(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    protected MediaControlBase(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    public TScope Play() => RunWithElement(e => e.Evaluate("el => el.play()"));
    public TScope Pause() => RunWithElement(e => e.Evaluate("el => el.pause()"));
    
    public bool IsPlaying() => RunWithElement(e => 
        !bool.Parse(e.GetDomProperty("paused") ?? "true"));
    
    public double GetCurrentTime() => RunWithElement(e => 
        double.Parse(e.GetDomProperty("currentTime") ?? "0"));
    
    public TScope Seek(double seconds) => RunWithElement(e => 
        e.Evaluate($"el => el.currentTime = {seconds}"));
    
    public double GetDuration() => RunWithElement(e => 
        double.Parse(e.GetDomProperty("duration") ?? "0"));
    
    public double GetVolume() => RunWithElement(e => 
        double.Parse(e.GetDomProperty("volume") ?? "1"));
    
    public TScope SetVolume(double volume) => RunWithElement(e => 
        e.Evaluate($"el => el.volume = {volume}"));
    
    public bool IsMuted() => RunWithElement(e => 
        bool.Parse(e.GetDomProperty("muted") ?? "false"));
    
    public TScope Mute() => RunWithElement(e => e.Evaluate("el => el.muted = true"));
    public TScope Unmute() => RunWithElement(e => e.Evaluate("el => el.muted = false"));
    
    public string? GetSource() => RunWithElement(e => e.GetDomProperty("src"));
}
```

**New unit test pattern (mock at IHtmlElement level):**

```csharp
public class ButtonControlTests
{
    private readonly Mock<IHtmlTestContext> _mockContext;
    private readonly Mock<IHtmlElement> _mockElement;
    private readonly TestPage _page;

    public ButtonControlTests()
    {
        _mockContext = new Mock<IHtmlTestContext>();
        _mockElement = new Mock<IHtmlElement>();
        _mockContext.Setup(c => c.Timeouts).Returns(new TimeoutSettings());
        _mockContext.Setup(c => c.FindElement(It.IsAny<Locator>())).Returns(_mockElement.Object);
        _page = new TestPage(_mockContext.Object);
    }

    [Fact]
    public void Click_CallsElementClick()
    {
        _page.TestButton.Click();
        _mockElement.Verify(e => e.Click(), Times.Once);
    }

    [Fact]
    public void IsExists_WhenElementFound_ReturnsTrue()
    {
        _mockContext.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns(_mockElement.Object);
        Assert.True(_page.TestButton.IsExists());
    }

    private sealed class TestPage : HtmlPageObjectBase<TestPage>
    {
        public TestPage(IHtmlTestContext context) : base(context) { }
        public ButtonControl<TestPage> TestButton => new(this, "test-btn");
    }
}
```

## Technical Scenarios

### 1. Source Architecture

**Description:** How `srcnew/Brinell.Blazor/` layers on `srcnew/Brinell.Html/` — project references, dependency graph, namespace mapping.

**Requirements:**

* `Brinell.Blazor` depends on `Brinell.Html` for base classes and interfaces
* `Brinell.Blazor` depends on `Brinell.Html.Playwright` for `PlaywrightTestContext` reuse
* Blazor controls inherit from Html controls where equivalents exist
* Blazor-only controls extend Html base classes (`Control<TScope>`, `ClickableControlBase<TScope>`)

**Preferred Approach:**

Update `srcnew/Brinell.Blazor/Brinell.Blazor.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Brinell.Blazor</RootNamespace>
    <Description>Blazor UI testing automation using Playwright, extending Brinell.Html</Description>
    <PackageId>Brinell.Blazor</PackageId>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Brinell.Html\Brinell.Html.csproj" />
    <ProjectReference Include="..\Brinell.Html.Playwright\Brinell.Html.Playwright.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" />
  </ItemGroup>
</Project>
```

**Namespace mapping:**

| Old namespace | New namespace | Purpose |
|---|---|---|
| `Brinell.Blazor.ControlObject6.Controls` | `Brinell.Blazor.Controls` | Controls |
| `Brinell.Blazor.ControlObject6.Context` | `Brinell.Blazor.Context` | Test context |
| `Brinell.Blazor.ControlObject6.Interfaces` | *(removed — use `Brinell.Html.Interfaces`)* | Interfaces |
| `Brinell.Blazor.ControlObject6.Pages` | `Brinell.Blazor.Pages` | Page objects |

```text
srcnew/Brinell.Blazor/
├── Brinell.Blazor.csproj          (updated references)
├── Context/
│   └── BlazorTestContext.cs       (implements IHtmlTestContext, wraps PlaywrightTestContext)
├── Controls/
│   ├── ButtonControl.cs           (inherits Html.Controls.Buttons.ButtonControl<TScope>)
│   ├── CheckBoxControl.cs         (inherits Html.Controls.Toggle.CheckBoxControl<TScope>)
│   ├── ... (14 more inherited controls)
│   ├── MediaControlBase.cs        (new base for Audio/Video)
│   ├── AudioControl.cs            (Blazor-only, extends MediaControlBase)
│   ├── VideoControl.cs            (Blazor-only, extends MediaControlBase)
│   ├── ImageControl.cs            (Blazor-only, extends ControlBase)
│   ├── IFrameControl.cs           (Blazor-only, extends ControlBase)
│   └── NavMenuControl.cs          (Blazor-only, extends ControlBase)
├── Pages/
│   └── BlazorPageObjectBase.cs    (inherits HtmlPageObjectBase<TSelf>)
└── Testing/
    └── BlazorTestFixtureBase.cs   (inherits HtmlTestFixtureBase)
```

---

### 2. Context and Page Implementation

**Description:** How `BlazorTestContext` and `BlazorPageObjectBase` map to the new architecture.

**Requirements:**

* `BlazorTestContext` implements `IHtmlTestContext`
* Reuse `PlaywrightTestContext` for browser lifecycle and element finding
* Preserve Blazor-specific capabilities (SSR detection, SignalR readiness) as extension points
* `BlazorPageObjectBase` inherits `HtmlPageObjectBase<TSelf>`

**Preferred Approach:**

```csharp
// Context — wraps PlaywrightTestContext, adds Blazor-specific behavior
namespace Brinell.Blazor.Context;

public sealed class BlazorTestContext : IHtmlTestContext, IAsyncDisposable
{
    private readonly PlaywrightTestContext _inner;

    private BlazorTestContext(PlaywrightTestContext inner) => _inner = inner;

    public static async Task<BlazorTestContext> CreateAsync(HtmlTestContextOptions options)
        => new(await PlaywrightTestContext.CreateAsync(options));

    public static BlazorTestContext ForPage(IPage page, HtmlTestContextOptions? options = null)
        => new(PlaywrightTestContext.ForPage(page, options));

    // Delegate all IHtmlTestContext members to _inner
    public IHtmlTestContext Context => this;
    public TimeoutSettings Timeouts => _inner.Timeouts;
    public ITestLogger Logger => _inner.Logger;
    public string CurrentUrl => _inner.CurrentUrl;
    public string PageTitle => _inner.PageTitle;
    // ... all other delegates

    // Blazor-specific extensions
    public void WaitForBlazorReady(int? timeoutMs = null) { /* poll for _blazor !== undefined */ }
}
```

```csharp
// Page base — thin wrapper for Blazor namespace
namespace Brinell.Blazor.Pages;

public abstract class BlazorPageObjectBase<TSelf> : HtmlPageObjectBase<TSelf>
    where TSelf : BlazorPageObjectBase<TSelf>
{
    protected BlazorPageObjectBase(IHtmlTestContext context) : base(context) { }
}
```

---

### 3. Control Migration Plan

**Description:** Detailed mapping of all 22 old controls to new implementations.

**14 controls with direct Html equivalents** — these inherit directly:

| # | Old Control | Old Base | New Blazor Class | Inherits From |
|---|---|---|---|---|
| 1 | `ButtonControl` | `AsyncClickableControlBase` | `ButtonControl<TScope>` | `Html.Controls.Buttons.ButtonControl<TScope>` |
| 2 | `InputControl` | `AsyncTextControlBase` | `TextInputControl<TScope>` | `Html.Controls.Text.TextInputControl<TScope>` |
| 3 | `TextAreaControl` | `AsyncTextControlBase` | `TextAreaControl<TScope>` | `Html.Controls.Text.TextAreaControl<TScope>` |
| 4 | `CheckBoxControl` | `AsyncClickableControlBase` | `CheckBoxControl<TScope>` | `Html.Controls.Toggle.CheckBoxControl<TScope>` |
| 5 | `RadioButtonControl` | `AsyncClickableControlBase` | `RadioButtonControl<TScope>` | `Html.Controls.Toggle.RadioButtonControl<TScope>` |
| 6 | `LinkControl` | `AsyncClickableControlBase` | `LinkControl<TScope>` | `Html.Controls.Buttons.LinkControl<TScope>` |
| 7 | `SelectControl` | `AsyncClickableControlBase` | `SelectControl<TScope>` | `Html.Controls.Selection.SelectControl<TScope>` |
| 8 | `DateInputControl` | `AsyncClickableControlBase` | `DateInputControl<TScope>` | `Html.Controls.DateTime.DateInputControl<TScope>` |
| 9 | `TimeInputControl` | `AsyncClickableControlBase` | `TimeInputControl<TScope>` | `Html.Controls.DateTime.TimeInputControl<TScope>` |
| 10 | `RangeControl` | `AsyncClickableControlBase` | `RangeInputControl<TScope>` | `Html.Controls.Range.RangeInputControl<TScope>` |
| 11 | `ListControl` | `AsyncControlObjectBase` | `ListControl<TScope>` | `Html.Controls.Collection.ListControl<TScope>` |
| 12 | `TableControl` | `AsyncControlObjectBase` | `TableControl<TScope>` | `Html.Controls.Collection.TableControl<TScope>` |
| 13 | `ProgressControl` | `AsyncControlObjectBase` | `ProgressControl<TScope>` | `Html.Controls.Display.ProgressControl<TScope>` |
| 14 | `TabControl` | `AsyncControlObjectBase` | `TabContainerControl<TParent, TScope>` | `Html.Controls.Container.TabContainerControl<TParent, TScope>` |

**5 Blazor-only controls** — new implementations:

| # | Old Control | Old Base | New Blazor Class | Extends |
|---|---|---|---|---|
| 15 | `AudioControl` | `AsyncClickableControlBase` | `AudioControl<TScope>` | `MediaControlBase<TScope>` (new) |
| 16 | `VideoControl` | `AsyncClickableControlBase` | `VideoControl<TScope>` | `MediaControlBase<TScope>` (new) |
| 17 | `ImageControl` | `AsyncClickableControlBase` | `ImageControl<TScope>` | `ClickableControlBase<TScope>` |
| 18 | `IFrameControl` | `AsyncControlObjectBase` | `IFrameControl<TScope>` | `ControlBase<TScope>` |
| 19 | `NavMenuControl` | `AsyncControlObjectBase` | `NavMenuControl<TScope>` | `ControlBase<TScope>` |

**New shared base:**

| Class | Extends | Purpose |
|---|---|---|
| `MediaControlBase<TScope>` | `ClickableControlBase<TScope>` | Shared Play/Pause/Seek/Volume/Mute/Duration/Source for Audio+Video |

---

### 4. Blazor-Only Controls: JS Evaluation Gap

**Description:** The 5 Blazor-only controls need JavaScript evaluation capabilities not currently exposed on `IHtmlElement`.

**Requirements:**

* Audio/Video need `el.play()`, `el.pause()`, `el.currentTime = X`, `el.volume = X`, `el.muted = true/false`
* Image needs `el.complete && el.naturalWidth > 0` (compound JS expression)
* IFrame needs `FrameLocator` for cross-frame interaction (Playwright-specific)

**Preferred Approach** (per D2):

Add to `IHtmlElement`:

```csharp
// In srcnew/Brinell.Html/Interfaces/IHtmlElement.cs
T? Evaluate<T>(string expression);
void Evaluate(string expression);
```

Add to `PlaywrightHtmlElement`:

```csharp
// In srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs
public T? Evaluate<T>(string expression)
    => _locator.EvaluateAsync<T>(expression).GetAwaiter().GetResult();

public void Evaluate(string expression)
    => _locator.EvaluateAsync(expression).GetAwaiter().GetResult();
```

**IFrame special case:** The `IFrameControl` uses Playwright's `FrameLocator` for cross-frame interaction. Options:
1. Add `ForFrame()` capability to `IHtmlElement` (similar to how `PlaywrightTestContext.ForFrame` works)
2. Implement IFrame methods using `Evaluate` with full JS expressions that cross frames
3. Access the Playwright `ILocator` through `Evaluate` to get `FrameLocator`

Recommend option 1: add `IHtmlElement ForFrame(string selector)` or similar to `IHtmlElement`. This is a natural HTML concept (iframes) and keeps things clean. But this can be deferred — implement IFrame with `Evaluate` initially.

#### Considered Alternatives

* **Read-only only:** Implement just `GetDomProperty`-based reads, skip actions. Delivers incomplete controls.
* **Cast to PlaywrightHtmlElement:** Breaks abstraction; makes async migration harder.
* **Add `SetDomProperty` + `InvokeMethod`:** More methods, narrower scope — `Evaluate` is simpler and more flexible.

---

### 5. Test Migration Plan

**Description:** Converting 20 old test files from async/FluentAssertions/Playwright-mocking to sync/xunit/IHtmlElement-mocking.

**Requirements:**

* Mock at `IHtmlElement`/`IHtmlScope<TScope>` level (not Playwright)
* Use xunit `Assert.*` (not FluentAssertions `.Should()`)
* All methods synchronous (not async Task)
* Constructor pattern: controls via page objects with `(scope, selectorOrId)`

**Preferred Approach:**

1. **Create `MockHtmlFactory`** — analogous to old `MockPlaywrightFactory` but for `IHtmlElement`/`IHtmlTestContext`:

```csharp
namespace Brinell.Blazor.Tests.Mocks;

public static class MockHtmlFactory
{
    public static Mock<IHtmlTestContext> CreateMockContext()
    {
        var mock = new Mock<IHtmlTestContext>();
        mock.Setup(c => c.Timeouts).Returns(new TimeoutSettings());
        mock.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.Css);
        mock.Setup(c => c.Context).Returns(() => mock.Object);
        return mock;
    }

    public static Mock<IHtmlElement> CreateMockElement(
        string? text = "Test Text",
        bool visible = true,
        bool enabled = true,
        bool isChecked = false)
    {
        var mock = new Mock<IHtmlElement>();
        mock.Setup(e => e.Text).Returns(text);
        mock.Setup(e => e.Visible).Returns(visible);
        mock.Setup(e => e.Enabled).Returns(enabled);
        mock.Setup(e => e.IsChecked).Returns(isChecked);
        mock.Setup(e => e.InputValue).Returns(text ?? "");
        return mock;
    }

    public static void SetupFindElement(Mock<IHtmlTestContext> context, Mock<IHtmlElement> element)
    {
        context.Setup(c => c.FindElement(It.IsAny<Locator>())).Returns(element.Object);
        context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns(element.Object);
    }
}
```

2. **Conversion pattern** for each old test:

| Old Pattern | New Pattern |
|---|---|
| `var mockPage = MockPlaywrightFactory.CreateMockPage()` | `var mockContext = MockHtmlFactory.CreateMockContext()` |
| `var mockLocator = MockPlaywrightFactory.CreateMockLocator()` | `var mockElement = MockHtmlFactory.CreateMockElement()` |
| `MockPlaywrightFactory.SetupLocator(mockPage, mockLocator)` | `MockHtmlFactory.SetupFindElement(mockContext, mockElement)` |
| `var context = new BlazorTestContext(mockPage.Object)` | `var page = new TestPage(mockContext.Object)` |
| `var button = new ButtonControl(context, "btn", null)` | `page.TestButton` (defined in TestPage) |
| `await button.ClickAsync()` | `page.TestButton.Click()` |
| `mockLocator.Verify(l => l.ClickAsync(...))` | `mockElement.Verify(e => e.Click())` |
| `result.Should().BeTrue()` | `Assert.True(result)` |
| `result.Should().Be("text")` | `Assert.Equal("text", result)` |
| `result.Should().Contain("text")` | `Assert.Contains("text", result)` |

3. **Test file mapping** (20 files):

| Old Test File | New Test File | Notes |
|---|---|---|
| `Controls/ButtonControlTests.cs` | `Controls/ButtonControlTests.cs` | Direct conversion |
| `Controls/CheckBoxControlTests.cs` | `Controls/CheckBoxControlTests.cs` | Direct conversion |
| `Controls/AudioControlTests.cs` | `Controls/AudioControlTests.cs` | Uses Evaluate mocks |
| `Controls/VideoControlTests.cs` | `Controls/VideoControlTests.cs` | Uses Evaluate mocks |
| `Controls/ImageControlTests.cs` | `Controls/ImageControlTests.cs` | Uses GetDomProperty mocks |
| `Controls/IFrameControlTests.cs` | `Controls/IFrameControlTests.cs` | Complex — frame mocking |
| `Controls/NavMenuControlTests.cs` | `Controls/NavMenuControlTests.cs` | FindElements mocking |
| `Controls/DateInputControlTests.cs` | `Controls/DateInputControlTests.cs` | Direct conversion |
| `Controls/TimeInputControlTests.cs` | `Controls/TimeInputControlTests.cs` | Direct conversion |
| `Controls/InputControlTests.cs` | `Controls/TextInputControlTests.cs` | Renamed |
| `Controls/TextAreaControlTests.cs` | `Controls/TextAreaControlTests.cs` | Direct conversion |
| `Controls/LinkControlTests.cs` | `Controls/LinkControlTests.cs` | Direct conversion |
| `Controls/ListControlTests.cs` | `Controls/ListControlTests.cs` | Direct conversion |
| `Controls/RadioButtonControlTests.cs` | `Controls/RadioButtonControlTests.cs` | Direct conversion |
| `Controls/RangeControlTests.cs` | `Controls/RangeInputControlTests.cs` | Renamed |
| `Controls/SelectControlTests.cs` | `Controls/SelectControlTests.cs` | Direct conversion |
| `Controls/TableControlTests.cs` | `Controls/TableControlTests.cs` | Direct conversion |
| `Controls/TabControlTests.cs` | `Controls/TabContainerControlTests.cs` | Renamed |
| `Controls/ProgressControlTests.cs` | `Controls/ProgressControlTests.cs` | Direct conversion |
| `Context/BlazorTestContextTests.cs` | `Context/BlazorTestContextTests.cs` | Rewrite for new context |

Plus new files:
- `Mocks/MockHtmlFactory.cs` — new mock helper
- `GlobalUsings.cs` — update with Blazor-specific usings

---

### 6. File Tree Changes

**Files to create in `srcnew/Brinell.Blazor/`:**

```text
srcnew/Brinell.Blazor/
├── Context/
│   └── BlazorTestContext.cs                    (new — replaces Placeholder.cs)
├── Controls/
│   ├── ButtonControl.cs                        (new — inherits Html)
│   ├── CheckBoxControl.cs                      (new — inherits Html)
│   ├── DateInputControl.cs                     (new — inherits Html)
│   ├── LinkControl.cs                          (new — inherits Html)
│   ├── ListControl.cs                          (new — inherits Html)
│   ├── ProgressControl.cs                      (new — inherits Html)
│   ├── RadioButtonControl.cs                   (new — inherits Html)
│   ├── RangeInputControl.cs                    (new — inherits Html)
│   ├── SelectControl.cs                        (new — inherits Html)
│   ├── TableControl.cs                         (new — inherits Html)
│   ├── TabContainerControl.cs                  (new — inherits Html)
│   ├── TextAreaControl.cs                      (new — inherits Html)
│   ├── TextInputControl.cs                     (new — inherits Html)
│   ├── TimeInputControl.cs                     (new — inherits Html)
│   ├── MediaControlBase.cs                     (new — Blazor-only base)
│   ├── AudioControl.cs                         (new — Blazor-only)
│   ├── VideoControl.cs                         (new — Blazor-only)
│   ├── ImageControl.cs                         (new — Blazor-only)
│   ├── IFrameControl.cs                        (new — Blazor-only)
│   └── NavMenuControl.cs                       (new — Blazor-only)
├── Pages/
│   └── BlazorPageObjectBase.cs                 (new — replaces Placeholder.cs)
└── Testing/
    └── BlazorTestFixtureBase.cs                (new — replaces Placeholder.cs)
```

**Files to create in `testsnew/Brinell.Blazor.Tests/`:**

```text
testsnew/Brinell.Blazor.Tests/
├── GlobalUsings.cs                              (update existing)
├── Mocks/
│   └── MockHtmlFactory.cs                       (new)
├── Context/
│   └── BlazorTestContextTests.cs                (new)
└── Controls/
    ├── AudioControlTests.cs                     (new)
    ├── ButtonControlTests.cs                    (new)
    ├── CheckBoxControlTests.cs                  (new)
    ├── DateInputControlTests.cs                 (new)
    ├── IFrameControlTests.cs                    (new)
    ├── ImageControlTests.cs                     (new)
    ├── LinkControlTests.cs                      (new)
    ├── ListControlTests.cs                      (new)
    ├── NavMenuControlTests.cs                   (new)
    ├── ProgressControlTests.cs                  (new)
    ├── RadioButtonControlTests.cs               (new)
    ├── RangeInputControlTests.cs                (new)
    ├── SelectControlTests.cs                    (new)
    ├── TabContainerControlTests.cs              (new)
    ├── TableControlTests.cs                     (new)
    ├── TextAreaControlTests.cs                  (new)
    ├── TextInputControlTests.cs                 (new)
    ├── TimeInputControlTests.cs                 (new)
    └── VideoControlTests.cs                     (new)
```

**Files to delete:**

```text
srcnew/Brinell.Blazor/Context/Placeholder.cs
srcnew/Brinell.Blazor/Controls/Placeholder.cs
srcnew/Brinell.Blazor/Pages/Placeholder.cs
srcnew/Brinell.Blazor/Testing/Placeholder.cs
```

**Files to modify (scope expansion for D2):**

```text
srcnew/Brinell.Html/Interfaces/IHtmlElement.cs            (add Evaluate<T> + Evaluate)
srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs    (implement Evaluate<T> + Evaluate)
```
