<!-- markdownlint-disable-file -->
# Implementation Details: Blazor Refactoring to srcnew/testsnew

## Context Reference

Sources:
* .copilot-tracking/Task/02_BlazorRefactoring/research/02-blazor-refactoring-research.md
* .copilot-tracking/Task/02_BlazorRefactoring/subagent/01-html-architecture-research.md
* .copilot-tracking/Task/02_BlazorRefactoring/subagent/02-playwright-context-research.md
* .copilot-tracking/Task/02_BlazorRefactoring/subagent/03-old-controls-migration-map.md
* .copilot-tracking/Task/02_BlazorRefactoring/subagent/04-test-migration-patterns.md

## Implementation Phase 1: Foundation — IHtmlElement + Project References

<!-- parallelizable: false -->

### Step 1.1: Add Evaluate methods to IHtmlElement interface

Add two methods to the end of the `IHtmlElement` interface, before the closing brace.

Files:
* `srcnew/Brinell.Html/Interfaces/IHtmlElement.cs` — Add `Evaluate<T>()` and `Evaluate()` methods

Add after the `Blur()` method:

```csharp
    T? Evaluate<T>(string expression);
    void Evaluate(string expression);
```

Success criteria:
* `IHtmlElement` declares `Evaluate<T>(string)` returning `T?`
* `IHtmlElement` declares `Evaluate(string)` returning void
* File compiles without errors

Context references:
* srcnew/Brinell.Html/Interfaces/IHtmlElement.cs — Current interface members end at `Blur()`
* Research document, Section "Blazor-Only Controls: JS Evaluation Gap"

### Step 1.2: Implement Evaluate methods in PlaywrightHtmlElement

Add implementations at the end of the class, before the closing brace.

Files:
* `srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs` — Add `Evaluate<T>()` and `Evaluate()` implementations

```csharp
    public T? Evaluate<T>(string expression)
        => _locator.EvaluateAsync<T>(expression).GetAwaiter().GetResult();

    public void Evaluate(string expression)
        => _locator.EvaluateAsync(expression).GetAwaiter().GetResult();
```

Success criteria:
* Both methods delegate to `_locator.EvaluateAsync` with `.GetAwaiter().GetResult()` sync bridge
* File compiles without errors

Context references:
* srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs — Existing `EvaluateAsync` pattern (Lines 26, 148, 151, 154, 158)

### Step 1.3: Update Brinell.Blazor.csproj

Replace the existing `ProjectReference` to `Brinell.Core` with references to `Brinell.Html` and `Brinell.Html.Playwright`. Remove the direct `Brinell.Core` reference since it is transitively included via `Brinell.Html`.

Files:
* `srcnew/Brinell.Blazor/Brinell.Blazor.csproj` — Update ItemGroup references

Replace:
```xml
  <ItemGroup>
    <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
  </ItemGroup>
```

With:
```xml
  <ItemGroup>
    <ProjectReference Include="..\Brinell.Html\Brinell.Html.csproj" />
    <ProjectReference Include="..\Brinell.Html.Playwright\Brinell.Html.Playwright.csproj" />
  </ItemGroup>
```

Success criteria:
* csproj references `Brinell.Html` and `Brinell.Html.Playwright`
* `Microsoft.Playwright` PackageReference remains unchanged
* Project restores successfully

### Step 1.4: Update Brinell.Blazor.Tests.csproj

Add `Brinell.Html` and `Brinell.Html.Playwright` project references alongside existing references.

Files:
* `testsnew/Brinell.Blazor.Tests/Brinell.Blazor.Tests.csproj` — Add project references

Replace the ProjectReference ItemGroup:
```xml
  <ItemGroup>
    <ProjectReference Include="..\..\srcnew\Brinell.Core\Brinell.Core.csproj" />
    <ProjectReference Include="..\..\srcnew\Brinell.Blazor\Brinell.Blazor.csproj" />
  </ItemGroup>
```

With:
```xml
  <ItemGroup>
    <ProjectReference Include="..\..\srcnew\Brinell.Core\Brinell.Core.csproj" />
    <ProjectReference Include="..\..\srcnew\Brinell.Html\Brinell.Html.csproj" />
    <ProjectReference Include="..\..\srcnew\Brinell.Html.Playwright\Brinell.Html.Playwright.csproj" />
    <ProjectReference Include="..\..\srcnew\Brinell.Blazor\Brinell.Blazor.csproj" />
  </ItemGroup>
```

Success criteria:
* Test project references Core, Html, Html.Playwright, and Blazor
* `Moq` PackageReference remains unchanged
* Project restores successfully

Dependencies:
* None — this is the first phase

## Implementation Phase 2: Infrastructure — Context, Page, Fixture

<!-- parallelizable: false -->

### Step 2.1: Implement BlazorTestContext

Create `BlazorTestContext` wrapping `PlaywrightTestContext`. Delegates all `IHtmlTestContext` members to the inner context. Adds Blazor-specific extension point `WaitForBlazorReady`.

Files:
* `srcnew/Brinell.Blazor/Context/BlazorTestContext.cs` — New file (replaces Placeholder.cs)

Key design:
* Wraps `PlaywrightTestContext` (composition, not inheritance)
* Two factory methods: `CreateAsync(HtmlTestContextOptions)` and `ForPage(IPage, HtmlTestContextOptions?)`
* Implements `IHtmlTestContext` by delegating every member to `_inner`
* Implements `IAsyncDisposable` by delegating to `_inner`
* Adds `WaitForBlazorReady(int?)` — evaluates JS `typeof window._blazor !== 'undefined'` using `_inner.InternalPage` with polling

`IHtmlTestContext` members to delegate (from `PlaywrightTestContext`):
* Properties: `Context`, `Timeouts`, `Logger`, `DefaultLocatorStrategy`, `Page`, `CurrentUrl`, `PageTitle`
* Methods: `IsReady()`, `WaitReady(int?)`, `TryFindElement(Locator)`, `FindElement(Locator, int)`, `FindElements(Locator, int)`, `NavigateTo(string)`, `NavigateBack()`, `GoForward()`, `Refresh()`, `TakeScreenshot(string?)`, `SaveScreenshot(string)`, `ResetAppState()`

Constructor pattern:
```csharp
private BlazorTestContext(PlaywrightTestContext inner) => _inner = inner;

public static async Task<BlazorTestContext> CreateAsync(HtmlTestContextOptions options)
    => new(await PlaywrightTestContext.CreateAsync(options));

public static BlazorTestContext ForPage(IPage page, HtmlTestContextOptions? options = null)
    => new(PlaywrightTestContext.ForPage(page, options));
```

Blazor-specific extension:
```csharp
public void WaitForBlazorReady(int? timeoutMs = null)
{
    var timeout = timeoutMs ?? Timeouts.DefaultTimeoutMs;
    Poll(() =>
    {
        var ready = _inner.InternalPage
            .EvaluateAsync<bool>("() => typeof window._blazor !== 'undefined'")
            .GetAwaiter().GetResult();
        return ready;
    }, timeout);
}
```

Success criteria:
* `BlazorTestContext` implements `IHtmlTestContext, IAsyncDisposable`
* All `IHtmlTestContext` members delegate to `_inner`
* `CreateAsync` and `ForPage` factory methods work
* `WaitForBlazorReady` polls via JS evaluation

Context references:
* srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs — All IHtmlTestContext members
* src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs — Old context for reference

### Step 2.2: Implement BlazorPageObjectBase

Thin wrapper that inherits `HtmlPageObjectBase<TSelf>` for the `Brinell.Blazor.Pages` namespace.

Files:
* `srcnew/Brinell.Blazor/Pages/BlazorPageObjectBase.cs` — New file (replaces Placeholder.cs)

```csharp
namespace Brinell.Blazor.Pages;

public abstract class BlazorPageObjectBase<TSelf> : HtmlPageObjectBase<TSelf>
    where TSelf : BlazorPageObjectBase<TSelf>
{
    protected BlazorPageObjectBase(IHtmlTestContext context) : base(context) { }
}
```

Success criteria:
* Class inherits `HtmlPageObjectBase<TSelf>` with CRTP constraint
* Constructor delegates to base
* Compiles without errors

Context references:
* srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs — Base class pattern

### Step 2.3: Implement BlazorTestFixtureBase

Extends `HtmlTestFixtureBase`, overrides `CreateContextAsync` to create `BlazorTestContext`.

Files:
* `srcnew/Brinell.Blazor/Testing/BlazorTestFixtureBase.cs` — New file (replaces Placeholder.cs)

```csharp
namespace Brinell.Blazor.Testing;

public abstract class BlazorTestFixtureBase : HtmlTestFixtureBase
{
    protected override async Task<IHtmlTestContext> CreateContextAsync(HtmlTestContextOptions options)
        => await BlazorTestContext.CreateAsync(options);

    protected BlazorTestContext BlazorContext => (BlazorTestContext)Context;
}
```

Success criteria:
* Class extends `HtmlTestFixtureBase`
* `CreateContextAsync` creates a `BlazorTestContext`
* `BlazorContext` provides typed access to the Blazor context
* Compiles without errors

Context references:
* srcnew/Brinell.Html/Testing/HtmlTestFixtureBase.cs — Base fixture pattern

Dependencies:
* Step 2.1 (BlazorTestContext must exist first)

## Implementation Phase 3: Inherited Controls (14 thin re-exports)

<!-- parallelizable: true -->

### Step 3.1: Implement ButtonControl and LinkControl (Buttons group)

Files:
* `srcnew/Brinell.Blazor/Controls/ButtonControl.cs` — New file
* `srcnew/Brinell.Blazor/Controls/LinkControl.cs` — New file

Pattern for each:
```csharp
namespace Brinell.Blazor.Controls;

public class ButtonControl<TScope> : Brinell.Html.Controls.Buttons.ButtonControl<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ButtonControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ButtonControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
```

`LinkControl` follows the identical pattern inheriting from `Brinell.Html.Controls.Buttons.LinkControl<TScope>`.

Success criteria:
* Both classes compile and inherit all base methods
* Two constructors each (Locator + string)

### Step 3.2: Implement CheckBoxControl and RadioButtonControl (Toggle group)

Files:
* `srcnew/Brinell.Blazor/Controls/CheckBoxControl.cs` — Inherits `Html.Controls.Toggle.CheckBoxControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/RadioButtonControl.cs` — Inherits `Html.Controls.Toggle.RadioButtonControl<TScope>`

Same pattern as Step 3.1 — two constructors, inherit from the Html Toggle namespace.

Success criteria:
* Both classes compile and inherit toggle-specific methods (Check, Uncheck, IsChecked, etc.)

### Step 3.3: Implement TextInputControl and TextAreaControl (Text group)

Files:
* `srcnew/Brinell.Blazor/Controls/TextInputControl.cs` — Inherits `Html.Controls.Text.TextInputControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/TextAreaControl.cs` — Inherits `Html.Controls.Text.TextAreaControl<TScope>`

Same pattern — two constructors each.

Success criteria:
* Both classes compile and inherit text-specific methods (Enter, GetValue, etc.)

### Step 3.4: Implement SelectControl (Selection group)

Files:
* `srcnew/Brinell.Blazor/Controls/SelectControl.cs` — Inherits `Html.Controls.Selection.SelectControl<TScope>`

Same pattern — two constructors.

Success criteria:
* Class compiles and inherits selection methods (SelectOption, GetSelectedValue, etc.)

### Step 3.5: Implement DateInputControl and TimeInputControl (DateTime group)

Files:
* `srcnew/Brinell.Blazor/Controls/DateInputControl.cs` — Inherits `Html.Controls.DateTime.DateInputControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/TimeInputControl.cs` — Inherits `Html.Controls.DateTime.TimeInputControl<TScope>`

Same pattern — two constructors each.

Success criteria:
* Both classes compile and inherit range/date-time methods

### Step 3.6: Implement RangeInputControl (Range group)

Files:
* `srcnew/Brinell.Blazor/Controls/RangeInputControl.cs` — Inherits `Html.Controls.Range.RangeInputControl<TScope>`

Same pattern — two constructors.

Success criteria:
* Class compiles and inherits range methods (GetValue, SetValue, GetMin, GetMax, etc.)

### Step 3.7: Implement ListControl, TableControl, ProgressControl (Collection/Display group)

Files:
* `srcnew/Brinell.Blazor/Controls/ListControl.cs` — Inherits `Html.Controls.Collection.ListControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/TableControl.cs` — Inherits `Html.Controls.Collection.TableControl<TScope>`
* `srcnew/Brinell.Blazor/Controls/ProgressControl.cs` — Inherits `Html.Controls.Display.ProgressControl<TScope>`

Same pattern — two constructors each. These all derive from `ControlBase<TScope>` (not clickable).

Success criteria:
* All three classes compile

### Step 3.8: Implement TabContainerControl (Container group — dual-generic)

This control uses the dual-generic `<TParent, TScope>` container pattern. The constructor takes `IHtmlScope<TParent>` (the parent scope, not the container itself).

Files:
* `srcnew/Brinell.Blazor/Controls/TabContainerControl.cs` — Inherits `Html.Controls.Container.TabContainerControl<TParent, TScope>`

```csharp
namespace Brinell.Blazor.Controls;

public class TabContainerControl<TParent, TScope> : Brinell.Html.Controls.Container.TabContainerControl<TParent, TScope>
    where TParent : IHtmlScope<TParent>
    where TScope : IHtmlContainer<TParent, TScope>
{
    public TabContainerControl(IHtmlScope<TParent> parentScope, Locator locator, string tabSelector = "[role='tab']")
        : base(parentScope, locator, tabSelector) { }
    public TabContainerControl(IHtmlScope<TParent> parentScope, string selectorOrId, string tabSelector = "[role='tab']")
        : base(parentScope, selectorOrId, tabSelector) { }
}
```

Success criteria:
* Dual-generic constraints match the Html base exactly
* Constructor takes `IHtmlScope<TParent>` (not `TScope`)
* Optional `tabSelector` parameter forwarded

Dependencies:
* Phase 1 completed (csproj references in place)

## Implementation Phase 4: Blazor-Only Controls (6 files)

<!-- parallelizable: true -->

### Step 4.1: Implement MediaControlBase

Shared base for Audio and Video controls. Provides all HTML5 Media API methods.

Files:
* `srcnew/Brinell.Blazor/Controls/MediaControlBase.cs` — New abstract class

```csharp
namespace Brinell.Blazor.Controls;

public abstract class MediaControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    protected MediaControlBase(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    protected MediaControlBase(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    // Playback control
    public TScope Play() => RunWithElement(e => e.Evaluate("el => el.play()"));
    public TScope Pause() => RunWithElement(e => e.Evaluate("el => el.pause()"));

    // Playback state (read via GetDomProperty)
    public bool IsPlaying() => RunWithElement(e =>
        !(e.GetDomProperty("paused") == "True" || e.GetDomProperty("paused") == "true")
        && !(e.GetDomProperty("ended") == "True" || e.GetDomProperty("ended") == "true"));
    public bool IsPaused() => RunWithElement(e =>
        e.GetDomProperty("paused") == "True" || e.GetDomProperty("paused") == "true");
    public bool IsEnded() => RunWithElement(e =>
        e.GetDomProperty("ended") == "True" || e.GetDomProperty("ended") == "true");

    // Time control
    public double GetCurrentTime() => RunWithElement(e =>
        double.Parse(e.GetDomProperty("currentTime") ?? "0", CultureInfo.InvariantCulture));
    public TScope Seek(double seconds) => RunWithElement(e =>
        e.Evaluate($"el => el.currentTime = {seconds.ToString(CultureInfo.InvariantCulture)}"));
    public double GetDuration() => RunWithElement(e =>
        double.Parse(e.GetDomProperty("duration") ?? "0", CultureInfo.InvariantCulture));

    // Volume control
    public double GetVolume() => RunWithElement(e =>
        double.Parse(e.GetDomProperty("volume") ?? "1", CultureInfo.InvariantCulture));
    public TScope SetVolume(double volume) => RunWithElement(e =>
        e.Evaluate($"el => el.volume = {Math.Clamp(volume, 0, 1).ToString(CultureInfo.InvariantCulture)}"));
    public bool IsMuted() => RunWithElement(e =>
        e.GetDomProperty("muted") == "True" || e.GetDomProperty("muted") == "true");
    public TScope Mute() => RunWithElement(e => e.Evaluate("el => el.muted = true"));
    public TScope Unmute() => RunWithElement(e => e.Evaluate("el => el.muted = false"));

    // Source
    public string? GetSource() => RunWithElement(e =>
        e.GetDomAttribute("src") ?? e.GetDomProperty("currentSrc"));

    // Assertions
    public TScope AssertPlaying(string? message = null) => RunAssert(e =>
    {
        var paused = e.GetDomProperty("paused");
        var ended = e.GetDomProperty("ended");
        if (paused == "True" || paused == "true" || ended == "True" || ended == "true")
            throw new AssertionException(message ?? "Expected media to be playing");
    });
    public TScope AssertPaused(string? message = null) => RunAssert(e =>
    {
        var paused = e.GetDomProperty("paused");
        if (paused != "True" && paused != "true")
            throw new AssertionException(message ?? "Expected media to be paused");
    });
}
```

Note: `RunWithElement` returns `TScope` for void actions and `TResult` for typed returns. `RunAssert` returns `TScope` after assertion.

The boolean property parsing (paused, ended, muted) must handle the string representation from `GetDomProperty`. The actual format depends on `PlaywrightHtmlElement.GetDomProperty` — it evaluates `el[prop]` and returns the JS value as a string. For booleans, Playwright returns `"true"`/`"false"`. Use `Evaluate<bool>` for cleaner boolean reads if it works better in practice.

Success criteria:
* Abstract class with `<TScope>` generic
* All 15 shared media methods implemented with `RunWithElement`/`RunAssert`
* Uses `Evaluate()` for actions (play, pause, seek, volume set, mute set)
* Uses `GetDomProperty()` for reads (paused, ended, currentTime, duration, volume, muted)
* Volume clamped to 0-1 range
* InvariantCulture for all numeric parsing/formatting

### Step 4.2: Implement AudioControl

Thin class extending `MediaControlBase`. Audio has no additional methods beyond what `MediaControlBase` provides.

Files:
* `srcnew/Brinell.Blazor/Controls/AudioControl.cs` — New class

```csharp
namespace Brinell.Blazor.Controls;

public class AudioControl<TScope> : MediaControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public AudioControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public AudioControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }
}
```

Success criteria:
* Inherits all media methods from `MediaControlBase`
* Two constructors

Dependencies:
* Step 4.1 (MediaControlBase must exist)

### Step 4.3: Implement VideoControl

Extends `MediaControlBase` with one additional method: `GetPoster()`.

Files:
* `srcnew/Brinell.Blazor/Controls/VideoControl.cs` — New class

```csharp
namespace Brinell.Blazor.Controls;

public class VideoControl<TScope> : MediaControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public VideoControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public VideoControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    public string? GetPoster() => RunWithElement(e => e.GetDomAttribute("poster"));
}
```

Success criteria:
* Inherits all media methods from `MediaControlBase`
* Adds `GetPoster()` using `GetDomAttribute`

Dependencies:
* Step 4.1 (MediaControlBase must exist)

### Step 4.4: Implement ImageControl

Image-specific methods for `<img>` elements. Uses `GetDomAttribute` for src/alt, `Evaluate<T>` for compound expressions.

Files:
* `srcnew/Brinell.Blazor/Controls/ImageControl.cs` — New class

```csharp
namespace Brinell.Blazor.Controls;

public class ImageControl<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ImageControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ImageControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    public string? GetSource() => RunWithElement(e => e.GetDomAttribute("src"));
    public string? GetAltText() => RunWithElement(e => e.GetDomAttribute("alt"));

    public bool IsLoaded() => RunWithElement(e =>
        e.Evaluate<bool>("img => img.complete && img.naturalWidth > 0"));

    public bool WaitLoaded(bool expected = true, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Context.Timeouts.DefaultTimeoutMs;
        return Poll(() => IsLoaded() == expected, timeout);
    }

    public int GetNaturalWidth() => RunWithElement(e =>
        e.Evaluate<int>("img => img.naturalWidth"));
    public int GetNaturalHeight() => RunWithElement(e =>
        e.Evaluate<int>("img => img.naturalHeight"));

    // Assertions
    public TScope AssertSource(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("src");
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected src '{expected}' but was '{actual}'");
    });
    public TScope AssertSourceContains(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("src");
        if (expected != null && (actual == null || !actual.Contains(expected, StringComparison.Ordinal)))
            throw new AssertionException(message ?? $"Expected src to contain '{expected}' but was '{actual}'");
    });
    public TScope AssertAltText(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("alt");
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected alt '{expected}' but was '{actual}'");
    });
}
```

Success criteria:
* `GetSource`, `GetAltText` use `GetDomAttribute`
* `IsLoaded` uses `Evaluate<bool>` for compound JS expression
* `GetNaturalWidth/Height` use `Evaluate<int>`
* `WaitLoaded` uses `Poll` pattern from `ObjectBase`
* Assertions use `RunAssert` pattern

### Step 4.5: Implement IFrameControl

Cross-frame interaction control. Uses `Evaluate` for actions inside iframes.

Files:
* `srcnew/Brinell.Blazor/Controls/IFrameControl.cs` — New class

The IFrame control is more complex due to cross-frame interaction. The old implementation uses `GetLocator().FrameLocator(".")` — Playwright's frame locator API. In the new architecture, we need to bridge this through `IHtmlElement`.

Approach: Use `Evaluate` for read operations. For cross-frame interactions (`ClickInside`, `FillInside`, `GetTextInside`), the control needs to access the iframe's content document via JS. This works for same-origin iframes but not cross-origin.

```csharp
namespace Brinell.Blazor.Controls;

public class IFrameControl<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public IFrameControl(IHtmlScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public IFrameControl(IHtmlScope<TScope> scope, string selectorOrId) : base(scope, selectorOrId) { }

    public string? GetSource() => RunWithElement(e => e.GetDomAttribute("src"));
    public string? GetTitle() => RunWithElement(e => e.GetDomAttribute("title"));
    public string? GetName() => RunWithElement(e => e.GetDomAttribute("name"));

    public TScope ClickInside(string selector) => RunWithElement(e =>
        e.Evaluate($"(iframe) => iframe.contentDocument.querySelector('{EscapeSelector(selector)}').click()"));

    public TScope FillInside(string selector, string? text) => RunWithElement(e =>
        e.Evaluate($"(iframe) => {{ const el = iframe.contentDocument.querySelector('{EscapeSelector(selector)}'); el.value = '{EscapeJsString(text)}'; el.dispatchEvent(new Event('input', {{ bubbles: true }})); }}"));

    public string? GetTextInside(string selector) => RunWithElement(e =>
        e.Evaluate<string?>($"(iframe) => {{ const el = iframe.contentDocument.querySelector('{EscapeSelector(selector)}'); return el ? el.textContent : null; }}"));

    public bool ElementExistsInside(string selector) => RunWithElement(e =>
        e.Evaluate<bool>($"(iframe) => iframe.contentDocument.querySelector('{EscapeSelector(selector)}') !== null"));

    public bool WaitForElementInside(string selector, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Context.Timeouts.DefaultTimeoutMs;
        return Poll(() => ElementExistsInside(selector), timeout);
    }

    // Assertions
    public TScope AssertSource(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("src");
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected iframe src '{expected}' but was '{actual}'");
    });

    public TScope AssertSourceContains(string? expected, string? message = null) => RunAssert(e =>
    {
        var actual = e.GetDomAttribute("src");
        if (expected != null && (actual == null || !actual.Contains(expected, StringComparison.Ordinal)))
            throw new AssertionException(message ?? $"Expected iframe src to contain '{expected}' but was '{actual}'");
    });

    public TScope AssertElementExistsInside(string selector, string? message = null) => RunAssert(e =>
    {
        var exists = e.Evaluate<bool>($"(iframe) => iframe.contentDocument.querySelector('{EscapeSelector(selector)}') !== null");
        if (!exists)
            throw new AssertionException(message ?? $"Expected element '{selector}' to exist inside iframe");
    });

    private static string EscapeSelector(string selector) => selector.Replace("'", "\\'");
    private static string EscapeJsString(string? value) => value?.Replace("\\", "\\\\").Replace("'", "\\'") ?? "";
}
```

Note: Cross-origin iframes will throw JS security errors. This matches the old behavior — the old control also used Playwright's FrameLocator which has the same same-origin constraint for content access. If cross-origin iframe support is needed later, it requires a different approach (Playwright FrameLocator API exposed through `IHtmlElement`).

Success criteria:
* Read methods (`GetSource`, `GetTitle`, `GetName`) use `GetDomAttribute`
* Cross-frame methods use `Evaluate` with `contentDocument`
* JS strings properly escaped to prevent injection
* `WaitForElementInside` uses `Poll` pattern
* Assertions use `RunAssert`

### Step 4.6: Implement NavMenuControl

Navigation menu control for Blazor `<nav>` elements. Uses `FindElements` for item discovery and `Evaluate` for active state.

Files:
* `srcnew/Brinell.Blazor/Controls/NavMenuControl.cs` — New class

```csharp
namespace Brinell.Blazor.Controls;

public class NavMenuControl<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    private readonly string _itemSelector;

    public NavMenuControl(IHtmlScope<TScope> scope, Locator locator, string itemSelector = "a, .nav-link, [role='menuitem']")
        : base(scope, locator) => _itemSelector = itemSelector;
    public NavMenuControl(IHtmlScope<TScope> scope, string selectorOrId, string itemSelector = "a, .nav-link, [role='menuitem']")
        : base(scope, selectorOrId) => _itemSelector = itemSelector;

    public int GetItemCount() => RunWithElement(e =>
        e.FindElements(Locator.ByCss(_itemSelector)).Count);

    public IReadOnlyList<string> GetItems() => RunWithElement(e =>
        e.FindElements(Locator.ByCss(_itemSelector))
            .Select(item => item.Text?.Trim() ?? "")
            .ToList());

    public string? GetActiveItem() => RunWithElement(e =>
    {
        var items = e.FindElements(Locator.ByCss(
            $"{_itemSelector}.active, {_itemSelector}[aria-current='page'], {_itemSelector}[aria-current='true']"));
        return items.Count > 0 ? items[0].Text?.Trim() : null;
    });

    public bool IsActive(string itemText) =>
        string.Equals(GetActiveItem(), itemText, StringComparison.OrdinalIgnoreCase);

    public TScope NavigateTo(string itemText)
    {
        return RunWithElement(e =>
        {
            var items = e.FindElements(Locator.ByCss(_itemSelector));
            var target = items.FirstOrDefault(i =>
                string.Equals(i.Text?.Trim(), itemText, StringComparison.OrdinalIgnoreCase));
            if (target == null)
                throw new InvalidOperationException($"Nav menu item '{itemText}' not found");
            target.Click();
        });
    }

    public TScope NavigateToIndex(int index)
    {
        return RunWithElement(e =>
        {
            var items = e.FindElements(Locator.ByCss(_itemSelector));
            if (index < 0 || index >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Index {index} out of range (0-{items.Count - 1})");
            items[index].Click();
        });
    }

    public string? GetItemHref(string itemText) => RunWithElement(e =>
    {
        var items = e.FindElements(Locator.ByCss(_itemSelector));
        var target = items.FirstOrDefault(i =>
            string.Equals(i.Text?.Trim(), itemText, StringComparison.OrdinalIgnoreCase));
        return target?.GetAttribute("href");
    });

    public bool HasItem(string itemText)
    {
        var items = GetItems();
        return items.Any(i => string.Equals(i, itemText, StringComparison.OrdinalIgnoreCase));
    }

    // Assertions
    public TScope AssertActiveItem(string? expected, string? message = null)
    {
        var actual = GetActiveItem();
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new AssertionException(message ?? $"Expected active item '{expected}' but was '{actual}'");
        return ContainingScope;
    }

    public TScope AssertHasItem(string itemText, string? message = null)
    {
        if (!HasItem(itemText))
            throw new AssertionException(message ?? $"Expected nav menu to contain item '{itemText}'");
        return ContainingScope;
    }

    public TScope AssertItemCount(int expected, string? message = null)
    {
        var actual = GetItemCount();
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected {expected} nav items but found {actual}");
        return ContainingScope;
    }
}
```

Success criteria:
* Uses `FindElements` with CSS selector for item discovery
* Active item detection uses `.active` CSS class and `aria-current` attributes
* `NavigateTo` finds item by text and clicks
* `NavigateToIndex` validates bounds before clicking
* `GetItemHref` reads `href` attribute from matched item
* Assertions follow `RunAssert` pattern or throw directly + return `ContainingScope`

Dependencies:
* Phase 1 completed (Evaluate methods available, csproj references in place)

## Implementation Phase 5: Test Infrastructure

<!-- parallelizable: false -->

### Step 5.1: Create MockHtmlFactory

Central mock factory for unit tests. Analogous to old `MockPlaywrightFactory` but for `IHtmlElement`/`IHtmlTestContext`.

Files:
* `testsnew/Brinell.Blazor.Tests/Mocks/MockHtmlFactory.cs` — New file

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
        bool enabled = true)
    {
        var mock = new Mock<IHtmlElement>();
        mock.Setup(e => e.Text).Returns(text);
        mock.Setup(e => e.Visible).Returns(visible);
        mock.Setup(e => e.Enabled).Returns(enabled);
        mock.Setup(e => e.InputValue).Returns(text ?? "");
        return mock;
    }

    public static Mock<IHtmlElement> CreateMockToggleElement(
        bool isChecked = false,
        string? text = null,
        bool visible = true,
        bool enabled = true)
    {
        var mock = CreateMockElement(text, visible, enabled);
        mock.Setup(e => e.IsChecked).Returns(isChecked);
        return mock;
    }

    public static void SetupFindElement(Mock<IHtmlTestContext> context, Mock<IHtmlElement> element)
    {
        context.Setup(c => c.FindElement(It.IsAny<Locator>(), It.IsAny<int>())).Returns(element.Object);
        context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns(element.Object);
    }

    public static void SetupFindElements(Mock<IHtmlTestContext> context, params Mock<IHtmlElement>[] elements)
    {
        var list = elements.Select(e => e.Object).ToList().AsReadOnly();
        context.Setup(c => c.FindElements(It.IsAny<Locator>(), It.IsAny<int>())).Returns(list);
    }

    public static void SetupElementNotFound(Mock<IHtmlTestContext> context)
    {
        context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns((IHtmlElement?)null);
        context.Setup(c => c.FindElement(It.IsAny<Locator>(), It.IsAny<int>()))
            .Throws(new ElementNotFoundException("Element not found"));
    }

    public static void SetupDomProperty(Mock<IHtmlElement> element, string property, string? value)
    {
        element.Setup(e => e.GetDomProperty(property)).Returns(value);
    }

    public static void SetupDomAttribute(Mock<IHtmlElement> element, string attribute, string? value)
    {
        element.Setup(e => e.GetDomAttribute(attribute)).Returns(value);
    }

    public static void SetupEvaluate<T>(Mock<IHtmlElement> element, string expression, T returnValue)
    {
        element.Setup(e => e.Evaluate<T>(It.Is<string>(s => s == expression))).Returns(returnValue);
    }
}
```

Note: `TimeoutSettings`, `LocatorStrategy`, `ElementNotFoundException` must be importable from `Brinell.Core`. Verify exact type names during implementation. The `FindElement` overload takes `(Locator, int)` where int is the timeout — match overloads with the real interface signature.

Success criteria:
* `CreateMockContext()` provides a fully configured mock `IHtmlTestContext`
* `CreateMockElement()` provides default properties (text, visible, enabled)
* `CreateMockToggleElement()` adds `IsChecked` for toggle controls
* `SetupFindElement/SetupFindElements/SetupElementNotFound` configure element resolution
* `SetupDomProperty/SetupDomAttribute/SetupEvaluate<T>` configure read helpers
* All methods are static for easy test access

### Step 5.2: Update GlobalUsings.cs

Uncomment and extend the global usings in the test project.

Files:
* `testsnew/Brinell.Blazor.Tests/GlobalUsings.cs` — Modify existing file

Replace commented usings with active ones and add Html-related usings:

```csharp
global using Xunit;
global using Moq;
global using Brinell.Core.Abstractions;
global using Brinell.Core.Interfaces;
global using Brinell.Core.Locators;
global using Brinell.Html.Interfaces;
global using Brinell.Html.Pages;
global using Brinell.Blazor.Context;
global using Brinell.Blazor.Controls;
global using Brinell.Blazor.Pages;
global using Brinell.Blazor.Tests.Mocks;
```

Success criteria:
* All commented usings replaced with active ones
* Added `Brinell.Html.Interfaces`, `Brinell.Html.Pages`, `Brinell.Blazor.Tests.Mocks`
* File compiles without errors

Dependencies:
* Phase 2 (types in Brinell.Blazor.Context, Controls, Pages must exist)
* Step 5.1 (MockHtmlFactory namespace must exist)

## Implementation Phase 6: Test Migration — Simple Controls (9 files)

<!-- parallelizable: true -->

### Step 6.1: Migrate ButtonControlTests.cs

Each test file follows the same migration pattern. This step shows the complete pattern; Steps 6.2-6.9 follow identically.

Files:
* `testsnew/Brinell.Blazor.Tests/Controls/ButtonControlTests.cs` — New file

Source: `tests/Brinell.Blazor.Tests.ControlObject6/Controls/ButtonControlTests.cs`

Conversion pattern applied:
1. Replace `using` statements with GlobalUsings-provided namespaces
2. Replace `Mock<IPage>` / `Mock<ILocator>` with `Mock<IHtmlTestContext>` / `Mock<IHtmlElement>`
3. Replace `MockPlaywrightFactory.Create*` with `MockHtmlFactory.Create*`
4. Replace async test methods with sync
5. Replace `await control.ClickAsync()` with `page.TestButton.Click()`
6. Replace FluentAssertions `.Should().Be*()` with `Assert.*`
7. Replace `mockLocator.Verify(l => l.ClickAsync(...))` with `mockElement.Verify(e => e.Click())`
8. Add inner `TestPage` class with control properties

Template for each simple control test:
```csharp
namespace Brinell.Blazor.Tests.Controls;

public class ButtonControlTests
{
    private readonly Mock<IHtmlTestContext> _mockContext;
    private readonly Mock<IHtmlElement> _mockElement;
    private readonly TestPage _page;

    public ButtonControlTests()
    {
        _mockContext = MockHtmlFactory.CreateMockContext();
        _mockElement = MockHtmlFactory.CreateMockElement();
        MockHtmlFactory.SetupFindElement(_mockContext, _mockElement);
        _page = new TestPage(_mockContext.Object);
    }

    [Fact]
    public void Click_CallsElementClick()
    {
        _page.TestButton.Click();
        _mockElement.Verify(e => e.Click(), Times.Once);
    }

    // ... additional tests migrated from old file

    private sealed class TestPage : BlazorPageObjectBase<TestPage>
    {
        public TestPage(IHtmlTestContext context) : base(context) { }
        public ButtonControl<TestPage> TestButton => new(this, "test-btn");
    }
}
```

Success criteria:
* All test methods sync (no `async Task`)
* Uses `MockHtmlFactory` for setup
* Uses `Assert.*` (no FluentAssertions)
* Controls accessed via `TestPage` property (not constructed directly)
* Each test verifies specific behavior on `_mockElement`

### Steps 6.2-6.9: Migrate remaining simple control tests

Each follows the Step 6.1 pattern. Control type and mock setup differences:

| Step | Test File | Control Type Property | Special Setup |
|------|-----------|----------------------|---------------|
| 6.2 | `CheckBoxControlTests.cs` | `CheckBoxControl<TestPage>` | `CreateMockToggleElement(isChecked: ...)` |
| 6.3 | `LinkControlTests.cs` | `LinkControl<TestPage>` | Standard |
| 6.4 | `RadioButtonControlTests.cs` | `RadioButtonControl<TestPage>` | `CreateMockToggleElement(isChecked: ...)` |
| 6.5 | `DateInputControlTests.cs` | `DateInputControl<TestPage>` | Standard |
| 6.6 | `TimeInputControlTests.cs` | `TimeInputControl<TestPage>` | Standard |
| 6.7 | `TextInputControlTests.cs` | `TextInputControl<TestPage>` | `InputValue` mock setup |
| 6.8 | `TextAreaControlTests.cs` | `TextAreaControl<TestPage>` | `InputValue` mock setup |
| 6.9 | `ProgressControlTests.cs` | `ProgressControl<TestPage>` | `GetDomProperty` for value/max |

## Implementation Phase 7: Test Migration — Collection/Container Controls (5 files)

<!-- parallelizable: true -->

### Steps 7.1-7.4: Collection control tests

Files:
* `testsnew/Brinell.Blazor.Tests/Controls/ListControlTests.cs`
* `testsnew/Brinell.Blazor.Tests/Controls/TableControlTests.cs`
* `testsnew/Brinell.Blazor.Tests/Controls/SelectControlTests.cs`
* `testsnew/Brinell.Blazor.Tests/Controls/RangeInputControlTests.cs`

These tests require `FindElements` mocking for list/table item discovery and `GetDomProperty` for range values.

Key differences from simple tests:
* Use `MockHtmlFactory.SetupFindElements()` for item collections
* Create multiple `Mock<IHtmlElement>` for list items

### Step 7.5: Migrate TabContainerControlTests

The `TabContainerControl` uses dual-generic pattern — the TestPage for this test needs both `TParent` and `TScope`.

Files:
* `testsnew/Brinell.Blazor.Tests/Controls/TabContainerControlTests.cs`

```csharp
private sealed class TestPage : BlazorPageObjectBase<TestPage>
{
    public TestPage(IHtmlTestContext context) : base(context) { }
    public TabContainerControl<TestPage, TestTabContainer> Tabs => new(this, "test-tabs");
}

private sealed class TestTabContainer : IHtmlContainer<TestPage, TestTabContainer>
{
    // Minimal implementation for testing
}
```

Note: The exact `IHtmlContainer` mock implementation depends on what `ContainerBase` requires. During implementation, check if `ContainerBase` can be tested with a simple mock or needs a full container implementation.

## Implementation Phase 8: Test Migration — Blazor-Only Controls (5 files)

<!-- parallelizable: true -->

### Step 8.1: Migrate AudioControlTests.cs

Audio tests need `Evaluate` and `GetDomProperty` mocking for media actions.

Files:
* `testsnew/Brinell.Blazor.Tests/Controls/AudioControlTests.cs`

Key test patterns:
```csharp
[Fact]
public void Play_CallsEvaluate()
{
    _page.TestAudio.Play();
    _mockElement.Verify(e => e.Evaluate("el => el.play()"), Times.Once);
}

[Fact]
public void GetCurrentTime_ReturnsParsedValue()
{
    MockHtmlFactory.SetupDomProperty(_mockElement, "currentTime", "42.5");
    var result = _page.TestAudio.GetCurrentTime();
    Assert.Equal(42.5, result);
}

[Fact]
public void IsPaused_WhenPaused_ReturnsTrue()
{
    MockHtmlFactory.SetupDomProperty(_mockElement, "paused", "true");
    Assert.True(_page.TestAudio.IsPaused());
}

[Fact]
public void Seek_CallsEvaluateWithTime()
{
    _page.TestAudio.Seek(30);
    _mockElement.Verify(e => e.Evaluate(It.Is<string>(s => s.Contains("currentTime = 30"))), Times.Once);
}
```

Inner TestPage:
```csharp
private sealed class TestPage : BlazorPageObjectBase<TestPage>
{
    public TestPage(IHtmlTestContext context) : base(context) { }
    public AudioControl<TestPage> TestAudio => new(this, "test-audio");
}
```

Success criteria:
* Play/Pause/Seek/Volume tests verify `Evaluate()` calls
* State query tests mock `GetDomProperty` returns
* Assertion tests verify exception throwing

### Step 8.2: Migrate VideoControlTests.cs

Same as Audio tests plus `GetPoster` test.

Files:
* `testsnew/Brinell.Blazor.Tests/Controls/VideoControlTests.cs`

Additional test:
```csharp
[Fact]
public void GetPoster_ReturnsAttribute()
{
    MockHtmlFactory.SetupDomAttribute(_mockElement, "poster", "poster.jpg");
    Assert.Equal("poster.jpg", _page.TestVideo.GetPoster());
}
```

### Step 8.3: Migrate ImageControlTests.cs

Image tests need `Evaluate<T>` mocking for compound JS expressions.

Files:
* `testsnew/Brinell.Blazor.Tests/Controls/ImageControlTests.cs`

Key test patterns:
```csharp
[Fact]
public void IsLoaded_WhenComplete_ReturnsTrue()
{
    MockHtmlFactory.SetupEvaluate(_mockElement, "img => img.complete && img.naturalWidth > 0", true);
    Assert.True(_page.TestImage.IsLoaded());
}

[Fact]
public void GetSource_ReturnsAttribute()
{
    MockHtmlFactory.SetupDomAttribute(_mockElement, "src", "image.png");
    Assert.Equal("image.png", _page.TestImage.GetSource());
}

[Fact]
public void GetNaturalWidth_ReturnsEvaluatedValue()
{
    MockHtmlFactory.SetupEvaluate(_mockElement, "img => img.naturalWidth", 800);
    Assert.Equal(800, _page.TestImage.GetNaturalWidth());
}
```

### Step 8.4: Migrate IFrameControlTests.cs

IFrame tests need `Evaluate` and `Evaluate<T>` mocking for cross-frame actions.

Files:
* `testsnew/Brinell.Blazor.Tests/Controls/IFrameControlTests.cs`

Key test patterns:
```csharp
[Fact]
public void GetSource_ReturnsAttribute()
{
    MockHtmlFactory.SetupDomAttribute(_mockElement, "src", "frame.html");
    Assert.Equal("frame.html", _page.TestIFrame.GetSource());
}

[Fact]
public void ClickInside_CallsEvaluate()
{
    _page.TestIFrame.ClickInside("#inner-button");
    _mockElement.Verify(e => e.Evaluate(It.Is<string>(s =>
        s.Contains("contentDocument") && s.Contains("#inner-button"))), Times.Once);
}

[Fact]
public void ElementExistsInside_ReturnsBool()
{
    _mockElement.Setup(e => e.Evaluate<bool>(It.IsAny<string>())).Returns(true);
    Assert.True(_page.TestIFrame.ElementExistsInside("#inner-element"));
}
```

### Step 8.5: Migrate NavMenuControlTests.cs

NavMenu tests need `FindElements` mocking for item discovery.

Files:
* `testsnew/Brinell.Blazor.Tests/Controls/NavMenuControlTests.cs`

Key test patterns:
```csharp
public NavMenuControlTests()
{
    _mockContext = MockHtmlFactory.CreateMockContext();
    _mockElement = MockHtmlFactory.CreateMockElement();
    MockHtmlFactory.SetupFindElement(_mockContext, _mockElement);

    // Setup child elements for nav items
    _mockItem1 = MockHtmlFactory.CreateMockElement("Home");
    _mockItem2 = MockHtmlFactory.CreateMockElement("About");
    _mockItem3 = MockHtmlFactory.CreateMockElement("Contact");

    // Setup FindElements on the nav element to return items
    _mockElement.Setup(e => e.FindElements(It.IsAny<Locator>(), It.IsAny<int>()))
        .Returns(new List<IHtmlElement> { _mockItem1.Object, _mockItem2.Object, _mockItem3.Object }.AsReadOnly());

    _page = new TestPage(_mockContext.Object);
}

[Fact]
public void GetItemCount_ReturnsCorrectCount()
{
    Assert.Equal(3, _page.TestNav.GetItemCount());
}

[Fact]
public void GetItems_ReturnsItemTexts()
{
    var items = _page.TestNav.GetItems();
    Assert.Equal(3, items.Count);
    Assert.Equal("Home", items[0]);
}

[Fact]
public void NavigateTo_ClicksMatchingItem()
{
    _page.TestNav.NavigateTo("About");
    _mockItem2.Verify(e => e.Click(), Times.Once);
}
```

## Implementation Phase 9: Test Migration — Context Test (1 file)

<!-- parallelizable: false -->

### Step 9.1: Implement BlazorTestContextTests.cs

Rewrite for the new `BlazorTestContext` wrapper pattern. Tests verify that `BlazorTestContext` correctly delegates to `PlaywrightTestContext`.

Files:
* `testsnew/Brinell.Blazor.Tests/Context/BlazorTestContextTests.cs`

Since `BlazorTestContext` wraps `PlaywrightTestContext` (which wraps Playwright), and we mock at the `IHtmlTestContext` level in control tests, the context tests should verify:

1. `ForPage` factory creates a valid context
2. Properties delegate correctly
3. `WaitForBlazorReady` behavior (if testable at the unit level)

Note: Some context tests may require actual Playwright page mocking (Mock<IPage>), making them closer to integration tests. If unit-level testing proves impractical for the context wrapper, create minimal smoke tests and note that full context testing is covered by integration/UI tests.

```csharp
namespace Brinell.Blazor.Tests.Context;

public class BlazorTestContextTests
{
    [Fact]
    public void ForPage_CreatesValidContext()
    {
        var mockPage = new Mock<IPage>();
        mockPage.Setup(p => p.Url).Returns("https://example.com");
        mockPage.Setup(p => p.TitleAsync(null)).ReturnsAsync("Test Page");

        var context = BlazorTestContext.ForPage(mockPage.Object);

        Assert.NotNull(context);
        Assert.Equal("https://example.com", context.CurrentUrl);
    }

    // Additional tests for property delegation, navigation, etc.
}
```

Success criteria:
* Basic factory method tests work
* Property delegation verified
* Tests compile and pass

Dependencies:
* Phase 2 (BlazorTestContext must be implemented)
* Phase 5 (test infrastructure must be in place)

## Implementation Phase 10: Cleanup and Validation

<!-- parallelizable: false -->

### Step 10.1: Delete Placeholder.cs files

Delete all 4 placeholder files:
* `srcnew/Brinell.Blazor/Context/Placeholder.cs`
* `srcnew/Brinell.Blazor/Controls/Placeholder.cs`
* `srcnew/Brinell.Blazor/Pages/Placeholder.cs`
* `srcnew/Brinell.Blazor/Testing/Placeholder.cs`

### Step 10.2: Run full project build

Command: `dotnet build srcnew/Brinell.sln`

Expected: No errors. All 22 control files + 3 infrastructure files compile.

### Step 10.3: Run all unit tests

Command: `dotnet test testsnew/Brinell.Blazor.Tests/Brinell.Blazor.Tests.csproj`

Expected: All tests pass.

### Step 10.4: Fix minor validation issues

Iterate on lint errors, build warnings, and test failures. Apply fixes directly when corrections are straightforward and isolated.

### Step 10.5: Report blocking issues

When validation failures require changes beyond minor fixes:
* Document the issues and affected files.
* Provide the user with next steps.
* Recommend additional research and planning rather than inline fixes.
* Avoid large-scale refactoring within this phase.

## Dependencies

* `srcnew/Brinell.Html` — Base classes, interfaces, and controls (must already be implemented)
* `srcnew/Brinell.Html.Playwright` — `PlaywrightHtmlElement` and `PlaywrightTestContext` (must already be implemented)
* `srcnew/Brinell.Core` — `Locator`, `IElement`, `ITestContext`, `TimeoutSettings`, `LocatorStrategy`

## Success Criteria

* `dotnet build srcnew/Brinell.sln` succeeds with no errors
* `dotnet test testsnew/Brinell.Blazor.Tests/` passes all tests
* All 22 controls implemented (14 inherited + 5 Blazor-only + 1 MediaControlBase + 2 thin Audio/Video)
* Infrastructure classes implemented (BlazorTestContext, BlazorPageObjectBase, BlazorTestFixtureBase)
* `IHtmlElement` has `Evaluate<T>()` + `Evaluate()` methods
* No Placeholder.cs files remain
* 20 test files + MockHtmlFactory created
