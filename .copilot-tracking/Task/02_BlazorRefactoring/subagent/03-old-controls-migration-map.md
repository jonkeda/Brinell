# Old Blazor ControlObject6 → New Brinell.Html Migration Map

**Date:** February 23, 2026  
**Source:** `src/Brinell.Blazor/ControlObject6/Controls/`  
**Target:** `srcnew/Brinell.Html/Controls/`

---

## 1. Base Class Hierarchy

### 1.1 AsyncControlObjectBase (Root)

**File:** `src/Brinell.Blazor/ControlObject6/Controls/AsyncControlObjectBase.cs` (L1–L422)  
**Implements:** `IAsyncInteractiveControlObject`  
**Constructor:** `(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)` — also has TestId overload

**Properties:**
| Property | Type | Visibility |
|----------|------|------------|
| `Locator` | `ControlLocator` | public |
| `Page` | `IAsyncPageObject?` | public |
| `Context` | `BlazorTestContext` | protected |
| `PlaywrightPage` | `IPage` | protected |
| `DefaultTimeoutMs` | `int` | protected |

**Methods provided (all async):**
| Category | Methods |
|----------|---------|
| Logging | `Log(string)` |
| Element Finding | `GetLocator()`, `ConvertLocator(ControlLocator)` |
| Existence | `IsExistsAsync`, `WaitExistsAsync`, `CheckExistsAsync`, `AssertExistsAsync` |
| Visibility | `IsVisibleAsync`, `WaitVisibleAsync`, `CheckVisibleAsync`, `AssertVisibleAsync` |
| Enabled | `IsEnabledAsync`, `WaitEnabledAsync`, `CheckEnabledAsync`, `AssertEnabledAsync` |
| Text | `GetTextAsync` (virtual), `AssertTextAsync`, `AssertTextContainsAsync`, `AssertTextStartsWithAsync`, `AssertTextEndsWithAsync`, `AssertTextMatchesAsync`, `AssertTextEmptyAsync` |

**Key detail:** `GetTextAsync` uses `InnerTextAsync()` by default (L329). `ConvertLocator` supports 16 locator strategies (L80–L100).

---

### 1.2 AsyncClickableControlBase

**File:** `src/Brinell.Blazor/ControlObject6/Controls/AsyncClickableControlBase.cs` (L1–L72)  
**Extends:** `AsyncControlObjectBase`  
**Implements:** `IAsyncClickableControlObject`

**NEW methods (4 virtual):**
| Method | Description |
|--------|-------------|
| `ClickAsync(int?, CancellationToken)` | Checks visible + enabled, then clicks |
| `DoubleClickAsync(int?, CancellationToken)` | Double-click |
| `RightClickAsync(int?, CancellationToken)` | Right-click via MouseButton.Right |
| `HoverAsync(int?, CancellationToken)` | Hover (checks visible only) |

All methods perform pre-condition checks (`CheckVisibleAsync` + `CheckEnabledAsync`) before the Playwright action.

---

### 1.3 AsyncTextControlBase

**File:** `src/Brinell.Blazor/ControlObject6/Controls/AsyncTextControlBase.cs` (L1–L231)  
**Extends:** `AsyncClickableControlBase`  
**Implements:** `IAsyncTextControlObject`

**NEW methods (all virtual):**
| Category | Methods |
|----------|---------|
| Focus | `IsFocusedAsync`, `WaitFocusedAsync`, `CheckFocusedAsync`, `AssertFocusedAsync`, `FocusAsync`, `BlurAsync` |
| Text Input | `EnterAsync(string?)`, `ClearAsync`, `ClearAndEnterAsync(string?)`, `AppendAsync(string?)` |
| Read-Only | `IsReadOnlyAsync`, `WaitReadOnlyAsync`, `AssertReadOnlyAsync` |
| Text Length | `GetTextLengthAsync`, `AssertTextLengthAsync` |
| Text (override) | `GetTextAsync` — **overrides** base to use `InputValueAsync()` instead of `InnerTextAsync()` (L225) |

---

## 2. Control-by-Control Mapping (19 Concrete Controls)

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Direct equivalent exists in `srcnew/Brinell.Html/Controls/` |
| ❌ | No equivalent — Blazor/HTML-only control |
| 🔄 | Partial equivalent (different name or structure) |
| 🟢 | Pure inheritance — no new methods beyond base |
| 🔵 | Adds Blazor-specific logic |

---

### 2.1 Pure Inheritance Controls (no new methods)

#### ButtonControl 🟢 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/ButtonControl.cs` (L1–L33)  
**Base:** `AsyncClickableControlBase`  
**New methods:** None — comment says "All click methods are inherited"  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Buttons/ButtonControl.cs`

#### InputControl 🟢 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/InputControl.cs` (L1–L30)  
**Base:** `AsyncTextControlBase`  
**New methods:** None — comment says "All text input methods are inherited"  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Text/TextInputControl.cs`

---

### 2.2 Controls with Additional Logic

#### AudioControl 🔵 ❌

**File:** `src/Brinell.Blazor/ControlObject6/Controls/AudioControl.cs` (L1–L214)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** **NONE — Blazor-only**

**New methods (15):**
| Category | Methods |
|----------|---------|
| Playback | `PlayAsync`, `PauseAsync`, `IsPlayingAsync`, `IsPausedAsync`, `IsEndedAsync` |
| Time | `GetCurrentTimeAsync`, `SeekAsync(double)`, `GetDurationAsync` |
| Volume | `GetVolumeAsync`, `SetVolumeAsync(double)`, `IsMutedAsync`, `MuteAsync`, `UnmuteAsync` |
| Source | `GetSourceAsync` |
| Assertions | `AssertPlayingAsync`, `AssertPausedAsync` |

All use Playwright `EvaluateAsync` with JS expressions on `<audio>` element API.

---

#### CheckBoxControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/CheckBoxControl.cs` (L1–L145)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Toggle/CheckBoxControl.cs`

**New methods (7):**
| Method | Description |
|--------|-------------|
| `IsCheckedAsync` | Uses Playwright `IsCheckedAsync()` |
| `WaitCheckedAsync(bool?)` | Polls until expected state |
| `CheckStateAsync(bool?)` | Throws on timeout |
| `AssertCheckedAsync(bool?)` | Assert checked state |
| `SetCheckedAsync(bool?)` | Uses Playwright `SetCheckedAsync()` |
| `CheckAsync` | Alias for `SetCheckedAsync(true)` |
| `UncheckAsync` | Alias for `SetCheckedAsync(false)` |
| `ToggleAsync` | Reads current, sets opposite |

---

#### DateInputControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/DateInputControl.cs` (L1–L104)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/DateTime/DateInputControl.cs`

**New methods (6):**
| Method | Description |
|--------|-------------|
| `GetDateAsync` | Returns `DateOnly?` from input value |
| `SetDateAsync(DateOnly?)` | Fills with `yyyy-MM-dd` format |
| `GetMinDateAsync` | From `min` attribute |
| `GetMaxDateAsync` | From `max` attribute |
| `AssertDateAsync(DateOnly?)` | Assert date value |
| `ClearAsync` | Clears the input |

---

#### IFrameControl 🔵 ❌

**File:** `src/Brinell.Blazor/ControlObject6/Controls/IFrameControl.cs` (L1–L163)  
**Base:** `AsyncControlObjectBase` (NOT clickable)  
**srcnew equivalent:** **NONE — Blazor-only**

**New methods (11):**
| Category | Methods |
|----------|---------|
| Properties | `GetSourceAsync`, `GetTitleAsync`, `GetNameAsync` |
| Frame Access | `GetFrameLocatorAsync` — returns `IFrameLocator` for inner content |
| Inner Interaction | `ClickInsideAsync(selector)`, `FillInsideAsync(selector, text)`, `GetTextInsideAsync(selector)`, `ElementExistsInsideAsync(selector)`, `WaitForElementInsideAsync(selector)` |
| Assertions | `AssertSourceAsync`, `AssertSourceContainsAsync`, `AssertElementExistsInsideAsync` |

**Key detail:** Uses Playwright `FrameLocator(".")` pattern for cross-frame interaction. This is unique to HTML/Playwright — no native equivalent.

---

#### ImageControl 🔵 ❌

**File:** `src/Brinell.Blazor/ControlObject6/Controls/ImageControl.cs` (L1–L150)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** **NONE — Blazor-only**

**New methods (9):**
| Category | Methods |
|----------|---------|
| Properties | `GetSourceAsync` (from `src` attr), `GetAltTextAsync` (from `alt` attr) |
| Load State | `IsLoadedAsync` (via JS `img.complete && img.naturalWidth > 0`), `WaitLoadedAsync(bool?)` |
| Dimensions | `GetNaturalWidthAsync`, `GetNaturalHeightAsync` |
| Assertions | `AssertSourceAsync`, `AssertSourceContainsAsync`, `AssertAltTextAsync` |

---

#### LinkControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/LinkControl.cs` (L1–L80)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Buttons/LinkControl.cs`

**New methods (4):**
| Method | Description |
|--------|-------------|
| `GetHrefAsync` | From `href` attribute |
| `GetTargetAsync` | From `target` attribute |
| `AssertHrefAsync(string?)` | Assert href matches |
| `AssertHrefContainsAsync(string?)` | Assert href contains |

---

#### ListControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/ListControl.cs` (L1–L159)  
**Base:** `AsyncControlObjectBase` (NOT clickable)  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Collection/ListControl.cs`

**New methods (9):**
| Category | Methods |
|----------|---------|
| Items | `GetItemCountAsync`, `GetItemsAsync` (returns `IReadOnlyList<string>`), `GetItemTextAsync(int)` |
| Interaction | `ClickItemAsync(int)`, `ClickItemByTextAsync(string?)` |
| Query | `HasItemAsync(string?)` |
| Assertions | `AssertItemCountAsync(int?)`, `AssertHasItemAsync(string?)`, `AssertItemTextAsync(int, string?)` |

Targets `<ul>`/`<ol>` with `li` child elements.

---

#### NavMenuControl 🔵 ❌

**File:** `src/Brinell.Blazor/ControlObject6/Controls/NavMenuControl.cs` (L1–L178)  
**Base:** `AsyncControlObjectBase` (NOT clickable)  
**srcnew equivalent:** **NONE — Blazor-only**

**New methods (12):**
| Category | Methods |
|----------|---------|
| Items | `GetItemCountAsync`, `GetItemsAsync` (returns `IReadOnlyList<string>`) |
| Active State | `GetActiveItemAsync` (checks `.active`, `aria-current`), `IsActiveAsync(string?)` |
| Navigation | `NavigateToAsync(string?)`, `NavigateToIndexAsync(int)` |
| Properties | `GetItemHrefAsync(string?)` |
| Query | `HasItemAsync(string?)` |
| Assertions | `AssertActiveItemAsync(string?)`, `AssertHasItemAsync(string?)`, `AssertItemCountAsync(int?)` |

Selectors: `a, .nav-link, [role='menuitem']` — Blazor nav pattern specific.

---

#### ProgressControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/ProgressControl.cs` (L1–L123)  
**Base:** `AsyncControlObjectBase` (NOT clickable)  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Display/ProgressControl.cs`

**New methods (6):**
| Method | Description |
|--------|-------------|
| `GetProgressAsync` | Returns 0–1 (value/max) |
| `IsIndeterminateAsync` | True when no `value` attribute |
| `WaitProgressAsync(double?)` | Polls with 0.01 tolerance |
| `AssertProgressAsync(double?)` | Assert progress value |
| `AssertProgressInRangeAsync(double?, double?)` | Assert within range |
| `WaitCompleteAsync` | Shortcut for `WaitProgressAsync(1.0)` |

---

#### RadioButtonControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/RadioButtonControl.cs` (L1–L106)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Toggle/RadioButtonControl.cs`

**New methods (6):**
| Method | Description |
|--------|-------------|
| `IsCheckedAsync` | Playwright `IsCheckedAsync()` |
| `WaitCheckedAsync(bool?)` | Polls until state |
| `AssertCheckedAsync(bool?)` | Assert state |
| `SelectAsync` | Uses Playwright `CheckAsync()` |
| `GetGroupNameAsync` | From `name` attribute |
| `GetValueAsync` | From `value` attribute |

---

#### RangeControl 🔵 🔄

**File:** `src/Brinell.Blazor/ControlObject6/Controls/RangeControl.cs` (L1–L169)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Range/RangeInputControl.cs` (renamed)

**New methods (10):**
| Category | Methods |
|----------|---------|
| Value | `GetValueAsync` (returns `double`), `SetValueAsync(double?)` |
| Range | `GetMinimumAsync`, `GetMaximumAsync`, `GetStepAsync` |
| Step | `IncrementAsync(int?)`, `DecrementAsync(int?)` — calculates new value from step/min/max |
| Assertions | `AssertValueAsync(double?)`, `AssertValueInRangeAsync(double?, double?)` |

---

#### SelectControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/SelectControl.cs` (L1–L195)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Selection/SelectControl.cs`

**New methods (10):**
| Category | Methods |
|----------|---------|
| Items | `GetItemsAsync` (returns `IReadOnlyList<string>`), `GetItemCountAsync` |
| Selected | `GetSelectedItemAsync`, `GetSelectedIndexAsync` |
| Selection | `SelectItemAsync(string?)`, `SelectItemByIndexAsync(int?)`, `SelectItemByValueAsync(string?)` |
| Assertions | `AssertSelectedItemAsync(string?)`, `AssertSelectedIndexAsync(int?)`, `AssertItemCountAsync(int?)`, `AssertHasItemAsync(string?)` |

Uses Playwright `SelectOptionAsync` with `SelectOptionValue`.

---

#### TabControl 🔵 🔄

**File:** `src/Brinell.Blazor/ControlObject6/Controls/TabControl.cs` (L1–L193)  
**Base:** `AsyncControlObjectBase` (NOT clickable)  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Container/TabContainerControl.cs` (renamed)

**New methods (10):**
| Category | Methods |
|----------|---------|
| Tabs | `GetTabCountAsync`, `GetTabsAsync` (returns `IReadOnlyList<string>`) |
| Selected | `GetSelectedIndexAsync` (checks `aria-selected` + `.active`), `GetSelectedTabAsync` |
| Selection | `SelectTabAsync(int)`, `SelectTabByTextAsync(string?)` |
| Waiting | `WaitSelectedAsync(int?)` |
| Assertions | `AssertSelectedIndexAsync(int?)`, `AssertSelectedTabAsync(string?)`, `AssertTabCountAsync(int?)` |

Selectors: `[role='tab'], .nav-link, .tab-link, [data-tab]`

---

#### TableControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/TableControl.cs` (L1–L238)  
**Base:** `AsyncControlObjectBase` (NOT clickable)  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Collection/TableControl.cs`

**New methods (13):**
| Category | Methods |
|----------|---------|
| Counts | `GetRowCountAsync`, `GetColumnCountAsync`, `GetHeaderRowCountAsync` |
| Cell Access | `GetCellTextAsync(row, col)`, `GetRowTextAsync(row)`, `GetColumnTextAsync(col)`, `GetHeaderTextAsync(col)`, `GetHeadersAsync` |
| Clicking | `ClickRowAsync(row)`, `ClickCellAsync(row, col)`, `ClickHeaderAsync(col)` |
| Assertions | `AssertRowCountAsync(int?)`, `AssertColumnCountAsync(int?)`, `AssertCellTextAsync(row, col, string?)` |

Uses `tbody tr` / `thead tr th` / `td:nth-child` CSS selectors.

---

#### TextAreaControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/TextAreaControl.cs` (L1–L52)  
**Base:** `AsyncTextControlBase`  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/Text/TextAreaControl.cs`

**New methods (3):**
| Method | Description |
|--------|-------------|
| `GetRowsAsync` | From `rows` attribute → `int?` |
| `GetColsAsync` | From `cols` attribute → `int?` |
| `GetMaxLengthAsync` | From `maxlength` attribute → `int?` |

---

#### TimeInputControl 🔵 ✅

**File:** `src/Brinell.Blazor/ControlObject6/Controls/TimeInputControl.cs` (L1–L97)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** `srcnew/Brinell.Html/Controls/DateTime/TimeInputControl.cs`

**New methods (6):**
| Method | Description |
|--------|-------------|
| `GetTimeAsync` | Returns `TimeOnly?` from input value |
| `SetTimeAsync(TimeOnly?)` | Fills with `HH:mm` format |
| `GetMinTimeAsync` | From `min` attribute |
| `GetMaxTimeAsync` | From `max` attribute |
| `AssertTimeAsync(TimeOnly?)` | Assert time value |
| `ClearAsync` | Clears the input |

---

#### VideoControl 🔵 ❌

**File:** `src/Brinell.Blazor/ControlObject6/Controls/VideoControl.cs` (L1–L234)  
**Base:** `AsyncClickableControlBase`  
**srcnew equivalent:** **NONE — Blazor-only**

**New methods (18):**
| Category | Methods |
|----------|---------|
| Playback | `PlayAsync`, `PauseAsync`, `IsPlayingAsync`, `IsPausedAsync`, `IsEndedAsync` |
| Time | `GetCurrentTimeAsync`, `SeekAsync(double)`, `GetDurationAsync` |
| Volume | `GetVolumeAsync`, `SetVolumeAsync(double)`, `IsMutedAsync`, `MuteAsync`, `UnmuteAsync` |
| Source | `GetSourceAsync`, `GetPosterAsync` |
| Assertions | `AssertPlayingAsync`, `AssertPausedAsync` |

Identical pattern to AudioControl but with `video` JS expressions + `GetPosterAsync`.

---

## 3. Summary Tables

### 3.1 Control Migration Status

| # | Old Control | Base Class | New Methods | srcnew Equivalent | Status |
|---|-------------|-----------|-------------|-------------------|--------|
| 1 | `ButtonControl` | `AsyncClickableControlBase` | 0 | `Buttons/ButtonControl.cs` | ✅ Direct |
| 2 | `InputControl` | `AsyncTextControlBase` | 0 | `Text/TextInputControl.cs` | 🔄 Renamed |
| 3 | `TextAreaControl` | `AsyncTextControlBase` | 3 | `Text/TextAreaControl.cs` | ✅ Direct |
| 4 | `CheckBoxControl` | `AsyncClickableControlBase` | 7 | `Toggle/CheckBoxControl.cs` | ✅ Direct |
| 5 | `RadioButtonControl` | `AsyncClickableControlBase` | 6 | `Toggle/RadioButtonControl.cs` | ✅ Direct |
| 6 | `LinkControl` | `AsyncClickableControlBase` | 4 | `Buttons/LinkControl.cs` | ✅ Direct |
| 7 | `SelectControl` | `AsyncClickableControlBase` | 10 | `Selection/SelectControl.cs` | ✅ Direct |
| 8 | `DateInputControl` | `AsyncClickableControlBase` | 6 | `DateTime/DateInputControl.cs` | ✅ Direct |
| 9 | `TimeInputControl` | `AsyncClickableControlBase` | 6 | `DateTime/TimeInputControl.cs` | ✅ Direct |
| 10 | `RangeControl` | `AsyncClickableControlBase` | 10 | `Range/RangeInputControl.cs` | 🔄 Renamed |
| 11 | `ListControl` | `AsyncControlObjectBase` | 9 | `Collection/ListControl.cs` | ✅ Direct |
| 12 | `TableControl` | `AsyncControlObjectBase` | 13 | `Collection/TableControl.cs` | ✅ Direct |
| 13 | `ProgressControl` | `AsyncControlObjectBase` | 6 | `Display/ProgressControl.cs` | ✅ Direct |
| 14 | `TabControl` | `AsyncControlObjectBase` | 10 | `Container/TabContainerControl.cs` | 🔄 Renamed |
| 15 | `AudioControl` | `AsyncClickableControlBase` | 15 | — | ❌ Blazor-only |
| 16 | `VideoControl` | `AsyncClickableControlBase` | 18 | — | ❌ Blazor-only |
| 17 | `IFrameControl` | `AsyncControlObjectBase` | 11 | — | ❌ Blazor-only |
| 18 | `ImageControl` | `AsyncClickableControlBase` | 9 | — | ❌ Blazor-only |
| 19 | `NavMenuControl` | `AsyncControlObjectBase` | 12 | — | ❌ Blazor-only |

### 3.2 Base Class Usage Distribution

| Base Class | Controls Using It |
|------------|-------------------|
| `AsyncControlObjectBase` | ListControl, NavMenuControl, ProgressControl, TabControl, TableControl, IFrameControl (6) |
| `AsyncClickableControlBase` | ButtonControl, AudioControl, CheckBoxControl, DateInputControl, ImageControl, LinkControl, RadioButtonControl, RangeControl, SelectControl, TimeInputControl, VideoControl (11) |
| `AsyncTextControlBase` | InputControl, TextAreaControl (2) |

### 3.3 Pure Inheritance vs. Adds Logic

| Category | Controls | Count |
|----------|----------|-------|
| **Pure inheritance** (0 new methods) | ButtonControl, InputControl | 2 |
| **Light additions** (1–6 methods) | TextAreaControl (3), LinkControl (4), DateInputControl (6), TimeInputControl (6), ProgressControl (6), RadioButtonControl (6) | 6 |
| **Medium additions** (7–10 methods) | CheckBoxControl (7), ListControl (9), ImageControl (9), RangeControl (10), SelectControl (10), TabControl (10) | 6 |
| **Heavy additions** (11+ methods) | IFrameControl (11), NavMenuControl (12), TableControl (13), AudioControl (15), VideoControl (18) | 5 |

### 3.4 The 5 Blazor-Only Controls — Detail

These controls have **no equivalent** in `srcnew/Brinell.Html/` and need decisions:

| Control | HTML Element | Method Count | Complexity | Recommendation |
|---------|-------------|-------------|------------|----------------|
| `AudioControl` | `<audio>` | 15 | Medium | Create in Html layer — uses standard HTML5 media API |
| `VideoControl` | `<video>` | 18 | Medium | Create in Html layer — uses standard HTML5 media API |
| `ImageControl` | `<img>` | 9 | Low | Create in Html layer — uses standard HTML attributes |
| `IFrameControl` | `<iframe>` | 11 | High | Create in Html layer — Playwright `FrameLocator` specific |
| `NavMenuControl` | `<nav>` | 12 | Medium | Could be Html layer or Blazor-specific (uses `.nav-link`, `aria-current`) |

**Note:** Audio/Video share identical patterns (Play, Pause, Seek, Volume, Mute). A shared `MediaControlBase` could reduce duplication.

---

## 4. Key Architectural Differences: Old → New

| Aspect | Old (ControlObject6) | New (Brinell.Html) |
|--------|---------------------|-------------------|
| Naming | `Async*ControlBase` | `*ControlBase` (sync wrappers) |
| Context | `BlazorTestContext` | Platform-agnostic via `IHtmlDriver` |
| Locators | `ControlLocator` + `ConvertLocator()` inline | Separate locator system |
| Constructor | `(context, locator, page)` | Interface-based DI |
| Base classes | 3 (`ControlObject`, `Clickable`, `Text`) | 8+ (adds `Focusable`, `Toggle`, `Range`, `Selector`, `Scrollable`, `Container`) |
| Method style | All `*Async` with `CancellationToken` | Sync with `IHtmlDriver` abstraction |

---

## 5. Constructor Pattern (All Controls)

Every old control follows the same constructor pattern:

```csharp
// Locator constructor
public XxxControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
    : base(context, locator, page) { }

// TestId shorthand constructor
public XxxControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
    : base(context, testId, page) { }
```

All 19 controls + 3 base classes use this exact same 2-constructor pattern.
