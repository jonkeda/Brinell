# UAT: TOD.07 Sync with the backend

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Sync |
| Target | MAUI, BLAZOR |
| Journey | TOD.07 |
| TestData | one-todo |
| Tags | maui, web, sync |

@TOD.07.2 @smoke @maui @web @sync
## Scenario: TOD.07.2 After a successful sync the pending markers disappear

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I set Title to "Order paint"
And I tap Save
Then I should be on the Todo List page
And the todo "Order paint" should be waiting to sync
When I tap Sync
Then the todo "Order paint" should be synced
And the server should have todos
  | Title       |
  | ----------- |
  | Order paint |
And the database should have todos
  | Title       | SyncState |
  | ----------- | --------- |
  | Order paint | Synced    |
```
