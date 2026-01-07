# 250 Specifications Index

**Block Type:** SPC (Specifications)  
**Parent:** [specs2](../specs2_INDEX.md)

---

## Overview

This folder contains all specifications for the Brinell UI Test Automation Framework. Specifications define the complete behavior, boundaries, and acceptance criteria for framework components.

Specifications are organized by levels per [PLAN-002](../Plan/PLAN-002-Specification-Levels.md):

- **Level 0:** Foundation (interfaces, base classes, contexts)
- **Level 1:** Core Controls (Button, Label, Entry, CheckBox, Container)
- **Level 2:** Selection Controls (Dropdown, ListBox, RadioGroup)
- **Level 3:** Advanced Controls (Slider, DatePicker, DataGrid, Tab)
- **Level 4:** Platform-Specific Controls
- **Level 5:** Remaining Controls

---

## Specification Folders

| ID | Folder | Level | Description |
|----|--------|-------|-------------|
| 250_000 | [Foundation](250_000_Foundation/250_000_INDEX.md) | 0 | Interfaces, base classes, contexts |
| 250_100 | [CoreControls](250_100_CoreControls/250_100_INDEX.md) | 1 | Button, Label, Entry, CheckBox, Container |
| 250_200 | [SelectionControls](250_200_SelectionControls/250_200_INDEX.md) | 2 | Dropdown, ListBox, RadioGroup |
| 250_300 | [AdvancedControls](250_300_AdvancedControls/250_300_INDEX.md) | 3 | Slider, DatePicker, DataGrid, Tab |
| 250_400 | [PlatformSpecific](250_400_PlatformSpecific/250_400_INDEX.md) | 4 | Platform-specific controls |
| 250_500 | [Remaining](250_500_Remaining/250_500_INDEX.md) | 5 | All other controls |

---

## SPX V7 Specification Blocks

Specifications use SPX V7 blocks (remapped to 250 series):

| SPX Block | Code | Purpose |
|-----------|------|---------|
| 250 specification | SPC | Main specification container |
| 251 behavior | BHV | What the component does |
| 252 boundary | BND | Edge cases, limits, error handling |
| 253 acceptance | ACC | Testable acceptance criteria |
| 254 assumption | ASM | Preconditions and dependencies |
| 255 exclusion | EXC | Explicit out-of-scope items |

---

## Level Progression

Each level must pass gate criteria before advancing to the next:

1. Specifications complete and reviewed
2. Implementation matches specification
3. Unit tests passing (with mocks)
4. UI tests passing (against sample apps)
5. No base class changes required

See [PLAN-002](../Plan/PLAN-002-Specification-Levels.md) for detailed level progression rules.

---

## Related Documents

- [PLAN-002 Specification Levels](../Plan/PLAN-002-Specification-Levels.md)
- [Architecture Overview](../200_architecture/200_000_Overview.spx.md)
- [Interfaces Module](../200_architecture/211_Modules/211_001_Interfaces.spx.md)
