# PLAN-006: WinForms Controls Rebuild

## Overview

Rebuild WinForms controls by extracting shared FlaUI infrastructure into a common project, then having both WPF and WinForms reference it. This follows the same pattern MAUI uses with Appium.

## Architecture Analysis

### Current State
```
Brinell.Core          - Abstractions (IDriverAdapter, ITestContext, IElementAdapter, etc.)
Brinell.Wpf           - FlaUI infrastructure + Controls (namespace: Brinell.Wpf)
Brinell.WinForms      - FlaUI infrastructure + Controls (namespace: Brinell.WinForms)  
Brinell.Maui          - Appium infrastructure + Controls
```

### Problem
- WPF and WinForms have **duplicate** FlaUI infrastructure (4 files each)
- Controls are 95% identical, only namespace differs
- Both use same FlaUI NuGet packages
- Violates DRY principle

### MAUI Pattern (Reference)
MAUI has its own infrastructure because it uses **Appium** (different driver):
- `AppiumDriverAdapter` - Appium-specific driver
- `AppiumTestContext` - Uses `AppiumElement` and `AppiumDriver`
- Container support: `FindElementInContainer(AppiumElement container, string automationId)`

### Proposed Architecture
```
Brinell.Core          - Keep as-is (abstractions)
Brinell.FlaUI         - NEW: Shared FlaUI infrastructure
Brinell.Wpf           - Controls only, references Brinell.FlaUI
Brinell.WinForms      - Controls only, references Brinell.FlaUI
Brinell.Maui          - Keep as-is (Appium-based)
```

## Key Finding: Container Support

All three platforms support container-scoped element finding:

| Platform | Container Type | FindElement Method |
|----------|---------------|-------------------|
| MAUI | `AppiumElement` | `container.FindElement(MobileBy.AccessibilityId(id))` |
| WPF | `AutomationElement` | `container.FindFirstDescendant(cf => cf.ByAutomationId(id))` |
| WinForms | `AutomationElement` | `container.FindFirstDescendant(cf => cf.ByAutomationId(id))` |

WPF and WinForms use **identical** FlaUI code for container support!

## Detailed Plan

### Phase 1: Create Brinell.FlaUI Project

Create new shared project with infrastructure:

```
src/Brinell.FlaUI/
├── Brinell.FlaUI.csproj
├── FlaUIDriverAdapter.cs      (from WPF, unchanged)
├── FlaUIElementAdapter.cs     (from WPF, unchanged)  
├── FlaUIScreenshotService.cs  (from WPF, unchanged)
├── FlaUITestContext.cs        (from WPF, unchanged)
└── Controls/
    └── Base/
        ├── ControlBase.cs     (shared, with GetRequiredElement fix)
        ├── PageBase.cs        (shared)
        ├── ContentControlBase.cs
        ├── TextControlBase.cs
        ├── ToggleControlBase.cs
        ├── RangeControlBase.cs
        ├── SelectorControlBase.cs
        └── ItemsControlBase.cs
```

**csproj settings:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
    <PackageReference Include="FlaUI.Core" />
    <PackageReference Include="FlaUI.UIA3" />
  </ItemGroup>
</Project>
```

### Phase 2: Refactor WPF to Use Brinell.FlaUI

1. Remove infrastructure files from `Brinell.Wpf/Infrastructure/`
2. Remove base classes from `Brinell.Wpf/Controls/Base/`
3. Add project reference to `Brinell.FlaUI`
4. Update `using` statements in concrete controls
5. Keep only WPF-specific concrete controls

**Resulting WPF structure:**
```
src/Brinell.Wpf/
├── Brinell.Wpf.csproj
├── Controls/
│   ├── ButtonControl.cs
│   ├── TextBoxControl.cs
│   ├── CheckBoxControl.cs
│   ├── ComboBoxControl.cs
│   ├── ... (concrete controls only)
│   └── MessageBoxDialog.cs
├── Testing/
└── VisualValidation/
```

### Phase 3: Rebuild WinForms Using Brinell.FlaUI

1. Add project reference to `Brinell.FlaUI`
2. Remove empty infrastructure/base folders
3. Create WinForms-specific controls that inherit from shared base classes

**Resulting WinForms structure:**
```
src/Brinell.WinForms/
├── Brinell.WinForms.csproj
├── Controls/
│   ├── ButtonControl.cs
│   ├── TextBoxControl.cs
│   ├── CheckBoxControl.cs
│   ├── ComboBoxControl.cs
│   ├── NumericUpDownControl.cs  (WinForms-specific)
│   ├── DateTimePickerControl.cs (WinForms-specific)
│   ├── DataGridViewControl.cs   (WinForms-specific)
│   ├── ... 
│   └── MessageBoxDialog.cs
├── Extensions/
└── Testing/
```

### Phase 4: Fix Known Issues in Shared Base

Add `GetRequiredElement()` to `ControlBase`:

```csharp
/// <summary>
/// Get the element, throwing CheckFailedException if not found.
/// Use this for Get* methods that require the element to exist.
/// </summary>
protected virtual AutomationElement GetRequiredElement(string action)
{
    var element = FindElement();
    if (element == null)
    {
        ThrowCheckFailed(action, $"Element '{AutomationId}' not found.");
    }
    return element!;
}
```

Update `GetText()` and similar methods:
```csharp
public virtual string GetText()
{
    var element = GetRequiredElement("GetText");  // THROWS if not found
    // ... rest of method
}
```

### Phase 5: Build and Test

1. Build solution
2. Run WPF tests (should pass unchanged)
3. Run WinForms tests
4. Fix any issues

## File-by-File Migration

### Infrastructure (Move to Brinell.FlaUI)

| Current Location | New Location | Changes |
|-----------------|--------------|---------|
| `Wpf/Infrastructure/FlaUIDriverAdapter.cs` | `FlaUI/FlaUIDriverAdapter.cs` | Namespace: `Brinell.FlaUI` |
| `Wpf/Infrastructure/FlaUIElementAdapter.cs` | `FlaUI/FlaUIElementAdapter.cs` | Namespace: `Brinell.FlaUI` |
| `Wpf/Infrastructure/FlaUIScreenshotService.cs` | `FlaUI/FlaUIScreenshotService.cs` | Namespace: `Brinell.FlaUI` |
| `Wpf/Infrastructure/FlaUITestContext.cs` | `FlaUI/FlaUITestContext.cs` | Namespace: `Brinell.FlaUI` |

### Base Controls (Move to Brinell.FlaUI)

| Current Location | New Location | Changes |
|-----------------|--------------|---------|
| `Wpf/Controls/Base/ControlBase.cs` | `FlaUI/Controls/Base/ControlBase.cs` | Namespace + GetRequiredElement |
| `Wpf/Controls/Base/PageBase.cs` | `FlaUI/Controls/Base/PageBase.cs` | Namespace |
| `Wpf/Controls/Base/ContentControlBase.cs` | `FlaUI/Controls/Base/ContentControlBase.cs` | Namespace |
| `Wpf/Controls/Base/TextControlBase.cs` | `FlaUI/Controls/Base/TextControlBase.cs` | Namespace |
| `Wpf/Controls/Base/ToggleControlBase.cs` | `FlaUI/Controls/Base/ToggleControlBase.cs` | Namespace |
| `Wpf/Controls/Base/RangeControlBase.cs` | `FlaUI/Controls/Base/RangeControlBase.cs` | Namespace |
| `Wpf/Controls/Base/SelectorControlBase.cs` | `FlaUI/Controls/Base/SelectorControlBase.cs` | Namespace |
| `Wpf/Controls/Base/ItemsControlBase.cs` | `FlaUI/Controls/Base/ItemsControlBase.cs` | Namespace |

### Delete from WinForms

| File | Action |
|------|--------|
| `WinForms/Infrastructure/*` | Delete (use Brinell.FlaUI) |
| `WinForms/Controls/Base/*` | Delete (use Brinell.FlaUI) |

## Concrete Controls Comparison

### Shared (WPF = WinForms, identical code)

| Control | WPF | WinForms | Notes |
|---------|-----|----------|-------|
| ButtonControl | ✅ | Copy | Identical |
| TextBoxControl | ✅ | Copy | Identical |
| CheckBoxControl | ✅ | Copy | Identical |
| LabelControl | ✅ | Copy | Identical |
| PasswordBoxControl | ✅ | Copy | Identical |
| ComboBoxControl | ✅ | Copy | Identical |
| ListBoxControl | ✅ | Copy | Identical |
| ProgressBarControl | ✅ | Copy | Identical |
| ScrollViewControl | ✅ | Copy | Identical |

### WPF-Specific

| Control | Purpose |
|---------|---------|
| SliderControl | WPF Slider (maps to TrackBar in WinForms) |
| TabItemControl | WPF TabItem |
| TreeViewControl | WPF TreeView |
| MessageBoxDialog | WPF MessageBox |

### WinForms-Specific

| Control | Purpose |
|---------|---------|
| TrackBarControl | WinForms TrackBar (like WPF Slider) |
| TabControlControl | WinForms TabControl |
| NumericUpDownControl | WinForms NumericUpDown (no WPF equivalent) |
| DateTimePickerControl | WinForms DateTimePicker |
| DataGridViewControl | WinForms DataGridView |
| RichTextBoxControl | WinForms RichTextBox |
| RadioButtonControl | WinForms RadioButton |
| GroupBoxControl | WinForms GroupBox |

## Estimated Effort

| Phase | Task | Time |
|-------|------|------|
| 1 | Create Brinell.FlaUI project | 30 min |
| 2 | Move infrastructure + base classes | 30 min |
| 3 | Refactor WPF to use Brinell.FlaUI | 30 min |
| 4 | Create WinForms controls | 1 hour |
| 5 | Fix GetRequiredElement issues | 30 min |
| 6 | Build and test | 1 hour |

**Total: ~4 hours**

## Benefits

1. **Single source of truth** - Infrastructure and base classes in one place
2. **DRY** - No code duplication between WPF and WinForms
3. **Easier maintenance** - Fix once, applies to both
4. **Consistent behavior** - Same Is/Wait/Check/Assert pattern
5. **Container support** - Properly supported in shared base

## Decision Point

**Proceed with this plan?**

This involves:
1. Creating a new project (Brinell.FlaUI)
2. Moving files from WPF
3. Updating project references
4. Rebuilding WinForms controls

Alternative: Keep separate projects but just copy files (faster but more duplication).

