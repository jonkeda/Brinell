# GitHub Actions CI/CD Pipeline - Phase 001

**Last Updated:** January 4, 2026  
**Status:** Phase 001 Task 4 - CI/CD Pipeline (✅ IMPLEMENTED)  
**Workflow File:** `.github/workflows/build-and-test.yml`

---

## Overview

This document describes the CI/CD pipeline implemented for the Brinell Framework as part of Phase 001, Task 4. The pipeline automates build, test, and validation processes for all platform implementations.

### Key Features

- ✅ **Multi-platform builds** (Windows, Linux, macOS)
- ✅ **Automated test execution** (Core + Fixtures)
- ✅ **Code coverage reporting** (preparation for Phase 002)
- ✅ **Artifact management** (30-90 day retention)
- ✅ **Manual dispatch support** (workflow_dispatch)
- ✅ **Selective triggers** (path-based filtering)

---

## Workflow Structure

### Trigger Configuration

**Automatic Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Changes to specific paths:
  - `src/**` - All platform implementations
  - `samples/Brinell.Samples.Maui.Fixtures/**` - Sample fixture tests
  - `tests/**` - Test projects
  - `.github/workflows/**` - CI/CD configuration
  - `Directory.*.props` - Central package management files

**Manual Trigger:**
- `workflow_dispatch` - Run from GitHub Actions UI

### Job Configuration

#### 1. **Build Job** (Multi-platform)

```
Runs on: Windows, Linux, macOS
Matrix: 3 parallel jobs (one per OS)
```

**Steps:**
1. Checkout code with full history (`fetch-depth: 0`)
2. Setup .NET 9.0.x
3. Restore dependencies from all projects
4. Build entire solution in Release configuration
5. Validate build succeeded

**Success Criteria:**
- Exit code 0 on all platforms
- No compilation errors
- Build completes in < 5 minutes per platform

**Required for:** Test job (depends_on: build)

#### 2. **Test Fixtures Job** (Windows-only)

```
Runs on: Windows-latest
Depends on: build
Phase 001 Task: Task 3 - Sample Data & Fixtures
```

**Purpose:** Validate sample fixture implementations and test data generation

**Steps:**
1. Checkout code
2. Setup .NET 9.0.x
3. Restore fixture project dependencies
4. Run fixture tests with TRX logging + code coverage
5. Upload test results artifact

**Test Coverage:**
- `Brinell.Samples.Maui.Fixtures` - 33 unit tests
  - UserFixture (7 tests) - Bogus generation, builder pattern
  - ProductFixture (15+ tests) - Product generation, convenience methods
  - TestDataFactory (9+ tests) - Factory patterns, test scenarios

**Success Criteria:**
- All 33 tests passing (< 100ms total)
- Code coverage > 85%
- No broken builder patterns

**Artifacts Retained:** test-results.trx (30 days)

#### 3. **Test Core Job** (Windows-only)

```
Runs on: Windows-latest
Depends on: build
Phase 001 Task: Task 5 - Interface Assembly
```

**Purpose:** Validate core interfaces and platform implementations

**Steps:**
1. Checkout code
2. Setup .NET 9.0.x
3. Restore all project dependencies
4. Run all unit tests with TRX logging + code coverage
5. Upload test results artifact

**Test Coverage:**
- Brinell.Core - Interface contracts (57 interfaces)
- Brinell.Maui - MAUI platform implementation
- Brinell.Wpf - WPF platform implementation
- Brinell.WinForms - WinForms platform implementation
- Brinell.Html - HTML/web platform
- Brinell.Html.Playwright - Playwright integration
- Brinell.Stride - Stride platform
- Brinell.FlaUI - FlaUI integration

**Success Criteria:**
- All unit tests passing
- Code coverage > 80%
- No breaking changes to interfaces
- All platform implementations compile

**Artifacts Retained:** test-results.trx (30 days)

---

## Environment Configuration

```
DOTNET_VERSION: 9.0.x (latest stable)
CONFIGURATION: Release
BUILD_VERBOSITY: minimal (faster logs, cleaner output)
```

### Platform-Specific Notes

**Windows (Primary)**
- Supports all platforms (MAUI, WPF, WinForms, FlaUI)
- Runs all tests
- Generates full code coverage

**Linux**
- Build only (no UI framework tests)
- Validates cross-platform compilation
- Used for .NET/console libraries

**macOS**
- Build only (no platform-specific frameworks)
- Validates macOS compatibility for shared code
- Future: MAUI iOS build/test

---

## Execution Flow

```
┌─────────────────────────────────────────────┐
│ Event: Push/PR/Manual Dispatch              │
└──────────────┬──────────────────────────────┘
               │
               ▼
        ┌──────────────┐
        │ Build Job    │ (Multi-OS: Windows/Linux/macOS)
        │ 3 parallel   │
        └──────┬───────┘
               │
        ┌──────▼─────────────────┐
        │ Test Fixtures Job      │ (Windows only)
        │ 33 tests in ~100ms     │
        └──────┬──────────────────┘
               │
        ┌──────▼─────────────────┐
        │ Test Core Job          │ (Windows only)
        │ All interfaces + impls │
        └──────────────────────┘
```

**Parallel Execution:** Build job completes on all 3 platforms before test jobs start.

**Total Pipeline Time:** ~10-15 minutes
- Build: 2-3 min per platform (parallel)
- Fixture Tests: 1-2 minutes
- Core Tests: 5-10 minutes (depends on count)

---

## Test Results & Artifacts

### Fixture Test Results

**Location:** `samples/Brinell.Samples.Maui.Fixtures/bin/Release/net9.0/test-results.trx`

**Retention:** 30 days (Phase 001 only)

**Contents:**
- UserFixture tests (7 passing)
- ProductFixture tests (15+ passing)
- TestDataFactory tests (9+ passing)

**Reporting:** Results viewable in GitHub Actions UI

### Core Test Results

**Location:** Multiple `.trx` files across all projects

**Retention:** 30 days

**Contents:**
- Brinell.Core interface contract tests
- Platform-specific implementation tests
- Mock object validation tests

---

## Phase 001 Task Validation

### Task 1: Version Management ✅
- VERSIONING.md - Defines semantic versioning
- VERSION-ROADMAP.md - 2026 roadmap
- BREAKING-CHANGES-POLICY.md - Deprecation policy
- **Pipeline Integration:** N/A (documentation only)

### Task 3: Sample Data & Fixtures ✅
- 33 unit tests in Brinell.Samples.Maui.Fixtures
- UserFixture, ProductFixture, TestDataFactory
- **Pipeline Integration:** `test-fixtures` job validates all 33 tests pass

### Task 5: Interface Assembly ✅
- 57 interfaces added to Brinell.Core
- Platform implementations updated
- **Pipeline Integration:** `test-core` job validates interface contracts

### Task 4: CI/CD Pipeline ✅
- GitHub Actions workflow implemented
- Multi-platform build validation
- Automated test execution
- Artifact management
- **Pipeline Integration:** This document describes implementation

---

## Usage

### Automatic Trigger

1. Push to `main` or `develop` branch:
   ```bash
   git push origin develop
   ```
   Pipeline automatically starts

2. Create pull request to `main` or `develop`:
   - Pipeline automatically validates PR changes

### Manual Trigger

1. Navigate to GitHub Actions tab
2. Select "Build & Test - Phase 001" workflow
3. Click "Run workflow"
4. Select branch (main/develop)
5. Click "Run workflow"

---

## Monitoring & Debugging

### View Workflow Status

1. GitHub Actions tab → "Build & Test - Phase 001"
2. Find run by commit/branch/date
3. Click run to view details

### View Job Logs

1. Click specific job (Build/Test Fixtures/Test Core)
2. Expand steps to see detailed logs
3. PowerShell errors include: ❌ or ✅ indicators

### Download Artifacts

1. Click job run
2. Scroll to "Artifacts" section
3. Download `.trx` files for offline analysis

### Common Issues

**Build Fails on Specific OS**
- Check matrix job logs
- Look for OS-specific dependencies
- Review build output for missing tools

**Test Artifacts Not Found**
- Verify test project has `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- Check test results path in step output
- Ensure TRX logger is installed

**Long Pipeline Duration**
- Reduce test count if Phase 002+ adds many tests
- Consider splitting into separate jobs
- Use `fail-fast: false` to complete all parallel builds

---

## Future Enhancements

### Phase 002+

**Code Coverage Reporting** (Currently prepared, not yet active)
- Integrate with Codecov.io
- Generate coverage badges
- Track coverage trends

**Package Distribution** (Future)
- Add `package` job (dotnet pack)
- Publish to NuGet.org
- Generate release notes

**Deployment** (Future)
- Add `deploy` job for sample apps
- Push to Azure App Service / GitHub Pages
- Update documentation sites

**Analysis** (Future)
- SonarQube integration
- StyleCop analysis (treat warnings as errors)
- Security scanning

---

## Configuration Files

### Main Workflow
- **File:** `.github/workflows/build-and-test.yml`
- **Version:** 1.0 (Phase 001 Task 4)
- **Last Updated:** Jan 4, 2026

### Supporting Files
- `.github/PIPELINE.md` - This documentation
- `.github/copilot-instructions.md` - Copilot guidelines
- `.github/ISSUE_TEMPLATE/` - Issue templates

### Environment Files
- `Directory.Build.props` - Central build config
- `Directory.Packages.props` - Central package management
- `global.json` - .NET SDK version lock

---

## Quick Reference

### Pipeline Commands

**View workflow status:**
```bash
gh run list --workflow build-and-test.yml
```

**View latest run details:**
```bash
gh run view --log
```

**Re-run failed job:**
```bash
gh run rerun [run-id] --failed
```

### Local Validation

Before pushing, validate locally:

```bash
# Build all platforms
dotnet build Brinell.sln -c Release

# Run fixture tests
dotnet test samples/Brinell.Samples.Maui.Fixtures/ -c Release

# Run all tests
dotnet test Brinell.sln -c Release
```

---

## Contacts & Support

- **Framework Owner:** Brinell Team
- **Pipeline Issues:** GitHub Issues with `[pipeline]` label
- **Questions:** See CONTRIBUTING.md

---

**Phase 001 Completion:** Task 4 - CI/CD Pipeline ✅ DONE (Jan 4, 2026)

Next: Phase 002 - Sample App Structure (2-4 weeks)
