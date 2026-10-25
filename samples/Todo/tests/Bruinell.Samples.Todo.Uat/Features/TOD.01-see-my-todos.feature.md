# Feature: TOD.01 See my todos

## Metadata

| Field | Value |
| --- | --- |
| App | Bruinell.Samples.Todo.App |
| Area | List |
| Target | MAUI, BLAZOR |
| Journey | TOD.01 |
| TestData | empty |
| Tags | maui, web, list |

@TOD.01.1 @smoke @maui @web @list
## Scenario: TOD.01.1 Opening the app shows the list; a loading state shows first, then the content

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title     |
  | --------- |
  | Buy bread |
And the server answers after 2 seconds
When I tap Sync
Then the list should show the Loading state
And the list should show the Content state
And Todos should have todo "Buy bread"
```

@TOD.01.2 @maui @web @list
## Scenario: TOD.01.2 Each row shows its title, due date and status

```gherkin
Given the synced list has todos
  | Title            | Status      | Due Date |
  | ---------------- | ----------- | -------- |
  | Post the letters | Open        | +2d      |
  | Paint the fence  | In Progress |          |
Then the list should show "Post the letters" and "Paint the fence"
And each todo shows its status
  | Title            | Status      |
  | ---------------- | ----------- |
  | Post the letters | Open        |
  | Paint the fence  | In progress |
And the todo "Post the letters" should show a due date
And the todo "Paint the fence" should show no due date
```

@TOD.01.3 @smoke @maui @web @list
## Scenario: TOD.01.3 With no todos the list shows "Nothing to do" and Add stays reachable

```gherkin
Given I am on the Todo List page
When I tap Sync
Then the list should show the Empty state
And Empty Message should equal "Nothing to do"
And Add should be enabled
```

@TOD.01.4 @maui @web @list @windows
## Scenario: TOD.01.4 Filtering by status shows only the matching rows

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title         | Status     |
  | ------------- | ---------- |
  | Mow the lawn  | Open       |
  | Sand the door | InProgress |
  | Return books  | Done       |
When I tap Sync
Then Todos should have todo "Return books"
When I select "Done" from Filter
Then Todos should have todo "Return books"
And Todos should not have todo "Mow the lawn"
And Todos should not have todo "Sand the door"
```

@TOD.01.6 @maui @web @list
## Scenario: TOD.01.6 A row with unsynced changes shows the pending marker

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title        |
  | ------------ |
  | Clean gutter |
When I tap Sync
Then the todo "Clean gutter" should be synced
When I create a todo "Oil the hinges"
Then I should be on the Todo List page
And the todo "Oil the hinges" should be waiting to sync
And the todo "Clean gutter" should be synced
When I tap Sync
Then the todo "Oil the hinges" should be synced
```
