# UAT: Todo cancelling a new todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Create |
| Target | MAUI |
| Journey | TOD.04.3 |
| Tags | maui, create, navigation |

@maui @create @navigation
## Scenario: Cancel on an untouched new todo returns to the list

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I tap Cancel
Then I should be on the Todo List page
```
