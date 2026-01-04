# Issue: MAUI UI Tests Are Extremely Slow

**Created:** January 4, 2026  
**Priority:** High  
**Category:** Performance / Test Infrastructure  
**Status:** Open

---

## Summary

MAUI UI tests in `Brinell.Samples.Maui.UITests.ControlObject6` take **10+ minutes to run 44 tests** (~14.5 seconds per test).

**Two Root Causes Identified:**

1. **`ms:waitForAppLaunch` = 10 seconds** - Mandatory wait after app launch! (See [ISSUE-MAUI-UITESTS-STARTUP-DELAY.md](ISSUE-MAUI-UITESTS-STARTUP-DELAY.md))
2. **App-per-test pattern** - Each test launches and closes the MAUI application

---

## Metrics

| Metric | Current | Expected |
|--------|---------|----------|
| **Total Tests** | 44 | 44 |
| **Total Time** | 10.6 minutes | ~2-3 minutes |
| **Time Per Test** | ~14.5 seconds | ~3-4 seconds |
| **App Launches** | 44 (one per test) | 1 (shared fixture) |
| **ms:waitForAppLaunch** | 10s (per launch!) | 2-3s (per launch) |

---

## Root Cause Analysis

### 1. App Launch Per Test (CRITICAL - ~10 seconds overhead per test)

**Current pattern in `MauiTestBase6`:**
```csharp
public abstract class MauiTestBase6 : IDisposable
{
    protected MauiTestBase6(ITestOutputHelper output)
    {
        // PROBLEM: Creates a new driver (and launches app) for EVERY test
        _driver = CreateDriver();  // ~10-12 seconds
        Context = new MauiTestContext(_driver);
    }

    public void Dispose()
    {
        _driver?.Quit();  // Closes app after EVERY test
    }
}
```

**Test class inherits this:**
```csharp
public class CounterTests6 : MauiTestBase6  // 8 tests = 8 app launches!
{
    public CounterTests6(ITestOutputHelper output) : base(output) { }
    
    [Fact] public void Test1() { ... }  // Launch app, run test, close app
    [Fact] public void Test2() { ... }  // Launch app, run test, close app
    // ... repeat for all tests
}
```

### 2. WinAppDriver Session Creation (~10-12 seconds)

From the Appium logs:
```
POST /session 200 12275 ms - 354
```

Each session creation takes **12+ seconds** because:
1. Appium creates WinAppDriver process
2. WinAppDriver launches the MAUI app
3. MAUI app initializes (.NET runtime, XAML, etc.)
4. Session waits for app to be ready (`ms:waitForAppLaunch: 10`)

### 3. No Shared Fixture Pattern

The WinForms tests already have a solution:

**WinForms uses `ICollectionFixture`:**
```csharp
// Fixture launches app ONCE
public class AppFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        _driver = new FlaUIDriverAdapter(AppPath);  // One launch
    }

    public async Task DisposeAsync()
    {
        _driver?.Dispose();  // One close
    }
}

// All tests share the same app instance
[Collection("UI Tests Collection")]
public class MyTests
{
    private readonly AppFixture _fixture;
    public MyTests(AppFixture fixture) => _fixture = fixture;
}
```

**MAUI tests don't use this pattern** - they launch a new app per test.

---

## Time Breakdown Per Test

| Phase | Time | Avoidable? |
|-------|------|-----------|
| Appium session creation | ~1-2s | Partially |
| WinAppDriver spawn | ~1s | Yes (reuse) |
| MAUI app launch | ~8-10s | Yes (reuse) |
| Actual test execution | ~1-2s | No |
| App close | ~0.5s | Yes (reuse) |
| **Total** | **~12-15s** | **~10s avoidable** |

---

## Solution Options

### Option 1: ICollectionFixture (Recommended)

Share a single app instance across all tests in a collection:

```csharp
// 1. Create fixture that launches app once
public class MauiAppFixture : IAsyncLifetime
{
    public AppiumDriver Driver { get; private set; } = null!;
    public MauiTestContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Driver = CreateDriver();  // Launch once
        Context = new MauiTestContext(Driver);
    }

    public async Task DisposeAsync()
    {
        Driver?.Quit();  // Close once
    }
}

// 2. Define collection
[CollectionDefinition("MAUI UI Tests", DisableParallelization = true)]
public class MauiUITestCollection : ICollectionFixture<MauiAppFixture> { }

// 3. Tests use shared fixture
[Collection("MAUI UI Tests")]
public class CounterTests6
{
    private readonly MauiAppFixture _fixture;
    
    public CounterTests6(MauiAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Counter_ClickIncrement_IncreasesCount()
    {
        var page = new MainPageObject6(_fixture.Context);
        // Test uses shared app instance
    }
}
```

**Pros:**
- ✅ Single app launch for all tests (~10 seconds total instead of ~440 seconds)
- ✅ Tests run ~4x faster
- ✅ Same pattern as WinForms tests

**Cons:**
- ⚠️ Tests must not leave app in bad state
- ⚠️ May need `ResetAppState()` method between tests

### Option 2: Test State Reset Between Tests

If tests can't share state, add reset logic:

```csharp
[Collection("MAUI UI Tests")]
public class CounterTests6
{
    private readonly MauiAppFixture _fixture;
    
    public CounterTests6(MauiAppFixture fixture)
    {
        _fixture = fixture;
        ResetAppState();  // Navigate to home, clear inputs, etc.
    }

    private void ResetAppState()
    {
        var page = new MainPageObject6(_fixture.Context);
        page.ResetButton.Click();  // Reset counter
        page.NameEntry.Clear();    // Clear text fields
    }
}
```

### Option 3: IClassFixture (Per Test Class)

Less optimal but simpler - share app within a single test class:

```csharp
public class CounterTests6 : IClassFixture<MauiAppFixture>
{
    private readonly MauiAppFixture _fixture;
    
    public CounterTests6(MauiAppFixture fixture)
    {
        _fixture = fixture;
    }
}
```

**Result:** 4 app launches (one per test class) instead of 44.

---

## Expected Improvement

| Approach | App Launches | Total Time | Improvement |
|----------|--------------|------------|-------------|
| Current | 44 | ~10.6 min | Baseline |
| IClassFixture | 4 | ~2-3 min | 3-4x faster |
| ICollectionFixture | 1 | ~1-2 min | 5-10x faster |

---

## Implementation Plan

### Phase 1: Create Fixture (1 hour)

1. Create `MauiAppFixture` class in new `Fixtures/` folder
2. Implement `IAsyncLifetime` with driver lifecycle
3. Add collection definition

### Phase 2: Update Test Classes (30 min)

1. Add `[Collection("MAUI UI Tests")]` attribute to each test class
2. Update constructors to accept `MauiAppFixture`
3. Remove inheritance from `MauiTestBase6` (or make it optional)

### Phase 3: Add State Reset (30 min)

1. Add `ResetAppState()` helper method
2. Call in test class constructor or `IAsyncLifetime.InitializeAsync`

### Phase 4: Verify (15 min)

1. Run all tests
2. Verify consistent results
3. Measure new execution time

---

## Files to Modify

### New Files
- `samples/Brinell.Samples.Maui.UITests.ControlObject6/Fixtures/MauiAppFixture.cs`
- `samples/Brinell.Samples.Maui.UITests.ControlObject6/Fixtures/MauiUITestCollection.cs`

### Modified Files
- `samples/Brinell.Samples.Maui.UITests.ControlObject6/Tests/CounterTests6.cs`
- `samples/Brinell.Samples.Maui.UITests.ControlObject6/Tests/TextInputTests6.cs`
- `samples/Brinell.Samples.Maui.UITests.ControlObject6/Tests/ControlStateTests6.cs`
- `samples/Brinell.Samples.Maui.UITests.ControlObject6/Tests/ClickTests6.cs`

### Optional Deprecation
- `MauiTestBase6.cs` - Can keep for quick/dirty tests, but fixture pattern preferred

---

## Acceptance Criteria

- [ ] All 44 tests pass with fixture pattern
- [ ] Total test execution time < 3 minutes (currently 10.6 min)
- [ ] App launches exactly once per test run
- [ ] Tests remain isolated (state reset between tests)
- [ ] Pattern documented for future test authors

---

## Related Patterns

### Existing WinForms Fixture (Working Example)
[samples/Brinell.Samples.WinForms.UITests/Fixtures/AppFixture.cs](../../samples/Brinell.Samples.WinForms.UITests/Fixtures/AppFixture.cs)

### xUnit Documentation
- [Class Fixtures](https://xunit.net/docs/shared-context#class-fixture)
- [Collection Fixtures](https://xunit.net/docs/shared-context#collection-fixture)

---

## Notes

- The 10+ second app launch is inherent to MAUI Windows apps (heavy runtime)
- Similar issue likely exists in original `Brinell.Samples.Maui.UITests`
- Blazor tests are faster (~3-4s per test) because browser reuse is simpler
- This pattern should be applied to all UI test projects
