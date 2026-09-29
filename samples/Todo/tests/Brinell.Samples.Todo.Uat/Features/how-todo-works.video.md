# Video: How the Todo app works

| Field | Value |
| --- | --- |
| Beat | 600 |

## Scene: Opening the app

Scenario: Features/TOD.01-see-my-todos.feature.md#TOD.01.1-Opening-the-app-shows-the-list-a-loading-state-shows-first-then-the-content

This is Todo: a small app to keep track of what you have to do. It works on Windows, Android and
the web, and keeps your todos on your device.

| Step | Say |
| --- | --- |
| Given I am on the Todo List page | The app opens on your list. |
| And the server also has todos | The server has a todo you have not seen here yet. |
| And the server answers after 2 seconds | The server is a little slow today. |
| When I tap Sync | You tap Sync to fetch it. |
| Then the list should show the Loading state | While the app waits, it tells you it is loading. |
| And the list should show the Content state | And when the answer arrives, the list shows it. |
| And Todos should have todo "Buy bread" | There it is: Buy bread. |

## Scene: Reading the list

Scenario: Features/TOD.01-see-my-todos.feature.md#TOD.01.2-Each-row-shows-its-title-due-date-and-status

Every row tells you three things at a glance.

| Step | Say |
| --- | --- |
| Given the synced list has todos | Here are two todos. |
| Then the list should show "Post the letters" and "Paint the fence" | Each row shows its title, |
| And each todo shows its status | how far along it is: open, in progress or done, |
| And the todo "Post the letters" should show a due date | and when it is due. |

## Scene: Filtering

Scenario: Features/TOD.01-see-my-todos.feature.md#TOD.01.4-Filtering-by-status-shows-only-the-matching-rows

When the list grows, the filter keeps it short.

| Step | Say |
| --- | --- |
| When I tap Sync | Here are three todos, each in another state. |
| When I select "Done" from Filter | Pick a status, |
| And Todos should not have todo "Mow the lawn" | and only those todos stay. |

## Scene: Adding a todo

Scenario: Features/TOD.05-change-status.feature.md#TOD.05.1-Next-cycles-the-status-to-Done-and-it-is-saved

Adding a todo takes a few seconds.

| Step | Say |
| --- | --- |
| When I tap Add | Tap Add. |
| When I set Title to "Plan the sprint" | Give it a title. |
| And I advance Status to "Done" | Next moves its status along, all the way to Done. |
| When I tap Save | Save, |
| And Todos should have todo "Plan the sprint" | and your todo is on the list. |

## Scene: The title is required

Scenario: Features/TOD.02-create-a-todo.feature.md#TOD.02.3-An-empty-or-whitespace-title-shows-Title-is-required-and-does-not-save

A todo needs a title.

| Step | Say |
| --- | --- |
| And I tap Save | Try to save without one, |
| Then Title Error should contain "Title is required" | and the app tells you, right at the field. |
| And the database should not have a todo with a blank title | Nothing is saved. |

## Scene: The detail page

Scenario: Features/TOD.03-read-a-todo.feature.md#TOD.03.1-Tapping-a-row-opens-its-detail-with-all-its-fields

Tap a todo to see all of it.

| Step | Say |
| --- | --- |
| When I open the todo "Plan the party" | The detail page shows the todo itself, |
| And Notes should equal "Invite the neighbours" | your notes, |
| And Sync State should equal "Synced" | and whether the server has it yet. |

## Scene: Editing

Scenario: Features/TOD.04-edit-a-todo.feature.md#TOD.04.2-Saving-updates-the-detail-and-the-list-and-marks-the-todo-pending

To change more than the status, tap Edit.

| Step | Say |
| --- | --- |
| When I tap Edit | Tap Edit, |
| When I set Title to "Call dad" | change what you need, |
| And I tap Save | and save. |
| And Sync State should equal "Waiting to sync" | It is marked as waiting to sync, |
| And Todos should have todo "Call dad" | and the list shows it straight away. |

## Scene: Deleting

Scenario: Features/TOD.06-delete-a-todo.feature.md#TOD.06.1-Delete-asks-for-confirmation

Deleting is safe too.

| Step | Say |
| --- | --- |
| When I tap Delete | Tap Delete, |
| Then the dialog should ask "Delete todo?" | and the app asks before anything is lost. |
| When I choose "Cancel" in the dialog | Changed your mind? Cancel keeps the todo. |

## Scene: Sync

Scenario: Features/TOD.07-sync-with-the-backend.feature.md#TOD.07.2-After-a-successful-sync-the-pending-markers-disappear

Your changes live on your device until you sync.

| Step | Say |
| --- | --- |
| And the todo "Order paint" should be waiting to sync | A new todo waits to sync. |
| When I tap Sync | Sync sends it to the server, |
| Then the todo "Order paint" should be synced | and the marker disappears. |

## Scene: When things go wrong

Scenario: Features/TOD.08-things-go-wrong.feature.md#TOD.08.1-A-server-error-on-sync-shows-the-Error-state-with-Retry-local-data-is-kept

| Step | Say |
| --- | --- |
| Given the server fails with status 500 | If the server has a problem, |
| Then the list should show the Error state | you see what went wrong, |
| And Retry should be enabled | and a Retry button. Your todos are still here. |
| And I tap Retry | Try again when it is back, |
| And Todos should have todo "Pay the bills" | and you are up to date. |

## Scene: Your todos stay

Scenario: Features/TOD.09-persistence-and-startup.feature.md#TOD.09.3-Todos-survive-an-app-restart

Close the app, and nothing is lost.

| Step | Say |
| --- | --- |
| When I restart the app | Open it again, |
| And Todos should have todo "Survives restart" | and your todos are where you left them. |

That is Todo: add, check off, and sync, on any device.
