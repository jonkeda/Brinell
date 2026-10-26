# Feature: TOD.02 Create a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinet.Samples.Todo.App |
| Area | Create |
| Target | MAUI, BLAZOR |
| Journey | TOD.02 |
| TestData | one-todo |
| Tags | maui, web, create |

## Background

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
```

@TOD.02.1 @maui @web @create
## Scenario: TOD.02.1 Add opens an empty edit form with status Open

```gherkin
Then Title should equal ""
And Status should show status "Open"
And Title Error should not exist
```

@TOD.02.2 @smoke @maui @web @create
## Scenario: TOD.02.2 Saving a valid title returns to the list, which shows the new todo

```gherkin
When I set Title to "Write the report"
And I tap Save
Then I should be on the Todo List page
And Todos should have todo "Write the report"
And Todos should have todo "Buy milk"
And the database should have todos
  | Title            | Status | SyncState |
  | ---------------- | ------ | --------- |
  | Write the report | Open   | LocalOnly |
```

@TOD.02.3 @smoke @maui @web @create @validation
## Scenario Outline: TOD.02.3 An empty or whitespace title shows "Title is required" and does not save

```gherkin
When I set Title to <title>
And I tap Save
Then Title Error should contain "Title is required"
And I should be on the Todo Edit page
And the database should not have a todo with a blank title
And the database should have todos
  | Title    | SyncState |
  | -------- | --------- |
  | Buy milk | Synced    |
```

### Examples

| title |
| --- |
| "" |
| "   " |

@TOD.02.5 @maui @web @create
## Scenario: TOD.02.5 A due date is optional; switching it off clears it

```gherkin
When I check Has Due Date
Then Due Date should be enabled
When I uncheck Has Due Date
Then Due Date should be disabled
When I set Title to "No deadline"
And I tap Save
Then I should be on the Todo List page
And Todos should have todo "No deadline"
And the database should have todos
  | Title       | DueDate |
  | ----------- | ------- |
  | No deadline | null    |
```
