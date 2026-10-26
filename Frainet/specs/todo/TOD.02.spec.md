# Spec: TOD.02 Create a todo

| Field | Value |
| --- | --- |
| Product | Bruinet's Todo sample (`samples/Todo`) |
| Journey | TOD.02, subtasks in `samples/Todo/journeys.md` |
| Heads | MAUI (Windows, Android), web (Blazor Server) |
| Status | draft, written from the code and tests of `move` on 2026-10-26 |

## Intent

A person writes down something they must do, in a few seconds, and finds it in their list at
once, even without a connection. A todo needs only a title; everything else can wait. The app
refuses what it cannot store or show well, and says why on the field.

## Rules

Each rule holds wherever a todo is created: the form and `TodoService.CreateAsync` alike.

| Id | Rule | Subtasks |
| --- | --- | --- |
| TOD.02.R1 | A new todo's form opens with an empty title, empty notes, no due date and status Open, and shows no error. | TOD.02.1 |
| TOD.02.R2 | The title is required: empty, only whitespace or missing is refused with "Title is required". | TOD.02.3 |
| TOD.02.R3 | The title is at most 100 characters, counted after trimming; longer is refused with "Title can be at most 100 characters". | TOD.02.4 |
| TOD.02.R4 | A refused save stores nothing, keeps the form open with what was typed, and shows the error on the title. | TOD.02.3 |
| TOD.02.R5 | An error shows only after Save; typing in the title clears it. | TOD.02.1, TOD.02.3 |
| TOD.02.R6 | Title and notes are stored trimmed; notes that are empty or only whitespace are stored as none. | TOD.02.6 |
| TOD.02.R7 | The due date is optional; when it is switched off, no due date is stored, whatever date was picked. | TOD.02.5 |
| TOD.02.R8 | The status may be changed before the first save; the todo is stored with the status shown. | TOD.02.2 |
| TOD.02.R9 | A new todo gets its id on the device, its created and updated times are the same moment in UTC, and it is local only until a sync. | TOD.02.6 |
| TOD.02.R10 | A successful save returns to the list, which shows the new todo beside the ones already there. | TOD.02.2 |

## Examples

| Rule | Given | When | Then |
| --- | --- | --- | --- |
| R1 | the list | Add | title "", status Open, no due date, no title error |
| R2 | the form | Save with title "" / "   " / tab and newline | "Title is required" |
| R3 | the form | Save with 100 x's | saved |
| R3 | the form | Save with 101 x's | "Title can be at most 100 characters" |
| R3 | the form | Save with 2 spaces, 100 x's, 2 spaces | saved, title is the 100 x's |
| R4 | one todo "Buy milk" | Save with title "   " | still on the form, only "Buy milk" stored |
| R5 | "Title is required" shown | type "B" | no error shown |
| R6 | the form | Save "  Buy milk  ", notes "  " | title "Buy milk", notes none |
| R7 | the form | switch the due date on, pick a date, switch it off, Save "No deadline" | no due date |
| R7 | the form | switch the due date on, pick 2026-11-01, Save | due 2026-11-01 |
| R8 | the form | Next once (In progress), Save "Busy" | stored In progress |
| R9 | the clock at 2026-10-26 09:00 UTC | Save "Buy milk" | a new non-empty id, created = updated = 09:00 UTC, local only |
| R10 | one todo "Buy milk" | Save "Write the report" | the list shows "Write the report" and "Buy milk" |

## Not in this journey

- Editing, status changes after the first save, deleting: TOD.04, TOD.05, TOD.06.
- Sending the todo to the server: TOD.07; R9 only says it waits for a sync.
- How a todo looks in the list (order, markers): TOD.01.
- Assigning a todo to a user: not built (`.my/uat/phrases/users-plan.md`).

## Decision boundaries

| An agent may | An agent asks first |
| --- | --- |
| Change how a rule is built, keeping its tests green | Change a rule, its message text or the 100 |
| Add a test for a rule at the lowest tier that proves it | Remove or loosen a test that proves a rule |
| Rename code, move it between Core's folders | Add a field to a todo, or change what is stored |
| Answer an open question below with its proposal, recorded here | Anything that changes `Contracts` or the API |

## Traceability, as it stands

Found by the `Journey` traits and `@TOD.02.x` tags; Frainet's own `Rule` traits do not exist yet.

| Rule | Proved by | Tier |
| --- | --- | --- |
| R1 | `TodoEditViewModelTests.New_StartsEmpty_Open_WithoutADueDate`; scenario TOD.02.1; `CreateEditJourneyTests`, web `JourneyTests` (TOD.02.1) | U, H |
| R2 | `TodoValidationTests.ValidateTitle_EmptyOrWhitespace_IsRequired`; `TodoServiceTests.Create_WithAnInvalidTitle_Throws_AndStoresNothing`; scenario TOD.02.3 | U, H |
| R3 | `TodoValidationTests.ValidateTitle_AllowsAtMost100Characters`, `ValidateTitle_MeasuresAfterTrimming` | U |
| R4 | `TodoEditViewModelTests.Save_WithoutATitle_ShowsTheError_AndStaysOnThePage`; scenario TOD.02.3 | U, H |
| R5 | `TodoEditViewModelTests.TypingAfterAnError_ClearsIt`; scenario TOD.02.1 (no error at first) | U, H |
| R6 | `TodoServiceTests.Create_StoresATrimmedTodo_LocalOnly_WithAClientId` | U |
| R7 | `TodoEditViewModelTests.Save_WithTheDueDateSwitchedOff_StoresNoDueDate`, `Save_WithADueDate_StoresThePickedDate`; scenario TOD.02.5 | U, H |
| R8 | `TodoEditViewModelTests.Save_New_CreatesTheTodo_AndGoesBack` (sets In progress) | U |
| R9 | `TodoServiceTests.Create_StoresATrimmedTodo_LocalOnly_WithAClientId`; `DatabaseTests.ACreatedTodo_IsStoredLocally_WithItsOwnId_AndLocalOnly` | U, I |
| R10 | `TodoEditViewModelTests.Save_New_CreatesTheTodo_AndGoesBack`; scenario TOD.02.2; `CreateEditJourneyTests`, web `JourneyTests` (TOD.02.2) | U, H |

Every rule has a test today. Gaps by example: R3's 101 characters is never tried on a screen (by
choice, `journeys.md`); R9's "updated = created" is not asserted; R8 has no UI example.

## Open questions

Found while writing the spec from the code; each needs the owner's yes before the rule changes.

- `journeys.md` TOD.02.6 says the new todo is stored as "pending"; the code and tests store it as
  local only (`SyncState.LocalOnly`). Proposal: correct `journeys.md` to "local only"; the code is
  right, pending means a synced todo changed since.
- May a new todo be due in the past? The code allows it and marks it past due. Proposal: allow
  it, and add the rule "a past due date is allowed and shown as past due" as R11.
- Notes have no length limit on a todo, while a user's notes stop at 500. Proposal: no limit for
  now; record it here as a choice, not a gap.
- Two todos may have the same title. Proposal: allowed, and say so as a rule only if an agent
  ever tries to prevent it.
