# Feature: TOD.05 Change status

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Status |
| Target | MAUI, BLAZOR |
| Journey | TOD.05 |
| TestData | one-todo |
| Tags | maui, web, status |

@TOD.05.1 @maui @web @status
## Scenario: TOD.05.1 Next cycles the status to Done, and it is saved

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I set Title to "Plan the sprint"
And I advance Status to "Done"
Then Status should show status "Done"
When I tap Save
Then I should be on the Todo List page
And Todos should have todo "Plan the sprint"
And the database should have todos
  | Title           | Status |
  | --------------- | ------ |
  | Plan the sprint | Done   |
```

@TOD.05.3 @maui @web @status
## Scenario: TOD.05.3 Overdue shows in the list row and on the detail

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title          | Status | DueDate |
  | -------------- | ------ | ------- |
  | Renew passport | Open   | -2d     |
  | Water plants   | Done   | -2d     |
When I tap Sync
Then Todos should have todo "Renew passport"
And the todo "Renew passport" should show status "Overdue"
And the todo "Water plants" should show status "Done"
When I open the todo "Renew passport"
Then I should be on the Todo Detail page
And Status should show status "Overdue"
```

@TOD.05.4 @maui @web @status
## Scenario: TOD.05.4 The status in a list row is read-only, with no Next button

```gherkin
Given I am on the Todo List page
Then Todos should have todo "Buy milk"
And the todo "Buy milk" should have a read-only status
```

@TOD.05.5 @maui @web @status
## Scenario: TOD.05.5 Changing status on the detail page is saved without opening Edit

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title        |
  | ------------ |
  | Fix the bike |
When I tap Sync
Then Todos should have todo "Fix the bike"
When I open the todo "Fix the bike"
Then I should be on the Todo Detail page
When I advance Status to "Done"
Then Sync State should equal "Waiting to sync"
When I tap Back
Then I should be on the Todo List page
And the todo "Fix the bike" should show status "Done"
And the todo "Fix the bike" should be waiting to sync
And the database should have todos
  | Title        | Status | SyncState |
  | ------------ | ------ | --------- |
  | Fix the bike | Done   | Pending   |
```
