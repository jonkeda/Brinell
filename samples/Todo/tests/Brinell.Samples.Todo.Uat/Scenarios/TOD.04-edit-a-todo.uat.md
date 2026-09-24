# UAT: TOD.04 Edit a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Edit |
| Target | MAUI |
| Journey | TOD.04 |
| TestData | one-todo |
| Tags | maui, edit |

@TOD.04.3 @maui @navigation
## Scenario: TOD.04.3 Cancel on an unchanged form leaves without asking

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I tap Cancel
Then I should be on the Todo List page
```
