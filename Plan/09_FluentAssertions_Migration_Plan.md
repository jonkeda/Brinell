# Plan 09: Migrate from FluentAssertions to xUnit Assertions

## Overview
Migrate all test projects from FluentAssertions (now owned by Xceed with commercial licensing requirements) to xUnit's built-in assertions. This eliminates licensing concerns and reduces external dependencies.

## Motivation

### Licensing Change
- FluentAssertions was acquired by Xceed in late 2024
- Version 8.x now requires a **commercial license** for commercial use
- Community license only available for non-commercial projects
- Warning message displayed on every test run:
  ```
  The component "Fluent Assertions" is governed by the rules defined in the Xceed License Agreement...
  An active subscription is required to use Fluent Assertions for commercial use.
  ```

### Alternatives Considered

| Library | Pros | Cons | Decision |
|---------|------|------|----------|
| **xUnit Assert** | Built-in, no extra deps, stable | Less fluent syntax | ✅ **Selected** |
| **Shouldly** | Fluent, MIT license | Extra dependency | Consider |
| **TUnit** | Modern, MIT license | Less mature | Future option |
| **NUnit Assert** | Powerful | Would need NUnit adapter | No |

### Decision: xUnit Assert
- Zero additional dependencies (already using xUnit)
- Stable, well-documented, widely used
- Syntax is simple and clear
- No licensing concerns

---

## Current State

### Affected Projects (Brinell)
| Project | FluentAssertions Usage |
|---------|------------------------|
| Brinell.Samples.Wpf.UITests | ~15 assertions |
| Brinell.Samples.Blazor.UITests | ~25 assertions |

### Affected Projects (Oravey)
| Project | FluentAssertions Usage |
|---------|------------------------|
| Oravey.Core.Tests | Extensive |
| Oravey.Server.Tests | Extensive |
| Oravey.Server.IntegrationTests | Moderate |
| Oravey.Persistence.Tests | Extensive |
| Oravey.Persistence.IntegrationTests | Moderate |
| Oravey.Tools.Wpf.Tests | Moderate |
| Oravey.Tools.Wpf.IntegrationTests | Moderate |
| Oravey.Tools.Wpf.UITests | Extensive |
| Oravey.Game.Tests | Moderate |

---

## Migration Guide

### Assertion Mappings

#### Boolean Assertions
```csharp
// FluentAssertions                        // xUnit
value.Should().BeTrue();                   Assert.True(value);
value.Should().BeFalse();                  Assert.False(value);
value.Should().BeTrue("reason");           Assert.True(value, "reason");
```

#### Null Assertions
```csharp
// FluentAssertions                        // xUnit
obj.Should().BeNull();                     Assert.Null(obj);
obj.Should().NotBeNull();                  Assert.NotNull(obj);
```

#### Equality Assertions
```csharp
// FluentAssertions                        // xUnit
value.Should().Be(expected);               Assert.Equal(expected, value);
value.Should().NotBe(expected);            Assert.NotEqual(expected, value);
value.Should().BeSameAs(expected);         Assert.Same(expected, value);
value.Should().NotBeSameAs(expected);      Assert.NotSame(expected, value);
```

#### String Assertions
```csharp
// FluentAssertions                        // xUnit
str.Should().BeEmpty();                    Assert.Empty(str);
str.Should().NotBeEmpty();                 Assert.NotEmpty(str);
str.Should().BeNullOrEmpty();              Assert.True(string.IsNullOrEmpty(str));
str.Should().NotBeNullOrEmpty();           Assert.False(string.IsNullOrEmpty(str));
str.Should().Contain("sub");               Assert.Contains("sub", str);
str.Should().NotContain("sub");            Assert.DoesNotContain("sub", str);
str.Should().StartWith("prefix");          Assert.StartsWith("prefix", str);
str.Should().EndWith("suffix");            Assert.EndsWith("suffix", str);
str.Should().Match("regex");               Assert.Matches("regex", str);
str.Should().HaveLength(5);                Assert.Equal(5, str.Length);
```

#### Collection Assertions
```csharp
// FluentAssertions                        // xUnit
list.Should().BeEmpty();                   Assert.Empty(list);
list.Should().NotBeEmpty();                Assert.NotEmpty(list);
list.Should().HaveCount(3);                Assert.Equal(3, list.Count);
list.Should().ContainSingle();             Assert.Single(list);
list.Should().Contain(item);               Assert.Contains(item, list);
list.Should().NotContain(item);            Assert.DoesNotContain(item, list);
list.Should().AllSatisfy(x => x > 0);      Assert.All(list, x => Assert.True(x > 0));
list.Should().BeEquivalentTo(expected);    Assert.Equivalent(expected, list);
```

#### Numeric Assertions
```csharp
// FluentAssertions                        // xUnit
num.Should().BeGreaterThan(5);             Assert.True(num > 5);
num.Should().BeLessThan(10);               Assert.True(num < 10);
num.Should().BeGreaterOrEqualTo(5);        Assert.True(num >= 5);
num.Should().BeLessOrEqualTo(10);          Assert.True(num <= 10);
num.Should().BeInRange(1, 10);             Assert.InRange(num, 1, 10);
num.Should().BePositive();                 Assert.True(num > 0);
num.Should().BeNegative();                 Assert.True(num < 0);
num.Should().BeApproximately(3.14, 0.01);  Assert.Equal(3.14, num, 2); // precision
```

#### Type Assertions
```csharp
// FluentAssertions                        // xUnit
obj.Should().BeOfType<MyClass>();          Assert.IsType<MyClass>(obj);
obj.Should().BeAssignableTo<IInterface>(); Assert.IsAssignableFrom<IInterface>(obj);
obj.Should().NotBeOfType<Other>();         Assert.IsNotType<Other>(obj);
```

#### Exception Assertions
```csharp
// FluentAssertions                        
action.Should().Throw<ArgumentException>();
action.Should().ThrowAsync<InvalidOperationException>();
action.Should().NotThrow();

// xUnit
Assert.Throws<ArgumentException>(action);
await Assert.ThrowsAsync<InvalidOperationException>(action);
var ex = Record.Exception(action);
Assert.Null(ex);
```

#### DateTime Assertions
```csharp
// FluentAssertions                        // xUnit
dt.Should().BeBefore(other);               Assert.True(dt < other);
dt.Should().BeAfter(other);                Assert.True(dt > other);
dt.Should().BeCloseTo(expected, 1.Seconds()); Assert.True(Math.Abs((dt - expected).TotalSeconds) < 1);
```

---

## Phase 1: Brinell Framework (1 day)

### 1.1 Update Brinell.Samples.Wpf.UITests

**Files to migrate:**
- Tests/LoginTests.cs
- Tests/IsBusyTests.cs
- Tests/NavigationTests.cs

**Example transformations:**
```csharp
// Before
loginPage.HasLoginError().Should().BeTrue("Login error should be displayed");
loginPage.GetLoginError().Should().Contain("Invalid");
loginPage.UsernameTextBox.GetText().Should().BeEmpty("Username should be cleared");

// After
Assert.True(loginPage.HasLoginError(), "Login error should be displayed");
Assert.Contains("Invalid", loginPage.GetLoginError());
Assert.Empty(loginPage.UsernameTextBox.GetText());
```

### 1.2 Update Brinell.Samples.Blazor.UITests

**Files to migrate:**
- Tests/CounterTests.cs
- Tests/LoginTests.cs
- Tests/NavigationTests.cs

**Example transformations:**
```csharp
// Before
counterPage.GetCurrentCount().Should().Be(0, "Initial count should be zero");
GetCurrentUrl().Should().Contain("/counter");
loginPage.WaitForError().Should().BeTrue("Error message should appear");

// After
Assert.Equal(0, counterPage.GetCurrentCount());
Assert.Contains("/counter", GetCurrentUrl());
Assert.True(loginPage.WaitForError(), "Error message should appear");
```

### 1.3 Remove FluentAssertions Package References

**Update Directory.Packages.props:**
```xml
<!-- Remove this line -->
<PackageVersion Include="FluentAssertions" Version="8.3.0" />
```

**Update project files:**
```xml
<!-- Remove from both UITests projects -->
<PackageReference Include="FluentAssertions" />
```

---

## Phase 2: Oravey Framework (2-3 days)

### 2.1 Oravey.Core.Tests
Priority: High (core domain logic)

### 2.2 Oravey.Server.Tests  
Priority: High (server logic)

### 2.3 Oravey.Persistence.Tests
Priority: High (data access)

### 2.4 Oravey.Tools.Wpf.Tests
Priority: Medium (tooling)

### 2.5 Oravey.Tools.Wpf.UITests
Priority: Medium (UI tests)

### 2.6 Oravey.Game.Tests
Priority: Low (game client)

### 2.7 Integration Test Projects
Priority: Medium

---

## Phase 3: Cleanup and Validation (0.5 days)

### 3.1 Remove Package References
- Remove from all Directory.Packages.props files
- Remove from all .csproj files

### 3.2 Remove Using Statements
```csharp
// Remove from all test files
using FluentAssertions;
```

### 3.3 Build and Test
```powershell
# Clean build
dotnet clean
dotnet restore
dotnet build

# Run all tests
dotnet test
```

### 3.4 Verify No Licensing Warnings
- Run tests and confirm no Xceed licensing message appears

---

## Helper Extension Methods (Optional)

If you find xUnit syntax too verbose, create thin wrappers:

```csharp
// TestExtensions.cs in a shared test utilities project
public static class AssertEx
{
    public static void IsTrue(bool condition, string? message = null) 
        => Assert.True(condition, message);
    
    public static void IsFalse(bool condition, string? message = null) 
        => Assert.False(condition, message);
    
    public static void AreEqual<T>(T expected, T actual, string? message = null)
    {
        if (message != null)
            Assert.True(EqualityComparer<T>.Default.Equals(expected, actual), message);
        else
            Assert.Equal(expected, actual);
    }
    
    public static void Contains(string substring, string actual, string? message = null)
    {
        if (message != null)
            Assert.True(actual.Contains(substring), message);
        else
            Assert.Contains(substring, actual);
    }
}
```

---

## Regex Find-Replace Patterns

Use these patterns in VS Code for bulk migration:

### Boolean True
- Find: `(\w+)\.Should\(\)\.BeTrue\("([^"]+)"\);`
- Replace: `Assert.True($1, "$2");`

### Boolean True (no message)
- Find: `(\w+)\.Should\(\)\.BeTrue\(\);`
- Replace: `Assert.True($1);`

### Boolean False
- Find: `(\w+)\.Should\(\)\.BeFalse\("([^"]+)"\);`
- Replace: `Assert.False($1, "$2");`

### Equality
- Find: `(\w+)\.Should\(\)\.Be\((\d+),\s*"([^"]+)"\);`
- Replace: `Assert.Equal($2, $1); // $3`

### Contain
- Find: `(\w+)\.Should\(\)\.Contain\("([^"]+)"(?:,\s*"[^"]+")?\);`
- Replace: `Assert.Contains("$2", $1);`

### BeEmpty
- Find: `(\w+)\.Should\(\)\.BeEmpty\("([^"]+)"\);`
- Replace: `Assert.Empty($1); // $2`

### NotBeNullOrEmpty
- Find: `(\w+)\.Should\(\)\.NotBeNullOrEmpty\("([^"]+)"\);`
- Replace: `Assert.False(string.IsNullOrEmpty($1), "$2");`

---

## Estimated Timeline

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Brinell | 1 day | None |
| Phase 2: Oravey | 2-3 days | Phase 1 |
| Phase 3: Cleanup | 0.5 days | Phase 2 |
| **Total** | **~4 days** | |

---

## Success Criteria

- [ ] All test projects build without FluentAssertions
- [ ] All existing tests pass with xUnit assertions
- [ ] No Xceed licensing warnings appear during test runs
- [ ] FluentAssertions removed from all package references
- [ ] Using statements removed from all files
- [ ] Documentation updated to reflect xUnit assertion usage

---

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Complex BeEquivalentTo assertions | Use Assert.Equivalent or manual property checks |
| Custom assertion messages less readable | Add comments or use Assert.True with message |
| Developer learning curve | Provide cheat sheet and examples |
| Missed migrations causing runtime errors | Full test coverage run after migration |

---

## Notes

- xUnit 2.x uses `Assert.Equal(expected, actual)` (expected first)
- xUnit doesn't have built-in collection equivalence, but `Assert.Equivalent` works
- For complex object comparisons, consider keeping one assertion library or using reflection-based comparers
