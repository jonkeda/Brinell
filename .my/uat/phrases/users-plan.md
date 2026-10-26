# Todo users: later

Status: **todo**. Date: 2026-10-03.
The users app, its UI tests and its UAT suite are built (TOD.11, in the existing Todo projects, in
the `Users` namespace); the full plan, its decisions and its progress are at
[6ecadac2](https://github.com/exact-mobile-apps/test-tooling/blob/6ecadac2/.my/uat/phrases/users-plan.md). What follows was left for later; the decisions are a starting point, not
settled.

- **Assignment.** `TodoDto.AssigneeIds` (a list, sorted by name when shown), syncing with the todo
  under the newer-`UpdatedAt` rule. An unknown id is kept and shown as "Unknown user". The API
  keeps assignees when the field is **missing** and replaces them when present, even `[]`. Only
  available users can be assigned; an inactive or expired assignee stays on the todo, marked.
  The edit page gets an "Assigned to" section with a CheckBox row per available user
  (`TodoEdit_Assignees`), which exercises Bruinet's collection and toggle controls.
- **"Me".** A **Sign in as** action on a user row, `TODO_USER` as a launch setting, "Signed in as"
  on the list (`TodoList_Me`), and `X-Todo-User` recorded by the API. No passwords. The
  multi-app `Apps` table's `User` column fills `TODO_USER`, so `Daan` in a step is both the app
  and the signed-in user.
- **The assignee filter.** A second Picker (`TodoList_Assignee`): Everyone, Mine, Unassigned, each
  available user, and perhaps each role. It combines with the status filter.
- **Journeys TOD.12 (assign a todo) and TOD.13 (filter by assignee)**, as in the first draft at
  [7162ac7](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/uat/phrases/users-plan.md#4-journeys).
- **The web head**, for parity. The Razor pages use the same `data-testid` values as the
  AutomationIds, so the scenarios serve both heads (AD-014).

## Risk

The user form exercises controls the Todo app had not used before. Android has not run them:
Picker was broken there and Stepper and Slider had gaps ([the Android plan](https://github.com/exact-mobile-apps/test-tooling/blob/ec96de1f/.my/android/plan.md) A1-A3).
Fix the gaps in Bruinet's controls when the Android tests are written, never in the app.
