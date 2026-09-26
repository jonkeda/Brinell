# UAT: TOD.03 Read a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Detail |
| Target | MAUI |
| Journey | TOD.03 |
| TestData | one-todo |
| Tags | maui, detail |

@TOD.03.1 @smoke @maui @detail
## Scenario: TOD.03.1 Tapping a row opens its detail with all its fields

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title          | Status     | DueDate | Notes                 |
  | -------------- | ---------- | ------- | --------------------- |
  | Plan the party | InProgress | +3d     | Invite the neighbours |
When I tap Sync
Then Todos should have todo "Plan the party"
When I open the todo "Plan the party"
Then I should be on the Todo Detail page
And Title should equal "Plan the party"
And Status should show status "In progress"
And Notes should equal "Invite the neighbours"
And Due should exist
And Created should exist
And Updated should exist
And Sync State should equal "Synced"
```

@TOD.03.3 @maui @detail @sync
## Scenario: TOD.03.3 The Sync card shows "Synced", "Waiting to sync" or "Local only"

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title           |
  | --------------- |
  | Water the ferns |
When I tap Sync
Then Todos should have todo "Water the ferns"
When I open the todo "Water the ferns"
Then I should be on the Todo Detail page
And Sync State should equal "Synced"
When I advance Status to "In progress"
Then Sync State should equal "Waiting to sync"
When I tap Back
Then I should be on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I set Title to "Pack a lunch"
And I tap Save
Then I should be on the Todo List page
When I open the todo "Pack a lunch"
Then I should be on the Todo Detail page
And Sync State should equal "Local only"
```

@TOD.03.4 @maui @detail @windows
## Scenario: TOD.03.4 Back returns to the list at the same filter

```gherkin
Given I am on the Todo List page
And the server also has todos
  | Title         | Status     |
  | ------------- | ---------- |
  | Fix the fence | InProgress |
When I tap Sync
Then Todos should have todo "Fix the fence"
When I select "In progress" from Filter
And I open the todo "Fix the fence"
Then I should be on the Todo Detail page
When I tap Back
Then I should be on the Todo List page
And Filter should have selected "In progress"
And Todos should have todo "Fix the fence"
And Todos should not have todo "Buy milk"
```
