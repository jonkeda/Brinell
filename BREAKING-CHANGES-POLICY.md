# Brinell Framework Breaking Changes Policy

**Version:** 1.0  
**Status:** Active  
**Date:** January 3, 2026  
**Applies to:** All Brinell packages (Core, MAUI, Blazor, etc.)

---

## Overview

This document defines what constitutes a breaking change, the deprecation process, and the timeline for removing deprecated code. All breaking changes MUST increment the MAJOR version and include migration paths.

---

## What Constitutes a Breaking Change?

### Category 1: Removal (ALWAYS Breaking)

**Removing a public API element:**
- Removing a public class
- Removing a public interface
- Removing a public method
- Removing a public property
- Removing a public event
- Removing a public constant/field

**Example:** Removing the old `Entry` class entirely (vs marking as deprecated)

```csharp
// v1.5.0 (DEPRECATED but still present)
[Obsolete("Use EntryControl instead", false)]
public class Entry { }

// v2.0.0 (Breaking change - REMOVED)
// public class Entry { }  // GONE - This is breaking!
```

**Solution:** Don't remove, deprecate first with 1-2 major version grace period

---

### Category 2: Signature Changes (ALWAYS Breaking)

**Changing method/property signatures incompatibly:**

**Adding required parameters:**
```csharp
// v1.5.0
public void Click() { }

// v2.0.0 - BREAKING! Existing code `control.Click()` fails
public void Click(int doubleClickDelay) { }  // Required param
```

**Removing parameters (even optional ones):**
```csharp
// v1.5.0
public void SetText(string text, bool validate = true) { }

// v2.0.0 - BREAKING! Existing code `control.SetText(text, false)` fails
public void SetText(string text) { }  // Param removed
```

**Changing parameter types:**
```csharp
// v1.5.0
public void SetValue(int value) { }

// v2.0.0 - BREAKING! Existing code `control.SetValue(5)` fails (int vs decimal)
public void SetValue(decimal value) { }  // Type changed
```

**Changing return types:**
```csharp
// v1.5.0
public int GetCount() { return 5; }

// v2.0.0 - BREAKING! Existing code expecting int fails
public string GetCount() { return "5"; }  // Type changed
```

**Renaming public members:**
```csharp
// v1.5.0
public string Title { get; set; }

// v2.0.0 - BREAKING! Existing code using `.Title` fails
public string Caption { get; set; }  // Renamed
```

**Solution:** Deprecate old method, keep it working, introduce new method

---

### Category 3: Behavior Changes (USUALLY Breaking)

**Changing what a method fundamentally does:**
```csharp
// v1.5.0
public int Count 
{ 
    get { return _items.Count; }  // Returns all items
}

// v2.0.0 - BREAKING! Existing code expecting all items now gets only selected
public int Count 
{ 
    get { return _selectedItems.Count; }  // Returns selected items only
}
```

**Changing event firing conditions:**
```csharp
// v1.5.0
public event EventHandler Clicked;  // Fires on any click
    
// v2.0.0 - BREAKING! Existing code expecting event on every click misses some
public event EventHandler DoubleClicked;  // Now only fires on double click
```

**Changing exception types:**
```csharp
// v1.5.0
public void Validate()
{
    if (invalid) throw new ArgumentException();
}

// v2.0.0 - BREAKING! Existing code catching ArgumentException misses validation errors
public void Validate()
{
    if (invalid) throw new ValidationException();  // Different exception!
}
```

**Solution:** Deprecate, document the change, provide migration path

---

### Category 4: Base Class Changes (SOMETIMES Breaking)

**Removing a base class:**
```csharp
// v1.5.0
public class EntryControl : TextControlBase { }

// v2.0.0 - POTENTIALLY BREAKING! If subclasses rely on TextControlBase methods
public class EntryControl : ITextInputControl { }  // Direct interface instead
```

**Changing base class:**
```csharp
// v1.5.0
public class EntryControl : TextControlBase { }

// v2.0.0 - BREAKING! Subclasses expecting TextControlBase methods fail
public class EntryControl : GenericControl { }  // Different base
```

**Solution:** Only change base classes in major versions with migration guide

---

### What Is NOT a Breaking Change?

**Adding new public members (backward compatible):**
```csharp
// v1.5.0
public class EntryControl 
{ 
    public void Click() { }
}

// v1.6.0 - NOT breaking (additive)
public class EntryControl 
{ 
    public void Click() { }
    public void DoubleClick() { }  // New method - okay in MINOR
}
```

**Adding optional parameters with defaults:**
```csharp
// v1.5.0
public void SetText(string text) { }

// v1.6.0 - NOT breaking (optional with default)
public void SetText(string text, bool validate = true) { }
// Existing calls still work: SetText("hello") uses validate=true
```

**Making classes sealed/internal (rarely breaking):**
```csharp
// v1.5.0
public class Helper { }

// v1.6.0 - Not breaking (nobody should subclass "Helper" anyway)
internal class Helper { }
```

**Bug fixes that restore intended behavior:**
```csharp
// v1.5.0
public bool IsValid { get { return _isValid; } }  // Bug: returns wrong value

// v1.5.1 - NOT breaking (fixing a bug)
public bool IsValid { get { return CheckValidation(); } }  // Fixed, now correct
```

**Performance improvements with same behavior:**
```csharp
// v1.5.0
public List<Item> GetItems() { return new List<Item>(items); }

// v1.5.1 - NOT breaking (optimization)
public List<Item> GetItems() { return items.ToList(); }  // Faster, same result
```

---

## Deprecation Process

### Step 1: Identify Deprecated API (Current Release)

When designing a new API that replaces old one:
```csharp
// New API introduced
public class EntryControl : ITextInputControl
{
    public void Enter(string text) { ... }
}

// But old API still exists
public class Entry
{
    public void SetText(string text) { ... }  // Old API
}
```

### Step 2: Mark as Obsolete (Current Release)

Add `[Obsolete]` attribute to old API:

```csharp
[Obsolete("Use EntryControl instead. Removed in v3.0.0", false)]
public class Entry
{
    public void SetText(string text) { ... }
}

// false = warning (still compiles)
// true = error (won't compile) - Don't use this for deprecation!
```

### Step 3: Document Migration (Current Release)

Create migration guide:

**docs/MIGRATION-GUIDE.md:**
```markdown
## v1.5.0 Deprecations

### Entry → EntryControl

**Old API (Deprecated):**
```csharp
Entry entry = new Entry();
entry.SetText("Hello");
string text = entry.Text;
```

**New API (Recommended):**
```csharp
EntryControl entry = new EntryControl();
entry.Enter("Hello");
string text = entry.GetText();
```

**Timeline:**
- v1.5.0: Deprecated with warning
- v2.0.0: Still works, warns
- v3.0.0: REMOVED
```

### Step 4: Support Period (Current + Next Major)

**Support Timeline:**
```
v1.5.0: [Obsolete] attribute added, code warns
v2.0.0: Old API still works, warns
v3.0.0: Old API removed entirely - BREAKING
```

**Minimum Support:**
- Current version: 6+ months
- Next major version: 6+ months
- Total: 12+ months before removal

### Step 5: Remove in Major Release

Only in major version bumps:

```csharp
// v3.0.0 - REMOVED (was deprecated since v1.5.0)
// public class Entry { }  // Gone, won't compile

// Everyone must use:
public class EntryControl : ITextInputControl { }  // New API only
```

---

## Breaking Change Categories by Severity

### TIER 1 (Severe - Major Version Required)

- Removing public classes/interfaces
- Changing method signatures incompatibly
- Changing fundamental behavior
- Removing interface implementations
- Removing base classes

**Release Impact:** All dependent code breaks, requires migration

### TIER 2 (Moderate - Major Version Required)

- Changing base class
- Changing exception types
- Changing return types
- Making class sealed when inherited

**Release Impact:** Some dependent code breaks, migration needed

### TIER 3 (Minor - Consider Major Version)

- Large behavioral changes
- Removing rarely-used overloads
- Changing default values significantly

**Release Impact:** Some code may break, deprecation recommended

---

## Communication of Breaking Changes

### In Commit Message:
```
BREAKING CHANGE: Remove EntryControl (replaced by TextInputControl)

- Remove public class EntryControl
- All functionality moved to TextInputControl
- See MIGRATION-GUIDE.md for upgrade path
- Migration period: v1.5.0 - v2.9.9
- Removed in: v3.0.0
```

### In Pull Request:
```markdown
## Breaking Changes
- [ ] Identified all breaking changes
- [ ] MIGRATION-GUIDE.md updated
- [ ] Version will be MAJOR increment
- [ ] Release notes prepared
```

### In Release Notes:
```markdown
## Breaking Changes in v2.0.0

### Entry → EntryControl
- Old `Entry` class removed
- Use new `EntryControl` class instead
- See [MIGRATION-GUIDE](docs/MIGRATION-GUIDE.md) for details
```

### In CHANGELOG.md:
```markdown
## [2.0.0] - 2026-05-01

### BREAKING CHANGES
- **Removed:** `Entry` class (deprecated since v1.5.0)
- **Changed:** `IClickable.Click()` now requires delay parameter
- **Removed:** `List<T> GetItems()` method

### Migration Path
See [MIGRATION-GUIDE.md](docs/MIGRATION-GUIDE.md) for upgrade instructions
```

---

## Backward Compatibility Matrix

### Support Guarantees

| Your Version | Uses v1.5.0 MAUI | Uses v2.0.0 MAUI | Uses v3.0.0 MAUI |
|---|---|---|---|
| **On v1.5.0** | ✅ Works | N/A | N/A |
| **On v2.0.0** | ✅ Works (warns) | ✅ Works | N/A |
| **On v3.0.0** | ❌ Fails* | ✅ Works | ✅ Works |

*You must migrate to v2.0.0 API before upgrading to v3.0.0

### Versioning Your Dependency

```xml
<!-- Most Restrictive -->
<PackageReference Include="Brinell.Maui.Controls" Version="1.5.0" />

<!-- Allow patches only (safer) -->
<PackageReference Include="Brinell.Maui.Controls" Version="1.5.*" />

<!-- Allow minor updates (with warnings) -->
<PackageReference Include="Brinell.Maui.Controls" Version="1.*" />

<!-- Allow major updates (may need migration) -->
<PackageReference Include="Brinell.Maui.Controls" Version="*" />
```

---

## Policy Exceptions

### When NOT to Deprecate

1. **Pre-release versions (v0.x.y):** Can break freely
2. **Major security vulnerability:** Break immediately
3. **Critical bug:** Can fix even if breaking
4. **API never shipped:** Doesn't need deprecation

### When to Expedite Removal

1. **Successor is mature:** Remove old API sooner
2. **Very few users:** Less impact
3. **Clear migration path:** Easy to update
4. **Community consensus:** Everyone agrees

---

## Enforcement

### During Code Review:
- [ ] All breaking changes identified
- [ ] Marked with `[Obsolete]` if replacing
- [ ] Migration guide added
- [ ] Version number planned (MAJOR)
- [ ] CHANGELOG entry added

### During Release:
- [ ] Breaking changes prominently documented
- [ ] Migration guide complete
- [ ] Release notes explain impact
- [ ] Community notified

### During Support:
- [ ] Deprecated code still compiles
- [ ] Warnings clear and helpful
- [ ] Support period honored (12+ months)
- [ ] Removal only in major version

---

## Examples

### ✅ Correct: Adding New Feature (Not Breaking)

```csharp
// v1.5.0
public interface ITextInputControl
{
    void Enter(string text);
}

// v1.6.0 - NOT breaking
public interface ITextInputControl
{
    void Enter(string text);
    void Clear();  // NEW - okay in MINOR
}
```

### ❌ Wrong: Removing Method (Breaking, Wrong Approach)

```csharp
// v1.5.0
public void SetText(string text) { }
public void Clear() { }

// v2.0.0 - BREAKING but no deprecation path!
public void SetText(string text) { }
// public void Clear() { }  // Removed with no warning

// ⚠️ Users code breaks without warning
```

### ✅ Correct: Removing Method (Breaking, Right Approach)

```csharp
// v1.5.0
[Obsolete("Use ClearAll() instead", false)]
public void Clear() { }
public void ClearAll() { }

// v2.0.0 - BREAKING but with migration path
[Obsolete("Use ClearAll() instead", false)]
public void Clear() { }  // Still works
public void ClearAll() { }

// v3.0.0 - NOW we can remove
// public void Clear() { }  // Gone
public void ClearAll() { }
```

---

## FAQ

**Q: Can I change parameter order?**  
A: Yes if you use named parameters. No if positional. Better: deprecate and introduce new method.

**Q: Can I change a method from instance to static?**  
A: No, this is breaking. Deprecate and create new static method.

**Q: How long do I have to support deprecated code?**  
A: Minimum 1-2 major versions (typically 6-12 months).

**Q: Can I remove deprecated code in a MINOR release?**  
A: No. Only in MAJOR releases. MINOR is backward compatible.

**Q: What if I mark something obsolete by mistake?**  
A: Remove the [Obsolete] attribute before release. Not a big deal pre-release.

**Q: Can I have multiple deprecated methods?**  
A: Yes. Just ensure clear migration paths for each.

---

## Approval & Updates

**Created:** January 3, 2026  
**Last Updated:** January 3, 2026  
**Next Review:** April 3, 2026  
**Owner:** Brinell Team  
**Status:** Active & Enforced

This policy is mandatory for all Brinell packages. Violations require exception approval.

---

See also:
- [VERSIONING.md](VERSIONING.md) - Version strategy
- [VERSION-ROADMAP.md](VERSION-ROADMAP.md) - Release schedule
- [MIGRATION-GUIDE.md](docs/MIGRATION-GUIDE.md) - User migration paths
- [CHANGELOG.md](CHANGELOG.md) - Version history
