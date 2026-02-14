# SPEC-022: FlyoutItemControl for MAUI Shell Navigation

**Status:** ✅ IMPLEMENTED  
**Created:** 2026-01-17  
**Implemented:** 2026-01-17  
**Priority:** High  
**Blocks:** Container tests (SPEC-017b) - see Note

---

## Implementation Summary

✅ All FlyoutItem-related tasks completed successfully.

### Test Results

| Test | Status | Duration |
|------|--------|----------|
| `MainFlyout_IsExists_ReturnsTrue` | ✅ PASS | 309ms |
| `ContainerDemoFlyout_IsExists_ReturnsTrue` | ✅ PASS | 1s |
| `ContainerDemoFlyout_IsClickable_ReturnsTrue` | ✅ PASS | 1s |
| `ContainerDemoFlyout_Click_NavigatesToContainerDemoPage` | ✅ PASS | 3s |

### Files Created

| File | Purpose |
|------|---------|
| `srcnew/Brinell.Maui/Controls/MauiFlyoutItemControl.cs` | FlyoutItem control using XPath @Name |
| `testsnew/Brinell.Maui.UITests/Tests/FlyoutItemControlTests.cs` | 4 tests for FlyoutItem |
| `testsnew/Brinell.Maui.UITests/Pages/AppShellPage.cs` | Shell page with all flyouts |

### Files Modified

| File | Change |
|------|--------|
| `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` | Added AppShellPage, updated NavigateToContainerDemo |

---

> **Note:** Container tests (SingleContainerTests) still fail because page elements like `ProfileTitle`, `ProfileSaveButton`, `UserProfileContainer` don't have their `AutomationId` propagated to the Windows UI tree. This is a **separate issue** from SPEC-022 and requires updating the ContainerDemoPage XAML or using XPath @Name for those elements too.

---

## 1. Problem Statement

MAUI Shell FlyoutItem elements cannot be located using `AutomationId` / `AccessibilityId`. Testing revealed:

| Locator Strategy | Result |
|------------------|--------|
| `MobileBy.AccessibilityId("FlyoutContainerDemo")` | 0 elements |
| `By.XPath("//*[@Name='Container Demo']")` | 1 element (works!) |

The FlyoutItem's `Title` property becomes `@Name` in the Windows UI Automation tree, NOT the `AutomationId`.

**Additional Discovery:** The flyout menu may need scrolling before clicking items at the bottom.

---

## 2. Solution Overview

### 2.1 Create `MauiFlyoutItemControl`

A control that:
- Uses **XPath with @Name** (Title) instead of AccessibilityId
- Implements `IClickableControlObject<TScope>` for Click()
- Handles flyout scrolling when needed

### 2.2 Locator Strategy

```csharp
// Instead of: MobileBy.AccessibilityId(automationId)
// Use: By.XPath($"//*[@Name='{title}']")
```

---

## 3. Implementation Tasks

### Task 1: Create `MauiFlyoutItemControl.cs`

**File:** `srcnew/Brinell.Maui/Controls/MauiFlyoutItemControl.cs`

```csharp
public class MauiFlyoutItemControl<TScope> : MauiControlBase<TScope>, IClickableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly string _title;
    
    public MauiFlyoutItemControl(TScope scope, string title)
        : base(scope, new Locator(LocatorStrategy.XPath, $"//*[@Name='{title}']"))
    {
        _title = title;
    }
    
    // IClickableControlObject implementation
    public TScope Click() { ... }
    public TScope DoubleClick() { ... }
    public TScope RightClick() { ... }
    public bool IsClickable() { ... }
    public bool WaitClickable(bool? expected = true, int? timeoutMs = null) { ... }
    public TScope AssertClickable(bool? expected = true, string? message = null, int? timeoutMs = null) { ... }
}
```

### Task 2: Create FlyoutItem Tests

**File:** `testsnew/Brinell.Maui.UITests/Tests/FlyoutItemControlTests.cs`

Tests:
- `FlyoutItemControl_IsExists_ReturnsTrue`
- `FlyoutItemControl_Click_NavigatesToPage`
- `FlyoutItemControl_IsClickable_ReturnsTrue`

### Task 3: Create AppShellPage with Flyout Navigation

**File:** `testsnew/Brinell.Maui.UITests/Pages/AppShellPage.cs`

```csharp
public class AppShellPage : MauiPageObjectBase<AppShellPage>
{
    public MauiFlyoutItemControl<AppShellPage> MainFlyout { get; }
    public MauiFlyoutItemControl<AppShellPage> ContainerDemoFlyout { get; }
    // etc.
    
    public void ScrollFlyoutToBottom() { ... }
}
```

### Task 4: Update AppiumFixture Navigation

Update `NavigateToContainerDemo()` to use the new `MauiFlyoutItemControl`:

```csharp
public void NavigateToContainerDemo()
{
    AppShell.ScrollFlyoutToBottom();
    AppShell.ContainerDemoFlyout.Click();
}
```

### Task 5: Verify Container Tests Pass

Run container tests after navigation is fixed.

---

## 4. Files to Create/Modify

### New Files

| File | Description |
|------|-------------|
| `srcnew/Brinell.Maui/Controls/MauiFlyoutItemControl.cs` | FlyoutItem control |
| `testsnew/Brinell.Maui.UITests/Tests/FlyoutItemControlTests.cs` | FlyoutItem tests |
| `testsnew/Brinell.Maui.UITests/Pages/AppShellPage.cs` | Shell page object |

### Modified Files

| File | Change |
|------|--------|
| `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` | Use AppShellPage for navigation |

---

## 5. Key Learnings Applied

1. **Use XPath @Name for FlyoutItems** - Title property becomes @Name
2. **Scroll flyout before clicking** - Items at bottom need scrolling
3. **AccessibilityId doesn't work** - Don't use MobileBy.AccessibilityId for FlyoutItems

---

## 6. Acceptance Criteria

- [x] `MauiFlyoutItemControl` class exists and compiles
- [x] `FlyoutItemControlTests` pass (4 tests)
- [x] `AppShellPage` provides flyout navigation
- [x] `NavigateToContainerDemo()` uses new control
- [ ] Container element tests pending (separate XAML issue)

---

## 7. Test Execution Order

To minimize test time:

1. First: `FlyoutItemControlTests.FlyoutItemControl_IsExists_ReturnsTrue`
2. Then: `FlyoutItemControlTests.FlyoutItemControl_Click_NavigatesToPage`
3. Finally: `SingleContainerTests.UserProfileContainer_IsExists_ReturnsTrue`
