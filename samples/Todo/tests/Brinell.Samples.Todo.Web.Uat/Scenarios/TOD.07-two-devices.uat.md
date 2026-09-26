# UAT: TOD.07 Sync between two devices

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.Web |
| Area | Sync |
| Target | BLAZOR |
| Journey | TOD.07 |
| Tags | web, sync, multi-app |

@TOD.07.3 @web @sync @multi-app
## Scenario: TOD.07.3 A todo Alice syncs appears on Bob's device after Bob syncs

```gherkin
Given Bob uses the app
And I am on the Todo List page
And Alice uses the app
And I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I set Title to "Pack for the trip"
And I tap Save
Then I should be on the Todo List page
When I tap Sync
Then the todo "Pack for the trip" should be synced
And the server should have received the todo "Pack for the trip" from Alice
When Bob uses the app
Then I should be on the Todo List page
And Todos should not have todo "Pack for the trip"
When I tap Sync
Then Todos should have todo "Pack for the trip"
And the todo "Pack for the trip" should be synced
And the database should have todos
  | Title             |
  | ----------------- |
  | Pack for the trip |
```
