# 15. Running Tests

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d15_RunningTests_CodeExamples.md](21d15_RunningTests_CodeExamples.md)  
**Previous:** [Test Categories](21d14_TestCategories.md)

---

## 15.1 Overview

This document covers all methods for running UI tests: locally, in CI/CD, and on cloud providers.

---

## 15.2 Local Execution

### 15.2.1 Visual Studio

1. **Test Explorer:** View > Test Explorer
2. **Run All:** Ctrl+R, A
3. **Run Selected:** Ctrl+R, T
4. **Filter by trait:** Use search box with `Trait:Category=Smoke`

### 15.2.2 Command Line (dotnet test)

```bash
# Run all tests in project
dotnet test ./src/Oravey.Tools.Wpf.UITests

# Run with filter
dotnet test --filter "Category=UITest"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~SettingsTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~Settings_Can_Change_Username"
```

---

## 15.3 Filter Syntax

### 15.3.1 Basic Filters

| Filter | Description |
|--------|-------------|
| `Name=TestName` | Exact test name |
| `FullyQualifiedName~Contains` | Contains substring |
| `Category=UITest` | By trait |
| `Priority=1` | By priority trait |

### 15.3.2 Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `=` | Equals | `Category=Smoke` |
| `!=` | Not equals | `Category!=E2E` |
| `~` | Contains | `Name~Settings` |
| `&` | AND | `Category=UITest&Platform=Windows` |
| `|` | OR | `Platform=Windows|Platform=Web` |
| `!` | NOT | `!Category=Slow` |
| `()` | Grouping | `(Category=Smoke|Category=E2E)&Platform=Windows` |

---

## 15.4 Output Formats

### 15.4.1 Console Logger

```bash
dotnet test --logger "console;verbosity=detailed"
```

Verbosity levels: quiet, minimal, normal, detailed, diagnostic

### 15.4.2 TRX (VS Test Results)

```bash
dotnet test --logger "trx;LogFileName=results.trx"
```

### 15.4.3 HTML Report

```bash
dotnet test --logger "html;LogFileName=results.html"
```

### 15.4.4 JUnit (for CI/CD)

```bash
dotnet test --logger "junit;LogFileName=results.xml"
```

Requires: `dotnet add package JunitXml.TestLogger`

---

## 15.5 Environment Configuration

### 15.5.1 Environment Variables

```bash
# Set platform
set PLATFORM=Windows

# Set app path
set APP_PATH=C:\path\to\app.exe

# Enable cloud execution
set CLOUD_PROVIDER=BrowserStack
set CLOUD_USERNAME=user
set CLOUD_ACCESS_KEY=key

# Configure logging
set LOG_OUTPUT_PATH=logs
set LOG_PREFIX=UITests
```

### 15.5.2 Run Configuration (.runsettings)

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <PLATFORM>Windows</PLATFORM>
      <APP_PATH>C:\path\to\app.exe</APP_PATH>
    </EnvironmentVariables>
    <MaxCpuCount>1</MaxCpuCount>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
  </RunConfiguration>
  <xUnit>
    <MaxParallelThreads>1</MaxParallelThreads>
    <ParallelizeAssembly>false</ParallelizeAssembly>
    <ParallelizeTestCollections>false</ParallelizeTestCollections>
  </xUnit>
</RunSettings>
```

Usage:
```bash
dotnet test --settings uitests.runsettings
```

---

## 15.6 CI/CD Integration

### 15.6.1 GitHub Actions

```yaml
name: UI Tests

on: [push, pull_request]

jobs:
  ui-tests:
    runs-on: windows-latest
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Build
        run: dotnet build
      
      - name: Run UI Tests
        run: dotnet test --filter "Category=UITest" --logger "trx"
        env:
          PLATFORM: Windows
      
      - name: Upload Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: '**/*.trx'
```

### 15.6.2 Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '9.0.x'
  
  - task: DotNetCoreCLI@2
    displayName: 'Build'
    inputs:
      command: 'build'
  
  - task: DotNetCoreCLI@2
    displayName: 'Run UI Tests'
    inputs:
      command: 'test'
      arguments: '--filter "Category=UITest" --logger trx'
    env:
      PLATFORM: Windows
  
  - task: PublishTestResults@2
    condition: always()
    inputs:
      testResultsFormat: 'VSTest'
      testResultsFiles: '**/*.trx'
```

---

## 15.7 Parallel Execution

### 15.7.1 Why Serial for UI Tests

- UI applications have shared state
- Only one instance can run at a time
- Window focus issues
- Screen capture conflicts

### 15.7.2 Configuration

```json
// xunit.runner.json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false,
  "maxParallelThreads": 1
}
```

### 15.7.3 Parallel by Platform

Run different platforms in parallel:

```yaml
# GitHub Actions matrix
strategy:
  matrix:
    platform: [Windows, Web]
  fail-fast: false
```

---

## 15.8 Debugging Tests

### 15.8.1 Visual Studio

1. Set breakpoint in test
2. Right-click test > Debug
3. Use Debug > Windows > Exception Settings for exceptions

### 15.8.2 Command Line

```bash
# Enable diagnostic logging
dotnet test --diag:test_diagnostic.log

# Blame mode (for hanging tests)
dotnet test --blame

# Blame with dumps
dotnet test --blame-crash --blame-hang --blame-hang-timeout 5m
```

### 15.8.3 Screenshots on Failure

```csharp
public void Dispose()
{
    if (_testFailed)
    {
        TakeScreenshot("failure");
    }
}
```

---

## 15.9 Performance Tips

### 15.9.1 Reduce Startup Time

- Use shared fixture for app launch
- Minimize app initialization in tests
- Cache page objects where appropriate

### 15.9.2 Optimize Waits

```csharp
// Use polling instead of fixed delays
Context.WaitFor(() => element.IsVisible);

// Not this
Thread.Sleep(5000);
```

### 15.9.3 Run Smoke Tests First

```bash
# Quick validation before full suite
dotnet test --filter "Category=Smoke"
if ($LASTEXITCODE -eq 0) {
    dotnet test --filter "Category=Regression"
}
```

---

## 15.10 Common Commands

```bash
# Build and test
dotnet build && dotnet test --filter "Category=UITest"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# List all tests without running
dotnet test --list-tests

# Run with timeout
dotnet test --blame-hang-timeout 60s
```

---

*Next: [Best Practices](21d16_BestPractices.md)*
