# UAT: Todo setting the status of a new todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Status |
| Target | MAUI |
| Journey | TOD.05.1 |
| Tags | maui, status |

@maui @status
## Scenario: Next cycles the status to Done, and it is saved

Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I set Title to "Plan the sprint"
And I advance Status to "Done"
Then Status should show status "Done"
When I tap Save
Then I should be on the Todo List page
And Todos should have todo "Plan the sprint"
