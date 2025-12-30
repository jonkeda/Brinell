# Plan 13: Fix Stride UI Tests

## Problem Analysis

The Brinell.Stride sample app UI tests are failing with:
- `Cannot click element 'X' - not found or has no bounds`
- 13/16 tests failing

### Root Cause
1. **Missing Graphics Compositor Setup**: The sample app creates UI elements but doesn't have a proper rendering pipeline
2. **UI Not Rendered**: Without `GraphicsCompositorHelper.CreateDefault()` or equivalent, `RenderSize` stays at (0,0)
3. **No UIRenderFeature**: The default compositor doesn't include UI rendering

### Key Insight from Oravey
Oravey uses **Stride.CommunityToolkit** with `this.SetupBase3DScene()` which:
- Sets up proper `GraphicsCompositor` with all render features
- Includes proper camera slot configuration
- Handles UI rendering correctly

However, Brinell sample should remain minimal without external toolkit dependencies.

## Solution Approach

### Option A: Use Stride.CommunityToolkit (Recommended)
Add the CommunityToolkit package and use `SetupBase3DScene()` - simplest and most reliable.

### Option B: Minimal Compositor Setup
Manually create a minimal `GraphicsCompositor` with `UIRenderFeature` - more code but no additional dependencies.

### Option C: Bypass Render-Dependent Bounds (Workaround)
Modify `StrideUIHandler.GetElementBounds()` to use element's `Size`/`Width`/`Height` properties instead of `RenderSize` when bounds are zero.

## Recommended Plan: Option A + C Hybrid

### Phase 1: Add CommunityToolkit for Proper Rendering
1. Add `Stride.CommunityToolkit` package to sample app
2. Update `SampleStrideGame` to use `this.SetupBase3DScene()`
3. Properly attach UI to scene

### Phase 2: Fallback Bounds Calculation (Defensive)
1. Update `StrideUIHandler.GetElementBounds()` to handle elements without render bounds
2. Use explicit `Width`/`Height` or `MinimumWidth`/`MinimumHeight` as fallback

---

## Implementation Tasks

### Task 1: Add Stride.CommunityToolkit
**File**: `samples/Brinell.Samples.Stride.App/Brinell.Samples.Stride.App.csproj`

Add package reference:
```xml
<PackageReference Include="Stride.CommunityToolkit.Engine" Version="1.0.0.0-preview.121" />
```

### Task 2: Update Sample Game Setup
**File**: `samples/Brinell.Samples.Stride.App/SampleStrideGame.cs`

1. Add using: `using Stride.CommunityToolkit.Engine;`
2. In `Initialize()`, call `this.SetupBase3DScene()` before creating UI
3. Remove manual compositor setup (revert complex changes)
4. Keep UI attachment via `UIComponent.Page`

### Task 3: Add Fallback Bounds Calculation
**File**: `src/Brinell.Stride.Automation/StrideUIHandler.cs`

Update `GetElementBounds()`:
```csharp
private ElementBounds GetElementBounds(UIElement element)
{
    var worldMatrix = element.WorldMatrix;
    var renderSize = element.RenderSize;
    
    // Fallback to explicit size if render size is zero
    if (renderSize.X == 0 && renderSize.Y == 0)
    {
        // Try Width/Height properties
        var width = element.Width;
        var height = element.Height;
        
        // Fallback to minimum size
        if (float.IsNaN(width) || width == 0)
            width = element.MinimumWidth;
        if (float.IsNaN(height) || height == 0)
            height = element.MinimumHeight;
        
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

### Task 4: Rebuild and Test
1. Build sample app
2. Run UI tests
3. Verify all 16 tests pass

---

## Acceptance Criteria
- [ ] Sample app builds successfully
- [ ] Sample app runs and displays UI
- [ ] All 16 UI tests pass
- [ ] No runtime errors related to rendering

## Status
- [ ] Task 1: Add Stride.CommunityToolkit
- [ ] Task 2: Update Sample Game Setup  
- [ ] Task 3: Add Fallback Bounds Calculation
- [ ] Task 4: Rebuild and Test

## Notes
- The CommunityToolkit version should match the Stride.Engine version (4.2.x)
- If CommunityToolkit causes issues, Option B (manual compositor) can be used
- The fallback bounds in Task 3 is defensive and helps with edge cases
