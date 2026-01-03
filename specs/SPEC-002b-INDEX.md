# SPEC-002b: Enhanced Design Documentation Index

**Version:** 1.0  
**Status:** For Review  
**Date:** January 2026  
**Purpose:** Enhanced design documents for control object hierarchy and interface improvements

---

## Document Overview

This package extends **SPEC-002: Interface Contracts** with detailed design analysis and improvement proposals based on current MAUI and Blazor implementations.

### Quick Navigation

| Document | Purpose | Best For | Length |
|----------|---------|----------|--------|
| **[SPEC-002b-SUMMARY](SPEC-002b-SUMMARY.md)** | Package overview & next steps | Getting started, executive summary | 10 min |
| **[SPEC-002b-ANALYSIS-AND-IMPROVEMENTS](SPEC-002b-ANALYSIS-AND-IMPROVEMENTS.md)** | Strategic analysis & 4 major improvements | Planning, team discussion, roadmap | 15 min |
| **[SPEC-002b-001-CONTROL-HIERARCHY-DIAGRAMS](SPEC-002b-001-CONTROL-HIERARCHY-DIAGRAMS.md)** | Visual hierarchy with Mermaid diagrams | Architecture review, visualization | 20 min |
| **[SPEC-002b-002-INTERFACE-CATALOG](SPEC-002b-002-INTERFACE-CATALOG.md)** | Complete method specifications | Implementation reference, API docs | 30 min |

---

## What's Included

### 📊 Diagrams (Mermaid)

- Core interface hierarchy (all interfaces and relationships)
- MAUI control class hierarchy (19 concrete controls)
- Blazor/Playwright control class hierarchy (14 controls + async variants)
- Capability matrix (controls grouped by interface)
- Method pattern overview
- Container scoping visualization

### 📋 Analysis

- Current state assessment (MAUI & Blazor implementations)
- Element location challenges
- Container nesting complexity
- Timeout inconsistencies
- Precondition verification patterns

### 💡 Four Major Improvement Proposals

1. **Improvement A:** Flexible Element Location Strategies
   - Support AutomationId, ElementId, ElementName, XPath, CSS Selector
   - Enable adaptation to diverse applications
   
2. **Improvement B:** Path-Based Element Selection Language
   - XPath-like syntax for nested element selection
   - Cleaner, more declarative code
   - Example: `"Parent/Child/GrandChild"` or `"dataGrid/[0]/checkbox"`
   
3. **Improvement C:** Universal Timeout Override
   - Add optional `timeoutMs` parameter to ALL public methods
   - Consistent API across all controls
   - Fine-grained timing control
   
4. **Improvement D:** Logical Precondition Verification Framework
   - Formalize precondition matrix for each method category
   - Base class helpers for common patterns
   - Consistent error messages

### ❓ Eight Clarifying Questions

Essential questions for team to answer before implementation:

- **Q1:** Element location priority when multiple methods available?
- **Q2:** How complex should path language syntax be?
- **Q3:** Keep container parameter AND add path language, or replace?
- **Q4:** How should timeouts cascade through preconditions?
- **Q5:** Handle IsReadOnly when not reliably detectable?
- **Q6:** Add Get*WithWait variants or timeoutMs on Get methods?
- **Q7:** IContainer and IListContainer as separate interfaces?
- **Q8:** Include async patterns in main SPEC-002b or platform-specific?

### 📑 Complete Interface Catalog

Detailed specifications for:
- **14 interfaces** with full method signatures
- **13 sections** (one per interface)
- **All controls** from both MAUI and Blazor
- **Preconditions, exceptions, and logging** for each method
- **Return values and parameter details**

### 🗓️ Implementation Roadmap

**Phase 1 (Q1 2026):** Foundation
- Add timeoutMs consistently
- Create precondition framework
- Add Check/Wait/Assert for values

**Phase 2 (Q2 2026):** Flexibility
- Implement IElementLocator abstraction
- Add alternative location methods
- Create platform-specific locator builders

**Phase 3 (Q3 2026):** Navigation
- Design path language
- Implement path parser
- Integrate with element finding

**Phase 4 (Q4 2026):** Polish
- Performance optimization
- Advanced filtering
- Visual tooling

---

## Control Coverage

### MAUI Platform
**19+ controls** across all categories:
- **Clickable:** Button, Label
- **Text:** Entry, Editor, SearchBar
- **Toggle:** CheckBox, Switch
- **Selector:** Picker, DatePicker, TimePicker
- **Range:** Slider, ProgressBar
- **Items:** CarouselView, CollectionView
- **Container:** Frame, ScrollView
- **Other:** ActivityIndicator, WebView, BorderControl, etc.

**Platforms Supported:** Windows, Android, iOS via Appium

### Blazor/Playwright Platform
**14 controls** across all categories:
- **Clickable:** Button, Link, Label
- **Text:** TextInput, TextArea
- **Toggle:** CheckBox
- **Selector:** Select (Dropdown)
- **Range:** RangeInput, ProgressControl
- **Items:** List, Table
- **Container:** ScrollContainer
- **Other:** Specialized controls

**Browsers Supported:** Chrome, Firefox, Edge, Safari (all modern versions)

### Async Variants (Blazor)
- ButtonControlAsync
- TextControlAsync
- Full async method support for modern Blazor patterns

---

## Key Statistics

| Metric | Count |
|--------|-------|
| **Total Control Types** | 33+ |
| **Core Interfaces** | 15 |
| **Base Classes Per Platform** | 8-10 |
| **Methods Catalogued** | 100+ |
| **Method Variants (Is/Wait/Check/Assert)** | 4+ per property |
| **Exception Types** | 5 |
| **Precondition Patterns** | 7-8 categories |
| **Implementation Tasks Proposed** | 16 |
| **Roadmap Phases** | 4 |

---

## Reading Recommendations

### For 10-Minute Overview
1. Read **SPEC-002b-SUMMARY** sections 1-3
2. Scan **SPEC-002b-ANALYSIS** sections 1-2 (Current State, Improvements)
3. Review recommendations section

### For Architecture Review
1. Start with **SPEC-002b-SUMMARY**
2. Study **SPEC-002b-001** diagrams thoroughly
3. Review **SPEC-002b-002** catalog for interface details
4. Reference **SPEC-002b-ANALYSIS** for enhancement context

### For Implementation Planning
1. Read **SPEC-002b-ANALYSIS** completely (improvements A-D, tasks)
2. Study **SPEC-002b-002** for exact method signatures
3. Review roadmap and Phase 1 implementation tasks
4. Answer clarifying questions before starting

### For API Documentation
1. Reference **SPEC-002b-002** Interface Catalog
2. Look up specific interface (e.g., IToggleControl)
3. Find method with its complete specification
4. Note preconditions, exceptions, and logging behavior

---

## Quality Assurance Notes

### ✅ Validation Done

- All 19 MAUI controls verified from source code
- All 14 Blazor controls verified from source code
- Interface hierarchies validated against implementations
- Method signatures extracted directly from codebase
- Container-scoping patterns confirmed in implementations
- Precondition verification traced through base classes

### ⚠️ Known Assumptions

- Blazor Async variants are documented but not fully integrated into main spec
- Path language is proposed but not yet designed in detail
- Platform-specific locator builders suggested but not implemented
- Some precondition checks may have platform-specific variations

### 📝 Recommendations for Review

- Verify mermaid diagrams match your architecture
- Confirm method signatures are complete
- Check for any controls missing from the catalog
- Validate precondition patterns for your use cases
- Answer clarifying questions before Phase 1 planning

---

## Integration with Other Specs

| Spec | Relationship | Impact |
|------|-------------|--------|
| **SPEC-002** | Extension | Provides detailed design for SPEC-002 interfaces |
| **SPEC-003** | Reference | Implements patterns defined in SPEC-003 |
| **SPEC-004** | Reference | Page objects use SPEC-002b control patterns |
| **SPEC-005** | Reference | Is/Wait/Check/Assert patterns defined here |
| **REQ-001** | Validates | All requirements traced to SPEC-002b |
| **REQ-002** | Validates | NFR requirements addressed in design |

---

## Next Steps

### For Leadership/Architects (This Week)
- [ ] Review SPEC-002b-SUMMARY
- [ ] Review diagrams in SPEC-002b-001
- [ ] Decide on improvement priorities
- [ ] Assign RFC owner for Path Language (if approved)

### For Development Team (This Week)
- [ ] Read complete SPEC-002b package
- [ ] Review how current implementation maps to specs
- [ ] Identify any missing controls or interfaces
- [ ] Identify any divergences from specification

### For Product/Planning (Next Week)
- [ ] Answer clarifying questions Q1-Q8
- [ ] Prioritize improvement tasks
- [ ] Map to roadmap/sprints
- [ ] Identify resource needs
- [ ] Identify risk areas

### For Implementation (Following Weeks)
- [ ] Phase 1: Foundation improvements
- [ ] Create RFC for Path Language
- [ ] Begin flexible locator abstraction
- [ ] Plan user testing with enhanced API

---

## Document Versions

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | Jan 2026 | For Review | Initial SPEC-002b package |

---

## Related Documents

- [SPEC-002: Interface Contracts](SPEC-002-interface-contracts.md) - Base specification
- [SPEC-003: Control Objects](SPEC-003-control-objects.md) - Implementation guide
- [SPEC-004: Page Objects](SPEC-004-page-objects.md) - Page patterns
- [SPEC-005: State Verification](SPEC-005-state-verification.md) - Pattern details
- [SPEC-006: Logging](SPEC-006-logging.md) - CSV logging format
- [SPEC-007: Platform Implementations](SPEC-007-platform-implementations.md) - Platform specifics

---

## Questions or Feedback?

- Review the clarifying questions in SPEC-002b-ANALYSIS section 4
- Comment on improvement proposals A-D
- Suggest additional controls or interfaces
- Identify roadmap adjustments needed

---

*The SPEC-002b package is ready for team review and discussion. Please provide feedback on the proposed improvements and answer the clarifying questions to move forward with implementation.*
