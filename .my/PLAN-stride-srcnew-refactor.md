# Plan: Stride Framework Refactor to srcnew

**Created:** February 22, 2026  
**Status:** ✅ COMPLETE  
**Goal:** Migrate the Stride UI test automation framework from `src/` to `srcnew/`, adopting the new generic `TScope` pattern used by `srcnew/Brinell.Maui`, and wire up tests + sample app.

---

## Overview

| Component | Old Location | New Location | Status |
|-----------|-------------|--------------|--------|
| In-game automation server | `src/Brinell.Stride.Automation/` | `srcnew/Brinell.Automation/` | ✅ DONE |
| Test-side client library | `src/Brinell.Stride/` | `srcnew/Brinell.Stride/` | ✅ DONE |
| UI tests | `samples/Brinell.Samples.Stride.UITests/` | `testsnew/Brinell.Stride.UITests/` | ✅ DONE |
| Unit tests | (none) | `testsnew/Brinell.Stride.Tests/` | ✅ DONE (skeletal) |
| Sample app | `samples/Brinell.Samples.Stride.App/` | (reuse, update refs) | ✅ DONE |

---

## Architecture Decisions

1. **Communication DTOs duplicated** in both `Brinell.Automation.Communication` and `Brinell.Stride.Communication` namespaces. They talk via JSON wire protocol over named pipes — no shared type references needed.
2. **`Brinell.Automation` does NOT reference `Brinell.Stride`** — breaks the old circular dependency. The automation server is a standalone in-game component.
3. **New generic `TScope` pattern** — `ControlBase<TScope>` inherits `ControlObjectBase<TScope>` from Core, with `IStrideScope<TScope>` parallel to `IMauiScope<TScope>`.
4. **No live element handles** — unlike MAUI (Appium/FlaUI elements), Stride controls get `ElementState` snapshots via pipe commands. Controls store `automationId` and query state on demand.
5. **CRTP page pattern** — `PageObjectBase<TSelf>` follows the same pattern as MAUI's `PageObjectBase<TSelf>`.

---

## Phase 1: Brinell.Automation (srcnew) — ✅ DONE

In-game automation server. All files created/replaced from placeholders.

| File | Description |
|------|-------------|
| `Communication/AutomationCommand.cs` | Wire protocol command DTO |
| `Communication/AutomationResponse.cs` | Wire protocol response DTO |
| `Communication/ElementState.cs` | Element state DTO with ElementBounds |
| `Communication/WindowInfo.cs` | Window position/size DTO |
| `IAutomationHandler.cs` | Handler interface |
| `AutomationGameSystem.cs` | Stride GameSystemBase, starts server |
| `AutomationServer.cs` | Named pipe server (multi-client, JSON protocol) |
| `StrideUIHandler.cs` | Full handler: UI tree search, state extraction, actions, screenshots |
| `StrideAutomationExtensions.cs` | `UseAutomation()` / `UseAutomationIfEnabled()` extensions |

---

## Phase 2: Brinell.Stride (srcnew) — 🔄 IN PROGRESS

Test-side client library. Follows the new Core/Maui pattern.

### Current state
- `GlobalUsings.cs` — ✅ created
- `Communication/AutomationCommand.cs` — ✅ created
- `Communication/Placeholder.cs` — remove after all Communication files done
- `Context/Placeholder.cs` — remove after Context files done
- `Controls/Placeholder.cs` — remove after Controls files done
- `Testing/Placeholder.cs` — remove after Testing files done

### 2a. Communication (client-side copies from old `src/Brinell.Stride/Communication/`)

| File | Source | Status |
|------|--------|--------|
| `Communication/AutomationCommand.cs` | old `Communication/AutomationCommand.cs` | ✅ DONE |
| `Communication/AutomationResponse.cs` | old `Communication/AutomationResponse.cs` | ❌ |
| `Communication/ElementState.cs` | old `Communication/ElementState.cs` | ❌ |
| `Communication/WindowInfo.cs` | old `Communication/WindowInfo.cs` | ❌ |
| `Communication/IAutomationChannel.cs` | old `Communication/IAutomationChannel.cs` | ❌ |
| `Communication/NamedPipeChannel.cs` | old `Communication/NamedPipeChannel.cs` | ❌ |

These are straight copies with namespace `Brinell.Stride.Communication` (same as old).

### 2b. Interfaces (NEW — parallel to Maui's `Interfaces/`)

| File | Description |
|------|-------------|
| `Interfaces/IStrideScope.cs` | `IStrideScope<TScope>` — parallel to `IMauiScope<TScope>`, extends `IElementScope`. Properties: `IStrideTestContext Context`, `TScope Self`. |
| `Interfaces/IStrideTestContext.cs` | Extends `ITestContext` — adds `GetElementState()`, `ClickElement()`, `SetElementText()`, `SetSliderValue()`, `SetToggleValue()`, `SendCommand()`, `Input` (simulator), `EnsureGameHasFocus()`, `IsGameReady`. |

### 2c. Context (adapted from old `Infrastructure/StrideTestContext.cs` + `StrideTestOptions.cs`)

| File | Description |
|------|-------------|
| `Context/StrideTestContext.cs` | Implements `IStrideTestContext` / `ITestContext`. Wraps `NamedPipeChannel`, `StrideGameDriver`, `StrideInputSimulator`. Sends commands, provides element state. |
| `Context/StrideTestContextOptions.cs` | Options class (pipe name, game exe path, timeouts). Adapted from old `StrideTestOptions`. |

### 2d. Infrastructure (adapted from old `Infrastructure/`)

| File | Description |
|------|-------------|
| `Infrastructure/StrideGameDriver.cs` | Game process lifecycle (launch, kill, wait for pipe). From old `StrideGameDriver.cs`. |
| `Infrastructure/StrideInputSimulator.cs` | Windows `SendInput` P/Invoke for keyboard/mouse. From old `StrideInputSimulator.cs`. |
| `Infrastructure/VirtualKey.cs` | VirtualKey enum (extracted from old StrideInputSimulator). |

### 2e. Controls (NEW generic pattern — adapted from old `Controls/Base/` + concrete)

**Base controls** (in `Controls/`):

| File | Old Source | Description |
|------|-----------|-------------|
| `Controls/ControlBase.cs` | `StrideControlBase.cs` | `ControlBase<TScope> : ControlObjectBase<TScope>` — core Stride control. Uses `ElementState` via pipe. Provides `Is*/Wait*/Assert*` methods. |
| `Controls/ClickableControlBase.cs` | (new, from MAUI pattern) | `ClickableControlBase<TScope> : ControlBase<TScope>` — adds `Click()`, `DoubleClick()`. |
| `Controls/TextControlBase.cs` | `StrideTextControlBase.cs` | `TextControlBase<TScope> : ControlBase<TScope>` — adds `GetText()`, `Enter()`, `Clear()`. |
| `Controls/ContentControlBase.cs` | `StrideContentControlBase.cs` | `ContentControlBase<TScope> : ControlBase<TScope>` — adds `GetContent()`. |
| `Controls/ToggleControlBase.cs` | `StrideToggleControlBase.cs` | `ToggleControlBase<TScope> : ClickableControlBase<TScope>` — adds `IsChecked()`, `Toggle()`. |
| `Controls/RangeControlBase.cs` | `StrideRangeControlBase.cs` | `RangeControlBase<TScope> : ControlBase<TScope>` — adds `GetValue()`, `SetValue()`, `GetMinimum()`, `GetMaximum()`. |
| `Controls/SelectorControlBase.cs` | `StrideSelectorControlBase.cs` | `SelectorControlBase<TScope> : ControlBase<TScope>` — adds `GetItems()`, `GetSelectedIndex()`, `SelectItem()`. |

**Concrete controls** (in `Controls/`):

| File | Old Source |
|------|-----------|
| `Controls/Button.cs` | `StrideButtonControl.cs` |
| `Controls/CheckBox.cs` | `StrideCheckBoxControl.cs` |
| `Controls/ComboBox.cs` | `StrideComboBoxControl.cs` |
| `Controls/EditText.cs` | `StrideEditTextControl.cs` |
| `Controls/Image.cs` | `StrideImageControl.cs` |
| `Controls/ListBox.cs` | `StrideListBoxControl.cs` |
| `Controls/Panel.cs` | `StridePanelControl.cs` |
| `Controls/ProgressBar.cs` | `StrideProgressBarControl.cs` |
| `Controls/Slider.cs` | `StrideSliderControl.cs` |
| `Controls/TextBlock.cs` | `StrideTextBlockControl.cs` |
| `Controls/ToggleButton.cs` | `StrideToggleButtonControl.cs` |

### 2f. Pages

| File | Description |
|------|-------------|
| `Pages/PageObjectBase.cs` | `PageObjectBase<TSelf>` — CRTP page base. Factory methods for creating controls by automation ID. Adapted from MAUI `PageObjectBase<TSelf>`. |

### 2g. Testing

| File | Description |
|------|-------------|
| `Testing/StrideTestFixtureBase.cs` | xUnit fixture base — manages game process lifecycle, pipe connection, creates `StrideTestContext`. Adapted from old test base + MAUI's `MauiTestFixtureBase`. |

### 2h. Cleanup

- Remove `Communication/Placeholder.cs`
- Remove `Context/Placeholder.cs`
- Remove `Controls/Placeholder.cs`
- Remove `Testing/Placeholder.cs`

---

## Phase 3: testsnew/Brinell.Stride.UITests

Populate with real UI tests adapted from `samples/Brinell.Samples.Stride.UITests/`.

| File | Source | Description |
|------|--------|-------------|
| `StrideUITestBase.cs` | `samples/.../StrideUITestBase.cs` | Base class using `StrideTestFixtureBase` |
| `PageObjects/MainPage.cs` | `samples/.../PageObjects/MainPage.cs` | Main page object using new `PageObjectBase<TSelf>` |
| `PageObjects/GamePage.cs` | `samples/.../PageObjects/GamePage.cs` | Game page object |
| `PageObjects/SettingsPage.cs` | `samples/.../PageObjects/SettingsPage.cs` | Settings page object |
| `Tests/CounterTests.cs` | `samples/.../Tests/CounterTests.cs` | Counter test |
| `Tests/GreetingTests.cs` | `samples/.../Tests/GreetingTests.cs` | Greeting/text test |
| `Tests/SettingsTests.cs` | `samples/.../Tests/SettingsTests.cs` | Settings toggle/combo test |
| `Tests/GameplayTests.cs` | `samples/.../Tests/GameplayTests.cs` | Gameplay-related test |

Also:
- Update `GlobalUsings.cs` to uncomment/add proper usings
- Verify `csproj` references are correct (srcnew/Brinell.Core, srcnew/Brinell.Stride, srcnew/Brinell.Automation)

---

## Phase 4: Sample App Integration

Update `samples/Brinell.Samples.Stride.App/` to reference the new srcnew automation:

| Change | File | Details |
|--------|------|---------|
| Update project reference | `Brinell.Samples.Stride.App.csproj` | Change `src\Brinell.Stride.Automation\...` → `srcnew\Brinell.Automation\Brinell.Automation.csproj` |
| Update using | `SampleStrideGame.cs` | Change `using Brinell.Stride.Automation;` → `using Brinell.Automation;` |

---

## Phase 5: Build Verification

1. `dotnet build srcnew/Brinell.Automation/Brinell.Automation.csproj`
2. `dotnet build srcnew/Brinell.Stride/Brinell.Stride.csproj`
3. `dotnet build testsnew/Brinell.Stride.UITests/Brinell.Stride.UITests.csproj`
4. `dotnet build testsnew/Brinell.Stride.Tests/Brinell.Stride.Tests.csproj`
5. `dotnet build samples/Brinell.Samples.Stride.App/Brinell.Samples.Stride.App.csproj`
6. Fix any compilation errors

---

## File Count Summary

| Phase | New Files | Modified Files |
|-------|-----------|----------------|
| Phase 1 (Automation) | 9 | 0 |
| Phase 2 (Stride lib) | ~27 | 0 (remove 4 placeholders) |
| Phase 3 (UI tests) | ~9 | 1 (GlobalUsings.cs) |
| Phase 4 (Sample app) | 0 | 2 |
| **Total** | **~45** | **~7** |
