# Brinell Framework Versioning Strategy

**Version:** 1.0  
**Status:** Active  
**Date:** January 3, 2026  
**Last Updated:** January 3, 2026

---

## Overview

This document defines the versioning strategy for all Brinell framework packages (Core, MAUI, Blazor, etc.). All packages follow semantic versioning (SemVer) with extensions for pre-release stability.

---

## Semantic Versioning (SemVer)

All packages use MAJOR.MINOR.PATCH format:

```
MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
Example: 2.1.3-beta.1+build.123
```

### MAJOR Version
- **Incremented when:** Breaking changes to public API
- **Examples:**
  - Removing a public interface
  - Changing method signature incompatibly
  - Changing behavior of existing method
  - Changing control implementation base
- **Compatibility:** Previous MAJOR versions not supported
- **Migration:** Users MUST migrate code or use old version

### MINOR Version
- **Incremented when:** New functionality, backwards compatible
- **Examples:**
  - Adding new control/component
  - Adding new interface
  - Adding new optional method
  - Adding new helper class
- **Compatibility:** Fully backward compatible
- **Migration:** No migration required, optional upgrade

### PATCH Version
- **Incremented when:** Bug fixes, internal improvements
- **Examples:**
  - Fix control behavior bug
  - Performance improvement
  - Internal refactoring (no API change)
  - Documentation improvements
  - Test coverage improvements
- **Compatibility:** Fully backward compatible
- **Migration:** None required

---

## Pre-Release Versions

Used for unstable releases and testing.

```
2.0.0-alpha.1      First alpha of 2.0
2.0.0-beta.1       First beta (more stable than alpha)
2.0.0-rc.1         Release candidate (nearly final)
```

### Alpha (α)
- **Use case:** Early feature preview, API still changing
- **Stability:** Low, expect bugs and API changes
- **Support:** Community testing only, no production use
- **Duration:** Until beta ready

### Beta (β)
- **Use case:** Feature complete, testing phase
- **Stability:** Medium, API stable but bugs may exist
- **Support:** Test in non-critical environments
- **Duration:** Until RC ready

### Release Candidate (RC)
- **Use case:** Pre-release validation
- **Stability:** High, expecting final release soon
- **Support:** Deploy to staging for final validation
- **Duration:** 1-2 weeks typically

---

## Version Evolution Strategy

### MAUI Platform Versioning

**Phase 1: Foundation (0.1.0)**
- Core interfaces defined
- Base classes implemented
- Sample app structure
- CI/CD pipeline
- No public controls yet
- Status: Internal development

**Phase 2: Core Controls (1.0.0)**
- First 3-5 essential controls implemented
- Text input controls functional
- Selection controls functional
- Test coverage minimum 70%
- Status: Beta release

**Phase 3: Extended Controls (1.1.0 → 1.5.0)**
- Additional controls added in batches
- Each control increment: PATCH version
- Each category of controls: MINOR version
  - 1.1.0: Text input controls (3 controls)
  - 1.2.0: Selection controls (6 controls)
  - 1.3.0: Toggle controls (3 controls)
  - 1.4.0: Range controls (3 controls)
  - 1.5.0: Date/time controls (3 controls)
- Status: Incremental releases

**Phase 4: All Controls (2.0.0)**
- All 30+ MAUI controls implemented
- Complete documentation
- Performance baselines established
- Test coverage >90%
- Status: Stable release

### Blazor Platform Versioning

**Phase 1: Foundation (1.0.0)**
- Shared interfaces from MAUI
- Base component wrappers
- Sample app structure
- Status: Initial release

**Phase 2: Form Components (1.0.1 → 1.0.8)**
- InputText, InputNumber, InputSelect wrappers
- Each component: PATCH increment
- Status: Incremental updates

**Phase 3: Layout & Utilities (1.1.0 → 1.2.9)**
- Layout components
- Navigation components
- Validation components
- Utilities and advanced features
- Status: Feature additions

**Phase 4: Release (2.0.0)**
- Aligned with MAUI 2.0.0
- Complete feature parity
- Full documentation
- Status: Stable release

---

## Version Increment Rules per Control/Component

### Adding a New Control (MAUI) → MINOR.PATCH

For each new control added:
- Increment PATCH version
- Group related controls for MINOR increment

**Examples:**
```
1.0.0 → 1.0.1  (Add EntryControl)
1.0.1 → 1.0.2  (Add EditorControl)
1.0.2 → 1.0.3  (Add SearchBarControl)
1.0.3 → 1.1.0  (Add PickerControl - start new category)
1.1.0 → 1.1.1  (Add CheckBoxControl)
...
```

### Breaking Change (Any) → MAJOR

For any incompatible change:
- Increment MAJOR version
- Reset MINOR and PATCH to 0

**Examples:**
```
1.5.3 → 2.0.0  (Change ITextInputControl interface signature)
1.2.1 → 2.0.0  (Remove deprecated control class)
```

### Bug Fix → PATCH

For fixes and improvements:
- Increment PATCH only

**Examples:**
```
1.1.5 → 1.1.6  (Fix EntryControl validation bug)
1.2.0 → 1.2.1  (Performance improvement in ListViewControl)
```

---

## Breaking Changes Policy

### What Constitutes a Breaking Change?

**Removal:**
- Removing public class, interface, method, property
- Example: Removing old `Entry` class marked `[Obsolete]`

**Signature Changes:**
- Changing method parameter types
- Adding required parameters
- Changing method return type
- Renaming public members

**Behavior Changes:**
- Changing what a method does fundamentally
- Changing event firing conditions
- Changing property validation rules

**Implementation Changes:**
- Changing base class
- Removing interface implementation
- Changing exception types thrown

### NOT Breaking Changes:

**Additions:**
- Adding new public classes, methods, properties
- Adding optional parameters with defaults
- Adding new interfaces to implement

**Improvements:**
- Bug fixes that restore intended behavior
- Performance improvements with same behavior
- Adding XML documentation

**Deprecations:**
- Marking classes/methods as `[Obsolete]`
- Providing migration path to new API
- Still compiles and works (just warns)

---

## Deprecation Process

### Step 1: Mark as Obsolete (Current Version)
```csharp
[Obsolete("Use EntryControl instead. Removed in v3.0.0", false)]
public class Entry
{
    // Old implementation still works
}
```

- `false` parameter = warning, not error
- Still functional for existing code
- Clear message on migration path
- Version when it will be removed specified

### Step 2: Support Period (Current + Next Major)
- Current version: Code compiles with warning
- Next version: Still works, warns
- Minimum 1-2 major versions of support

**Example Timeline:**
```
v1.5.0: Mark old Entry as [Obsolete("Use EntryControl")]
v2.0.0: Old Entry still works, warns user
v3.0.0: Old Entry removed entirely
```

### Step 3: Remove in Major Version
- Only remove in major version bumps
- Provide migration guide
- Clear changelog entry

---

## NuGet Package Versioning

### Package Structure
```
Brinell.Maui.Controls.1.0.0.nupkg
Brinell.Maui.Controls.1.0.0-beta.1.nupkg
Brinell.Maui.Controls.2.0.0-rc.1.nupkg
```

### Package Metadata
- **AssemblyVersion:** MAJOR.0.0.0 (MAJOR only, for binary compatibility)
- **FileVersion:** MAJOR.MINOR.PATCH.BUILD
- **InformationalVersion:** MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]

### NuGet Distribution
- **Stable Releases:** Published to nuget.org
- **Pre-releases:** Published to nuget.org with pre-release tag
- **Internal Builds:** Published to internal NuGet feed with +build suffix

---

## Release Schedule

### Stable Releases (Production)
- Minimum 2 weeks between MAJOR releases
- MINOR releases within a MAJOR version: weekly possible
- PATCH releases: as needed for critical fixes

### Pre-Release Timeline
- Alpha phase: 2-4 weeks
- Beta phase: 1-3 weeks  
- RC phase: 1-2 weeks
- Total pre-release: 4-9 weeks before stable

### Support Schedule
- Current version: Full support
- Previous MAJOR: Security/critical fixes only
- Older: Community support only

---

## Version Roadmap

### 2026 Timeline

**Q1 2026:**
- v0.1.0 (Jan): MAUI Foundation + Interfaces (Phase 1)
- v1.0.0 (Feb): MAUI Core Controls + Sample App (Phases 2-4)
- v1.0.0 (Feb): Blazor Foundation (Phase 1)

**Q2 2026:**
- v1.1.0-1.5.0 (Mar-Apr): MAUI Extended Controls
- v1.0.1-1.2.9 (Mar-Apr): Blazor Components
- v2.0.0 (May): MAUI + Blazor Stable Release

**Q3-Q4 2026:**
- v2.0.1+ (Jun+): Bug fixes and patches
- v2.1.0+ (Aug+): New features
- v3.0.0 (Nov): Next major (removes deprecated code)

---

## Version Documentation

### What to Document per Release

**All Releases:**
- Version number and date
- Supported .NET versions
- Breaking changes (if any)
- New features
- Bug fixes
- Deprecations

**Major Releases:**
- Migration guide
- Compatibility matrix
- Removal of deprecated APIs
- Performance improvements

**Pre-releases:**
- Known issues
- Testing areas needed
- Stability warnings
- Timeline to stable

---

## Implementation in CI/CD

### Automatic Version Detection
- Version read from `.csproj` file
- Enforced in build step
- Package created with correct version
- Release notes linked to version

### Version Validation
- Confirm version increment is appropriate
- Verify previous version correct
- Check against version roadmap

### Documentation of Versions
- CHANGELOG.md updated per version
- Release notes generated from commits
- Version history in NuGet package

---

## Tools & Utilities

### Version Checking
```powershell
# Check current version
dotnet package-info Brinell.Maui.Controls

# Compare versions
[version]"2.0.0" -gt [version]"1.5.0"  # Returns True
```

### Version Bumping
```powershell
# Manual (before commit)
# Edit src/Brinell.Maui.Controls/Brinell.Maui.Controls.csproj
# Change <Version>1.0.0</Version> to <Version>1.0.1</Version>

# Build to verify
dotnet build
```

---

## Frequently Asked Questions

**Q: Can I skip versions?**  
A: No. Follow the roadmap. Skipping confuses users about what's in each version.

**Q: What if I add a breaking change but don't want major version?**  
A: You must use MAJOR version. SemVer requires it. Alternatively, deprecate first, remove later.

**Q: How do I document a breaking change?**  
A: Include in CHANGELOG.md under "BREAKING CHANGES" section with migration path.

**Q: When should I remove deprecated code?**  
A: Only in MAJOR version, minimum 2 MAJOR versions after deprecation (e.g., deprecated in v1.5, removed in v3.0).

**Q: Can patch versions go beyond .99?**  
A: Yes, 1.0.100+ is valid. But if you have many patches, consider MINOR instead.

---

## Approval & Updates

**Last Reviewed:** January 3, 2026  
**Next Review:** April 3, 2026  
**Owner:** Brinell Team  
**Status:** Active & Enforced

Changes to this document require team consensus before implementation.

---

See also:
- [PLAN-011: MAUI Implementation](plan/PLAN-011-MAUI-Detailed-Implementation.md) - Version targets per phase
- [CHANGELOG.md](CHANGELOG.md) - Version history
- [PACKAGE-README.md](src/Brinell.Maui.Controls/PACKAGE-README.md) - NuGet package info
