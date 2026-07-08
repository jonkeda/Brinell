# Test Plan: Property Handler on ControlBase

**Date:** 2026-07-08  
**Scope:** Validate IsPropertyHandler generates correct Is/Wait/Assert methods from ControlBase patterns  
**Reference:** `srcnew/Brinell.Maui/Controls/ControlBase.cs`

---

## Test Objectives

Verify that IsPropertyHandler correctly:
1. Matches `Is*Core(IMauiElement?)` → `bool?` protected virtual methods
2. Extracts property names by stripping "Is" prefix and "Core" suffix
3. Generates three public methods (Is*, Wait*, Assert*) with correct signatures
4. Produces code matching ControlBase.cs implementation exactly

---

## Test Patterns from ControlBase

### Pattern 1: IsVisibleCore

**Input (Core Method):**
```csharp
protected bool? IsVisibleCore(IMauiElement? element)
{
    return element?.Visible;
}
```

**Expected Public Methods:**
```csharp
public bool IsVisible()
{
    return IsVisibleCore(TryFindElement()) == true;
}

public bool WaitVisible(bool? expected, int? timeoutMs = null)
{
    if (expected == null) return true;
    return RunWaitWithElement(
        element => IsVisibleCore(element) == expected.Value,
        timeoutMs);
}

public TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
{
    return RunAssertWithElement(expected,
        IsVisibleCore, (actual, expected1) => (actual == expected1),
        null, timeoutMs);
}
```

---

### Pattern 2: IsEnabledCore

**Input (Core Method):**
```csharp
protected bool? IsEnabledCore(IMauiElement? element)
{
    return element?.Enabled;
}
```

**Expected Public Methods:**
```csharp
public bool IsEnabled()
{
    return IsEnabledCore(TryFindElement()) == true;
}

public bool WaitEnabled(bool? expected, int? timeoutMs = null)
{
    if (expected == null) return true;
    return RunWaitWithElement(
        element => IsEnabledCore(element) == expected.Value,
        timeoutMs);
}

public TScope AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
{
    return RunAssertWithElement(expected,
        IsEnabledCore, (actual, expected1) => (actual == expected1),
        null, timeoutMs);
}
```

---

### Pattern 3: IsExistsCore

**Input (Core Method):**
```csharp
protected bool? IsExistsCore(IMauiElement? element)
{
    return element != null;
}
```

**Expected Public Methods:**
```csharp
public bool IsExists()
{
    return IsExistsCore(TryFindElement()) == true;
}

public bool WaitExists(bool? expected, int? timeoutMs = null)
{
    if (expected == null) return true;
    return RunWaitWithElement(
        element => IsExistsCore(element) == expected.Value,
        timeoutMs);
}

public TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
{
    return RunAssertWithElement(expected,
        IsExistsCore, (actual, expected1) => (actual == expected1),
        null, timeoutMs);
}
```

---

## Test Cases

### TC1: Match Protection
- **Input:** Class with three Is*Core methods (protected virtual)
- **Expected:** All three methods matched by IsPropertyHandler
- **Verify:** `handler.Matches()` returns true for each

### TC2: Property Name Extraction
- **Input:** Method names: IsVisibleCore, IsEnabledCore, IsExistsCore
- **Expected:** Property names: Visible, Enabled, Exists
- **Verify:** `handler.Extract()` strips "Is" and "Core" correctly

### TC3: IsMethod Generation
- **Input:** IsVisibleCore with property name "Visible"
- **Expected:** 
  - Method name: `IsVisible`
  - Return type: `bool`
  - Body: `return IsVisibleCore(TryFindElement()) == true;`
- **Verify:** Generated code matches signature and logic

### TC4: WaitMethod Generation
- **Input:** IsVisibleCore with property name "Visible"
- **Expected:**
  - Method name: `WaitVisible`
  - Signature: `(bool? expected, int? timeoutMs = null)`
  - Early return if expected == null
  - Call to RunWaitWithElement with lambda
- **Verify:** Null-check optimization present, lambda correct

### TC5: AssertMethod Generation
- **Input:** IsVisibleCore with property name "Visible"
- **Expected:**
  - Method name: `AssertVisible`
  - Signature: `(bool? expected, string? message = null, int? timeoutMs = null)`
  - Call to RunAssertWithElement with comparison lambda
- **Verify:** Method delegates to RunAssertWithElement correctly

### TC6: Full Class Generation
- **Input:** ControlBase excerpt with three Is*Core methods
- **Expected:** All nine methods generated (3 patterns × 3 methods each)
- **Verify:** Output matches ControlBase.cs implementations exactly

---

## Test Data Files

### Input Files
- `testsnew/Brinell.Generator.Tests/TestData/Input/ControlBase.input.cs` — Extract of Is*Core methods only

### Expected Output Files
- `testsnew/Brinell.Generator.Tests/TestData/Expected/ControlBase.expected.cs` — Full Is/Wait/Assert methods

---

## Acceptance Criteria

- [x] IsPropertyHandler matches all three Is*Core methods
- [x] Property names extracted correctly (Visible, Enabled, Exists)
- [x] Is{PropertyName}() returns bool, calls TryFindElement()
- [x] Wait{PropertyName}() includes null-check optimization
- [x] Assert{PropertyName}() uses RunAssertWithElement with equality comparison
- [x] All nine generated methods compile without error
- [x] Generated output matches ControlBase.cs implementation exactly

---

## Edge Cases to Consider

1. **Null Element Handling:** Is*Core receives null element, returns false
2. **Null Expected Handling:** Wait/Assert with expected=null should skip (return true/ContainingScope)
3. **Lambda Comparison:** AssertVisible uses `(actual, expected1) => (actual == expected1)`
4. **Timeout Propagation:** timeoutMs parameter flows to RunWaitWithElement/RunAssertWithElement

---

## Notes

- ControlBase has manual implementations that should match the generated code
- This test plan validates the generator can reproduce existing working code
- Success means ControlBase could be generated instead of hand-written
