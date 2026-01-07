# Review: Requirements Clarity and Consistency

**Date:** January 6, 2026  
**Reviewer:** Automated Analysis  
**Status:** Complete

---

## Purpose

Identify clarity issues, inconsistencies, and structural improvements for existing requirements.

---

## 1. Terminology Consistency

### Issue

Different terms used for similar concepts across documents:

| Concept | Terms Used | Recommended |
|---------|-----------|-------------|
| Automation identifier | AutomationId, locator, identifier, element ID | `locator` for search, `AutomationId` for specific attribute |
| Wait timeout | timeoutMs, timeout, waitTimeout | `timeoutMs` consistently |
| Page readiness | ready, loaded, displayed, available | `ready` (loaded AND not busy), `loaded` (element present) |
| Container vs Parent | container, parent, scope, page | `container` for scoping, `parent` for hierarchy |

### Proposed Fix

Add terminology section to specs2 or create a glossary document (e.g., 000_basics/003_Glossary.spx.md).

---

## 2. Requirement Structure Inconsistency

### Issue

Requirements vary in structure:
- Some have capabilities as `###` sections (FR-002, FR-004)
- Some have capabilities as `##` sections (none currently, but inconsistent depth)
- Some lack capability IDs
- Some reference other FRs, some don't

### Proposed Standard Structure

```markdown
# functional <Name>
- **id**: FR-NNN
- **title**: Short title
- **priority**: high | medium | low
- **status**: draft | approved
- **tags**: comma, separated, tags

Overview paragraph.

## capabilities

### <CapabilityName>
- **id**: FR-NNN.N
- **title**: Capability title
- **priority**: (if different from parent)

Description paragraph.

## related

- FR-XXX: Related requirement
- G-XXX: Related goal
```

### Affected Documents

| Document | Issue |
|----------|-------|
| FR-001 | Missing `## related` section |
| FR-002 | Good structure, use as template |
| FR-003 | Missing `## related` section |
| FR-004 | Nested capability (FR-004.4.1) inconsistent with others |
| FR-005 | BusyPageBase (FR-005.4.1) nested inconsistently |
| FR-006 | Missing capability IDs |
| FR-007 | Good structure |
| FR-008 | Missing `## related` section |
| FR-009 | Good structure after recent updates |
| FR-010 | Missing `## related` section |
| FR-011 | Very short, could expand |
| FR-012 | Good structure |
| FR-013 | Good structure |

---

## 3. Ambiguous Requirements

### FR-004.1: Nullable Return Ambiguity

**Current:**
> All Is* and Get* methods must return nullable types

**Issue:**
Does "element does not exist" mean:
- Element never existed?
- Element existed but was removed (stale)?
- Element exists but is not visible?
- Timeout expired while searching?

**Proposed Clarification:**
```
- `null` — Element not found within implicit search time (not timeout)
- `false` — Element found, condition not met
- `true` — Element found, condition met

Note: If explicit timeout is specified, methods may throw 
TimeoutException instead of returning null.
```

### FR-005.6: "Exceptional Cases" Undefined

**Current:**
> Only in exceptional cases should a wait be placed after an action

**Issue:**
What constitutes an "exceptional case"?

**Proposed Clarification:**
```
Exceptional cases where post-action delays may be necessary:
- Platform-specific animation completion (when no busy indicator exists)
- Third-party component initialization
- External system synchronization
- Hardware response time (printers, scanners)

Even in these cases, prefer polling with timeout over fixed delays.
```

### FR-009.3: Reset Behavior Undefined

**Current:**
> Navigate to a known starting point

**Issue:**
How is "known starting point" determined? What about state that can't be reset via navigation?

**Proposed Clarification:**
```
Reset behavior is application-specific. Framework provides hooks:
- ResetToHome() — Navigate to application home/landing page
- ClearState() — Clear session data, caches, etc.
- Custom reset logic via override

If reset fails after N attempts (configurable), restart application.
```

---

## 4. Missing Examples

### Issue

Several requirements lack code examples:
- FR-001: No example of platform-specific code
- FR-006: No logging output example
- FR-009: No test isolation example
- FR-010: No exception handling example
- FR-011: No alternative assertion example

### Proposed Fix

Add `## examples` section to requirements showing:
- Correct usage
- Incorrect usage (anti-patterns)
- Edge cases

---

## 5. Cross-Reference Completeness

### Issue

Requirements don't consistently reference:
- Which goals they achieve
- Related requirements
- Which platform(s) they apply to

### Proposed Fix

Add standardized footer to each requirement:

```markdown
---

## traceability

**Achieves Goals:** G-001, G-002
**Related Requirements:** FR-004, FR-005
**Platforms:** All | MAUI | Blazor | WPF | Web
**Implemented In:** Brinell.Core, Brinell.Maui
```

---

## 6. Priority Justification

### Issue

Priorities assigned but not justified:
- Why is FR-008 (Extensibility) medium priority?
- Why is FR-011 (Licensing) medium priority?

### Proposed Fix

Add brief justification for non-high priorities:

```markdown
- **priority**: medium
- **priority-rationale**: Core functionality complete without extensibility; 
  can be added incrementally
```

---

## 7. Specific Document Issues

### FR-002: Control Object Pattern

**Issues:**
1. FR-002.5 says interfaces are "an example" but doesn't clarify where definitive list lives
2. No mention of interface discovery/reflection

**Proposed Changes:**
- Add reference: "Definitive interface hierarchy defined in SPEC-006-001-INTERFACES"
- Consider: Should framework support interface discovery for tooling?

### FR-003: Page Object Pattern

**Issues:**
1. FR-003.3 says "Navigation methods must not create or return target page objects" — why?
2. FR-003.4 automatic readiness could be clearer about what "readiness" means

**Proposed Changes:**
- Add rationale: "Prevents hidden dependencies and makes navigation explicit in tests"
- Clarify: "Readiness = page element visible AND page not busy (if IBusyPageObject)"

### FR-012: Container Pattern

**Issues:**
1. Relationship to FR-002.6 is confusing (FR-002.6 just says "see FR-012")
2. Container vs Page distinction could be clearer

**Proposed Changes:**
- Remove FR-002.6 or merge into FR-012 reference
- Add diagram showing Page → Container → Control hierarchy

---

## 8. Proposed New Section: Anti-Patterns

### Rationale

Requirements say what to do, but not what to avoid. Common mistakes should be documented.

### Proposed Addition

Add anti-patterns section to relevant requirements:

**FR-004 Anti-Patterns:**
```markdown
## antipatterns

### Using Is* in loops instead of Wait*
❌ while (!button.IsVisible()) { Thread.Sleep(100); }
✅ button.WaitVisible(true, timeoutMs: 5000);

### Ignoring null returns from Is* methods
❌ if (button.IsEnabled()) // null treated as false
✅ if (button.IsEnabled() == true) // explicit null handling
```

**FR-005 Anti-Patterns:**
```markdown
## antipatterns

### Fixed delays instead of condition waits
❌ button.Click(); Thread.Sleep(2000);
✅ button.Click(); page.WaitForNotBusy();

### Polling in test code
❌ while (page.IsBusy()) { Thread.Sleep(100); }
✅ page.WaitForNotBusy();
```

---

## Summary of Proposed Changes

| Category | Action | Priority |
|----------|--------|----------|
| Terminology | Create glossary document | Medium |
| Structure | Standardize requirement format | Medium |
| Ambiguity | Clarify FR-004.1, FR-005.6, FR-009.3 | High |
| Examples | Add code examples to FR-001, FR-006, FR-009, FR-010, FR-011 | Medium |
| Cross-references | Add traceability section to all FRs | Low |
| Priority | Add rationale for non-high priorities | Low |
| Anti-patterns | Add anti-pattern sections | Medium |

---

## Next Steps

1. Create glossary document (003_Glossary.spx.md)
2. Update ambiguous requirements with clarifications
3. Add examples to requirements lacking them
4. Add anti-pattern sections to FR-004, FR-005, FR-010
5. Standardize requirement structure across all documents
