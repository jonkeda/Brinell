# UAT: TOD.05 Change status

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Status |
| Target | MAUI |
| Journey | TOD.05 |
| TestData | one-todo |
| Tags | maui, status |

@TOD.05.1 @maui @status
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
