# Feature: TOD.09 Persistence and startup

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinell.Samples.Todo.App |
| Area | Persistence |
| Target | MAUI, BLAZOR |
| Journey | TOD.09 |
| TestData | one-todo |
| Tags | maui, web, persistence |

@TOD.09.3 @maui @web @persistence @windows
## Scenario: TOD.09.3 Todos survive an app restart

```gherkin
Given I am on the Todo List page
When I create a todo "Survives restart"
Then I should be on the Todo List page
And Todos should have todo "Survives restart"
When I restart the app
Then I should be on the Todo List page
And Todos should have todo "Survives restart"
And the database should have todos
  | Title            |
  | ---------------- |
  | Survives restart |
```

@TOD.09.3 @service @maui @persistence
## Scenario: TOD.09.3 Stored todos survive a restart

```gherkin
When I restart the app
Then the database should have todos
  | Title    |
  | -------- |
  | Buy milk |
```

@TOD.09.4 @maui @web @persistence @windows
## Scenario: TOD.09.4 The app starts with the backend unreachable and shows local todos

```gherkin
Given I am on the Todo List page
When I restart the app with the server unreachable
Then I should be on the Todo List page
And the list should show the Content state
And Todos should have todo "Buy milk"
And Last Sync should equal "Server unreachable"
```
