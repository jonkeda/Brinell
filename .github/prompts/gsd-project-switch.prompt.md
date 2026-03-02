---
description: Switch the active project and optionally milestone context
---

Switch the active GSD project context. All subsequent GSD tool calls resolve paths relative to the selected project and milestone.

## Steps

1. **Read current context**: Call `gsd_active_read` to show the current active project and milestone.

2. **Parse arguments from user input**:
   - If both `{project}` and `{milestone}` are provided: call `gsd_switch_project` directly with both args.
   - If only `{project}` is provided: call `gsd_switch_project` with project only — it auto-selects the latest milestone.
   - If no args: call `gsd_list_projects` to list available projects, then present:

     ```
     Available projects:
     1. {name} — active milestone: {active_milestone}
     2. ...
     ```

     Ask the user to choose one, then call `gsd_switch_project` with the chosen project.

3. **Confirm the switch**:

   > Active project is now **{project}** / **{milestone}**

4. **Optional follow-up**: If the user wants to also switch milestone within the new project,
   suggest running `/gsd-milestone-switch`.
