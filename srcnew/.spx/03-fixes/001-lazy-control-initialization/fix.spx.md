# SPX-FIX-001: Lazy Control Initialization in Page Objects

**Status:** Implemented (Option C)  
**Priority:** Medium  
**Created:** 2025-01-14  
**Author:** Copilot  
**Component:** Brinell.Maui.UITests, Page Object Pattern

---

## 1. Problem Statement

Currently, page object properties create a new control instance on every property access:

```csharp
// Current: Creates new instance on EVERY access
public MauiControlBase<MainPage> TitleLabel => Control("TitleLabel");
```

This leads to:
- Unnecessary object allocations
- Potential performance issues in tests with many control accesses
- Possible inconsistent state if control caches internal data

---

## 2. Proposed Solutions

### Option A: Lazy<T> Pattern (Recommended)

Use `Lazy<T>` for thread-safe, deferred initialization:

```csharp
public class MainPage : MauiPageObjectBase<MainPage>
{
    // Private lazy fields
    private readonly Lazy<MauiControlBase<MainPage>> _titleLabel;
    private readonly Lazy<MauiButtonControl<MainPage>> _incrementButton;

    public MainPage(IMauiTestContext context)
        : base(context)
    {
        _titleLabel = new Lazy<MauiControlBase<MainPage>>(() => Control("TitleLabel"));
        _incrementButton = new Lazy<MauiButtonControl<MainPage>>(() => Button("IncrementButton"));
    }

    public MauiControlBase<MainPage> TitleLabel => _titleLabel.Value;
    public MauiButtonControl<MainPage> IncrementButton => _incrementButton.Value;
}
```

**Pros:**
- Thread-safe by default
- Deferred initialization (only creates when first accessed)
- Single instance guaranteed
- Standard .NET pattern

**Cons:**
- Verbose (requires field + property + constructor init)
- Constructor becomes large with many controls

---

### Option B: Null-Coalescing Backing Field Pattern

Use backing fields with null-coalescing assignment:

```csharp
public class MainPage : MauiPageObjectBase<MainPage>
{
    private MauiControlBase<MainPage>? _titleLabel;
    private MauiButtonControl<MainPage>? _incrementButton;

    public MainPage(IMauiTestContext context) : base(context) { }

    public MauiControlBase<MainPage> TitleLabel => _titleLabel ??= Control("TitleLabel");
    public MauiButtonControl<MainPage> IncrementButton => _incrementButton ??= Button("IncrementButton");
}
```

**Pros:**
- Concise syntax (C# 8+)
- No constructor bloat
- Lazy initialization
- Single instance guaranteed

**Cons:**
- Not thread-safe (acceptable for single-threaded test scenarios)
- Requires nullable field declarations

---

### Option C: Constructor Initialization

Initialize all controls in constructor:

```csharp
public class MainPage : MauiPageObjectBase<MainPage>
{
    public MauiControlBase<MainPage> TitleLabel { get; }
    public MauiButtonControl<MainPage> IncrementButton { get; }

    public MainPage(IMauiTestContext context)
        : base(context)
    {
        TitleLabel = Control("TitleLabel");
        IncrementButton = Button("IncrementButton");
    }
}
```

**Pros:**
- Simple and explicit
- All controls available immediately
- Read-only properties (immutable)

**Cons:**
- Creates ALL controls even if test only uses one
- Constructor becomes very long with many controls
- Higher memory usage upfront

---

## 3. Recommendation

**Option B (Null-Coalescing)** is recommended because:

1. **Concise**: Minimal code change from current pattern
2. **Lazy**: Only creates controls when accessed
3. **Test-Friendly**: Tests are typically single-threaded, so thread-safety is not critical
4. **Modern C#**: Uses C# 8+ null-coalescing assignment (`??=`)

---

## 4. Implementation

### Before (Current)

```csharp
public class MainPage : MauiPageObjectBase<MainPage>
{
    public MainPage(IMauiTestContext context) : base(context) { }

    public MauiControlBase<MainPage> TitleLabel => Control("TitleLabel");
    public MauiControlBase<MainPage> SubtitleLabel => Control("SubtitleLabel");
    public MauiControlBase<MainPage> CounterLabel => Control("CounterLabel");
    public MauiButtonControl<MainPage> IncrementButton => Button("IncrementButton");
    public MauiButtonControl<MainPage> DecrementButton => Button("DecrementButton");
    public MauiEntryControl<MainPage> NameEntry => Entry("NameEntry");
}
```

### After (Option B)

```csharp
public class MainPage : MauiPageObjectBase<MainPage>
{
    public MainPage(IMauiTestContext context) : base(context) { }

    #region Labels

    private MauiControlBase<MainPage>? _titleLabel;
    public MauiControlBase<MainPage> TitleLabel => _titleLabel ??= Control("TitleLabel");

    private MauiControlBase<MainPage>? _subtitleLabel;
    public MauiControlBase<MainPage> SubtitleLabel => _subtitleLabel ??= Control("SubtitleLabel");

    private MauiControlBase<MainPage>? _counterLabel;
    public MauiControlBase<MainPage> CounterLabel => _counterLabel ??= Control("CounterLabel");

    #endregion

    #region Buttons

    private MauiButtonControl<MainPage>? _incrementButton;
    public MauiButtonControl<MainPage> IncrementButton => _incrementButton ??= Button("IncrementButton");

    private MauiButtonControl<MainPage>? _decrementButton;
    public MauiButtonControl<MainPage> DecrementButton => _decrementButton ??= Button("DecrementButton");

    #endregion

    #region Entry Controls

    private MauiEntryControl<MainPage>? _nameEntry;
    public MauiEntryControl<MainPage> NameEntry => _nameEntry ??= Entry("NameEntry");

    #endregion
}
```

---

## 5. Alternative: Base Class Caching

A more sophisticated approach could add caching to the base class:

```csharp
public abstract class MauiPageObjectBase<TPage>
{
    private readonly Dictionary<string, object> _controlCache = new();

    protected TControl GetOrCreateControl<TControl>(string automationId, Func<string, TControl> factory)
        where TControl : class
    {
        if (!_controlCache.TryGetValue(automationId, out var control))
        {
            control = factory(automationId);
            _controlCache[automationId] = control;
        }
        return (TControl)control;
    }

    // Usage in derived page:
    public MauiButtonControl<MainPage> IncrementButton 
        => GetOrCreateControl("IncrementButton", Button);
}
```

This would require changes to the framework itself but would eliminate boilerplate in page objects.

---

## 6. Files to Update

| File | Changes |
|------|---------|
| `testsnew/Brinell.Maui.UITests/Pages/MainPage.cs` | Add backing fields, use `??=` pattern |
| Any other page objects in `Pages/` folder | Same pattern |

---

## 7. Testing

After implementation:
- [ ] Verify controls are only created once per page instance
- [ ] Verify fluent chaining still works
- [ ] Verify all existing tests pass
- [ ] Performance test with many control accesses

---

## 8. Decision

**Awaiting approval to implement Option B (Null-Coalescing Pattern).**
