# Todo (TOD) - user journeys

User journeys for the Todo sample, one checklist item per observable behaviour. This is the
acceptance backbone the UAT scenarios in [Scenarios](Scenarios) draw from: a scenario file that
proves a subtask names it in its `Journey` metadata.

The statements are the user-facing halves of the pyramid backbone in
[../../journeys.md](../../journeys.md), with the tier and rationale columns dropped - a journey doc
says *what the user can do*, not *where it is tested*.

## Ids

Subtask ids read `TOD.<journey>.<subtask>`, e.g. `TOD.02.3` - the dotted form the rest of the Todo
sample already uses in its `[Trait("Journey", ...)]` attributes and its coverage report, not the
`T01.1` form of the generic user-journey prompt. Keeping one id shape across the sample is worth the
small deviation.

## Legend

- `[ ]` not yet passing / not yet covered by a UAT scenario
- `[x]` passing through a UAT scenario in this project
- `[!]` covered elsewhere in the pyramid (unit, integration, contract, live or manual), out of
  scope for hermetic Windows UAT
- `[-]` not applicable to UAT (needs real hardware, a real network, a real device or a human)

A journey line is checked when every one of its subtasks is.

---

## TOD.01 See my todos

- [ ] **Journey passed**
  - [ ] **TOD.01.1** Opening the app shows the list; a loading state shows first, then the content
  - [ ] **TOD.01.2** Each row shows its title, due date and status
  - [ ] **TOD.01.3** With no todos the list shows "Nothing to do" and Add stays reachable
  - [ ] **TOD.01.4** Filtering by status shows only the matching rows
  - [!] **TOD.01.5** Rows are ordered: not done first, then by due date, undated last
  - [ ] **TOD.01.6** A row with unsynced changes shows the pending marker

## TOD.02 Create a todo

- [ ] **Journey passed**
  - [ ] **TOD.02.1** Add opens an empty edit form with status Open
  - [x] **TOD.02.2** Saving a valid title returns to the list, which shows the new todo
  - [x] **TOD.02.3** An empty or whitespace title shows "Title is required" and does not save
  - [!] **TOD.02.4** A title over 100 characters is refused
  - [ ] **TOD.02.5** A due date is optional; switching it off clears it
  - [!] **TOD.02.6** A new todo is stored locally with a client id and marked pending

## TOD.03 Read a todo

- [ ] **Journey passed**
  - [ ] **TOD.03.1** Tapping a row opens its detail with all its fields
  - [!] **TOD.03.2** Dates show in the device's local format and are stored in UTC
  - [ ] **TOD.03.3** The Sync card shows "Synced", "Waiting to sync" or "Local only"
  - [ ] **TOD.03.4** Back returns to the list at the same filter

## TOD.04 Edit a todo

- [ ] **Journey passed**
  - [ ] **TOD.04.1** Edit opens the form filled with the current values
  - [ ] **TOD.04.2** Saving updates the detail and the list and marks the todo pending
  - [x] **TOD.04.3** Cancel on an unchanged form leaves without asking
  - [ ] **TOD.04.4** Cancel on a changed form asks; Keep editing stays, Discard leaves it unchanged
  - [!] **TOD.04.5** `UpdatedAt` changes on save; `CreatedAt` never does

## TOD.05 Change status

- [ ] **Journey passed**
  - [x] **TOD.05.1** Next cycles the status Open -> In progress -> Done -> Open
  - [!] **TOD.05.2** A non-Done todo past its due date shows Overdue
  - [ ] **TOD.05.3** Overdue shows in the list row and on the detail
  - [ ] **TOD.05.4** The status in a list row is read-only, with no Next button
  - [ ] **TOD.05.5** Changing status on the detail page is saved without opening Edit

## TOD.06 Delete a todo

- [ ] **Journey passed**
  - [ ] **TOD.06.1** Delete asks for confirmation
  - [ ] **TOD.06.2** Confirming returns to the list and the row is gone
  - [ ] **TOD.06.3** Cancelling keeps the todo and stays on the detail
  - [!] **TOD.06.4** A deleted todo stays hidden after a restart
  - [!] **TOD.06.5** Deleting a never-synced todo removes it without a server call

## TOD.07 Sync with the backend

- [ ] **Journey passed**
  - [!] **TOD.07.1** Sync pushes pending changes, then pulls the server list
  - [ ] **TOD.07.2** After a successful sync the pending markers disappear
  - [!] **TOD.07.3** A todo created on the server appears after Sync
  - [-] **TOD.07.4** A todo created in the app exists on the server after Sync
  - [!] **TOD.07.5** A todo deleted in the app is gone on the server after Sync
  - [!] **TOD.07.6** On conflict the newer change wins
  - [!] **TOD.07.7** A second sync with no changes writes nothing and adds no rows
  - [!] **TOD.07.8** Requests carry the API key header
  - [!] **TOD.07.9** The app's DTOs match what the real API sends and accepts

## TOD.08 Things go wrong

- [ ] **Journey passed**
  - [ ] **TOD.08.1** A server error on sync shows the Error state with Retry; local data is kept
  - [ ] **TOD.08.2** Retry after the server recovers shows the content
  - [ ] **TOD.08.3** A slow server shows Loading until the answer arrives
  - [!] **TOD.08.4** A timeout keeps changes pending and reports an error
  - [!] **TOD.08.5** A wrong API key reports "not authorised" rather than crashing
  - [-] **TOD.08.6** Losing the network mid-sync on a real device is handled
  - [-] **TOD.08.7** Killing the app during a save leaves data saved or not, never half
  - [ ] **TOD.08.8** Offline disables Sync and shows "Offline"; back online, Sync clears the pending edits

## TOD.09 Persistence and startup

- [ ] **Journey passed**
  - [!] **TOD.09.1** A fresh install creates the database
  - [!] **TOD.09.2** Upgrading the schema keeps all todos
  - [ ] **TOD.09.3** Todos survive an app restart
  - [ ] **TOD.09.4** The app starts with the backend unreachable and shows local todos

## TOD.10 Look and accessibility

- [ ] **Journey passed**
  - [ ] **TOD.10.1** Every screen loads and its key controls exist
  - [-] **TOD.10.2** Interactive controls have AutomationIds and accessible names
