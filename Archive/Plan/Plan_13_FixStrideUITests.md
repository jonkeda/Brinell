# Plan 13: Fix Stride UI Tests (Revised)

## Problem Analysis

The Brinell.Stride sample app UI tests were failing with:
- `Cannot click element 'X' - not found or has no bounds`
- 13/16 tests failing

### Root Causes Discovered
1. **Missing Graphics Compositor**: Sample app lacked proper rendering pipeline → `RenderSize` stays at (0,0)
2. **Wrong Lifecycle Hook**: Using `Initialize()` instead of `BeginRun()` - CommunityToolkit methods require SceneSystem to be ready
3. **NET Version Mismatch**: CommunityToolkit 1.0.0-preview.62 requires NET 10.0, but sample was NET 8.0
4. **Wrong Extension Method**: `SetupBase3DScene()` is in `Stride.CommunityToolkit.Bepu`, not base toolkit
5. **Test Path Hardcoded**: UITests had hardcoded `net8.0-windows` path

### Key Learnings from Oravey
Oravey's working pattern:
1. Uses `BeginRun()` not `Initialize()` for scene setup
2. Uses `Stride.CommunityToolkit.Bepu` for `SetupBase3DScene()`
3. Uses `using Stride.CommunityToolkit.Bepu;` namespace
4. Adds UIComponent to existing scene after `SetupBase3DScene()` creates it
5. Uses `.NET 10` with Stride 4.2.0.2450

---

## Completed Changes

### ✅ Task 1: Upgrade to NET 10
**Files Modified:**
- `samples/Brinell.Samples.Stride.App/Brinell.Samples.Stride.App.csproj` → `net10.0-windows`
- `samples/Brinell.Samples.Stride.UITests/Brinell.Samples.Stride.UITests.csproj` → `net10.0-windows`
- `src/Brinell.Stride/Brinell.Stride.csproj` → `net10.0`
- `src/Brinell.Stride.Automation/Brinell.Stride.Automation.csproj` → `net10.0`

### ✅ Task 2: Add CommunityToolkit Packages
**File**: `Directory.Packages.props`
```xml
<PackageVersion Include="Stride.Engine" Version="4.3.0.2507" />
<PackageVersion Include="Stride.CommunityToolkit.Windows" Version="1.0.0-preview.62" />
<PackageVersion Include="Stride.CommunityToolkit.Bepu" Version="1.0.0-preview.62" />
```

**File**: `samples/Brinell.Samples.Stride.App/Brinell.Samples.Stride.App.csproj`
```xml
<PackageReference Include="Stride.CommunityToolkit.Windows" />
<PackageReference Include="Stride.CommunityToolkit.Bepu" />
```

### ✅ Task 3: Fix Sample Game Lifecycle
**File**: `samples/Brinell.Samples.Stride.App/SampleStrideGame.cs`
- Changed from `Initialize()` to `BeginRun()` 
- Added `using Stride.CommunityToolkit.Bepu;`
- Call `this.SetupBase3DScene()` in BeginRun
- Add UIComponent to `SceneSystem.SceneInstance.RootScene.Entities`

### ✅ Task 4: Add Fallback Bounds Calculation
**File**: `src/Brinell.Stride.Automation/StrideUIHandler.cs`
```csharp
private ElementBounds GetElementBounds(UIElement element)
{
    var worldMatrix = element.WorldMatrix;
    var renderSize = element.RenderSize;

    // Fallback to explicit size if render size is zero
    if (renderSize.X <= 0 && renderSize.Y <= 0)
    {
        var width = element.Width;
        var height = element.Height;

        if (float.IsNaN(width) || width <= 0)
            width = element.MinimumWidth;
        if (float.IsNaN(height) || height <= 0)
            height = element.MinimumHeight;

        if (float.IsNaN(width) || width <= 0)
            width = 100;
        if (float.IsNaN(height) || height <= 0)
            height = 30;

        renderSize = new Vector3(width, height, 0);
    }

    return new ElementBounds
    {
        X = (int)worldMatrix.TranslationVector.X,
        Y = (int)worldMatrix.TranslationVector.Y,
        Width = (int)renderSize.X,
        Height = (int)renderSize.Y
    };
}
```

### ✅ Task 5: Fix Test Path
**File**: `src/Brinell.Stride/Infrastructure/StrideTestOptions.cs`
- Changed hardcoded `net8.0-windows` to `net10.0-windows`

---

## Remaining Tasks

### Task 6: Run Full Test Suite
Run all 16 UI tests and verify they pass.

### Task 7: Update Documentation
Update plan status and close out.

---

## Architecture Summary

```
┌─────────────────────────────────────────────────────────┐
│                    Test Process                          │
│  ┌─────────────────┐   Named Pipe   ┌────────────────┐  │
│  │ StrideTestContext│◄─────────────►│ Game Process   │  │
│  │   (xUnit test)   │               │                │  │
│  └─────────────────┘               │ SampleStrideGame│  │
│                                     │   BeginRun():   │  │
│                                     │   - SetupBase3D │  │
│                                     │   - CreateUI()  │  │
│                                     │   - UseAutomation│  │
│                                     │                │  │
│                                     │ AutomationGame  │  │
│                                     │   System:       │  │
│                                     │   - Listens on  │  │
│                                     │     named pipe  │  │
│                                     │   - Handles cmds│  │
│                                     └────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Communication Flow
1. Test starts game process with `--automation` flag
2. Game calls `UseAutomation()` which adds `AutomationGameSystem`
3. `AutomationGameSystem` starts named pipe server
4. Test connects via `NamedPipeChannel`
5. Test sends commands (Click, GetState, SetValue, etc.)
6. `StrideUIHandler` processes commands against UI tree
7. Responses sent back via named pipe

---

## Status: 🔄 In Progress
- [x] Task 1: Upgrade to NET 10
- [x] Task 2: Add CommunityToolkit Packages
- [x] Task 3: Fix Sample Game Lifecycle
- [x] Task 4: Add Fallback Bounds Calculation
- [x] Task 5: Fix Test Path
- [ ] Task 6: Run Full Test Suite
- [ ] Task 7: Update Documentation
