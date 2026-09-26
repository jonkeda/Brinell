# UAT: TOD.06 Delete a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Delete |
| Target | MAUI, BLAZOR |
| Journey | TOD.06 |
| TestData | one-todo |
| Tags | maui, web, delete |

@TOD.06.1 @maui @web @delete
## Scenario: TOD.06.1 Delete asks for confirmation

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title         |
  | ------------- |
  | Sell the sofa |
When I tap Sync
Then Todos should have todo "Sell the sofa"
When I open the todo "Sell the sofa"
Then I should be on the Todo Detail page
When I tap Delete
Then the dialog should ask "Delete todo?"
When I choose "Cancel" in the dialog
Then no dialog should be shown
```

@TOD.06.2 @smoke @maui @web @delete
## Scenario: TOD.06.2 Confirming returns to the list and the row is gone

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title      |
  | ---------- |
  | Cancel gym |
When I tap Sync
Then Todos should have todo "Cancel gym"
When I open the todo "Cancel gym"
Then I should be on the Todo Detail page
When I tap Delete
Then the dialog should ask "Delete todo?"
When I choose "Delete" in the dialog
Then I should be on the Todo List page
And Todos should not have todo "Cancel gym"
And Todos should have todo "Buy milk"
```

@TOD.06.3 @maui @web @delete
## Scenario: TOD.06.3 Cancelling keeps the todo and stays on the detail

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title          |
  | -------------- |
  | Keep the piano |
When I tap Sync
Then Todos should have todo "Keep the piano"
When I open the todo "Keep the piano"
Then I should be on the Todo Detail page
When I tap Delete
Then the dialog should ask "Delete todo?"
When I choose "Cancel" in the dialog
Then no dialog should be shown
And I should be on the Todo Detail page
And Title should equal "Keep the piano"
When I tap Back
Then I should be on the Todo List page
And Todos should have todo "Keep the piano"
```
