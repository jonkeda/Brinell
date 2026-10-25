# Feature: TOD.04 Edit a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinell.Samples.Todo.App |
| Area | Edit |
| Target | MAUI, BLAZOR |
| Journey | TOD.04 |
| TestData | one-todo |
| Tags | maui, web, edit |

@TOD.04.1 @maui @web @edit
## Scenario: TOD.04.1 Edit opens the form filled with the current values

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title           | Status     | DueDate |
  | --------------- | ---------- | ------- |
  | Renew insurance | InProgress | +20d    |
When I tap Sync
Then Todos should have todo "Renew insurance"
When I open the todo "Renew insurance"
Then I should be on the Todo Detail page
When I tap Edit
Then I should be on the Todo Edit page
And Title should equal "Renew insurance"
And Status should show status "In progress"
And Has Due Date should be checked
And Due Date should be enabled
When I tap Cancel
Then I should be on the Todo Detail page
```

@TOD.04.2 @smoke @maui @web @edit
## Scenario: TOD.04.2 Saving updates the detail and the list and marks the todo pending

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title    |
  | -------- |
  | Call mum |
When I tap Sync
Then Todos should have todo "Call mum"
When I open the todo "Call mum"
Then I should be on the Todo Detail page
When I tap Edit
Then I should be on the Todo Edit page
When I set Title to "Call dad"
And I tap Save
Then I should be on the Todo Detail page
And Title should equal "Call dad"
And Sync State should equal "Waiting to sync"
When I tap Back
Then I should be on the Todo List page
And Todos should have todo "Call dad"
And Todos should not have todo "Call mum"
And the todo "Call dad" should be waiting to sync
And the database should have todos
  | Title    | SyncState |
  | -------- | --------- |
  | Call dad | Pending   |
```

@TOD.04.3 @maui @web @navigation
## Scenario: TOD.04.3 Cancel on an unchanged form leaves without asking

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I tap Cancel
Then I should be on the Todo List page
```

@TOD.04.4 @maui @web @edit
## Scenario: TOD.04.4 Cancel on a changed form asks; Keep editing stays, Discard leaves it unchanged

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title        |
  | ------------ |
  | Book flights |
When I tap Sync
Then Todos should have todo "Book flights"
When I open the todo "Book flights"
Then I should be on the Todo Detail page
When I tap Edit
Then I should be on the Todo Edit page
When I set Title to "Something else"
And I tap Cancel
Then the dialog should ask "Discard changes?"
When I choose "Keep editing" in the dialog
Then no dialog should be shown
And I should be on the Todo Edit page
And Title should equal "Something else"
When I tap Cancel
Then the dialog should ask "Discard changes?"
When I choose "Discard" in the dialog
Then I should be on the Todo Detail page
And Title should equal "Book flights"
```
