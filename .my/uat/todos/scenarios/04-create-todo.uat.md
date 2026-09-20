# UAT: Creating a todo

## Metadata

| Field | Value |
| --- | --- |
| App | Brinell.Samples.Todo.App |
| Area | Create a todo |
| Target | MAUI |
| Tags | smoke, todo, create |
| Mode | Automated |
| Requires | Deterministic |
| Priority | Smoke |
| Evidence | none |
| Traces | TOD.02.1, TOD.02.2 |

@smoke @todo @create @automated @deterministic
## Scenario: Saving a valid title returns to the list showing the new todo

```gherkin
Given I am on the Todo List page
When I tap Add
Then I should be on the Todo Edit page
When I enter "Buy milk" into Title
And I tap Save
Then I should be on the Todo List page
And I should see "Buy milk"
```
