# Video: Todo in a minute

| Field | Value |
| --- | --- |
| Beat | 400 |

## Scene: Your list

Scenario: Features/TOD.01-see-my-todos.feature.md#TOD.01.1-Opening-the-app-shows-the-list-a-loading-state-shows-first-then-the-content

This is Todo: your list of things to do, on your own device.<break time="500ms"/>

| Step | Say |
| --- | --- |
| When I tap Sync | Tap Sync to fetch what the server has. |
| And Todos should have todo "Buy bread" | There it is: Buy bread. |

## Scene: Adding a todo

Scenario: Features/TOD.05-change-status.feature.md#TOD.05.1-Next-cycles-the-status-to-Done-and-it-is-saved

| Step | Say |
| --- | --- |
| When I tap Add | Tap Add, |
| When I set Title to "Plan the sprint" | give it a title, |
| When I tap Save | and save. |
| And Todos should have todo "Plan the sprint" | Your todo is on the list. |

## Scene: Syncing

Scenario: Features/TOD.07-sync-with-the-backend.feature.md#TOD.07.2-After-a-successful-sync-the-pending-markers-disappear

| Step | Say |
| --- | --- |
| And the todo "Order paint" should be waiting to sync | A new todo waits to sync. |
| When I tap Sync | Sync sends it to the server. |

That is Todo: add, check off, and sync.
