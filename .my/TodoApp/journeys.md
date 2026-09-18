# Todo (TOD) - journey backbone and pyramid allocation

Module id: `TOD`. Subtask ids: `TOD.<journey>.<subtask>` (e.g. `TOD.02.3`), the same notation as
the Contacts bundle (`CON.xx.y`). Every automated test carries `[Trait("Journey", "TOD.xx.y")]`
and `[Trait("Pyramid", "...")]`; the coverage report in [plan.md](plan.md#journey-coverage-report)
compares those traits with this file.

Tiers: **U** Unit, **I** Integration (headless, WireMock + SQLite), **C** Contract (real API in
process), **H** UI hermetic (Brinell + WireMock + seeded DB), **L** UI live (Brinell + real API),
**M** Manual. Gates: **S** Smoke, **R** Reviews.

The rule is the one from the pyramid discussion: each subtask gets the **lowest** tier that can
give confidence. A higher tier appears only for what the lower one cannot see (a rendered control,
a navigation, a real wire). Where a subtask lists two tiers, the second checks one example only.

---

## TOD.01 See my todos

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.01.1 | Opening the app shows the list; loading is shown, then the content | U + **S** | VM state machine in U; Smoke proves the page arrives |
| TOD.01.2 | Each row shows title, due date and status | H | Rendering and binding of the row + status component |
| TOD.01.3 | No todos shows "Nothing to do" and the Add button stays reachable | U + H | U for the state; H for the StateContainer's Empty view (`empty.json`) |
| TOD.01.4 | Filter by status shows only matching rows | U + H | Filtering logic in U; one filter choice in H |
| TOD.01.5 | Rows are ordered: not done first, then by due date, undated last | U | Pure sort |
| TOD.01.6 | A row with unsynced changes shows the pending marker | H | `pending-local-change.json` seeded into the DB |

## TOD.02 Create a todo

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.02.1 | Add opens an empty edit form with status Open | H | Navigation + form defaults on screen |
| TOD.02.2 | Save with a valid title returns to the list, which shows the new todo | H | The core create flow |
| TOD.02.3 | Empty or whitespace title shows "Title is required" and does not save | U + H | All variants in U; one in H for the error label |
| TOD.02.4 | Title over 100 characters is refused | U | Boundary values (100 / 101) |
| TOD.02.5 | A due date is optional; switching it off clears it | U + H | U for the rule; H for Switch + DatePicker interplay |
| TOD.02.6 | The new todo is stored locally with a client-generated id and "pending" | I | Real repository + SQLite |

## TOD.03 Read a todo

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.03.1 | Tapping a row opens its detail with all fields | H + **S** | Row tap -> route with id |
| TOD.03.2 | Dates show in the device's local format; stored in UTC | U + I | Formatting in U; UTC round trip in I |
| TOD.03.3 | The Sync card shows "Synced", "Waiting to sync" or "Local only" | U + H | Mapping in U; one state rendered in the SectionCard in H |
| TOD.03.4 | Back returns to the list at the same filter | H | Back stack |

## TOD.04 Edit a todo

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.04.1 | Edit opens the form filled with the current values | H | |
| TOD.04.2 | Save updates detail and list, and marks the todo pending | H | The whole round trip once |
| TOD.04.3 | Cancel on an unchanged form leaves without asking | U | Dirty tracking |
| TOD.04.4 | Cancel on a changed form asks; "Keep editing" stays, "Discard" leaves unchanged | U + H | Branches in U; the dialog once in H |
| TOD.04.5 | `UpdatedAt` changes on save, `CreatedAt` never does | U + I | |

## TOD.05 Change status

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.05.1 | Next cycles Open -> In progress -> Done -> Open | U + H | Cycle in U; the component's Next button in H |
| TOD.05.2 | A non-Done todo past its due date shows Overdue (⚠) | U | Rule table: status x due x today (`[Theory]`) |
| TOD.05.3 | Overdue is shown in the list row and on the detail | H | `overdue.json` + pinned clock (`TODO_NOW`) |
| TOD.05.4 | The status in the list row is read-only (no Next button) | H | Component `IsReadOnly` |
| TOD.05.5 | Changing status on the detail page is saved without opening Edit | H | |

## TOD.06 Delete a todo

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.06.1 | Delete asks for confirmation | U + H | |
| TOD.06.2 | Confirm returns to the list and the row is gone | H | |
| TOD.06.3 | Cancel keeps the todo and stays on the detail | U | Dialog result branch |
| TOD.06.4 | A deleted todo stays hidden after a restart (tombstone) | I | Container rebuilt on the same DB file |
| TOD.06.5 | Deleting a never-synced todo removes it without a server call | U + I | Planner in U; no `DELETE` in WireMock's log in I |

## TOD.07 Sync with the backend

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.07.1 | Sync pushes pending changes, then pulls the server list | U + I | Plan in U; order of requests in I |
| TOD.07.2 | After a successful sync the pending markers disappear | H | Marker + `WaitForRequest(PUT)` |
| TOD.07.3 | A todo created on the server appears after Sync | I + L | Pull + merge in I; the real wire once in L |
| TOD.07.4 | A todo created in the app exists on the server after Sync | L | Real API, checked through the API |
| TOD.07.5 | A todo deleted in the app is gone on the server after Sync | I + L | `DELETE` in I; 404 from the real API in L |
| TOD.07.6 | Conflict: the newer `UpdatedAt` wins | U | Merge rule table |
| TOD.07.7 | A second sync with no changes sends no writes and adds no rows | I | Idempotence |
| TOD.07.8 | Requests carry the API key header | I | WireMock request log |
| TOD.07.9 | The app's DTOs match what the real API sends and accepts | C | Keeps the WireMock stubs honest |

## TOD.08 Things go wrong

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.08.1 | Server error (500) on sync shows the Error state with Retry; local data is kept | I + H | Kept data in I; Error view + Retry in H |
| TOD.08.2 | Retry after the server recovers shows the content | H | Stub changed between attempts |
| TOD.08.3 | A slow server shows Loading until the answer arrives | H | Stub with delay; wait for state, no sleeps |
| TOD.08.4 | Timeout keeps changes pending and reports an error | I | |
| TOD.08.5 | 401 (wrong API key) reports "not authorised", not a crash | U + I | |
| TOD.08.6 | Network lost mid-sync on a real device | M | Physical network; cannot be made deterministic |
| TOD.08.7 | App killed during save; data is either saved or not, never half | I + M | Transaction in I; the real kill in M |
| TOD.08.8 | Offline: Sync is disabled and shows "Offline", edits stay pending; back online, Sync clears them | U + H | Guard in U; H flips the network-state file (Construction's `NetworkStateOverride` pattern) |

## TOD.09 Persistence and startup

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.09.1 | A fresh install creates the database | I | Migrations on an empty file |
| TOD.09.2 | Upgrading from schema v1 keeps all todos | I | Migration v1 -> v2 |
| TOD.09.3 | Todos survive an app restart | I + H | I rebuilds the container; H relaunches the app once |
| TOD.09.4 | The app starts with the backend unreachable and shows local todos | H | Launch with an unused port |

## TOD.10 Look and accessibility

| Id | Subtask | Tier | Why there |
| --- | --- | --- | --- |
| TOD.10.1 | Every screen loads and its key controls exist | **R** | Reviews crawl: list, detail, edit, empty, error |
| TOD.10.2 | Interactive controls have AutomationIds and accessible names | **R** | `AccessibilityAudit` |
| TOD.10.3 | Screenshots per screen for design review | **R** | Artifacts (AD-007) |
| TOD.10.4 | Dark mode, 200 % font scaling, small-screen keyboard overlap | M | Visual judgement |

---

## Allocation summary

Counts are per assignment, so a subtask with two tiers counts twice.

| Tier | Count | Share |
| --- | --- | --- |
| Unit | 21 | |
| Integration | 17 | |
| Contract | 1 | |
| UI hermetic | 27 | one example per behaviour |
| UI live | 3 | real wire only |
| Manual | 3 | |
| Smoke / Reviews gates | 2 / 3 | |

At the subtask level, UI hermetic has more assignments than Unit. That is expected for a small CRUD
app, where most journeys are about screens. The pyramid shows up in the **test count**. The unit
subtasks are rule tables (`[Theory]` over validation, the Overdue rule, sort order, sync merge) and
expand to roughly 80-120 cases. Each H subtask is one test of one example. When the tests are
written, check the report's test counts, not these subtask counts. If H tests outnumber U tests,
look at each H test: does it repeat a variant U already covers? If so, push it down.

## Gherkin example (phase 6, optional)

The journey text does not decide the tier. The same scenario can bind to a view model (U) or to
page objects (H):

```gherkin
@Todo @TOD.02.3 @Pyramid:UiHermetic
Scenario: A todo needs a title
  Given the todo list is empty
  When I add a todo with title ""
  And I save
  Then I see "Title is required"
  And the todo list is still empty
```
