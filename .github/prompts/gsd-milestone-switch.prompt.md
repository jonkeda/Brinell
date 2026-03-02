---
description: Switch the active milestone within the current project
---

Switch the active milestone within the current GSD project. The active project does not change.

## Steps

1. **Check active context**: Call `gsd_active_read` to confirm the current project.
   - If no active project is set: inform the user to run `/gsd-project-switch` first.

2. **Parse arguments**:
   - If `{milestone}` is provided: call `gsd_switch_milestone` with that milestone directly.
   - If no arg: call `gsd_switch_milestone` with no args — it returns a list of available milestones.
     Present the list and ask the user to select one, then call `gsd_switch_milestone` with the chosen milestone.

3. **Confirm the switch**:

   > Active milestone is now **{milestone}** (project: **{project}**)
