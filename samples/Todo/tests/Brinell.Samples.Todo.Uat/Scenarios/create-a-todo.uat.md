# UAT: Todo creating a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Create |
| Target | MAUI |
| Journey | TOD.02.2 |
| Tags | smoke, maui, create |

@smoke @maui @create
## Scenario: A saved todo appears in the list

Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I set Title to "Write the report"
And I tap Save
Then I should be on the Todo List page
And Todos should have todo "Write the report"
