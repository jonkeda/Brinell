# 14. Test Categories

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d14_TestCategories_CodeExamples.md](21d14_TestCategories_CodeExamples.md)  
**Previous:** [Application UITest Projects](21d13_ApplicationUITestProjects.md)

---

## 14.1 Overview

Test categories enable selective test execution based on traits. Use xUnit's `[Trait]` attribute to categorize tests.

---

## 14.2 Category Hierarchy

### 14.2.1 Standard Categories

| Category | Trait | Description |
|----------|-------|-------------|
| **UITest** | `Category=UITest` | All UI tests |
| **Smoke** | `Category=Smoke` | Quick validation tests |
| **Regression** | `Category=Regression` | Full regression suite |
| **E2E** | `Category=E2E` | End-to-end scenarios |
| **MockedAPITest** | `Category=MockedAPITest` | Tests with mocked API |

### 14.2.2 Platform Categories

| Category | Trait | Description |
|----------|-------|-------------|
| **Windows** | `Platform=Windows` | WPF tests |
| **WindowsMaui** | `Platform=WindowsMaui` | Windows MAUI tests |
| **Android** | `Platform=Android` | Android tests |
| **iOS** | `Platform=iOS` | iOS tests |
| **Web** | `Platform=Web` | Web/Selenium tests |

### 14.2.3 Feature Categories

| Category | Trait | Description |
|----------|-------|-------------|
| **Navigation** | `Feature=Navigation` | Navigation tests |
| **Settings** | `Feature=Settings` | Settings tests |
| **Authentication** | `Feature=Authentication` | Login/auth tests |

---

## 14.3 Trait Usage

### 14.3.1 Single Trait

```csharp
[Trait("Category", "UITest")]
public class NavigationTests { }
```

### 14.3.2 Multiple Traits

```csharp
[Trait("Category", "UITest")]
[Trait("Category", "Smoke")]
[Trait("Platform", "Windows")]
[Trait("Feature", "Navigation")]
public class NavigationTests { }
```

### 14.3.3 Method-Level Traits

```csharp
[Fact]
[Trait("Category", "Smoke")]
public void Quick_Validation_Test() { }

[Fact]
[Trait("Category", "Regression")]
public void Full_Test_Scenario() { }
```

---

## 14.4 Test Collections

### 14.4.1 Purpose

- Group tests that share fixtures
- Control parallel execution
- Share expensive resources

### 14.4.2 Definition

```csharp
[CollectionDefinition("UITests")]
public class UITestCollection : ICollectionFixture<UITestFixture>
{
}
```

### 14.4.3 Usage

```csharp
[Collection("UITests")]
public class SettingsTests : UITestBase
{
}
```

---

## 14.5 Filtering Commands

### 14.5.1 By Category

```bash
# Run all UI tests
dotnet test --filter "Category=UITest"

# Run smoke tests only
dotnet test --filter "Category=Smoke"

# Run mocked API tests
dotnet test --filter "Category=MockedAPITest"
```

### 14.5.2 By Platform

```bash
# Run Windows tests
dotnet test --filter "Platform=Windows"

# Run mobile tests (Android OR iOS)
dotnet test --filter "Platform=Android|Platform=iOS"
```

### 14.5.3 By Feature

```bash
# Run navigation tests
dotnet test --filter "Feature=Navigation"

# Run settings and authentication
dotnet test --filter "Feature=Settings|Feature=Authentication"
```

### 14.5.4 Combined Filters

```bash
# Smoke tests on Windows
dotnet test --filter "Category=Smoke&Platform=Windows"

# UI tests except mocked API
dotnet test --filter "Category=UITest&Category!=MockedAPITest"
```

---

## 14.6 Parallel Execution

### 14.6.1 xunit.runner.json

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 1
}
```

### 14.6.2 Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `parallelizeAssembly` | false | Run assemblies in parallel |
| `parallelizeTestCollections` | true | Run collections in parallel |
| `maxParallelThreads` | 0 (auto) | Max parallel threads |

### 14.6.3 UI Test Recommendation

For UI tests, typically use:
- `parallelizeTestCollections: false` - Serial execution
- `maxParallelThreads: 1` - Single thread

This prevents UI state conflicts.

---

## 14.7 Test Priority

### 14.7.1 Priority Trait

```csharp
[Trait("Priority", "1")]  // Highest
[Trait("Priority", "2")]  // Medium
[Trait("Priority", "3")]  // Lower
```

### 14.7.2 Running by Priority

```bash
# Run highest priority only
dotnet test --filter "Priority=1"

# Run priority 1 and 2
dotnet test --filter "Priority=1|Priority=2"
```

---

## 14.8 Skip Traits

### 14.8.1 Platform Skip

```csharp
[Fact]
[Trait("SkipOn", "CI")]
public void Local_Only_Test() { }
```

### 14.8.2 Conditional Skip

```csharp
[Fact(Skip = "Waiting for bug fix #123")]
public void Broken_Test() { }

[Fact]
[PlatformSkip(Platform.iOS, "Not implemented on iOS")]
public void Windows_Only_Feature() { }
```

---

## 14.9 Best Practices

### 14.9.1 DO

- ✅ Always include base `Category=UITest`
- ✅ Add platform trait for cross-platform tests
- ✅ Use feature traits for organization
- ✅ Mark smoke tests for quick validation
- ✅ Document category meanings

### 14.9.2 DON'T

- ❌ Use too many categories (hard to maintain)
- ❌ Create overlapping categories
- ❌ Skip tests without reason
- ❌ Run all tests in parallel with UI tests

---

*Next: [Running Tests](21d15_RunningTests.md)*
