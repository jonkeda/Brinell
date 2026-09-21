# UAT: Todo saving a new todo needs a title

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Create |
| Target | MAUI |
| Journey | TOD.02.3 |
| Tags | smoke, maui, create, validation |

@smoke @maui @create @validation
## Scenario: Saving with an empty title shows the error and stays

Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I clear Title
And I tap Save
Then Title Error should be visible
And Title Error should contain "Title is required"
